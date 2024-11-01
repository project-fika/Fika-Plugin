using EFT.Achievements;
using SPT.Reflection.Patching;
using System.Reflection;
using TMPro;

namespace Fika.Core.UI.Patches
{
	/// <summary>
	/// By default the amount of players that have earned an achievement is not shown in if the session mode is <see cref="ESessionMode.Pve"/>, this patch forces it to be shown regardless of mode
	/// </summary>
	public class AchievementView_Show_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(AchievementView).GetMethod("GInterface466.Show");
		}

		[PatchPostfix]
		public static void Postfix(TMP_Text ____globalProgressText)
		{
			____globalProgressText.alpha = 1f;
		}
	}
}
