using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Controls;
using DisableProjectedBlocks;
using NLog;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using VRage.Network;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using VRage.Game;
using Sandbox.Game.World;
using Torch.Managers;
using Torch.Managers.PatchManager;
using Torch.Utils;
using VRage.Collections;
using VRage.Game.ModAPI.Ingame;
using VRage.Utils;
using VRageMath;

namespace DisabledProjectedBlocks
{
    public static class ProjectorPatch
    {
        private static readonly MethodInfo NewBlueprintMethod = typeof(MyProjectorBase).GetMethod("OnNewBlueprintSuccess", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo RemoveBlueprintMethod = typeof(MyProjectorBase).GetMethod("OnRemoveProjectionRequest", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly Logger Log = MainLogic.Log;

        public static void Patch(PatchContext ctx)
        {
            ctx.GetPattern(typeof(MyProjectorBase).GetMethod("RemoveProjection", BindingFlags.Instance | BindingFlags.NonPublic)).
                Suffixes.Add(typeof(ProjectorPatch).GetMethod(nameof(IncreaseCount), BindingFlags.Static| BindingFlags.Instance| BindingFlags.NonPublic));

            ctx.GetPattern(typeof(MyProjectorBase).GetMethod("InitializeClipboard", BindingFlags.Instance | BindingFlags.NonPublic)).
                Suffixes.Add(typeof(ProjectorPatch).GetMethod(nameof(DecreaseCount), BindingFlags.Static| BindingFlags.Instance| BindingFlags.NonPublic));

            ctx.GetPattern(typeof(MyProjectorBase).GetMethod("BuildInternal", BindingFlags.Instance | BindingFlags.NonPublic)).
                Prefixes.Add(typeof(ProjectorPatch).GetMethod(nameof(ExtraCheck), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance));

            ctx.GetPattern(typeof(MyProjectorBase).GetMethod("OnNewBlueprintSuccess", BindingFlags.Instance | BindingFlags.NonPublic)).
                Prefixes.Add(typeof(ProjectorPatch).GetMethod(nameof(PrefixNewBlueprint), BindingFlags.Static| BindingFlags.Instance| BindingFlags.NonPublic));
           

        }

        private static bool ExtraCheck(MyProjectorBase __instance, ref Vector3I cubeBlockPosition)
        {
            if (__instance == null || cubeBlockPosition != null) return true;
            Validations.ValidationFailed();
            return false;
        }

        private static void DecreaseCount(MyProjectorBase __instance)
        {
            if (__instance == null) return;
            if ( MainLogic.Instance.Config?.CounterPcuIncrease == false)return;

            var identity = MySession.Static.Players.TryGetIdentity(__instance.BuiltBy);

            var copiedGrids = __instance?.Clipboard?.CopiedGrids;
            if (copiedGrids == null) return;

            int num1 = 0;

            foreach (MyObjectBuilder_CubeGrid copiedGrid in copiedGrids)
                num1 += copiedGrid.CubeBlocks.Count;
            if (identity == null || num1 == 0) return;
            var num2 = num1;
            if (!MySession.Static.CheckLimitsAndNotify(__instance.BuiltBy, __instance.BlockDefinition.BlockPairName, num2, 0, 0, (Dictionary<string, int>) null))
                return;
            identity.BlockLimits.DecreaseBlocksBuilt(__instance.BlockDefinition.BlockPairName, num2, __instance.CubeGrid, false);

            __instance.CubeGrid.BlocksPCU -= num2;

        }

        private static void IncreaseCount(MyProjectorBase __instance)
        {
            if (__instance == null) return;
            if ( MainLogic.Instance.Config?.CounterPcuIncrease == false)return;
            var identity = MySession.Static.Players.TryGetIdentity(__instance.BuiltBy);

            var previewGrids = __instance?.Clipboard?.PreviewGrids;
            if (previewGrids == null) return;
            int num = 0;

            foreach (MyCubeGrid previewGrid in previewGrids)
                num += previewGrid.CubeBlocks.Count;

            if (identity == null || num == 0) return;
            var num2 = num;
            if (!MySession.Static.CheckLimitsAndNotify(__instance.BuiltBy, __instance.BlockDefinition.BlockPairName, num2, 0, 0, (Dictionary<string, int>) null))
                return;
            identity.BlockLimits.IncreaseBlocksBuilt(__instance.BlockDefinition.BlockPairName, num2, __instance.CubeGrid, false);

            __instance.CubeGrid.BlocksPCU += num2;

        }

        private static bool PrefixNewBlueprint(MyProjectorBase __instance,
            ref List<MyObjectBuilder_CubeGrid> projectedGrids)
        {

            var remoteUserId = MyEventContext.Current.Sender.Value;
            var proj = __instance;

            if (proj == null)
            {
                Log.Warn("Null projector detected");
                return true;
            }
            var grid = projectedGrids[0];
            if (grid == null) return true;

            var blocks = grid.CubeBlocks;

            if (MainLogic.Instance.Config.MaxGridSize > 0 && grid.CubeBlocks.Count > MainLogic.Instance.Config.MaxGridSize)
            {
                var diff = Math.Abs(grid.CubeBlocks.Count - MainLogic.Instance.Config.MaxGridSize);
                NetworkManager.RaiseEvent(__instance,RemoveBlueprintMethod);
                Validations.SendFailSound(remoteUserId);
                Validations.ValidationFailed();
                MyVisualScriptLogicProvider.SendChatMessage($" Projection is {diff} blocks too big", MainLogic.ChatName ,MySession.Static.Players.TryGetIdentityId(remoteUserId),MyFontEnum.Red);
                Log.Info($"Blocked an oversized grid projection by {MySession.Static.Players.TryGetIdentityNameFromSteamId(remoteUserId)}");
                return false;
            }

            if (blocks.Count == 0)
            {
                NetworkManager.RaiseEvent(__instance,RemoveBlueprintMethod);
                Validations.SendFailSound(remoteUserId);
                Validations.ValidationFailed();
                return false;
            }

            var invCount = 0;
            var jCount = 0;
            var tCount = 0;
            var pbCount = 0;
            var fCount = 0;
            var pRCount = 0;

            var removalCount = 0;
            var blockList = new HashSet<string>();

            for (var i = blocks.Count - 1; i >= 0; i--)
            {
                var block = blocks[i];
                var def = Utilities.GetDefinition(block);

                if (Utilities.IsMatch(def))
                {
                    blocks.RemoveAtFast(i);
                    removalCount++;
                    if (def==null || blockList.Contains(def.ToString().Substring(16))) continue;
                    blockList.Add(def.ToString().Substring(16));
                    continue;
                }

                if (block is MyObjectBuilder_OreDetector oreDetector && MainLogic.Instance.Config.ResetOreDetectors)
                {
                    oreDetector.DetectionRadius = 0;
                }

                if (block is MyObjectBuilder_RemoteControl rm && MainLogic.Instance.Config.ResetRemoteControl)
                {
                    rm.AutopilotSpeedLimit = 0;
                    rm.AutomaticBehaviour = null;
                }

                if (MainLogic.Instance.Config.ClearInventory)
                {
                  
                    switch (block)
                    {
                        case MyObjectBuilder_Drill drill:
                            if (drill.Inventory?.Items.Count > 0)
                            {
                                invCount++;
                                drill.Inventory?.Clear();
                            }
                            break;
                        case MyObjectBuilder_ShipToolBase shipTool:
                            if (shipTool.Inventory?.Items.Count > 0)
                            {
                                invCount++;
                                shipTool.Inventory?.Clear();
                            }
                            break;
                        case MyObjectBuilder_Reactor reactor:
                            if (reactor.Inventory?.Items.Count > 0)
                            {
                                invCount++;
                                reactor.Inventory?.Clear();
                            }
                            break;
                        case MyObjectBuilder_SmallMissileLauncherReload mm:
                        {
                            if (mm.GunBase?.RemainingAmmo > 0)
                            {
                                mm.GunBase = null;
                                invCount++;
                            }
                        }
                            break;
                        

                    }

                    var components = block.ComponentContainer?.Components;
                    
                    if (components != null)
                    {
                        foreach (var data in components)
                        {
                            if (!(data.Component is MyObjectBuilder_Inventory inv) || inv.Items.Count == 0 ) continue;
                            inv.Clear();
                            invCount++;
                        }
                    }

                    try
                    {
                        block.GetType()?.GetField("Inventory")?.SetValue(block,null);
                        block.GetType()?.GetField("InputInventory")?.SetValue(block,null);
                        block.GetType()?.GetField("OutputInventory")?.SetValue(block,null);
                    }
                    catch (Exception e)
                    {
                        Log.Warn(e,"There was an issue clearing projection inventories");
                    }
                }

                if (MainLogic.Instance.Config.ResetJumpDrives && block is MyObjectBuilder_JumpDrive jDrive && Math.Abs(jDrive.StoredPower) > 0)
                {
                    jDrive.StoredPower = 0;
                    jCount++;
                }

                if (MainLogic.Instance.Config.ResetTanks)
                {
                    if (block is MyObjectBuilder_GasTank tank && tank.FilledRatio > 0)
                    {
                        tank.FilledRatio = 0;
                        tCount++;
                    }

                    if (block is MyObjectBuilder_HydrogenEngine hEngine && hEngine.Capacity > 0)
                    {
                        hEngine.Capacity = 0;
                        tCount++;
                    }
                }

                if (MainLogic.Instance.Config.RemoveScripts && block is MyObjectBuilder_MyProgrammableBlock pbBlock)
                {
                    if (pbBlock.Program != null)
                    {
                        pbBlock.Program = null;
                        pbCount++;
                    }
                }

                if (MainLogic.Instance.Config.RemoveProjections && block is MyObjectBuilder_Projector projectedProjector)
                {
                    if (projectedProjector.ProjectedGrids != null)
                    {
                        projectedProjector.ProjectedGrid = null; 
                        pRCount++;
                    }

                }

                if (!MainLogic.Instance.Config.ShutOffBlocks || !(block is MyObjectBuilder_FunctionalBlock fBlock) || !fBlock.Enabled ||
                    fBlock is MyObjectBuilder_MergeBlock) continue;
                fBlock.Enabled = false;
                fCount++;

            }

            if (jCount > 0) Log.Info($"{jCount} JumpDrives edited");

            if (invCount > 0) Log.Info($"{invCount} inventory blocks cleaned");

            if (tCount > 0) Log.Info($"{tCount} tanks reset in projection");

            if (pbCount > 0) Log.Info($"{pbCount} programmable blocks cleared from projection");
             
            if (fCount > 0) Log.Info($"{fCount} blocks switched off in projection");

            if (pRCount > 0) Log.Info($"{pRCount} projections removed from projection");
        
            if (removalCount == 0) return true;

            Log.Info($"{removalCount} blocks removed from projection");

            NetworkManager.RaiseEvent(__instance,RemoveBlueprintMethod);

            var msg = string.Join("\n", $"Removed {removalCount} Blocks", string.Join("\n", blockList));

            MyVisualScriptLogicProvider.SendChatMessage(msg, MainLogic.ChatName,MySession.Static.Players.TryGetIdentityId(remoteUserId),MyFontEnum.Red);
            try
            {
                NetworkManager.RaiseEvent(__instance, NewBlueprintMethod,
                    new List<MyObjectBuilder_CubeGrid> {grid}, new EndpointId(remoteUserId));
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
            return true;
        }
        


    }

    public static class Utilities
    {
        private static readonly Logger Log = MainLogic.Log;

        private static readonly MyConcurrentDictionary<MyStringHash, MyCubeBlockDefinition> DefCache =
            new MyConcurrentDictionary<MyStringHash, MyCubeBlockDefinition>();

        private static readonly Config Config = MainLogic.Instance.Config;

        public static bool IsMatch(MyCubeBlockDefinition block)
        {
            var removeBlocks = Config.RemoveBlocks;
            if (!removeBlocks.Any() || block == null) return false;

            return removeBlocks.Any(x => x.Equals(block.Id.SubtypeId.ToString(), StringComparison.OrdinalIgnoreCase)
                                         || x.Equals(block.Id.TypeId.ToString().Substring(16),
                                             StringComparison.OrdinalIgnoreCase)
                                         || x.Equals(block.BlockPairName, StringComparison.OrdinalIgnoreCase));
        }

        public static MyCubeBlockDefinition GetDefinition(MyObjectBuilder_CubeBlock block)
        {
            if (DefCache.TryGetValue(block.SubtypeId, out var def))
                return def;

            var blockDefinition = MyDefinitionManager.Static.GetCubeBlockDefinition(block);
            DefCache[block.SubtypeId] = blockDefinition;
            return blockDefinition;

        }

        public static void CanProject(List<MyObjectBuilder_CubeGrid> projectedGrids, ulong remoteUserId, out bool changesMade)
        {

            changesMade = false;
            if (projectedGrids == null || projectedGrids.Count == 0) return;
            var grid = projectedGrids[0];

            var blocks = grid?.CubeBlocks;

            if (blocks == null || blocks.Count == 0)
            {
                return;
            }


            if (Config.MaxGridSize > 0 && blocks.Count > Config.MaxGridSize)
            {
                grid.CubeBlocks.Clear();
                changesMade = true;
                var diff = Math.Abs(blocks.Count - Config.MaxGridSize);
                if (remoteUserId > 0) MyVisualScriptLogicProvider.SendChatMessage($" Projection is {diff} blocks too big",
                    MainLogic.ChatName, MySession.Static.Players.TryGetIdentityId(remoteUserId), MyFontEnum.Red);
                Log.Info(
                    $"Blocked an oversized grid projection by {MySession.Static.Players.TryGetIdentityNameFromSteamId(remoteUserId)}");
                return;
            }



            var invCount = 0;
            var jCount = 0;
            var tCount = 0;
            var pbCount = 0;
            var fCount = 0;
            var pRCount = 0;

            var removalCount = 0;
            var blockList = new HashSet<string>();

            for (var i = blocks.Count - 1; i >= 0; i--)
            {
                var block = blocks[i];
                var def = GetDefinition(block);

                if (IsMatch(def))
                {
                    blocks.RemoveAtFast(i);
                    removalCount++;
                    if (def == null || blockList.Contains(def.ToString().Substring(16))) continue;
                    blockList.Add(def.ToString().Substring(16));
                    continue;
                }

                if (Config.ClearInventory)
                {
                    var components = block.ComponentContainer?.Components;
                    if (components != null)
                    {
                        foreach (var componentData in components)
                        {
                            if (!(componentData?.Component is MyObjectBuilder_Inventory inv)) continue;
                            if (inv.Items.Count == 0) continue;
                            inv.Clear();
                            invCount++;
                        }
                    }
                    switch (block)
                    {
                        case MyObjectBuilder_Drill drill:
                            if (drill.Inventory?.Items.Count > 0)
                            {
                                invCount++;
                                drill.Inventory?.Clear();
                            }
                            break;
                        case MyObjectBuilder_ShipToolBase shipTool:
                            if (shipTool.Inventory?.Items.Count > 0)
                            {
                                invCount++;
                                shipTool.Inventory?.Clear();
                            }
                            break;
                        case MyObjectBuilder_Reactor reactor:
                            if (reactor.Inventory?.Items.Count > 0)
                            {
                                invCount++;
                                reactor.Inventory?.Clear();
                            }
                            break;
                        case MyObjectBuilder_OxygenGenerator oxygen:
                            if (oxygen.Inventory?.Items.Count > 0)
                            {
                                invCount++;
                                oxygen.Inventory?.Clear();
                            }
                            break;
                    }

                }

                if (Config.ResetJumpDrives && block is MyObjectBuilder_JumpDrive jDrive &&
                    Math.Abs(jDrive.StoredPower) > 0)
                {
                    jDrive.StoredPower = 0;
                    jCount++;
                }

                if (Config.ResetTanks && block is MyObjectBuilder_GasTank tank && tank.FilledRatio > 0)
                {
                    tank.FilledRatio = 0;
                    tCount++;
                }

                if (Config.RemoveScripts && block is MyObjectBuilder_MyProgrammableBlock pbBlock)
                {
                    if (pbBlock.Program != null)
                    {
                        pbBlock.Program = null;
                        pbCount++;
                    }
                }

                if (Config.RemoveProjections && block is MyObjectBuilder_Projector projectedProjector)
                {
                    if (projectedProjector.ProjectedGrids != null)
                    {
                        projectedProjector.ProjectedGrid = null;
                        pRCount++;
                    }

                }

                if (!Config.ShutOffBlocks || !(block is MyObjectBuilder_FunctionalBlock fBlock) ||
                    !fBlock.Enabled ||
                    fBlock is MyObjectBuilder_MergeBlock) continue;
                fBlock.Enabled = false;
                fCount++;

            }

            if (jCount > 0) Log.Info($"{jCount} JumpDrives edited");

            if (invCount > 0) Log.Info($"{invCount} inventory blocks cleaned");

            if (tCount > 0) Log.Info($"{tCount} tanks reset in projection");

            if (pbCount > 0) Log.Info($"{pbCount} programmable blocks cleared from projection");

            if (fCount > 0) Log.Info($"{fCount} blocks switched off in projection");

            if (pRCount > 0) Log.Info($"{pRCount} projections removed from projection");

            if (removalCount == 0) return;
            changesMade = true;
            Log.Info($"{removalCount} blocks removed from projection");

            var msg = string.Join("\n", $"Removed {removalCount} Blocks", string.Join("\n", blockList));
            if (remoteUserId > 0) MyVisualScriptLogicProvider.SendChatMessage(msg, MainLogic.ChatName,
                MySession.Static.Players.TryGetIdentityId(remoteUserId), MyFontEnum.Red);

        }

        }

    public static class Validations
    {
        [ReflectedStaticMethod(Type = typeof(MyCubeBuilder), Name = "SpawnGridReply", OverrideTypes = new []{typeof(bool), typeof(ulong)})]
        private static Action<bool, ulong> _spawnGridReply;


        public static void SendFailSound(ulong target)
        {
            _spawnGridReply(false, target);
        }

        public static void ValidationFailed()
        {
            ((MyMultiplayerServerBase)MyMultiplayer.Static).ValidationFailed(MyEventContext.Current.Sender.Value);
        }


    }
}
