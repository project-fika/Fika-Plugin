// © 2025 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using Fika.Core.Main.BotClasses;
using Fika.Core.Main.Components;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.ObservedClasses;
using Fika.Core.Main.PacketHandlers;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets;
using Fika.Core.Networking.Packets.Communication;
using Fika.Core.Networking.Packets.Player;
using Fika.Core.Utils;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.Main.Players
{
    /// <summary>
    /// Used to simulate bots for the host.
    /// </summary>
    public class FikaBot : FikaPlayer
    {

        public override bool IsVisible
        {
            get
            {
                return _isHeadless || OnScreen;
            }
            set
            {

            }
        }

        private bool _isHeadless;

        public BotPacketSender BotPacketSender { get; internal set; }

        public static async Task<FikaBot> CreateBot(GameWorld gameWorld, int playerId, Vector3 position, Quaternion rotation,
            string layerName, string prefix, EPointOfView pointOfView, Profile profile, bool aiControl,
            EUpdateQueue updateQueue, EUpdateMode armsUpdateMode, EUpdateMode bodyUpdateMode,
            CharacterControllerSpawner.Mode characterControllerMode, Func<float> getSensitivity,
            Func<float> getAimingSensitivity, IViewFilter filter, MongoID currentId, ushort nextOperationId)
        {
            bool useSimpleAnimator = profile.Info.Settings.UseSimpleAnimator;
            ResourceKey resourceKey = useSimpleAnimator ? ResourceKeyManagerAbstractClass.ZOMBIE_BUNDLE_NAME : ResourceKeyManagerAbstractClass.PLAYER_BUNDLE_NAME;
            FikaBot player = Create<FikaBot>(gameWorld, resourceKey, playerId, position, updateQueue, armsUpdateMode,
                bodyUpdateMode, characterControllerMode, getSensitivity, getAimingSensitivity, prefix, aiControl, useSimpleAnimator);

            player._isHeadless = FikaBackendUtils.IsHeadless;
            player.IsYourPlayer = false;
            player.NetId = playerId;
            player.CommonPacket = new()
            {
                NetId = playerId
            };

            BotInventoryController inventoryController = new(player, profile, true, currentId, nextOperationId);

            BotPacketSender sender = BotPacketSender.Create(player);
            player.BotPacketSender = sender;
            player.PacketSender = sender;

            await player.Init(rotation, layerName, pointOfView, profile, inventoryController,
                new BotHealthController(profile.Health, player, inventoryController, profile.Skills, aiControl),
                new ObservedStatisticsManager(), null, null, null, filter,
                EVoipState.NotAvailable, aiControl, false);

            player.Pedometer.Stop();
            player._handsController = EmptyHandsController.smethod_6<EmptyHandsController>(player);
            player._handsController.Spawn(1f, delegate { });

            player.AIData = new PlayerAIDataClass(null, player)
            {
                IsAI = true
            };

            Traverse botTraverse = Traverse.Create(player);
            botTraverse.Field<LocalPlayerCullingHandlerClass>("localPlayerCullingHandlerClass").Value = new();
            botTraverse.Field<LocalPlayerCullingHandlerClass>("localPlayerCullingHandlerClass").Value.Initialize(player, player.PlayerBones);

            if (FikaBackendUtils.IsHeadless)
            {
                botTraverse.Field<LocalPlayerCullingHandlerClass>("localPlayerCullingHandlerClass").Value.SetMode(LocalPlayerCullingHandlerClass.EMode.Disabled);
            }

            player.AggressorFound = false;
            player._animators[0].enabled = true;

            return player;
        }

        public override void InitVoip(EVoipState voipState)
        {
            // Do nothing
        }

        public override void OnVaulting()
        {
            // Do nothing
        }

        public override void OnSkillLevelChanged(AbstractSkillClass skill)
        {
            // Do nothing
        }

        public override void OnWeaponMastered(MasterSkillClass masterSkill)
        {
            // Do nothing
        }

        public override void ConnectSkillManager()
        {
            // Do nothing
        }

        public override void InitAudioController()
        {
            if (!_isHeadless)
            {
                base.InitAudioController();
            }
        }

        public override void CreateSpeechSource()
        {
            if (!_isHeadless)
            {
                base.CreateSpeechSource();
            }
        }

        public override void UpdateMuffledState()
        {
            if (!_isHeadless)
            {
                base.UpdateMuffledState();
            }
        }

        public override void OnPhraseTold(EPhraseTrigger @event, TaggedClip clip, TagBank bank, PhraseSpeakerClass speaker)
        {
            if (_isHeadless)
            {
                if (ActiveHealthController.IsAlive)
                {
                    CommonPacket.Type = ECommonSubPacketType.Phrase;
                    CommonPacket.SubPacket = PhrasePacket.FromValue(@event, clip.NetId);
                    PacketSender.NetworkManager.SendNetReusable(ref CommonPacket, DeliveryMethod.ReliableOrdered, true);
                }
            }
            else
            {
                base.OnPhraseTold(@event, clip, bank, speaker);
            }
        }

        public override void OnBeenKilledByAggressor(IPlayer aggressor, DamageInfoStruct damageInfo, EBodyPart bodyPart, EDamageType lethalDamageType)
        {
            base.OnBeenKilledByAggressor(aggressor, damageInfo, bodyPart, lethalDamageType);

            if (aggressor.GroupId == "Fika" && !aggressor.IsYourPlayer)
            {
                FikaPlayer mainPlayer = (FikaPlayer)Singleton<GameWorld>.Instance.MainPlayer;
                if (mainPlayer == null)
                {
                    return;
                }

                if (!mainPlayer.HealthController.IsAlive)
                {
                    return;
                }

                WildSpawnType role = Profile.Info.Settings.Role;
                bool countAsBoss = role.CountAsBossForStatistics() && !(role is WildSpawnType.pmcUSEC or WildSpawnType.pmcBEAR);
                int experience = Profile.Info.Settings.Experience;
                SessionCountersClass sessionCounters = mainPlayer.Profile.EftStats.SessionCounters;
                HandleSharedExperience(countAsBoss, experience, sessionCounters);

                if (FikaPlugin.Instance.SharedQuestProgression && FikaPlugin.EasyKillConditions.Value)
                {
#if DEBUG
                    FikaPlugin.Instance.FikaLogger.LogInfo("Handling teammate kill from teammate: " + aggressor.Profile.Nickname);
#endif

                    float distance = Vector3.Distance(aggressor.Position, Position);
                    mainPlayer.HandleTeammateKill(damageInfo, bodyPart, Side, role, ProfileId,
                        distance, Inventory.EquippedInSlotsTemplateIds, HealthController.BodyPartEffects, TriggerZones,
                        (FikaPlayer)aggressor);
                }
            }
        }

        public override ShotInfoClass ApplyShot(DamageInfoStruct damageInfo, EBodyPart bodyPartType, EBodyPartColliderType colliderType, EArmorPlateCollider armorPlateCollider, ShotIdStruct shotId)
        {
            ActiveHealthController activeHealthController = ActiveHealthController;
            if (activeHealthController != null && !activeHealthController.IsAlive)
            {
                return null;
            }
            bool flag = damageInfo.DeflectedBy != null;
            float damage = damageInfo.Damage;
            List<ArmorComponent> list = ProceedDamageThroughArmor(ref damageInfo, colliderType, armorPlateCollider, true);
            method_97(list);
            MaterialType materialType = flag ? MaterialType.HelmetRicochet : ((list == null || list.Count < 1)
                ? MaterialType.Body : list[0].Material);
            ShotInfoClass hitInfo = new()
            {
                PoV = PointOfView,
                Penetrated = damageInfo.Penetrated,
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

            if (damageInfo.Weapon != null)
            {
                _lastWeaponId = damageInfo.Weapon.Id;
            }

            return hitInfo;
        }

        public override void ApplyDamageInfo(DamageInfoStruct damageInfo, EBodyPart bodyPartType, EBodyPartColliderType colliderType, float absorbed)
        {
            if (damageInfo.Weapon != null)
            {
                _lastWeaponId = damageInfo.Weapon.Id;
            }
            base.ApplyDamageInfo(damageInfo, bodyPartType, colliderType, absorbed);
        }

        public override void ApplyExplosionDamageToArmor(Dictionary<ExplosiveHitArmorColliderStruct, float> armorDamage, DamageInfoStruct damageInfo)
        {
            _preAllocatedArmorComponents.Clear();
            List<ArmorComponent> listTocheck = [];
            Inventory.GetPutOnArmorsNonAlloc(listTocheck);
            foreach (ArmorComponent armorComponent in listTocheck)
            {
                float num = 0f;
                foreach (KeyValuePair<ExplosiveHitArmorColliderStruct, float> keyValuePair in armorDamage)
                {
                    if (armorComponent.ShotMatches(keyValuePair.Key.BodyPartColliderType, keyValuePair.Key.ArmorPlateCollider))
                    {
                        num += keyValuePair.Value;
                        _preAllocatedArmorComponents.Add(armorComponent);
                    }
                }
                if (num > 0f)
                {
                    num = armorComponent.ApplyExplosionDurabilityDamage(num, damageInfo, _preAllocatedArmorComponents);
                    method_96(num, armorComponent);
                    OnArmorPointsChanged(armorComponent);
                }
            }
        }

        public override void Proceed(Weapon weapon, Callback<IFirearmHandsController> callback, bool scheduled = true)
        {
            BotFirearmControllerHandler handler = new(this, weapon);

            bool flag = false;
            FirearmController firearmController;
            if ((firearmController = _handsController as FirearmController) != null)
            {
                flag = firearmController.CheckForFastWeaponSwitch(handler.Weapon);
            }
            Func<FirearmController> func = new(handler.ReturnController);
            handler.Process = new Process<FirearmController, IFirearmHandsController>(this, func, handler.Weapon, flag);
            handler.ConfirmCallback = new(handler.SendPacket);
            handler.Process.method_0(new(handler.HandleResult), callback, scheduled);
        }

        public override void OnDead(EDamageType damageType)
        {
            float num = EFTHardSettings.Instance.HIT_FORCE;
            num *= 0.3f + 0.7f * Mathf.InverseLerp(50f, 20f, LastDamageInfo.PenetrationPower);
            _corpseAppliedForce = num;

            if (FikaPlugin.ShowNotifications.Value)
            {
                if (LocaleUtils.IsBoss(Profile.Info.Settings.Role, out string name) && LastAggressor != null)
                {
                    if (LastAggressor is FikaPlayer aggressor)
                    {
                        if (aggressor.gameObject.name.StartsWith("Player_") || aggressor.IsYourPlayer)
                            NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.KILLED_BOSS.Localized(),
                                [ColorizeText(EColor.GREEN, LastAggressor.Profile.Info.MainProfileNickname), ColorizeText(EColor.BROWN, name)]),
                                iconType: EFT.Communications.ENotificationIconType.Friend);
                    }
                }
            }

            Singleton<IFikaGame>.Instance.GameController.Bots.Remove(ProfileId);

            base.OnDead(damageType);
        }

        public override void ShowHelloNotification(string sender)
        {
            // Do nothing
        }

        protected void OnEnable()
        {
            IFikaGame fikaGame = Singleton<IFikaGame>.Instance;
            if (fikaGame != null && fikaGame.GameController.GameInstance.Status == GameStatus.Started)
            {
                BotStatePacket packet = new()
                {
                    NetId = NetId,
                    Type = BotStatePacket.EStateType.EnableBot
                };
                if (PacketSender != null)
                {
                    PacketSender.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
                }
            }
        }

        protected void OnDisable()
        {
            IFikaGame fikaGame = Singleton<IFikaGame>.Instance;
            if (fikaGame != null && fikaGame.GameController.GameInstance.Status == GameStatus.Started)
            {
                BotStatePacket packet = new()
                {
                    NetId = NetId,
                    Type = BotStatePacket.EStateType.DisableBot
                };
                if (PacketSender != null)
                {
                    PacketSender.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
                }
            }
        }

        public override void OnDestroy()
        {
#if DEBUG
            FikaPlugin.Instance.FikaLogger.LogInfo("Destroying " + Profile.Info.Nickname);
#endif
            if (Singleton<FikaServer>.Instantiated)
            {
                IFikaGame fikaGame = Singleton<IFikaGame>.Instance;
                if (fikaGame != null && fikaGame.GameController.GameInstance.Status == GameStatus.Started)
                {
                    FikaServer server = Singleton<FikaServer>.Instance;
                    BotStatePacket packet = new()
                    {
                        NetId = NetId,
                        Type = BotStatePacket.EStateType.DisposeBot
                    };

                    server.SendData(ref packet, DeliveryMethod.ReliableOrdered);
                    fikaGame.GameController.Bots.Remove(ProfileId);
                }
            }
            if (CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
            {
                coopHandler.Players.Remove(NetId);
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

        private class BotFirearmControllerHandler(FikaBot fikaBot, Weapon weapon)
        {
            private readonly FikaBot _fikaBot = fikaBot;
            public readonly Weapon Weapon = weapon;
            public Process<FirearmController, IFirearmHandsController> Process;
            public Action ConfirmCallback;

            internal BotFirearmController ReturnController()
            {
                return BotFirearmController.Create(_fikaBot, Weapon);
            }

            internal void SendPacket()
            {
                _fikaBot.CommonPacket.Type = ECommonSubPacketType.Proceed;
                _fikaBot.CommonPacket.SubPacket = ProceedPacket.FromValue(default, Weapon.Id, 0f, 0,
                    Weapon.IsStationaryWeapon ? EProceedType.Stationary : EProceedType.Weapon, false);
                _fikaBot.PacketSender.NetworkManager.SendNetReusable(ref _fikaBot.CommonPacket, DeliveryMethod.ReliableOrdered, true);
            }

            internal void HandleResult(IResult result)
            {
                if (result.Succeed)
                {
                    ConfirmCallback();
                }
            }
        }
    }
}
