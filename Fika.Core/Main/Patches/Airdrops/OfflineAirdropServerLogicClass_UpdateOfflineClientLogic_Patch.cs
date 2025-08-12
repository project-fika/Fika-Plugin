using Comfort.Common;
using Fika.Core.Main.HostClasses;
using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.Main.Patches.Airdrops;

public class OfflineAirdropServerLogicClass_UpdateOfflineClientLogic_Patch : FikaPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(OfflineAirdropServerLogicClass).GetMethod(nameof(OfflineAirdropServerLogicClass.UpdateOfflineClientLogic));
    }

    [PatchPostfix]
    public static void Postfix(AirplaneDataPacketStruct ___AirplaneDataPacketStruct)
    {
        Singleton<FikaHostGameWorld>.Instance.FikaHostWorld.WorldPacket.SyncObjectPackets.Add(___AirplaneDataPacketStruct);
    }
}
