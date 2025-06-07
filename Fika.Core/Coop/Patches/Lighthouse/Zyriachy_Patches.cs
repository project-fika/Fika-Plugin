using Comfort.Common;
using EFT;
using EFT.BufferZone;
using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches.Lighthouse
{
    internal class Zyriachy_Patches
    {
        internal class GClass449_Activate_Patch : FikaPatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return typeof(GClass449).GetMethod(nameof(GClass449.Activate));
            }

            [PatchPostfix]
            public static void Postfix(ref BotOwner ___BotOwner_0)
            {
                ___BotOwner_0.GetPlayer.OnPlayerDead += OnZryachiyDead;
            }

            private static void OnZryachiyDead(Player player, IPlayer lastAggressor, DamageInfoStruct damageInfo, EBodyPart part)
            {
                player.OnPlayerDead -= OnZryachiyDead;
                Singleton<GameWorld>.Instance.BufferZoneController.SetInnerZoneAvailabilityStatus(false, EBufferZoneData.DisableByZryachiyDead);
            }
        }

        /*[DebugPatch]
        internal class zryachiydebugpatch1 : FikaPatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return typeof(GClass437).GetMethod(nameof(GClass437.IsEnemyNow));
            }

            [PatchPostfix]
            public static void Postfix(IPlayer person, ref bool __result)
            {
                Logger.LogInfo($"zryachiydebugpatch1: {person.Profile.ProfileId} ({person.Profile.Nickname}) - Is Enemy: {__result}");
            }
        }

        [DebugPatch]
        internal class zryachiydebugpatch2 : FikaPatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return typeof(GClass437).GetMethod(nameof(GClass437.method_6));
            }

            [PatchPostfix]
            public static void Postfix(IPlayer player, ref bool __result)
            {
                RadioTransmitterRecodableComponent radioTransmitterRecodableComponent = player.FindRadioTransmitter();
                bool test = radioTransmitterRecodableComponent != null && radioTransmitterRecodableComponent.Handler.Status == RadioTransmitterStatus.Green;

                if (test)
                {
                    Logger.LogInfo($"zryachiydebugpatch2: {player.Profile.ProfileId} ({player.Profile.Nickname}) - Has transmitter: Yes");
                }
                else
                {
                    Logger.LogInfo($"zryachiydebugpatch2: {player.Profile.ProfileId} ({player.Profile.Nickname}) - Has transmitter: Nope");
                }
            }
        }

        [DebugPatch]
        internal class zryachiydebugpatch3 : FikaPatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return typeof(BotsGroup).GetMethod(nameof(BotsGroup.IsPlayerEnemy));
            }

            [PatchPostfix]
            public static void Postfix(BotsGroup __instance, bool __result, IPlayer player)
            {
                if (__instance.InitialBotType is WildSpawnType.bossZryachiy or WildSpawnType.followerZryachiy)
                {
                    FikaPlugin.Instance.FikaLogger.LogWarning($"LightkeeperDebug::IsPlayerEnemy: Result {__result}, player {player.Profile.Nickname}");
                }
            }
        }

        [DebugPatch]
        internal class zryachiydebugpatch4 : FikaPatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return typeof(ShallBeGroupParams).GetProperty(nameof(ShallBeGroupParams.IsBossSetted)).GetGetMethod();
            }

            [PatchPostfix]
            public static void Postfix(bool __result)
            {
                FikaPlugin.Instance.FikaLogger.LogWarning($"ShallBeGroupParams.IsBossSetted: Result {__result}");
            }
        }

        [DebugPatch]
        internal class zryachiydebugpatch5 : FikaPatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return typeof(BotBoss).GetProperty(nameof(BotBoss.BossLogic)).GetSetMethod();
            }

            [PatchPostfix]
            public static void Postfix(BotBoss __instance)
            {
                FikaPlugin.Instance.FikaLogger.LogWarning($"BotBoss.BossLogic: Role {__instance.Owner.Profile.Info.Settings.Role}");
                //FikaPlugin.Instance.FikaLogger.LogWarning($"BotBoss.BossLogic: Stack {Environment.StackTrace}");				
                FikaPlugin.Instance.FikaLogger.LogWarning($"BotBoss.BossLogic: Result {__instance.BossLogic}");
            }
        }

        [DebugPatch]
        internal class zryachiydebugpatch6 : FikaPatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return typeof(BossSpawnerClass.Class329).GetMethod(nameof(BossSpawnerClass.Class329.method_0));
            }

            [PatchPostfix]
            public static void Postfix(BotOwner owner)
            {
                FikaPlugin.Instance.FikaLogger.LogWarning($"BossSpawnerClass.Class324.method_0: State {owner.BotState}, role {owner.Profile.Info.Settings.Role}");
            }
        }*/
    }
}
