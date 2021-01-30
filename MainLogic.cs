using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using DisabledProjectedBlocks;
using NLog;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Managers.PatchManager;
using Torch.Session;
using VRage.Game;

namespace DisableProjectedBlocks
{
    public class MainLogic : TorchPluginBase, IWpfPlugin
    {

        public static readonly Logger Log = LogManager.GetLogger("DisableProjectedBlocks Plugin");

        public static MainLogic Instance { get; private set; }
        public static IPluginManager PluginManager { get; private set; }
        public static string ChatName;

        private Control _control;
        public UserControl GetControl() => _control ?? (_control = new Control(this));
        private Persistent<Config> _config;
        public Config Config => _config?.Data;

        public void Save() => _config.Save();

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);

            Instance = this;

            ChatName = Torch.Config.ChatName;
            PluginManager = Torch.Managers.GetManager<IPluginManager>();

            try
            {
                var ctx = torch.Managers.GetManager<PatchManager>().AcquireContext();
                if (ctx == null) Log.Error("Context Error");
                else
                {
                    ProjectorPatch.Patch(ctx);
                }

            }
            catch (Exception e)
            {
                Log.Error(e,"Patch failed bitch!!!");
                throw;
            }

            var configFile = Path.Combine(StoragePath, "DisableProjectedBlocks.cfg");

            try 
            {

                _config = Persistent<Config>.Load(configFile);

            }
            catch (Exception e) 
            {
                Log.Warn(e);
            }

            if (_config?.Data != null) return;
            Log.Info("Created Default Config, because none was found!");

            _config = new Persistent<Config>(configFile, new Config());
            _config.Save();

        }

        public static void CanProject(List<MyObjectBuilder_CubeGrid> projectedGrids, ulong remoteUserId, out bool changesMade)
        {
            Utilities.CanProject(projectedGrids, remoteUserId, out changesMade);
        }

    }
}
