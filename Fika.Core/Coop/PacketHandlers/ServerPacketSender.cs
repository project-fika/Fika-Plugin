// © 2025 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.UI;
using Fika.Core.Bundles;
using Fika.Core.Coop.ClientClasses;
using Fika.Core.Coop.Factories;
using Fika.Core.Coop.FreeCamera;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Linq;
using UnityEngine;

namespace Fika.Core.Coop.PacketHandlers
{
    public class ServerPacketSender : MonoBehaviour, IPacketSender
    {
        public bool Enabled { get; set; }
        public bool SendState { get; set; }
        public FikaServer Server { get; set; }
        public FikaClient Client { get; set; }
        public bool IsMoving
        {
            get
            {
                return _player.CurrentManagedState.Name is not (EPlayerState.Idle
                    or EPlayerState.IdleWeaponMounting
                    or EPlayerState.ProneIdle);
            }
        }

        private CoopPlayer _player;
        private PlayerStatePacket _state;

        private bool CanPing
        {
            get
            {
                return FikaPlugin.UsePingSystem.Value && _player.IsYourPlayer && Input.GetKey(FikaPlugin.PingButton.Value.MainKey)
                    && FikaPlugin.PingButton.Value.Modifiers.All(Input.GetKey) && !MonoBehaviourSingleton<PreloaderUI>.Instance.Console.IsConsoleVisible
                    && lastPingTime < DateTime.Now.AddSeconds(-3) && Singleton<IFikaGame>.Instantiated &&
                    Singleton<IFikaGame>.Instance.GameController.GameInstance.Status is GameStatus.Started && !_player.IsInventoryOpened;
            }
        }

        private DateTime lastPingTime;
        private float updateRate;
        private float updateCount;
        private float updatesPerTick;

        public static ServerPacketSender Create(CoopPlayer player)
        {
            ServerPacketSender sender = player.gameObject.AddComponent<ServerPacketSender>();
            sender._player = player;
            sender.Server = Singleton<FikaServer>.Instance;
            sender.enabled = false;
            sender.lastPingTime = DateTime.Now;
            sender.updateRate = sender.Server.SendRate;
            sender.updateCount = 0;
            sender.updatesPerTick = 1f / sender.updateRate;
            sender._state = new(player.NetId);
            return sender;
        }

        public void Init()
        {
            enabled = true;
            Enabled = true;
            SendState = true;
            if (_player.AbstractQuestControllerClass is CoopClientSharedQuestController sharedQuestController)
            {
                sharedQuestController.LateInit();
            }
        }

        public void SendPacket<T>(ref T packet, bool forced = false) where T : INetSerializable
        {
            if (!Enabled && !forced)
            {
                return;
            }

            Server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
        }

        protected void Update()
        {
            if (!SendState)
            {
                return;
            }

            updateCount += Time.unscaledDeltaTime;
            if (updateCount >= updatesPerTick)
            {
                SendPlayerState();
                updateCount -= updatesPerTick;
            }
        }

        private void SendPlayerState()
        {
            _state.UpdateData(_player, IsMoving);
            Server.SendDataToAll(ref _state, DeliveryMethod.Unreliable);
        }

        protected void LateUpdate()
        {
            if (CanPing)
            {
                SendPing();
            }
        }

        private void SendPing()
        {
            Transform originTransform;
            Ray sourceRaycast;
            FreeCameraController freeCamController = Singleton<FreeCameraController>.Instance;
            if (freeCamController != null && freeCamController.IsScriptActive)
            {
                originTransform = freeCamController.CameraMain.gameObject.transform;
                sourceRaycast = new(originTransform.position + originTransform.forward / 2f, originTransform.forward);
            }
            else if (_player.HealthController.IsAlive)
            {
                if (_player.HandsController is CoopClientFirearmController controller && controller.IsAiming)
                {
                    sourceRaycast = new(controller.FireportPosition, controller.WeaponDirection);
                }
                else
                {
                    originTransform = _player.CameraPosition;
                    sourceRaycast = new(originTransform.position + originTransform.forward / 2f, _player.LookDirection);
                }
            }
            else
            {
                return;
            }
            int layer = LayerMask.GetMask(["HighPolyCollider", "Interactive", "Deadbody", "Player", "Loot", "Terrain"]);
            if (Physics.Raycast(sourceRaycast, out RaycastHit hit, FikaGlobals.PingRange, layer))
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

                GameObject basePingPrefab = InternalBundleLoader.Instance.GetFikaAsset<GameObject>(InternalBundleLoader.EFikaAsset.Ping);
                GameObject basePing = GameObject.Instantiate(basePingPrefab);
                Vector3 hitPoint = hit.point;
                PingFactory.AbstractPing abstractPing = PingFactory.FromPingType(pingType, basePing);
                Color pingColor = FikaPlugin.PingColor.Value;
                pingColor = new(pingColor.r, pingColor.g, pingColor.b, 1);
                // ref so that we can mutate it if we want to, ex: if I ping a switch I want it at the switch.gameObject.position + Vector3.up
                abstractPing.Initialize(ref hitPoint, userData, pingColor);

                PingPacket packet = new()
                {
                    PingLocation = hitPoint,
                    PingType = pingType,
                    PingColor = pingColor,
                    Nickname = _player.Profile.Info.MainProfileNickname,
                    LocaleId = string.IsNullOrEmpty(localeId) ? string.Empty : localeId
                };

                SendPacket(ref packet, true);

                if (FikaPlugin.PlayPingAnimation.Value && _player.HealthController.IsAlive)
                {
                    _player.vmethod_7(EInteraction.ThereGesture);
                }
            }
        }

        public void DestroyThis()
        {
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
