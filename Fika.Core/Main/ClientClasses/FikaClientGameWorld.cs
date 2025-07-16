using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.SynchronizableObjects;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using HarmonyLib;
using System;
using System.Collections.Generic;
using Systems.Effects;
using UnityEngine;

namespace Fika.Core.Main.ClientClasses
{
    /// <summary>
    /// <see cref="ClientLocalGameWorld"/> used in Fika for clients to override methods and logic
    /// </summary>
    public class FikaClientGameWorld : ClientLocalGameWorld
    {
        public FikaClientWorld FikaClientWorld { get; private set; }

        public static FikaClientGameWorld Create(GameObject gameObject, PoolManagerClass objectsFactory, EUpdateQueue updateQueue, string currentProfileId)
        {
            FikaClientGameWorld gameWorld = gameObject.AddComponent<FikaClientGameWorld>();
            gameWorld.ObjectsFactory = objectsFactory;
            Traverse.Create(gameWorld).Field<EUpdateQueue>("eupdateQueue_0").Value = updateQueue;
            gameWorld.SpeakerManager = gameObject.AddComponent<SpeakerManager>();
            gameWorld.ExfiltrationController = new ExfiltrationControllerClass();
            gameWorld.BufferZoneController = new BufferZoneControllerClass();
            gameWorld.CurrentProfileId = currentProfileId;
            gameWorld.UnityTickListener = GameWorldUnityTickListener.Create(gameObject, gameWorld);
            gameWorld.AudioSourceCulling = gameObject.GetOrAddComponent<AudioSourceCulling>();
            gameWorld.FikaClientWorld = FikaClientWorld.Create(gameWorld);
            Singleton<FikaClientGameWorld>.Create(gameWorld);
            return gameWorld;
        }

        public override void ShotDelegate(EftBulletClass shotResult)
        {
            if (!shotResult.IsFlyingOutOfTime)
            {
                DamageInfoStruct damageInfoStruct = new(EDamageType.Bullet, shotResult);
                ShotIdStruct shotIdStruct = new(shotResult.Ammo.Id, shotResult.FragmentIndex);
                ShotInfoClass shotInfoClass = (shotResult.HittedBallisticCollider != null) ? shotResult.HittedBallisticCollider.ApplyHit(damageInfoStruct, shotIdStruct) : null;
                shotResult.AddClientHitPosition(shotInfoClass);
                GClass2983 itemComponent = shotResult.Ammo.GetItemComponent<GClass2983>();
                if (itemComponent != null && shotResult.TimeSinceShot >= itemComponent.Template.FuzeArmTimeSec)
                {
                    if (Singleton<Effects>.Instantiated)
                    {
                        string explosionType = itemComponent.Template.ExplosionType;
                        if (!string.IsNullOrEmpty(explosionType) && shotResult.IsFirstHit)
                        {
                            Singleton<Effects>.Instance.EmitGrenade(explosionType, shotResult.HitPoint, shotResult.HitNormal, (float)(shotResult.IsForwardHit ? 1 : 0));
                        }
                        if (itemComponent.Template.ShowHitEffectOnExplode)
                        {
                            Singleton<Effects>.Instance.EffectsCommutator.PlayHitEffect(shotResult, shotInfoClass);
                        }
                    }
                    Grenade.Explosion(null, itemComponent, shotResult.HitPoint,
                        shotResult.Player.iPlayer.ProfileId, SharedBallisticsCalculator,
                        shotResult.Weapon, shotResult.HitNormal * 0.08f, false);
                    return;
                }
                if (Singleton<Effects>.Instantiated)
                {
                    Singleton<Effects>.Instance.EffectsCommutator.PlayHitEffect(shotResult, shotInfoClass);
                }
            }
        }

        public override GrenadeFactoryClass CreateGrenadeFactory()
        {
            return new ClientNetworkGrenadeFactoryClass();
        }

        public override void PlayerTick(float dt)
        {
            for (int i = AllAlivePlayersList.Count - 1; i >= 0; i--)
            {
                Player player = AllAlivePlayersList[i];
                try
                {
                    player.UpdateTick();
                }
                catch (Exception ex)
                {
                    FikaGlobals.LogError($"[{player.FullIdInfo}] tick operation exception: {ex}");
                }
            }
        }

        public override void AfterPlayerTick(float dt)
        {
            // Do nothing
        }

        public override void vmethod_1(float dt)
        {
            // Do nothing
        }

        public override void InitAirdrop(string lootTemplateId = null, bool takeNearbyPoint = false, Vector3 position = default)
        {
            // Do nothing
        }

        public override SyncObjectProcessorClass SyncObjectProcessorFactory()
        {
            ClientSynchronizableObjectLogicProcessor = new SynchronizableObjectLogicProcessorClass
            {
                TripwireManager = new(Singleton<GameWorld>.Instance)
            };
            return ClientSynchronizableObjectLogicProcessor;
        }

        public override void Dispose()
        {
            base.Dispose();
            Singleton<FikaClientGameWorld>.Release(this);
            NetManagerUtils.DestroyNetManager(false);
            List<SynchronizableObject> syncObjects = [.. SynchronizableObjectLogicProcessor.GetSynchronizableObjects()];
            for (int i = 0; i < syncObjects.Count; i++)
            {
                SynchronizableObject syncObject = syncObjects[i];
                syncObject.OnUpdateRequired -= SynchronizableObjectLogicProcessor.method_1;
                syncObject.Logic.ReturnToPool();
                syncObject.ReturnToPool();
            }
        }

        public override void PlantTripwire(Item item, string profileId, Vector3 fromPosition, Vector3 toPosition)
        {
            // Do nothing
        }

        public override void TriggerTripwire(TripwireSynchronizableObject tripwire)
        {
            // Do nothing
        }

        public override void DeActivateTripwire(TripwireSynchronizableObject tripwire)
        {
            // Do nothing
        }
    }
}
