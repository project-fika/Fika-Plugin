using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.GameTriggers;
using EFT.GlobalEvents;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.World;

namespace Fika.Core.Main.Components;

public static class FikaStoryTriggers
{
    private static readonly List<FikaPlayer> _humans = [];

    public static List<FikaPlayer> AliveHumanPlayers(CoopHandler coopHandler)
    {
        _humans.Clear();
        var extracted = FikaGlobals.FikaGame?.ExtractedPlayers;
        foreach (var player in coopHandler.HumanPlayers)
        {
            if (player == null || !player.HealthController.IsAlive || extracted?.Contains(player.NetId) == true
                || (FikaBackendUtils.IsHeadless && player == coopHandler.MyPlayer))
            {
                continue;
            }

            _humans.Add(player);
        }

        return _humans;
    }

    public static void Subscribe(HandlerStartCutscene handler)
    {
        var triggerId = handler._triggerId;
        var cutsceneId = handler._cutsceneId;
        var emitter = TriggersEmitter.Instance;
        if (!FikaBackendUtils.IsServer || emitter == null || string.IsNullOrEmpty(triggerId) || string.IsNullOrEmpty(cutsceneId))
        {
            return;
        }

        emitter.Subscribe(triggerId, new Action<TriggerEvent>(_ => StartCutscene(cutsceneId)));
    }

    public static void SendPresence(TriggerRallyZone zone, FikaPlayer player, bool entered)
    {
        RallyZonePresencePacket packet = new()
        {
            ZoneId = zone._zoneIdHash,
            NetId = player.NetId,
            Entered = entered
        };

        FikaGlobals.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered);
    }

    public static void ReceivePresence(RallyZonePresencePacket packet)
    {
        var zone = FindZone(packet.ZoneId);
        if (zone == null || !CoopHandler.TryGetCoopHandler(out var coopHandler)
            || !coopHandler.Players.TryGetValue(packet.NetId, out var player) || player == null)
        {
            return;
        }

        if (packet.Entered)
        {
            zone.OnEnterAuthority(player);
        }
        else
        {
            zone.OnExitAuthority(player);
        }
    }

    public static void SendUpdate(TriggerRallyZone zone)
    {
        RallyZoneUpdatePacket packet = new()
        {
            ZoneId = zone._zoneIdHash,
            PlayersInZone = (byte)zone._playersInside.Count,
            TotalAlive = (byte)zone.GetAliveHumanPlayerCount()
        };

        FikaGlobals.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered);
    }
    
    public static void ReceiveUpdate(RallyZoneUpdatePacket packet)
    {
        var zone = FindZone(packet.ZoneId);
        if (zone == null)
        {
            return;
        }

        RallyZoneUpdateEvent update = new()
        {
            ZoneId = packet.ZoneId,
            PlayersInZone = packet.PlayersInZone,
            TotalAlive = packet.TotalAlive
        };

        zone.OnZoneUpdate(update);
    }

    public static void SendTrigger(TriggerRallyZone zone, int originRaidId)
    {
        RallyZoneTriggerPacket packet = new()
        {
            TriggerId = zone._triggerId,
            OriginNetId = FindNetId(originRaidId)
        };
        FikaGlobals.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered);
        FikaGlobals.LogInfo($"Rally zone '{zone._triggerId}' complete");
    }

    public static void ReceiveRallyZoneTrigger(RallyZoneTriggerPacket packet)
    {
        var world = Singleton<GameWorld>.Instance;
        if (world == null || world.TriggersEmitter == null || !CoopHandler.TryGetCoopHandler(out var coopHandler))
        {
            return;
        }

        foreach (var zone in UnityEngine.Object.FindObjectsOfType<TriggerRallyZone>(true))
        {
            if (zone._triggerId == packet.TriggerId)
            {
                zone._wasTriggered = true;
            }
        }

        // Trigger handlers look the origin up among the alive players, so fall back to our own player
        var origin = coopHandler.Players.TryGetValue(packet.OriginNetId, out var player) && player != null
            ? player
            : coopHandler.MyPlayer;
        world.TriggersEmitter.Emit(packet.TriggerId, origin == null ? 0 : origin.RaidId);
    }

    private static void StartCutscene(string cutsceneId)
    {
        var server = Singleton<GameWorld>.Instance?.CutscenesServerController;

        if (server == null || !CoopHandler.TryGetCoopHandler(out var coopHandler))
        {
            return;
        }

        Il2CppSystem.Collections.Generic.List<int> ids = new();
        foreach (var player in AliveHumanPlayers(coopHandler))
        {
            ids.Add(player.PlayerId);
        }

        var process = server.StartCutscene(cutsceneId, ids, false, false, 0f);
        FikaGlobals.LogInfo($"Trigger cutscene '{cutsceneId}' {(process == null ? "refused" : "started")} for {ids.Count} players");
    }

    private static TriggerRallyZone FindZone(int zoneId)
    {
        foreach (var zone in UnityEngine.Object.FindObjectsOfType<TriggerRallyZone>(true))
        {
            if (zone._zoneIdHash == zoneId)
            {
                return zone;
            }
        }

        return null;
    }

    private static int FindNetId(int raidId)
    {
        if (CoopHandler.TryGetCoopHandler(out var coopHandler))
        {
            foreach (var player in coopHandler.Players.Values)
            {
                if (player != null && player.RaidId == raidId)
                {
                    return player.NetId;
                }
            }
        }

        return 0;
    }
}
