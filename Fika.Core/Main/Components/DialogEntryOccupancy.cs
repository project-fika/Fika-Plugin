using System;
using System.Collections.Generic;
using Comfort.Common;
using CommonAssets.Scripts.Game;
using EFT;
using EFT.Interactive;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.Generic;
using Fika.Core.Networking.LiteNetLib;
using Fika.Core.Networking.Packets.Generic.SubPackets;

namespace Fika.Core.Main.Components;

public static class DialogEntryOccupancy
{
    private const string BUSY_LOCALE_KEY = "RaidDialogEntryPoint/Busy";

    private static readonly Dictionary<string, int> _hostOccupants = [];
    private static Action<string> _occupancyChangedHandler;
    private static IntPtr _controllerPointer;
    private static RaidDialogEntryPoint _pendingEntryPoint;
    private static string _pendingPointId;
    private static string _heldPointId;
    private static bool _applying;
    private static bool _subscribedToPlayers;

    public static void OnEntryPointInit()
    {
        var controller = GetController();
        if (controller == null || controller.Pointer == _controllerPointer)
        {
            return;
        }

        _controllerPointer = controller.Pointer;
        _hostOccupants.Clear();
        _pendingEntryPoint = null;
        _pendingPointId = null;
        _heldPointId = null;
        _applying = false;

        _occupancyChangedHandler ??= new Action<string>(OnPointOccupancyChanged);
        controller.PointOccupancyChanged += _occupancyChangedHandler;

        if (!_subscribedToPlayers)
        {
            FikaPlayer.OnPlayerDeath += OnPlayerGone;
            FikaPlayer.OnPlayerDestroyed += OnPlayerGone;
            _subscribedToPlayers = true;
        }

        if (!FikaBackendUtils.IsServer)
        {
            SendToHost(DialogEntryPacket.RequestFullState());
        }
    }

    public static bool TryStartDialog(RaidDialogEntryPoint entryPoint)
    {
        var controller = GetController();
        if (controller == null)
        {
            return true;
        }

        var pointId = entryPoint.PointId;
        var raidId = MyRaidId;
        if (string.IsNullOrEmpty(pointId) || raidId == 0 || _heldPointId == pointId)
        {
            return true;
        }

        if (FikaBackendUtils.IsServer)
        {
            if (HostResolveClaim(pointId, raidId) == raidId)
            {
                return true;
            }

            DisplayBusy();
            return false;
        }

        if (controller.IsOccupiedByOther(pointId, raidId))
        {
            DisplayBusy();
            return false;
        }

        _pendingPointId = pointId;
        _pendingEntryPoint = entryPoint;
        SendToHost(DialogEntryPacket.Claim(pointId, raidId));
        return false;
    }

    public static void HandleClaimRequest(string pointId, int raidId)
    {
        if (!FikaBackendUtils.IsServer)
        {
            return;
        }

        HostResolveClaim(pointId, raidId);
    }

    public static void HandleReleaseRequest(string pointId, int raidId)
    {
        if (!FikaBackendUtils.IsServer)
        {
            return;
        }

        HostRelease(pointId, raidId);
    }

    public static void HandleFullStateRequest()
    {
        if (!FikaBackendUtils.IsServer)
        {
            return;
        }

        Send(DialogEntryPacket.FullState(_hostOccupants), true);
    }

    public static void ApplyOccupancy(string pointId, int occupantId)
    {
        if (FikaBackendUtils.IsServer)
        {
            return;
        }

        ApplyLocal(pointId, occupantId);
        if (_pendingPointId != pointId)
        {
            return;
        }

        var entryPoint = _pendingEntryPoint;
        _pendingPointId = null;
        _pendingEntryPoint = null;

        if (occupantId == MyRaidId && entryPoint != null)
        {
            entryPoint.StartDialog();
            return;
        }

        DisplayBusy();
    }

    public static void ApplyFullState(List<KeyValuePair<string, int>> occupants)
    {
        if (FikaBackendUtils.IsServer)
        {
            return;
        }

        var controller = GetController();
        if (controller == null)
        {
            return;
        }

        List<string> knownPoints = [];
        foreach (var entry in controller._occupantByPointId)
        {
            knownPoints.Add(entry.Key);
        }

        _applying = true;
        foreach (var pointId in knownPoints)
        {
            controller.ApplyOccupancyState(pointId, 0);
        }
        foreach (var entry in occupants)
        {
            if (entry.Value != 0)
            {
                controller.ApplyOccupancyState(entry.Key, entry.Value);
            }
        }
        _applying = false;

        var raidId = MyRaidId;
        _heldPointId = null;
        foreach (var entry in occupants)
        {
            if (entry.Value == raidId)
            {
                _heldPointId = entry.Key;
                break;
            }
        }
    }

    private static int HostResolveClaim(string pointId, int raidId)
    {
        if (_hostOccupants.TryGetValue(pointId, out var occupant) && occupant != 0 && occupant != raidId)
        {
            Broadcast(pointId, occupant);
            return occupant;
        }

        _hostOccupants[pointId] = raidId;
        Broadcast(pointId, raidId);
        return raidId;
    }

    private static void HostRelease(string pointId, int raidId)
    {
        if (!_hostOccupants.TryGetValue(pointId, out var occupant) || occupant != raidId)
        {
            return;
        }

        _hostOccupants.Remove(pointId);
        Broadcast(pointId, 0);
    }

    private static void Broadcast(string pointId, int occupantId)
    {
        ApplyLocal(pointId, occupantId);
        Send(DialogEntryPacket.Occupancy(pointId, occupantId), true);
    }

    private static void ApplyLocal(string pointId, int occupantId)
    {
        var controller = GetController();
        if (controller == null)
        {
            return;
        }

        _applying = true;
        controller.ApplyOccupancyState(pointId, occupantId);
        _applying = false;

        if (occupantId == MyRaidId)
        {
            _heldPointId = pointId;
        }
        else if (_heldPointId == pointId)
        {
            _heldPointId = null;
        }
    }

    private static void OnPointOccupancyChanged(string pointId)
    {
        if (_applying || _heldPointId == null || pointId != _heldPointId)
        {
            return;
        }

        var controller = GetController();
        if (controller == null)
        {
            return;
        }

        var raidId = MyRaidId;
        if (controller._occupantByPointId.TryGetValue(pointId, out var occupant) && occupant == raidId)
        {
            return;
        }

        _heldPointId = null;
        if (FikaBackendUtils.IsServer)
        {
            HostRelease(pointId, raidId);
            return;
        }

        SendToHost(DialogEntryPacket.Release(pointId, raidId));
    }
    
    public static void ReleaseAllFor(int netId)
    {
        if (!FikaBackendUtils.IsServer || _hostOccupants.Count < 1)
        {
            return;
        }

        List<string> held = null;
        foreach (var entry in _hostOccupants)
        {
            if (entry.Value == netId)
            {
                held ??= [];
                held.Add(entry.Key);
            }
        }

        if (held == null)
        {
            return;
        }

        foreach (var pointId in held)
        {
            _hostOccupants.Remove(pointId);
            Broadcast(pointId, 0);
        }
    }

    private static void OnPlayerGone(FikaPlayer player)
    {
        if (player == null || player.IsAI)
        {
            return;
        }

        ReleaseAllFor(player.NetId);
    }

    private static RaidDialogEntryController GetController()
    {
        return Singleton<GameWorld>.Instantiated ? Singleton<GameWorld>.Instance.RaidDialogEntryController : null;
    }

    private static int MyRaidId
    {
        get
        {
            if (CoopHandler.TryGetCoopHandler(out var coopHandler) && coopHandler != null && coopHandler.MyPlayer != null)
            {
                return coopHandler.MyPlayer.NetId;
            }

            return 0;
        }
    }

    private static void SendToHost(DialogEntryPacket packet)
    {
        Send(packet, false);
    }

    private static void Send(DialogEntryPacket packet, bool broadcast)
    {
        FikaGlobals.NetworkManager?.SendGenericPacket(in packet, DeliveryMethod.ReliableOrdered, broadcast);
    }

    private static void DisplayBusy()
    {
        FikaGlobals.DisplayMessage(BUSY_LOCALE_KEY.Localized());
    }
}
