// © 2025 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.UI;
using Fika.Core.Bundles;
using Fika.Core.Main.ClientClasses;
using Fika.Core.Main.ClientClasses.HandsControllers;
using Fika.Core.Main.Factories;
using Fika.Core.Main.FreeCamera;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.Communication;
using Fika.Core.Networking.Packets.Player;
using Fika.Core.UI.Custom;
using LiteNetLib;
using System;
using System.Linq;
using UnityEngine;

namespace Fika.Core.Main.PacketHandlers
{
    public class ClientPacketSender : MonoBehaviour, IPacketSender
    {
        public bool SendState { get; set; }
        public IFikaNetworkManager NetworkManager { get; set; }

        private FikaPlayer _player;
        private PlayerStatePacket _state;
        private int _animHash;
        private bool IsMoving
        {
            get
            {
                return _player.MovementContext.PlayerAnimator.Animator.GetBool(_animHash);
            }
        }


        private bool CanPing
        {
            get
            {
                return FikaPlugin.UsePingSystem.Value && _player.IsYourPlayer && Input.GetKey(FikaPlugin.PingButton.Value.MainKey)
                    && FikaPlugin.PingButton.Value.Modifiers.All(Input.GetKey) && !MonoBehaviourSingleton<PreloaderUI>.Instance.Console.IsConsoleVisible
                    && _lastPingTime < DateTime.Now.AddSeconds(-3) && !FikaChatUIScript.IsActive && Singleton<IFikaGame>.Instance is CoopGame coopGame && coopGame.Status is GameStatus.Started
                    && !_player.IsInventoryOpened;
            }
        }

        private DateTime _lastPingTime;
        private float _updateRate;
        private float _updateCount;
        private float _updatesPerTick;

        public static ClientPacketSender Create(FikaPlayer player)
        {
            ClientPacketSender sender = player.gameObject.AddComponent<ClientPacketSender>();
            sender._player = player;
            sender.NetworkManager = Singleton<FikaClient>.Instance;
            sender.enabled = false;
            sender._lastPingTime = DateTime.Now;
            sender._updateRate = sender.NetworkManager.SendRate;
            sender._updateCount = 0;
            sender._updatesPerTick = 1f / sender._updateRate;
            sender._state = new(player.NetId);
            sender._animHash = PlayerAnimator.INERT_PARAM_HASH;
            return sender;
        }

        public void Init()
        {
            enabled = true;
            SendState = true;
            if (_player.AbstractQuestControllerClass is ClientSharedQuestController sharedQuestController)
            {
                sharedQuestController.LateInit();
            }
        }

        protected void Update()
        {
            if (!SendState)
            {
                return;
            }

            _updateCount += Time.unscaledDeltaTime;
            if (_updateCount >= _updatesPerTick)
            {
                SendPlayerState();
                _updateCount -= _updatesPerTick;
            }
        }

        private void SendPlayerState()
        {
            _state.UpdateData(_player, IsMoving);
            NetworkManager.SendData(ref _state, DeliveryMethod.Unreliable, true);
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
                if (_player.HandsController is FikaClientFirearmController controller && controller.IsAiming)
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
                _lastPingTime = DateTime.Now;
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

                GameObject basePingPrefab = InternalBundleLoader.Instance.GetFikaAsset(InternalBundleLoader.EFikaAsset.Ping);
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

                NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);

                if (FikaPlugin.PlayPingAnimation.Value && _player.HealthController.IsAlive)
                {
                    _player.vmethod_7(EInteraction.ThereGesture);
                }
            }
        }

        public void DestroyThis()
        {
            NetworkManager = null;
            Destroy(this);
        }
    }
}
