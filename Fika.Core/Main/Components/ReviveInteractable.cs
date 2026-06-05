using System;
using Comfort.Common;
using EFT;
using EFT.AssetsManager;
using EFT.Interactive;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Packets.Player.Common;
using Fika.Core.Networking.Packets.Player.Common.SubPackets;
using static EFT.Player;

namespace Fika.Core.Main.Components;

internal sealed class ReviveInteractable : InteractableObject
{
    /// <summary>
    /// If the player is currently being revived
    /// </summary>
    public bool BeingRevived { get; internal set; }

    private float ReviveTime => FikaPlugin.Instance.Settings.ReviveConfig.ReviveTime;
    private bool AllowLooting => FikaPlugin.Instance.Settings.ReviveConfig.AllowLooting;

    private ObservedPlayer _observedPlayer;
    private Action _startReviveDelegate;
    private Action<bool> _revivePlayerDelegate;
    private Action _startSearchingDelegate;
    private Callback _finishLootingDelegate;
    private GamePlayerOwner _owner;
    private FikaPlayer _localPlayer;
    private RagdollClass _ragdoll;

    public static ReviveInteractable Create(ObservedPlayer observedPlayer)
    {
        var component = observedPlayer.gameObject.AddComponent<ReviveInteractable>();
        component._observedPlayer = observedPlayer;
        component._startReviveDelegate = component.StartRevive;
        component._revivePlayerDelegate = component.RevivePlayer;
        component._startSearchingDelegate = component.StartSearching;
        component._finishLootingDelegate = component.FinishLooting;
        component.Init();
        return component;
    }

    private void OnDestroy()
    {
        CancelInvoke(nameof(AgonySFX));
    }

    private void Init()
    {
        if (FikaBackendUtils.IsHeadless)
        {
            return;
        }

#if DEBUG
        FikaGlobals.LogInfo($"Adding ragdoll to {_observedPlayer.Profile.GetCorrectedNickname()}");
#endif

        _observedPlayer.MovementContext.ReleaseDoorIfInteractingWithOne();
        _observedPlayer.MovementContext.OnStateChanged -= _observedPlayer.method_17;
        _observedPlayer.MovementContext.PhysicalConditionChanged -= _observedPlayer.ProceduralWeaponAnimation.PhysicalConditionUpdated;
        _observedPlayer.EnabledAnimators = 0;
        _observedPlayer.BodyAnimatorCommon.enabled = false;
        _observedPlayer.ArmsAnimatorCommon.enabled = false;
        _observedPlayer._characterController.isEnabled = false;
        _observedPlayer.POM.Off();

        _observedPlayer.ProceduralWeaponAnimation.OnPreCollision -= _observedPlayer.IkStoreRaw;

        var num = LayerMask.NameToLayer("Deadbody");
        TransformHelperClass.SetLayersRecursively(_observedPlayer.gameObject, num);

        var poolObject = _observedPlayer.gameObject.GetComponent<PlayerPoolObject>();
        _ragdoll = new RagdollClass
        (
            poolObject.RigidbodySpawners,
            poolObject.JointSpawners,
            poolObject.PlayerRigidbodySleepHierarchy,
            _observedPlayer.Velocity,
            EFTHardSettings.Instance.CorpseMaxDepenetrationVelocity,
            CollisionDetectionMode.Discrete,
            _observedPlayer,
            CheckCorpseIsStill,
            _observedPlayer.PlayerBody,
            _observedPlayer.PlayerBody.IsVisible,
            FikaGlobals.EmptyActionDelegate,
            false,
            false
        );

        _observedPlayer.enabled = false;
        InvokeRepeating(nameof(AgonySFX), 10f, 10f);
    }

    private void AgonySFX()
    {
        if (_observedPlayer == null || _observedPlayer.Speaker == null)
        {
            return;
        }

        _observedPlayer.Speaker.Play(EPhraseTrigger.OnAgony, _observedPlayer.HealthStatus, true);
    }

    public void RemoveRagdoll()
    {
        if (FikaBackendUtils.IsHeadless)
        {
            return;
        }

#if DEBUG
        FikaGlobals.LogInfo($"Removing ragdoll from {_observedPlayer.Profile.GetCorrectedNickname()}");
#endif
        _observedPlayer.MovementContext.OnStateChanged += _observedPlayer.method_17;
        _observedPlayer.MovementContext.PhysicalConditionChanged += _observedPlayer.ProceduralWeaponAnimation.PhysicalConditionUpdated;
        _observedPlayer.EnabledAnimators = EAnimatorMask.Thirdperson | EAnimatorMask.Arms | EAnimatorMask.Procedural | EAnimatorMask.FBBIK | EAnimatorMask.IK;
        _observedPlayer.BodyAnimatorCommon.enabled = true;
        _observedPlayer.ArmsAnimatorCommon.enabled = true;
        _observedPlayer._characterController.isEnabled = true;
        _observedPlayer.POM.On();

        foreach (var joint in _observedPlayer.gameObject.GetComponentsInChildren<CharacterJoint>())
        {
            joint.enableProjection = false;
            joint.enablePreprocessing = true;
            joint.massScale = 1f;
        }

        foreach (var rb in _observedPlayer.gameObject.GetComponentsInChildren<Rigidbody>())
        {
            EFTPhysicsClass.GClass745.UnsupportRigidbody(rb);
        }

        _observedPlayer.ProceduralWeaponAnimation.OnPreCollision += _observedPlayer.IkStoreRaw;
        _observedPlayer.enabled = true;
    }

    private static bool CheckCorpseIsStill(bool sleeping, float timePass)
    {
        return sleeping || timePass >= 15f;
    }

    public ActionsReturnClass GetActions(GamePlayerOwner owner)
    {
        if (BeingRevived)
        {
            return null;
        }

        _owner = owner;
        _localPlayer = owner.Player as FikaPlayer;

        var actions = new ActionsReturnClass();
        actions.Actions.Add(new ActionsTypesClass
        {
            Action = _startReviveDelegate,
            Name = string.Format(LocaleUtils.UI_REVIVE_PLAYER.Localized(), _observedPlayer.Profile.GetCorrectedNickname())
        });
        if (AllowLooting)
        {
            actions.Actions.Add(new ActionsTypesClass
            {
                Action = _startSearchingDelegate,
                Name = "Search"
            });
        }

        return actions;
    }

    private void StartSearching()
    {
        _localPlayer.SaveInteractionRayInfo();
        _localPlayer.Interact(_observedPlayer.InventoryController, _finishLootingDelegate);
    }

    private void FinishLooting(IResult result)
    {
        if (result.Failed)
        {
            return;
        }

        _owner.ShowInventoryScreenLoot((CompoundItem)_observedPlayer.InventoryController.RootItem, FikaGlobals.EmptyActionDelegate);
        _localPlayer.StatisticsManager.OnInteractWithLootContainer(_observedPlayer.InventoryController.RootItem);
    }

    public void StartRevive()
    {
        if (_localPlayer.CurrentState is IdleStateClass)
        {
            var reviveTime = ReviveTime;
            _owner.ShowObjectivesPanel(LocaleUtils.UI_REVIVING_PLAYER.Localized(), reviveTime);
            _localPlayer.CurrentManagedState.Plant(true, false, reviveTime, _revivePlayerDelegate);
            var nickname = _localPlayer.Profile.GetCorrectedNickname();
            _observedPlayer.ToggleRevive(true, nickname);

            _observedPlayer.CommonPacket.Type = ECommonSubPacketType.RevivingPlayer;
            _observedPlayer.CommonPacket.SubPacket = RevivingPlayerPacket.FromValue(true, nickname);
            _observedPlayer.PacketSender.NetworkManager.SendNetReusable(ref _observedPlayer.CommonPacket, DeliveryMethod.ReliableOrdered, true);
        }
    }

    public void RevivePlayer(bool success)
    {
        _owner.CloseObjectivesPanel();
        _observedPlayer.ToggleRevive(false, string.Empty);

        if (!_localPlayer.HealthController.IsAlive)
        {
            return;
        }

        if (!success)
        {
            _observedPlayer.CommonPacket.Type = ECommonSubPacketType.RevivingPlayer;
            _observedPlayer.CommonPacket.SubPacket = RevivingPlayerPacket.FromValue(false, string.Empty);
            _observedPlayer.PacketSender.NetworkManager.SendNetReusable(ref _observedPlayer.CommonPacket, DeliveryMethod.ReliableOrdered, true);
            return;
        }

        if (_localPlayer != null)
        {
            if (_observedPlayer != null)
            {
                _observedPlayer.CommonPacket.Type = ECommonSubPacketType.RevivedPlayer;
                _observedPlayer.CommonPacket.SubPacket = RevivedPlayerPacket.FromValue();
                _observedPlayer.PacketSender.NetworkManager.SendNetReusable(ref _observedPlayer.CommonPacket, DeliveryMethod.ReliableOrdered, true);

#if DEBUG
                FikaGlobals.LogInfo($"Reviving {_observedPlayer.NetId}");
#endif
                RemoveRagdoll();
                _observedPlayer.ClearReviveInteractable();
                _localPlayer.InteractableObject = null;
                _localPlayer.ForceInteractionsChanged();
                return;
            }

            FikaGlobals.LogError("ObservedPlayer was null!");
            return;
        }

        FikaGlobals.LogError("_localPlayer was null!");
    }
}
