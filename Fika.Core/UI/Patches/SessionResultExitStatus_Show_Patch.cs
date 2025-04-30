using EFT;
using EFT.UI;
using EFT.UI.SessionEnd;
using Fika.Core.Coop.Utils;
using Fika.Core.Patching;
using System;
using System.Reflection;

namespace Fika.Core.UI.Patches
{
    public class SessionResultExitStatus_Show_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SessionResultExitStatus).GetMethod(nameof(SessionResultExitStatus.Show), [typeof(Profile),
                typeof(LastPlayerStateClass),
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
            if (FikaBackendUtils.IsSpectator)
            {
                ____mainMenuButton.OnClick.Invoke();
            }
        }
    }
}
