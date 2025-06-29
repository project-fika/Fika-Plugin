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
            public MongoID ControllerId;
            public string ItemId;

            public byte[] HealthByteArray;

            public ushort FirstOperationId;
            public EHandsControllerType ControllerType;

            public bool IsStationary;
            public bool IsZombie;
        }

        public struct CorpseSyncPacket
        {
            public InventoryDescriptorClass InventoryDescriptor;
            public Item ItemInHands;

            public EBodyPartColliderType BodyPartColliderType;

            public Vector3 Direction;
            public Vector3 Point;
            public Vector3 OverallVelocity;

            public float Force;

            public EquipmentSlot ItemSlot;
        }

        public struct DeathInfoPacket
        {
            public string AccountId;
            public string ProfileId;
            public string Nickname;
            public string KillerAccountId;
            public string KillerProfileId;
            public string KillerName;
            public string Status;
            public string WeaponName;
            public string GroupId;

            public EPlayerSide Side;
            public int Level;
            public DateTime Time;
        }
    }
}
