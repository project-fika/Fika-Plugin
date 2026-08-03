using System.Reflection;
using Comfort.Common;
using EFT;
using Fika.Core.Main.HostClasses;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.GameWorldPatches;

public class GameWorld_GrenadeDestroyEvent_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GameWorld)
            .GetMethod(nameof(GameWorld.GrenadeDestroyEvent));
    }

    [PatchPrefix]
    public static bool Prefix(Throwable grenade)
    {
        if (Singleton<FikaHostGameWorld>.Instantiated && grenade.HasNetData)
        {
            var hostWorld = Singleton<FikaHostGameWorld>.Instance.FikaHostWorld;
            if (hostWorld != null)
            {
                hostWorld.WorldPacket?.GrenadePackets?.Add(grenade.GetNetPacket());
                hostWorld.SetCritical();
            }
        }

        return false;
    }
}
