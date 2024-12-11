using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches.Lighthouse
{
	internal class Zyriachy_Patches
	{
		public static void Enable()
		{
			new GClass422_Activate_Patch().Enable();

#if DEBUG
			/*new zryachiydebugpatch1().Enable();
			new zryachiydebugpatch2().Enable();
			new zryachiydebugpatch3().Enable();
			new zryachiydebugpatch4().Enable();
			new zryachiydebugpatch5().Enable();
			new zryachiydebugpatch6().Enable();*/
#endif
		}

		internal class GClass422_Activate_Patch : ModulePatch
		{
			protected override MethodBase GetTargetMethod()
			{
				return typeof(GClass422).GetMethod(nameof(GClass422.Activate));
			}

			[PatchPostfix]
			public static void Postfix(ref BotOwner ___botOwner_0)
			{
				___botOwner_0.GetPlayer.OnPlayerDead += OnZryachiyDead;
			}

			private static void OnZryachiyDead(Player player, IPlayer lastAggressor, DamageInfoStruct damageInfo, EBodyPart part)
			{
				player.OnPlayerDead -= OnZryachiyDead;

				Singleton<GameWorld>.Instance.BufferZoneController.SetInnerZoneAvailabilityStatus(false, EFT.BufferZone.EBufferZoneData.DisableByZryachiyDead);
			}
		}

		internal class zryachiydebugpatch1 : ModulePatch
		{
			protected override MethodBase GetTargetMethod()
			{
				return typeof(GClass422).GetMethod(nameof(GClass422.IsEnemyNow));
			}

			[PatchPostfix]
			public static void Postfix(IPlayer person, ref bool __result)
			{
				Logger.LogInfo($"zryachiydebugpatch1: {person.Profile.ProfileId} ({person.Profile.Nickname}) - Is Enemy: {__result}");
			}
		}

		internal class zryachiydebugpatch2 : ModulePatch
		{
			protected override MethodBase GetTargetMethod()
			{
				return typeof(GClass422).GetMethod(nameof(GClass422.method_6));
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

		internal class zryachiydebugpatch3 : ModulePatch
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

		internal class zryachiydebugpatch4 : ModulePatch
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

		internal class zryachiydebugpatch5 : ModulePatch
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

		internal class zryachiydebugpatch6 : ModulePatch
		{
			protected override MethodBase GetTargetMethod()
			{
				return typeof(BossSpawnerClass.Class328).GetMethod(nameof(BossSpawnerClass.Class328.method_0));
			}

			[PatchPostfix]
			public static void Postfix(BotOwner owner)
			{
				FikaPlugin.Instance.FikaLogger.LogWarning($"BossSpawnerClass.Class324.method_0: State {owner.BotState}, role {owner.Profile.Info.Settings.Role}");
			}
		}
	}
}
