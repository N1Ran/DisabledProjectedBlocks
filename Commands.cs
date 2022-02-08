using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Torch.Commands;
using VRage.Game.Entity;
using VRage.Game.Entity.EntityComponents.Interfaces;
using VRage.ModAPI;

namespace DisableProjectedBlocks
{
    [Torch.Commands.Category("dpb")]
    public  partial class Commands:CommandModule
    {
        [Command("clearprojectors", "Clears all current projectors")]
        public void ClearProjectors()
        {
            var cubeBlocks = MyEntities.GetEntities().OfType<MyCubeGrid>().SelectMany(x => x.CubeBlocks).ToList();

            var count = 0;
            foreach (var block in cubeBlocks)
            {
                if (!(block.FatBlock is IMyProjector projector)) continue;
                var currentState = projector.Enabled;
                projector.Enabled = true;
                var grid = block.CubeGrid;
                RegisterRecursive(grid);
                if (projector.IsProjecting)
                {
                    count++;
                }
                projector.SetProjectedGrid(null);

                projector.Enabled = currentState;

            }
            void RegisterRecursive(MyEntity e)
            {
                if (e.IsPreview)
                    return;
                
                MyEntities.RegisterForUpdate(e);
                (e.GameLogic as IMyGameLogicComponent)?.RegisterForUpdate();
                e.Flags &= ~(EntityFlags)4;
                if (e.Hierarchy == null)
                    return;

                foreach (var child in e.Hierarchy.Children)
                    RegisterRecursive((MyEntity)child.Container.Entity);
            }

            Context.Respond($"{count} projectors cleared");
        }
    }
}
