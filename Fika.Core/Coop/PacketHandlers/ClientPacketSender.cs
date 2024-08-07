// © 2024 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.UI;
using EFT.Weather;
using Fika.Core.Coop.ClientClasses;
using Fika.Core.Coop.Factories;
using Fika.Core.Coop.FreeCamera;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using LiteNetLib;
using LiteNetLib.Utils;
using System;
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
        private DateTime lastPingTime;

        protected void Awake()
        {
            player = GetComponent<CoopPlayer>();
            Client = Singleton<FikaClient>.Instance;
            enabled = false;
            lastPingTime = DateTime.Now;
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
            if (player == null || Writer == null || Client == null || !Enabled)
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

            if (player.MovementIdlingTime > 0.05f)
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
                SendPing();
            }
        }

        private void SendPing()
        {
            CoopGame coopGame = (CoopGame)Singleton<IFikaGame>.Instance;
            if (coopGame.Status != GameStatus.Started)
            {
                return;
            }

            if (lastPingTime < DateTime.Now.AddSeconds(-3))
            {
                Transform origin;
                FreeCameraController freeCamController = Singleton<FreeCameraController>.Instance;
                if (freeCamController != null && freeCamController.IsScriptActive)
                {
                    origin = freeCamController.CameraMain.gameObject.transform;
                }
                else if (player.HealthController.IsAlive)
                {
                    origin = player.CameraPosition;
                }
                else
                {
                    return;
                }

                Ray sourceRaycast = new(origin.position + origin.forward / 2f,
                    origin.forward);
                int layer = LayerMask.GetMask(["HighPolyCollider", "Interactive", "Deadbody", "Player", "Loot", "Terrain"]);
                if (Physics.Raycast(sourceRaycast, out RaycastHit hit, 500f, layer))
                {
                    lastPingTime = DateTime.Now;
                    //GameObject gameObject = new("Ping", typeof(FikaPing));
                    //gameObject.transform.localPosition = hit.point;
                    Singleton<GUISounds>.Instance.PlayUISound(PingFactory.GetPingSound());
                    GameObject hitGameObject = hit.collider.gameObject;
                    int hitLayer = hitGameObject.layer;

                    PingFactory.EPingType pingType = PingFactory.EPingType.Point;
                    object userData = null;
                    string localeId = null;

#if DEBUG
                    ConsoleScreen.Log(statement: $"{hit.collider.GetFullPath()}: {LayerMask.LayerToName(hitLayer)}/{hitGameObject.name}");
#endif

                    if (LayerMask.LayerToName(hitLayer) == "Player")
                    {
                        if (hitGameObject.TryGetComponent(out Player player))
                        {
                            pingType = PingFactory.EPingType.Player;
                            userData = player;
                        }
                    }
                    else if (LayerMask.LayerToName(hitLayer) == "Deadbody")
                    {
                        pingType = PingFactory.EPingType.DeadBody;
                        userData = hitGameObject;
                    }
                    else if (hitGameObject.TryGetComponent(out LootableContainer container))
                    {
                        pingType = PingFactory.EPingType.LootContainer;
                        userData = container;
                        localeId = container.ItemOwner.Name;
                    }
                    else if (hitGameObject.TryGetComponent(out LootItem lootItem))
                    {
                        pingType = PingFactory.EPingType.LootItem;
                        userData = lootItem;
                        localeId = lootItem.Item.ShortName;
                    }
                    else if (hitGameObject.TryGetComponent(out Door door))
                    {
                        pingType = PingFactory.EPingType.Door;
                        userData = door;
                    }
                    else if (hitGameObject.TryGetComponent(out InteractableObject interactable))
                    {
                        pingType = PingFactory.EPingType.Interactable;
                        userData = interactable;
                    }

                    GameObject basePingPrefab = PingFactory.AbstractPing.pingBundle.LoadAsset<GameObject>("BasePingPrefab");
                    GameObject basePing = GameObject.Instantiate(basePingPrefab);
                    Vector3 hitPoint = hit.point;
                    PingFactory.AbstractPing abstractPing = PingFactory.FromPingType(pingType, basePing);
                    Color pingColor = FikaPlugin.PingColor.Value;
                    pingColor = new(pingColor.r, pingColor.g, pingColor.b, 1);
                    // ref so that we can mutate it if we want to, ex: if I ping a switch I want it at the switch.gameObject.position + Vector3.up
                    abstractPing.Initialize(ref hitPoint, userData, pingColor);

                    GenericPacket genericPacket = new()
                    {
                        NetId = player.NetId,
                        PacketType = EPackageType.Ping,
                        PingLocation = hitPoint,
                        PingType = pingType,
                        PingColor = pingColor,
                        Nickname = player.Profile.Nickname,
                        LocaleId = string.IsNullOrEmpty(localeId) ? string.Empty : localeId
                    };

                    SendPacket(ref genericPacket);

                    if (FikaPlugin.PlayPingAnimation.Value)
                    {
                        player.vmethod_3(EGesture.ThatDirection);
                    }
                }
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
