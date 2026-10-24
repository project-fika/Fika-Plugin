using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.Passcodes;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.World;
using Il2CppInterop.Runtime.Injection;

namespace Fika.Core.Main.ClientClasses;

public class FikaClientPasscodeController : ClientPasscodeController
{
    public FikaClientPasscodeController(IntPtr pointer) : base(pointer)
    {
    }

    public FikaClientPasscodeController(PasscodeLocationSettings settings) : base(ClassInjector.DerivedConstructorPointer<FikaClientPasscodeController>())
    {
        ClassInjector.DerivedConstructorBody(this);
        ClassInjector.InvokeBaseConstructor<ClientPasscodeController>(this, settings);
        PasscodeInitPacket request = new(true);
        FikaGlobals.NetworkManager.SendData(ref request, DeliveryMethod.ReliableOrdered);
    }

    private bool _isInit;
    private bool _awaitingResponse;
    private string _pendingPasscodeId;

    /// <summary>
    /// Applies the hints the host generated, only the first sync of a raid counts
    /// </summary>
    public void ApplyHints(Dictionary<string, string[]> hints)
    {
        if (_isInit)
        {
            return;
        }

        _isInit = true;
        Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppSystem.Collections.Generic.List<string>> generated = new();
        foreach ((var passcodeId, var values) in hints)
        {
            Il2CppSystem.Collections.Generic.List<string> list = new();
            for (var i = 0; i < values.Length; i++)
            {
                list.Add(values[i]);
            }

            generated[passcodeId] = list;
        }

        SetupHints(generated);
    }

    public override bool HandleSubmitPasscode(Player player, PasscodeTerminal terminal, string passcode)
    {
        if (_awaitingResponse || !IsInteractable(terminal))
        {
            return false;
        }

        _awaitingResponse = true;
        _pendingPasscodeId = terminal.PasscodeId;
        PasscodeAttemptPacket packet = new()
        {
            NetId = player is FikaPlayer fikaPlayer ? fikaPlayer.NetId : 0,
            PasscodeId = terminal.PasscodeId,
            TerminalNetId = terminal.NetId,
            Input = passcode
        };

        FikaGlobals.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered);
        return true;
    }

    /// <summary>
    /// Applies the host verdict, whoever made the attempt
    /// </summary>
    public void HandleResult(PasscodeResultPacket packet, int myNetId)
    {
        var found = TryGetTerminal(packet.PasscodeId, packet.TerminalNetId, out var terminal);
        if (found && packet.Successful && IsInteractable(terminal))
        {
            Unlock(terminal);
        }

        if (packet.NetId != myNetId)
        {
            PlayObserverTerminalSounds(packet.PasscodeId, packet.TerminalNetId, packet.Successful);
            return;
        }

        if (!_awaitingResponse || _pendingPasscodeId != packet.PasscodeId)
        {
            return;
        }

        _awaitingResponse = false;
        _pendingPasscodeId = string.Empty;
        RaiseInteractionResult(packet.PasscodeId, packet.Successful);
    }

    private bool TryGetTerminal(string passcodeId, int terminalNetId, out PasscodeTerminal terminal)
    {
        terminal = null;
        if (!_passcodeTerminals.TryGetValue(passcodeId, out var terminals))
        {
            return false;
        }

        for (var i = 0; i < terminals.Count; i++)
        {
            if (terminals[i].NetId == terminalNetId)
            {
                terminal = terminals[i];
                return true;
            }
        }

        return false;
    }

    private void PlayObserverTerminalSounds(string passcodeId, int terminalNetId, bool successful)
    {
        if (!_passcodeTerminals.TryGetValue(passcodeId, out var terminals))
        {
            return;
        }

        for (var i = 0; i < terminals.Count; i++)
        {
            if (terminals[i].NetId == terminalNetId)
            {
                terminals[i].PlayResultSound(successful, false);
            }
        }
    }

    private static bool IsInteractable(PasscodeTerminal terminal)
    {
        return terminal != null && terminal.ObjectToUnlock != null && terminal.IsInteractable;
    }

    private static void Unlock(PasscodeTerminal terminal)
    {
        terminal.ObjectToUnlock.Interact(EInteractionType.Unlock);
        var additional = terminal.AdditionalObjectsToUnlock;
        if (additional == null)
        {
            return;
        }

        for (var i = 0; i < additional.Length; i++)
        {
            additional[i].Interact(EInteractionType.Unlock);
        }
    }
}
