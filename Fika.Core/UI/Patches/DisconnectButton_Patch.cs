using Comfort.Common;
using EFT.UI;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.UI.Patches;

public class DisconnectButton_Patch : FikaPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(MenuScreen).GetMethod(nameof(MenuScreen.method_11));
    }

    [PatchPrefix]
    static bool Prefix()
    {
        if (FikaBackendUtils.IsServer)
        {
            FikaServer server = Singleton<FikaServer>.Instance;
            if (server != null && server.NetServer.ConnectedPeersCount > 0)
            {
                NotificationManagerClass.DisplayWarningNotification(string.Format(LocaleUtils.HOST_CANNOT_EXTRACT_MENU.Localized(),
                    server.NetServer.ConnectedPeersCount));
                return false;
            }
        }
        return true;
    }
}
