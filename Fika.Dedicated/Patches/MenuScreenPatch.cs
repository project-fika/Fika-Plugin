using SPT.Reflection.Patching;
using EFT.UI.SessionEnd;
using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT.UI;
using SPT.Common.Http;
using Fika.Core.Models;
using Fika.Core.Networking.Http;

namespace Fika.Dedicated.Patches
{
    public class MenuScreenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MenuScreen).GetMethod(nameof(MenuScreen.Show), 
                [
                    typeof(Profile),
                    typeof(MatchmakerPlayerControllerClass),
                    typeof(ESessionMode)
                ]);
        }

        [PatchPostfix]
        static void PatchPostfix()
        {
            FikaDedicatedPlugin.Instance.StartSetDedicatedStatusRoutine();
        }
    }
}