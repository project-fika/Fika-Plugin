using SPT.Reflection.Patching;
using EFT;
using EFT.UI;
using EFT.UI.SessionEnd;
using System;
using System.Reflection;
namespace Fika.Dedicated.Patches
{
    public class SessionResultExitStatusPatch : ModulePatch
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
            ____mainMenuButton.OnClick.Invoke();
        }
    }
}
