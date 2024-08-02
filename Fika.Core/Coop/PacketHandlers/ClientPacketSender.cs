// © 2024 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.UI;
using EFT.Weather;
using Fika.Core.Coop.ClientClasses;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fika.Core.Coop.PacketHandlers
{
    public class ClientPacketSender : MonoBehaviour, IPacketSender
    {
        private CoopPlayer player;

        public bool Enabled { get; set; } = true;
        public FikaServer Server { get; set; }
        public FikaClient Client { get; set; }
        public NetDataWriter Writer { get; set; } = new();
        public Queue<WeaponPacket> FirearmPackets { get; set; } = new(50);
        public Queue<DamagePacket> DamagePackets { get; set; } = new(50);
        public Queue<ArmorDamagePacket> ArmorDamagePackets { get; set; } = new(50);
        public Queue<InventoryPacket> InventoryPackets { get; set; } = new(50);
        public Queue<CommonPlayerPacket> CommonPlayerPackets { get; set; } = new(50);
        public Queue<HealthSyncPacket> HealthSyncPackets { get; set; } = new(50);

        protected void Awake()
        {
            player = GetComponent<CoopPlayer>();
            Client = Singleton<FikaClient>.Instance;
            enabled = false;
        }

        public void Init()
        {
            enabled = true;
            if (player.AbstractQuestControllerClass is CoopClientSharedQuestController sharedQuestController)
            {
                sharedQuestController.LateInit();
            }
            StartCoroutine(SyncWorld());
            StartCoroutine(SyncWeather());
        }

        public void SendPacket<T>(ref T packet) where T : INetSerializable
        {
            Writer.Reset();
            Client.SendData(Writer, ref packet, DeliveryMethod.ReliableUnordered);
        }

        protected void FixedUpdate()
        {
            if (player == null || Writer == null)
            {
                return;
            }

            PlayerStatePacket playerStatePacket = new(player.NetId, player.Position, player.Rotation,
                player.HeadRotation, player.LastDirection, player.CurrentManagedState.Name,
                player.MovementContext.SmoothedTilt, player.MovementContext.Step, player.CurrentAnimatorStateIndex,
                player.MovementContext.SmoothedCharacterMovementSpeed, player.IsInPronePose, player.PoseLevel,
                player.MovementContext.IsSprintEnabled, player.Physical.SerializationStruct,
                player.MovementContext.BlindFire, player.observedOverlap, player.leftStanceDisabled,
                player.MovementContext.IsGrounded, player.hasGround, player.CurrentSurface,
                player.MovementContext.SurfaceNormal);

            Writer.Reset();
            Client.SendData(Writer, ref playerStatePacket, DeliveryMethod.Unreliable);

            if (player.MovementIdlingTime > 0f)
            {
                player.LastDirection = Vector2.zero;
            }
        }

        protected void Update()
        {
            int firearmPackets = FirearmPackets.Count;
            if (firearmPackets > 0)
            {
                for (int i = 0; i < firearmPackets; i++)
                {
                    WeaponPacket firearmPacket = FirearmPackets.Dequeue();
                    firearmPacket.NetId = player.NetId;

                    Writer.Reset();
                    Client.SendData(Writer, ref firearmPacket, DeliveryMethod.ReliableOrdered);
                }
            }
            int damagePackets = DamagePackets.Count;
            if (damagePackets > 0)
            {
                for (int i = 0; i < damagePackets; i++)
                {
                    DamagePacket damagePacket = DamagePackets.Dequeue();
                    damagePacket.NetId = player.NetId;

                    Writer.Reset();
                    Client.SendData(Writer, ref damagePacket, DeliveryMethod.ReliableOrdered);
                }
            }
            int armorDamagePackets = ArmorDamagePackets.Count;
            if (armorDamagePackets > 0)
            {
                for (int i = 0; i < armorDamagePackets; i++)
                {
                    ArmorDamagePacket armorDamagePacket = ArmorDamagePackets.Dequeue();
                    armorDamagePacket.NetId = player.NetId;

                    Writer.Reset();
                    Client.SendData(Writer, ref armorDamagePacket, DeliveryMethod.ReliableOrdered);
                }
            }
            int inventoryPackets = InventoryPackets.Count;
            if (inventoryPackets > 0)
            {
                for (int i = 0; i < inventoryPackets; i++)
                {
                    InventoryPacket inventoryPacket = InventoryPackets.Dequeue();
                    inventoryPacket.NetId = player.NetId;

                    Writer.Reset();
                    Client.SendData(Writer, ref inventoryPacket, DeliveryMethod.ReliableOrdered);
                }
            }
            int commonPlayerPackets = CommonPlayerPackets.Count;
            if (commonPlayerPackets > 0)
            {
                for (int i = 0; i < commonPlayerPackets; i++)
                {
                    CommonPlayerPacket commonPlayerPacket = CommonPlayerPackets.Dequeue();
                    commonPlayerPacket.NetId = player.NetId;

                    Writer.Reset();
                    Client.SendData(Writer, ref commonPlayerPacket, DeliveryMethod.ReliableOrdered);
                }
            }
            int healthSyncPackets = HealthSyncPackets.Count;
            if (healthSyncPackets > 0)
            {
                for (int i = 0; i < healthSyncPackets; i++)
                {
                    HealthSyncPacket healthSyncPacket = HealthSyncPackets.Dequeue();
                    healthSyncPacket.NetId = player.NetId;

                    Writer.Reset();
                    Client.SendData(Writer, ref healthSyncPacket, DeliveryMethod.ReliableOrdered);
                }
            }
            if (FikaPlugin.UsePingSystem.Value
                && player.IsYourPlayer
                && Input.GetKey(FikaPlugin.PingButton.Value.MainKey)
                && FikaPlugin.PingButton.Value.Modifiers.All(Input.GetKey))
            {
                if (MonoBehaviourSingleton<PreloaderUI>.Instance.Console.IsConsoleVisible)
                {
                    return;
                }
                player.Ping();
            }
        }

        private IEnumerator SyncWorld()
        {
            while (Client.NetClient.FirstPeer == null)
            {
                yield return null;
            }

            CoopGame coopGame = (CoopGame)Singleton<IFikaGame>.Instance;

            if (coopGame == null)
            {
                yield break;
            }

            while (coopGame.Status != GameStatus.Started)
            {
                yield return null;
            }

            yield return new WaitForSeconds(10f);

            Writer.Reset();
            GameTimerPacket gameTimerPacket = new(true);
            Client.SendData(Writer, ref gameTimerPacket, DeliveryMethod.ReliableOrdered);

            Writer.Reset();
            ExfiltrationPacket exfilPacket = new(true);
            Client.SendData(Writer, ref exfilPacket, DeliveryMethod.ReliableOrdered);
        }

        private IEnumerator SyncWeather()
        {
            if (WeatherController.Instance == null)
            {
                yield break;
            }

            CoopGame coopGame = (CoopGame)Singleton<IFikaGame>.Instance;

            if (coopGame == null)
            {
                yield break;
            }

            while (coopGame.Status != GameStatus.Started)
            {
                yield return null;
            }

            WeatherPacket packet = new()
            {
                IsRequest = true,
                HasData = false
            };

            Writer.Reset();
            Client.SendData(Writer, ref packet, DeliveryMethod.ReliableOrdered);
        }

        public void DestroyThis()
        {
            Writer = null;
            FirearmPackets.Clear();
            DamagePackets.Clear();
            InventoryPackets.Clear();
            CommonPlayerPackets.Clear();
            HealthSyncPackets.Clear();
            if (Server != null)
            {
                Server = null;
            }
            if (Client != null)
            {
                Client = null;
            }
            Destroy(this);
        }
    }
}
