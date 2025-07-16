using Comfort.Common;
using EFT;
using EFT.Airdrop;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.SynchronizableObjects;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using LiteNetLib.Utils;
using System;
using UnityEngine;
using static Fika.Core.Networking.Packets.SubPacket;

namespace Fika.Core.Networking.Packets.World
{
    public class SpawnSyncObjectSubPackets
    {
        public class SpawnTripwire : ISubPacket
        {
            public int ObjectId;
            public bool IsStatic;
            public string GrenadeTemplate;
            public string GrenadeId;
            public string ProfileId;
            public Vector3 ToPosition;
            public Vector3 Position;
            public Quaternion Rotation;

            public SpawnTripwire()
            {

            }

            public SpawnTripwire(NetDataReader reader)
            {
                ObjectId = reader.GetInt();
                IsStatic = reader.GetBool();
                GrenadeTemplate = reader.GetString();
                GrenadeId = reader.GetString();
                ProfileId = reader.GetString();
                Position = reader.GetVector3();
                ToPosition = reader.GetVector3();
                Rotation = reader.GetQuaternion();
            }

            public void Execute(FikaPlayer player)
            {
                SyncObjectProcessorClass processor = Singleton<GameWorld>.Instance.SynchronizableObjectLogicProcessor;
                if (processor == null)
                {
                    return;
                }

                if (Singleton<ItemFactoryClass>.Instance.CreateItem(GrenadeId, GrenadeTemplate, null) is not ThrowWeapItemClass grenadeClass)
                {
                    FikaPlugin.Instance.FikaLogger.LogError("OnSpawnSyncObjectPacketReceived: Item with id " + GrenadeId + " is not a grenade!");
                    return;
                }

                TripwireSynchronizableObject syncObject = (TripwireSynchronizableObject)processor.TakeFromPool(SynchronizableObjectType.Tripwire);
                syncObject.ObjectId = ObjectId;
                syncObject.IsStatic = IsStatic;
                syncObject.transform.SetPositionAndRotation(Position, Rotation);
                processor.InitSyncObject(syncObject, syncObject.transform.position, syncObject.transform.rotation.eulerAngles, syncObject.ObjectId);

                syncObject.SetupGrenade(grenadeClass, ProfileId, Position, ToPosition);
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(ObjectId);
                writer.Put(IsStatic);
                writer.Put(GrenadeTemplate);
                writer.Put(GrenadeId);
                writer.Put(ProfileId);
                writer.PutVector3(Position);
                writer.PutVector3(ToPosition);
                writer.PutQuaternion(Rotation);
            }
        }

        [Obsolete("Not used in client", true)]
        public class SpawnAirplane : ISubPacket
        {
            public int ObjectId;
            public Vector3 Position;
            public Quaternion Rotation;

            public SpawnAirplane()
            {

            }

            public SpawnAirplane(NetDataReader reader)
            {
                ObjectId = reader.GetInt();
                Position = reader.GetVector3();
                Rotation = reader.GetQuaternion();
            }

            public void Execute(FikaPlayer player)
            {
                SyncObjectProcessorClass processor = Singleton<GameWorld>.Instance.SynchronizableObjectLogicProcessor;
                if (processor == null)
                {
                    return;
                }

                AirplaneSynchronizableObject syncObject = (AirplaneSynchronizableObject)processor.TakeFromPool(SynchronizableObjectType.AirPlane);
                syncObject.ObjectId = ObjectId;
                syncObject.transform.SetPositionAndRotation(Position, Rotation);
                processor.InitSyncObject(syncObject, Position, Rotation.eulerAngles, ObjectId);
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(ObjectId);
                writer.PutVector3(Position);
                writer.PutQuaternion(Rotation);
            }
        }

        public class SpawnAirdrop : ISubPacket
        {
            public int ObjectId;
            public bool IsStatic;
            public Vector3 Position;
            public Quaternion Rotation;
            public EAirdropType AirdropType;
            public Item AirdropItem;
            public MongoID ContainerId;
            public int NetId;

            public SpawnAirdrop()
            {

            }

            public SpawnAirdrop(NetDataReader reader)
            {
                ObjectId = reader.GetInt();
                IsStatic = reader.GetBool();
                Position = reader.GetVector3();
                Rotation = reader.GetQuaternion();
                AirdropType = (EAirdropType)reader.GetByte();
                AirdropItem = reader.GetAirdropItem();
                ContainerId = reader.GetMongoID();
                NetId = reader.GetInt();
            }

            public void Execute(FikaPlayer player)
            {
                SyncObjectProcessorClass processor = Singleton<GameWorld>.Instance.SynchronizableObjectLogicProcessor;
                if (processor == null)
                {
                    return;
                }

#if DEBUG
                FikaGlobals.LogWarning($"Spawning airdrop at {Position} with id {ObjectId}");
#endif

                AirdropSynchronizableObject syncObject = (AirdropSynchronizableObject)processor.TakeFromPool(SynchronizableObjectType.AirDrop);
                syncObject.ObjectId = ObjectId;
                syncObject.transform.position = Position;
                syncObject.transform.rotation = Rotation;
                if (syncObject.Logic is AirdropLogicClass airdropLogicClass)
                {
                    airdropLogicClass.Vector3_0 = Position;
                }
                else
                {
                    FikaGlobals.LogWarning("AirdropSynchronizableObject logic was not of type AirdropLogicClass!");
                }
                syncObject.AirdropType = AirdropType;
                LootableContainer container = syncObject.GetComponentInChildren<LootableContainer>().gameObject.GetComponentInChildren<LootableContainer>();
                container.enabled = true;
                container.Id = ContainerId;
                if (NetId > 0)
                {
                    container.NetId = NetId;
                }
                Singleton<GameWorld>.Instance.RegisterWorldInteractionObject(container);
                LootItem.CreateLootContainer(container, AirdropItem, AirdropItem.ShortName.Localized(null),
                        Singleton<GameWorld>.Instance, null);
                if (!syncObject.IsStatic)
                {
                    processor.InitSyncObject(syncObject, syncObject.transform.position, syncObject.transform.rotation.eulerAngles, syncObject.ObjectId);
                    return;
                }
                processor.InitStaticObject(syncObject);
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(ObjectId);
                writer.Put(IsStatic);
                writer.PutVector3(Position);
                writer.PutQuaternion(Rotation);
                writer.Put((byte)AirdropType);
                writer.PutItem(AirdropItem);
                writer.PutMongoID(ContainerId);
                writer.Put(NetId);
            }
        }
    }
}
