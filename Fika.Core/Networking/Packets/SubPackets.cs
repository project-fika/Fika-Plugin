// © 2025 Lacyway All Rights Reserved

using EFT;
using EFT.InventoryLogic;
using System;
using UnityEngine;

namespace Fika.Core.Networking
{
    /// <summary>
    /// Class containing several static methods to serialize/deserialize sub-packages
    /// </summary>
    public class SubPackets
    {
        public struct PlayerInfoPacket
        {
            public Profile Profile;
            public byte[] HealthByteArray;
            public MongoID ControllerId;
            public ushort FirstOperationId;
            public EHandsControllerType ControllerType;
            public string ItemId;
            public bool IsStationary;
            public bool IsZombie;
        }

        public struct CorpseSyncPacket
        {
            public EBodyPartColliderType BodyPartColliderType;
            public Vector3 Direction;
            public Vector3 Point;
            public float Force;
            public Vector3 OverallVelocity;
            public InventoryDescriptorClass InventoryDescriptor;
            public EquipmentSlot ItemSlot;
            public Item ItemInHands;
        }

        public struct DeathInfoPacket
        {
            public string AccountId;
            public string ProfileId;
            public string Nickname;
            public string KillerAccountId;
            public string KillerProfileId;
            public string KillerName;
            public EPlayerSide Side;
            public int Level;
            public DateTime Time;
            public string Status;
            public string WeaponName;
            public string GroupId;
        }
    }
}
