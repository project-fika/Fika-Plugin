using EFT.Airdrop;
using System.Reflection;
using EFT.SynchronizableObjects;
using Fika.Core.Main.Utils;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.Airdrops;

public class SyncObjectProcessorClass_GetSyncObjectStrategyByType_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(SynchronizableObjectLogicProcessor).GetMethod(nameof(SynchronizableObjectLogicProcessor.GetSyncObjectStrategyByType), BindingFlags.Static | BindingFlags.Public);
    }

    [PatchPrefix]
    public static bool Prefix(SynchronizableObjectType type, ref ISynchronizableLogic __result)
    {
        switch (type)
        {
            case SynchronizableObjectType.Tripwire:
                __result = new BaseTripwire();
                break;
            case SynchronizableObjectType.AirPlane:
                __result = new ClientAirPlane(FikaBackendUtils.IsServer);
                break;
            case SynchronizableObjectType.AirDrop:
                __result = new ClientAirDrop(FikaBackendUtils.IsServer);
                break;
            default:
                break;
        }

        return false;
    }
}
