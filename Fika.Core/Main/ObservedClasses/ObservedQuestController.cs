using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Packets.Backend;
using System.Collections.Generic;

namespace Fika.Core.Main.ObservedClasses;

public class ObservedQuestController(Profile profile, InventoryController inventoryController, IPlayerSearchController searchController, IQuestActions session)
    : GClass4007(profile, inventoryController, searchController, session)
{
    public override void Run()
    {
        // do nothing
    }

    public override void ManageConditional(QuestClass conditional)
    {
        // do nothing
    }

    public void HandleInraidQuestPacket(InRaidQuestPacket packet)
    {
        switch (packet.Type)
        {
            case InRaidQuestPacket.InraidQuestType.Finish:
                {
                    FikaGlobals.LogInfo($"Processing {packet.Items.Count} items fom quest reward for {Profile.Info.MainProfileNickname}");
                    List<QuestRewardDataClass> readList = [];
                    foreach (var item in packet.Items)
                    {
                        readList.Add(new()
                        {
                            items = item,
                            MongoID_0 = MongoID.Generate(true),
                            type = EFT.Quests.ERewardType.Item
                        });
                    }

                    var generatedItems = 0;
                    List<GClass3411> results = [];
                    GStruct153 appendResult = default;
                    foreach (var item in readList)
                    {
                        appendResult = item.TryAppendClaimResults(InventoryController_0, results, out var clonedCount);
                        generatedItems += clonedCount;
                        if (appendResult.Failed)
                        {
                            break;
                        }
                    }
                    if (appendResult.Failed)
                    {
                        results.RollBack();
                        for (var i = 0; i < generatedItems; i++)
                        {
                            InventoryController_0.RollBack();
                        }
                        return;
                    }

                    method_5(results);
                }
                break;
            case InRaidQuestPacket.InraidQuestType.Handover:
                {
                    FikaGlobals.LogInfo($"Discarding {packet.ItemIdsToRemove.Count} items from {Profile.Info.MainProfileNickname}");
                    List<Item> itemsToRemove = [];
                    var gameWorld = Singleton<GameWorld>.Instance;
                    foreach (string itemId in packet.ItemIdsToRemove)
                    {
                        var result = gameWorld.FindItemById(itemId);
                        if (result.Failed)
                        {
                            FikaGlobals.LogError($"Could not find itemId {itemId}: {result.Error}");
                            continue;
                        }
                        itemsToRemove.Add(result.Value);
                    }

                    List<GStruct154<GClass3408>> list = [];
                    GStruct154<GClass3408> discardResult = default;
                    for (var i = 0; i < itemsToRemove.Count; i++)
                    {
                        discardResult = InteractionsHandlerClass.Discard(itemsToRemove[i], InventoryController_0, false);
                        if (discardResult.Failed)
                        {
                            break;
                        }
                        list.Add(discardResult);
                    }

                    if (discardResult.Failed)
                    {
                        list.RollBack();
                        FikaGlobals.LogError($"Could not discard items: {discardResult.Error.Localized()}");
                    }
                }
                break;
        }
    }
}
