using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Torch.Commands;

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
                if (projector.IsProjecting)
                {
                    projector.SetProjectedGrid(null);
                    count++;
                }
                projector.Enabled = currentState;

            }

            Context.Respond($"{count} projectors cleared");
        }
    }
}
