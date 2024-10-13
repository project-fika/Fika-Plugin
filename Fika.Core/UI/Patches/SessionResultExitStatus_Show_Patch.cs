using EFT.UI.SessionEnd;
using EFT.UI;
using EFT;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using Fika.Core.Coop.Utils;

namespace Fika.Core.UI.Patches
{
    public class SessionResultExitStatus_Show_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SessionResultExitStatus).GetMethod(nameof(SessionResultExitStatus.Show), [typeof(Profile),
                typeof(PlayerVisualRepresentation),
                typeof(ESideType),
                typeof(ExitStatus),
                typeof(TimeSpan),
                typeof(ISession),
                typeof(bool)]);
        }

        [PatchPostfix]
        static void PatchPostfix(DefaultUIButton ____mainMenuButton)
        {
            // Skip Session result exit status screen when spectator
            if(FikaBackendUtils.IsSpectator)
            {
                ____mainMenuButton.OnClick.Invoke();
            }
        }
    }
}
