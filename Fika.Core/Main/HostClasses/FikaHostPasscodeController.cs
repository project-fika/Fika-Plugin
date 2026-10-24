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

namespace Fika.Core.Main.HostClasses;

public class FikaHostPasscodeController : ClientPasscodeController
{
    public FikaHostPasscodeController(IntPtr pointer) : base(pointer)
    {
    }

    public FikaHostPasscodeController(PasscodeLocationSettings settings) : base(ClassInjector.DerivedConstructorPointer<FikaHostPasscodeController>())
    {
        ClassInjector.DerivedConstructorBody(this);
        _passcodes = PasscodeGenerator.GeneratePasscodes(settings.Entries);
        ClassInjector.InvokeBaseConstructor<ClientPasscodeController>(this, settings);
        _hints = GenerateHints(_passcodes);
        SetupHints(_hints);
    }

    private readonly Il2CppSystem.Collections.Generic.Dictionary<string, string> _passcodes;
    private readonly Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppSystem.Collections.Generic.List<string>> _hints;

    public Dictionary<string, string[]> GetHints()
    {
        Dictionary<string, string[]> hints = new(_hints.Count);
        foreach (var entry in _hints)
        {
            var generated = entry.Value;
            var values = new string[generated.Count];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = generated[i];
            }

            hints[entry.Key] = values;
        }

        return hints;
    }

    public override bool HandleSubmitPasscode(Player player, PasscodeTerminal terminal, string passcode)
    {
        if (!IsInteractable(terminal))
        {
            return false;
        }

        var successful = Matches(terminal.PasscodeId, passcode);

        // The input panel plays the submitting player's own sound when the result is raised
        RaiseInteractionResult(terminal.PasscodeId, successful);
        if (successful)
        {
            Unlock(terminal);
        }

        Broadcast(player is FikaPlayer fikaPlayer ? fikaPlayer.NetId : 0, terminal.PasscodeId, terminal.NetId, successful);
        return true;
    }

    /// <summary>
    /// Decides an attempt a client sent and tells every peer the outcome
    /// </summary>
    public void HandleAttempt(PasscodeAttemptPacket packet)
    {
        var successful = Matches(packet.PasscodeId, packet.Input);
        if (TryGetTerminal(packet.PasscodeId, packet.TerminalNetId, out var terminal))
        {
            if (successful && IsInteractable(terminal))
            {
                Unlock(terminal);
            }

            PlayObserverTerminalSounds(packet.PasscodeId, packet.TerminalNetId, successful);
        }
        else
        {
            FikaGlobals.LogError($"Could not find passcode terminal {packet.TerminalNetId} for {packet.PasscodeId}");
        }

        Broadcast(packet.NetId, packet.PasscodeId, packet.TerminalNetId, successful);
    }

    private bool Matches(string passcodeId, string input)
    {
        return _passcodes.TryGetValue(passcodeId, out var passcode) && passcode == input;
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

    private static void Broadcast(int netId, string passcodeId, int terminalNetId, bool successful)
    {
        PasscodeResultPacket packet = new()
        {
            NetId = netId,
            PasscodeId = passcodeId,
            TerminalNetId = terminalNetId,
            Successful = successful
        };

        FikaGlobals.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered);
    }
}
