using System;
using System.Collections.Generic;
using Comfort.Common;
using ComponentAce.Compression.Libs.zlib;
using EFT.Quests;
using Fika.Core.Main.Components;
using Fika.Core.Main.ObservedClasses;
using Fika.Core.Main.Players;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.Backend;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Newtonsoft.Json;

namespace Fika.Core.Main.Utils;

/// <summary>
/// Gives every peer the quest templates of every human player.
/// </summary>
public static class QuestTemplateSync
{
    private static readonly Dictionary<int, byte[]> _templates = [];

    public static IReadOnlyDictionary<int, byte[]> Templates
    {
        get
        {
            return _templates;
        }
    }

    public static void SendLocal(FikaPlayer player)
    {
        Il2CppSystem.Collections.Generic.List<QuestTemplate> templates = new();
        List<Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppSystem.Collections.Generic.Dictionary<string, string>>> localizations = [];
        foreach (var quest in player.QuestController.Quests)
        {
            if (quest.Template != null)
            {
                templates.Add(quest.Template);
            }
        }
        
        Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppSystem.Collections.Generic.Dictionary<string, string>> empty = new();
        byte[] data;
        try
        {
            foreach (var template in templates)
            {
                localizations.Add(template.Localization);
                template.Localization = empty;
            }

            data = SimpleZlib.CompressToBytes(JsonExtensions.ToJson(templates, new Il2CppReferenceArray<JsonConverter>(0L)), 6);
        }
        catch (Exception ex)
        {
            FikaGlobals.LogError($"Could not serialize quest templates: {ex}");
            return;
        }
        finally
        {
            for (var i = 0; i < localizations.Count; i++)
            {
                templates[i].Localization = localizations[i];
            }
        }

        QuestTemplatesPacket packet = new()
        {
            NetId = player.NetId,
            Data = data
        };

        if (FikaBackendUtils.IsServer)
        {
            _templates[packet.NetId] = packet.Data;
            Singleton<FikaServer>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
            return;
        }

        Singleton<FikaClient>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
    }

    public static void Receive(QuestTemplatesPacket packet)
    {
        _templates[packet.NetId] = packet.Data;
        if (CoopHandler.TryGetCoopHandler(out var coopHandler) && coopHandler.Players.TryGetValue(packet.NetId, out var player))
        {
            Apply(player);
        }
    }

    public static void Apply(FikaPlayer player)
    {
        if (player is not ObservedPlayer || player.QuestController is not ObservedQuestController questController
            || !_templates.TryGetValue(player.NetId, out var data))
        {
            return;
        }

        var quests = questController.Quests;
        try
        {
            var templates = JsonExtensions.ParseJsonTo<Il2CppSystem.Collections.Generic.List<QuestTemplate>>(SimpleZlib.Decompress(data, null),
                new Il2CppReferenceArray<JsonConverter>(0L));
            quests.AddTemplates(templates.Cast<Il2CppSystem.Collections.Generic.IEnumerable<QuestTemplate>>());
            FikaGlobals.LogInfo($"Added {quests.Count} quests from {templates.Count} templates for {player.Profile.Info.MainProfileNickname}");
        }
        catch (Exception ex)
        {
            FikaGlobals.LogError($"Could not add quest templates for {player.Profile.Info.MainProfileNickname}: {ex}");
        }
    }

    public static void Clear()
    {
        _templates.Clear();
    }
}
