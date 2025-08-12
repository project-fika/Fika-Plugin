using Comfort.Common;
using EFT.Vehicle;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.World;
using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.Main.Patches.BTR;

public class BTRView_SyncViewFromServer_Patch : FikaPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(BTRView).GetMethod(nameof(BTRView.SyncViewFromServer));
    }

    [PatchPrefix]
    public static void Prefix(ref BTRDataPacketStruct packet)
    {
        if (FikaBackendUtils.IsClient)
        {
            return;
        }

        Singleton<FikaServer>.Instance.SendBTRPacket(ref packet);
    }
}
