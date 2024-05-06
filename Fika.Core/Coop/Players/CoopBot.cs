// © 2024 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.Counters;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using Fika.Core.Coop.BotClasses;
using Fika.Core.Coop.ClientClasses;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.Custom;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.ObservedClasses;
using Fika.Core.Coop.PacketHandlers;
using Fika.Core.Networking;
using LiteNetLib.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static Fika.Core.Networking.FikaSerialization;

namespace Fika.Core.Coop.Players
{
    /// <summary>
    /// Used to simulate bots for the host.
    /// </summary>
    public class CoopBot : CoopPlayer
    {
        public CoopPlayer MainPlayer => (CoopPlayer)Singleton<GameWorld>.Instance.MainPlayer;
        /// <summary>
        /// The amount of players that have loaded this bot
        /// </summary>
        public int loadedPlayers = 0;
        private FikaDynamicAI dynamicAi;
        public bool isStarted = false;

        public static async Task<LocalPlayer> CreateBot(
            int playerId,
            Vector3 position,
            Quaternion rotation,
            string layerName,
            string prefix,
            EPointOfView pointOfView,
            Profile profile,
            bool aiControl,
            EUpdateQueue updateQueue,
            EUpdateMode armsUpdateMode,
            EUpdateMode bodyUpdateMode,
            CharacterControllerSpawner.Mode characterControllerMode,
            Func<float> getSensitivity, Func<float> getAimingSensitivity,
            GInterface99 filter,
            AbstractQuestControllerClass questController = null,
            AbstractAchievementControllerClass achievementController = null,
            bool isYourPlayer = false
            )
        {
            CoopBot player = null;

            player = Create<CoopBot>(
                    GClass1388.PLAYER_BUNDLE_NAME,
                    playerId,
                    position,
                    updateQueue,
                    armsUpdateMode,
                    bodyUpdateMode,
                    characterControllerMode,
                    getSensitivity,
                    getAimingSensitivity,
                    prefix,
                    aiControl);
            player.IsYourPlayer = isYourPlayer;

            InventoryControllerClass inventoryController = new CoopBotInventoryController(player, profile, true);

            await player.Init(rotation, layerName, pointOfView, profile, inventoryController,
                new CoopClientHealthController(profile.Health, player, inventoryController, profile.Skills, aiControl),
                new CoopObservedStatisticsManager(), questController, achievementController, filter,
                EVoipState.NotAvailable, aiControl, async: false);

            player._handsController = EmptyHandsController.smethod_5<EmptyHandsController>(player);
            player._handsController.Spawn(1f, delegate { });
            player.AIData = new AIData(null, player)
            {
                IsAI = true
            };
            player.AggressorFound = false;
            player._animators[0].enabled = true;
            if (!player.IsYourPlayer)
            {
                player._armsUpdateQueue = EUpdateQueue.Update;
            }

            return player;
        }

        public override void OnVaulting()
        {
            // Do nothing
        }

        public override void OnSkillLevelChanged(GClass1766 skill)
        {
            // Do nothing
        }

        public override void OnWeaponMastered(MasterSkillClass masterSkill)
        {
            // Do nothing
        }

        public override void CreateMovementContext()
        {
            LayerMask movement_MASK = EFTHardSettings.Instance.MOVEMENT_MASK;
            MovementContext = BotMovementContext.Create(this, new Func<IAnimator>(GetBodyAnimatorCommon), new Func<ICharacterController>(GetCharacterControllerCommon), movement_MASK);
        }

        /*public override void ApplyDamageInfo(DamageInfo damageInfo, EBodyPart bodyPartType, EBodyPartColliderType colliderType, float absorbed)
        {
            if (damageInfo.Player != null && damageInfo.Player.iPlayer is ObservedCoopPlayer)
                return;

            base.ApplyDamageInfo(damageInfo, bodyPartType, colliderType, absorbed);
        }*/

        public override GClass1676 ApplyShot(DamageInfo damageInfo, EBodyPart bodyPartType, EBodyPartColliderType colliderType, EArmorPlateCollider armorPlateCollider, GStruct390 shotId)
        {
            if (damageInfo.Player != null && damageInfo.Player.iPlayer is ObservedCoopPlayer)
            {
                LastDamageInfo.BodyPartColliderType = damageInfo.BodyPartColliderType;
                LastDamageInfo.Direction = damageInfo.Direction;
                LastDamageInfo.HitPoint = damageInfo.HitPoint;
                LastDamageInfo.PenetrationPower = damageInfo.PenetrationPower;
                return null;
            }

            ActiveHealthController activeHealthController = ActiveHealthController;
            if (activeHealthController != null && !activeHealthController.IsAlive)
            {
                return null;
            }
            bool flag = !string.IsNullOrEmpty(damageInfo.DeflectedBy);
            float damage = damageInfo.Damage;
            List<ArmorComponent> list = ProceedDamageThroughArmor(ref damageInfo, colliderType, armorPlateCollider, true);
            MaterialType materialType = (flag ? MaterialType.HelmetRicochet : ((list == null || list.Count < 1) ? MaterialType.Body : list[0].Material));
            GClass1676 hitInfo = new()
            {
                PoV = PointOfView,
                Penetrated = (string.IsNullOrEmpty(damageInfo.BlockedBy) || string.IsNullOrEmpty(damageInfo.DeflectedBy)),
                Material = materialType
            };
            float num = damage - damageInfo.Damage;
            if (num > 0)
            {
                damageInfo.DidArmorDamage = num;
            }
            ApplyDamageInfo(damageInfo, bodyPartType, colliderType, 0f);
            ShotReactions(damageInfo, bodyPartType);
            ReceiveDamage(damageInfo.Damage, bodyPartType, damageInfo.DamageType, num, hitInfo.Material);
            return hitInfo;
        }

        protected override void Start()
        {
            PacketSender = gameObject.AddComponent<BotPacketSender>();
            PacketReceiver = gameObject.AddComponent<PacketReceiver>();

            if (Singleton<FikaServer>.Instance.NetServer.ConnectedPeersCount > 0)
            {
                StartCoroutine(WaitForPlayersToLoadBot());
            }
            else
            {
                isStarted = true;
            }

            WildSpawnType botType = Profile.Info.Settings.Role;

            if ((botType == (WildSpawnType)FikaPlugin.sptUsecValue || botType == (WildSpawnType)FikaPlugin.sptBearValue) && FikaPlugin.DynamicAIPMC.Value)
            {
                dynamicAi = gameObject.AddComponent<FikaDynamicAI>();
            }
            else if (botType == WildSpawnType.assault && FikaPlugin.DynamicAIScav.Value)
            {
                dynamicAi = gameObject.AddComponent<FikaDynamicAI>();
            }
            else if (FikaPlugin.DynamicAIRest.Value)
            {
                dynamicAi = gameObject.AddComponent<FikaDynamicAI>();
            }
        }

        public override void BtrInteraction()
        {
            // Do nothing
        }

        public override void Proceed(Weapon weapon, Callback<IFirearmHandsController> callback, bool scheduled = true)
        {
            BotFirearmControllerHandler handler = new()
            {
                coopBot = this,
                weapon = weapon
            };
            bool flag = false;
            FirearmController firearmController;
            if ((firearmController = _handsController as FirearmController) != null)
            {
                flag = firearmController.CheckForFastWeaponSwitch(handler.weapon);
            }
            Func<FirearmController> func = new(handler.ReturnController);
            handler.process = new Process<FirearmController, IFirearmHandsController>(this, func, handler.weapon, flag);
            handler.confirmCallback = new(handler.SendPacket);
            handler.process.method_0(new(handler.HandleResult), callback, scheduled);
        }

        private IEnumerator WaitForPlayersToLoadBot()
        {
            float originalDamageCoeff = ActiveHealthController.DamageCoeff;
            AIData.BotOwner.Disable();
            ActiveHealthController.SetDamageCoeff(0);
            Vector3 spawnPosition = Position;
            int connectedPeers = Singleton<FikaServer>.Instance.NetServer.ConnectedPeersCount;
            float randomY = UnityEngine.Random.RandomRange(-9500, -10000);
            DateTime start = DateTime.Now;
            Teleport(new(0, randomY, 0));

            while (loadedPlayers < connectedPeers && Math.Abs((start - DateTime.Now).TotalSeconds) < 30)
            {
                yield return new WaitForSeconds(1);
                Teleport(new(0, randomY, 0));
            }

            // Wait a little bit longer just to make sure...
            yield return new WaitForSeconds(2);

            Teleport(new Vector3(spawnPosition.x, spawnPosition.y + 0.5f, spawnPosition.z));
            yield return new WaitForSeconds(1);
            ActiveHealthController.SetDamageCoeff(originalDamageCoeff);
            AIData.BotOwner.BotState = EBotState.PreActive;
            isStarted = true;
        }

        public override void OnDead(EDamageType damageType)
        {
            PacketSender.FirearmPackets.Clear();

            float num = EFTHardSettings.Instance.HIT_FORCE;
            num *= 0.3f + 0.7f * Mathf.InverseLerp(50f, 20f, LastDamageInfo.PenetrationPower);
            _corpseAppliedForce = num;

            if (Side is EPlayerSide.Usec or EPlayerSide.Bear)
            {
                SetupDogTag();
            }

            /*DeathPacket packet = new(ProfileId)
            {
                RagdollPacket = new()
                {
                    BodyPartColliderType = LastDamageInfo.BodyPartColliderType,
                    Direction = LastDamageInfo.Direction,
                    Point = LastDamageInfo.HitPoint,
                    Force = _corpseAppliedForce,
                    OverallVelocity = Velocity
                },
                HasInventory = true,
                Equipment = Inventory.Equipment
            };

            PacketSender?.Server?.SendDataToAll(new NetDataWriter(), ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
            PacketSender?.Server?.NetServer?.TriggerUpdate();*/

            if (FikaPlugin.ShowNotifications.Value)
            {
                if (IsBoss(Profile.Info.Settings.Role, out string name) && LastAggressor != null)
                {
                    if (LastAggressor is CoopPlayer aggressor)
                    {
                        if (aggressor.gameObject.name.StartsWith("Player_") || aggressor.IsYourPlayer)
                            NotificationManagerClass.DisplayMessageNotification($"{LastAggressor.Profile.Nickname} killed boss {name}", iconType: EFT.Communications.ENotificationIconType.Friend);
                    }
                }
            }

            if (Side == EPlayerSide.Savage)
            {
                if (LastAggressor != null)
                {
                    if (LastAggressor is CoopPlayer coopPlayer)
                    {
                        coopPlayer.hasKilledScav = true;
                    }
                }
            }

            if (hasKilledScav)
            {
                if (LastAggressor != null)
                {
                    if (LastAggressor.IsYourPlayer && LastAggressor.Side == EPlayerSide.Savage)
                    {
                        if (Side is EPlayerSide.Usec or EPlayerSide.Bear)
                        {
                            // This one is already handled by SPT, so we do not add directly to profile until they move it to client side
                            // They also do a flat value of 0.02 rather than 0.01 for 1 scav kill or 0.03 for >1
                            LastAggressor.Profile.EftStats.SessionCounters.AddDouble(0.02, [CounterTag.FenceStanding, EFenceStandingSource.ScavHelp]);
                            //LastAggressor.Profile.FenceInfo.AddStanding(0.01, EFenceStandingSource.ScavHelp);
                        }
                        else if (Side == EPlayerSide.Savage)
                        {
                            //LastAggressor.Profile.EftStats.SessionCounters.AddDouble(0.03, [CounterTag.FenceStanding, EFenceStandingSource.TraitorKill]);
                            LastAggressor.Profile.FenceInfo.AddStanding(0.03, EFenceStandingSource.TraitorKill);
                        }
                    }
                }
            }
            base.OnDead(damageType);
        }

        private IEnumerator DestroyNetworkedComponents()
        {
            yield return new WaitForSeconds(2);

            PacketSender?.DestroyThis();
        }

        public override void UpdateTick()
        {
            base.UpdateTick();

            // Temp test to fix AI dying from hydration and energy...
            /*if (ActiveHealthController.Energy.Current < 1)
            {
                logger($"Setting energy to 50 on {ProfileId}");
                ActiveHealthController.ChangeEnergy(50);
            }

            if (ActiveHealthController.Hydration.Current < 1)
            {
                logger($"Setting hydration to 50 on {ProfileId}");
                ActiveHealthController.ChangeHydration(50);
            }*/
        }

        public override void OnDestroy()
        {
            FikaPlugin.Instance.FikaLogger.LogInfo("Destroying " + ProfileId);
            if (Singleton<FikaServer>.Instantiated)
            {
                CoopGame coopGame = (CoopGame)Singleton<IFikaGame>.Instance;
                if (coopGame != null && coopGame.Status == GameStatus.Started)
                {
                    FikaServer server = Singleton<FikaServer>.Instance;
                    GenericPacket packet = new(EPackageType.DisposeBot)
                    {
                        NetId = MainPlayer.NetId,
                        BotNetId = NetId
                    };
                    server.SendDataToAll(new NetDataWriter(), ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
                }
            }
            if (CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
            {
                if (!coopHandler.Players.Remove(NetId))
                {
                    FikaPlugin.Instance.FikaLogger.LogWarning("Unable to remove " + NetId + " from CoopHandler.Players when Destroying");
                }
            }
            base.OnDestroy();
        }

        public override void SendHandsInteractionStateChanged(bool value, int animationId)
        {
            if (value)
            {
                MovementContext.SetBlindFire(0);
            }
        }

        private class BotFirearmControllerHandler
        {
            internal BotFirearmController ReturnController()
            {
                return BotFirearmController.Create(coopBot, weapon);
            }

            internal void SendPacket()
            {
                if (weapon.IsStationaryWeapon)
                {
                    return;
                }

                coopBot.PacketSender?.CommonPlayerPackets?.Enqueue(new()
                {
                    HasProceedPacket = true,
                    ProceedPacket = new()
                    {
                        ProceedType = EProceedType.Weapon,
                        ItemId = weapon.Id,
                        ItemTemplateId = weapon.TemplateId
                    }
                });
            }

            internal void HandleResult(IResult result)
            {
                if (result.Succeed)
                {
                    confirmCallback();
                }
            }

            public CoopBot coopBot;

            public Weapon weapon;

            public Process<FirearmController, IFirearmHandsController> process;

            public Action confirmCallback;
        }
    }
}
