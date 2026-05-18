using System;
using EFT;
using EFT.Interactive;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Packets.Player.Common;
using Fika.Core.Networking.Packets.Player.Common.SubPackets;

namespace Fika.Core.Main.Components;

internal sealed class ReviveInteractable : InteractableObject
{
    private ObservedPlayer _observedPlayer;
    private Action _startReviveDelegate;
    private Action<bool> _revivePlayerDelegate;
    private GamePlayerOwner _owner;
    private FikaPlayer _localPlayer;

    public static ReviveInteractable Create(ObservedPlayer observedPlayer)
    {
        var component = observedPlayer.gameObject.AddComponent<ReviveInteractable>();
        component._observedPlayer = observedPlayer;
        component._startReviveDelegate = component.StartRevive;
        component._revivePlayerDelegate = component.RevivePlayer;
        return component;
    }

    public ActionsReturnClass GetActions(GamePlayerOwner owner)
    {
        _owner = owner;
        _localPlayer = owner.Player as FikaPlayer;

        var actions = new ActionsReturnClass();
        actions.Actions.Add(new ActionsTypesClass
        {
            Action = _startReviveDelegate,
            Name = string.Format(LocaleUtils.UI_REVIVE_PLAYER.Localized(), _observedPlayer.Profile.GetCorrectedNickname())
        });

        return actions;
    }

    public void StartRevive()
    {
        if (_localPlayer.CurrentState is IdleStateClass)
        {
            const float reviveTime = 5f;
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
