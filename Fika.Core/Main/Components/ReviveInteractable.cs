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
    private Action _revivePlayerDelegate;
    private FikaPlayer _localPlayer;

    public static ReviveInteractable Create(ObservedPlayer observedPlayer)
    {
        var component = observedPlayer.gameObject.AddComponent<ReviveInteractable>();
        component._observedPlayer = observedPlayer;
        component._revivePlayerDelegate = component.RevivePlayer;
        return component;
    }

    public ActionsReturnClass GetActions(GamePlayerOwner owner)
    {
        _localPlayer = owner.Player as FikaPlayer;

        var actions = new ActionsReturnClass();
        actions.Actions.Add(new ActionsTypesClass
        {
            Action = _revivePlayerDelegate,
            Name = LocaleUtils.UI_REVIVE_PLAYER.Localized()
        });

        return actions;
    }

    public void RevivePlayer()
    {
        if (_localPlayer != null)
        {
            if (_observedPlayer != null)
            {
                _observedPlayer.CommonPacket.Type = ECommonSubPacketType.RevivePlayer;
                _observedPlayer.CommonPacket.SubPacket = RevivePlayerPacket.FromValue();
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
