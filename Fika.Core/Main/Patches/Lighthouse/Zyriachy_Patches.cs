using Comfort.Common;
using EFT;
using EFT.BufferZone;
using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.Main.Patches.Lighthouse;

public static class Zyriachy_Patches
{
    /// <summary>
    /// Search for 'Zryachiy don't have controllable zone FIX it' string in assembly to find class
    /// </summary>
    public class ZyriachyBossLogicClass_Activate_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ZyriachyBossLogicClass)
                .GetMethod(nameof(ZyriachyBossLogicClass.Activate));
        }

        [PatchPostfix]
        public static void Postfix(ref BotOwner ___BotOwner_0)
        {
            ___BotOwner_0.GetPlayer.OnPlayerDead += OnZryachiyDead;
        }

        private static void OnZryachiyDead(Player player, IPlayer lastAggressor, DamageInfoStruct damageInfo, EBodyPart part)
        {
            player.OnPlayerDead -= OnZryachiyDead;
            Singleton<GameWorld>.Instance.BufferZoneController.SetInnerZoneAvailabilityStatus(false,
                EBufferZoneData.DisableByZryachiyDead);
        }
    }
}
