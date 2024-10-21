using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Networking;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches.Lighthouse
{
	internal class Zyriachy_Patches
	{
		public static void Enable()
		{
			new GClass414_Activate_Patch().Enable();

#if DEBUG
			new zryachiydebugpatch1().Enable();
			new zryachiydebugpatch2().Enable();
#endif
		}

		internal class GClass414_Activate_Patch : ModulePatch
		{
			protected override MethodBase GetTargetMethod()
			{
				return typeof(GClass414).GetMethod(nameof(GClass414.Activate));
			}

			[PatchPostfix]
			public static void Postfix(ref BotOwner ___botOwner_0)
			{
				___botOwner_0.GetPlayer.OnPlayerDead += OnBossOrFollowerDead;
				___botOwner_0.Boss.OnFollowerStatusChange += Boss_OnFollowerStatusChange;
			}

			private static void Boss_OnFollowerStatusChange(BotOwner follower, FollowerStatusChange status)
			{
				switch (status)
				{
					case FollowerStatusChange.Add:
						follower.GetPlayer.OnPlayerDead += OnBossOrFollowerDead;
						break;
					case FollowerStatusChange.Remove:
						follower.GetPlayer.OnPlayerDead -= OnBossOrFollowerDead;
						break;
				}
			}

			private static void OnBossOrFollowerDead(Player player, IPlayer lastAggressor, DamageInfo damageInfo, EBodyPart part)
			{
				player.OnPlayerDead -= OnBossOrFollowerDead;

				WildSpawnType SpawnType = WildSpawnType.followerZryachiy;

				if (player.AIData.BotOwner.IsRole(WildSpawnType.bossZryachiy))
				{
					SpawnType = WildSpawnType.bossZryachiy;
				}

				Singleton<FikaServer>.Instance.SendLighthouseBossOrFollowerDeadEvent(player.KillerId, SpawnType);
			}
		}

		internal class zryachiydebugpatch1 : ModulePatch
		{
			protected override MethodBase GetTargetMethod()
			{
				return typeof(GClass414).GetMethod(nameof(GClass414.IsEnemyNow));
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
				return typeof(GClass414).GetMethod(nameof(GClass414.method_6));
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
	}
}
