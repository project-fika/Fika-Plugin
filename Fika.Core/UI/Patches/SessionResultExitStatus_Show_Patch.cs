using EFT;
using EFT.UI;
using EFT.UI.SessionEnd;
using Fika.Core.Coop.Utils;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace Fika.Core.UI.Patches
{
	public class SessionResultExitStatus_Show_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(SessionResultExitStatus).GetMethod(nameof(SessionResultExitStatus.Show), [typeof(Profile),
				typeof(GClass1917),
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
