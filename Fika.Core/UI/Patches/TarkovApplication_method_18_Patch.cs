using EFT;
using Fika.Core.Networking.Websocket;
using SPT.Reflection.Patching;
using System.Reflection;
using Comfort.Common;
using EFT.UI;

namespace Fika.Core.UI.Patches
{
	/// <summary>
	/// The intention of this patch is to enable FikaNotificationManager after NotificationManagerClass and the NotifierView are initialized.
	/// </summary>
	public class TarkovApplication_method_18_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(TarkovApplication).GetMethod(nameof(TarkovApplication.method_18));
		}

		[PatchPostfix]
		internal static void PatchPostfix()
		{
			Singleton<PreloaderUI>.Instance.gameObject.AddComponent<FikaNotificationManager>();
		}
	}
}
