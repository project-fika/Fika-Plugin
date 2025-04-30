using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking.Packets.Backend;
using System.Collections.Generic;

namespace Fika.Core.Coop.ObservedClasses
{
    public class ObservedQuestController(Profile profile, InventoryController inventoryController, IPlayerSearchController searchController, IQuestActions session)
        : GClass3765(profile, inventoryController, searchController, session)
    {
        public void HandleInraidQuestPacket(InraidQuestPacket packet)
        {
            switch (packet.Type)
            {
                case InraidQuestPacket.InraidQuestType.Finish:
                    {
                        FikaGlobals.LogInfo($"Processing {packet.Items.Count} items fom quest reward for {Profile.Info.MainProfileNickname}");
                        List<GClass3806> readList = [];
                        foreach (GClass1356[] item in packet.Items)
                        {
                            readList.Add(new()
                            {
                                items = item,
                                MongoID_0 = MongoID.Generate(true),
                                type = EFT.Quests.ERewardType.Item
                            });
                        }

                        int generatedItems = 0;
                        List<GClass3250> results = [];
                        GStruct439 appendResult = default;
                        foreach (GClass3806 item in readList)
                        {
                            appendResult = item.TryAppendClaimResults(InventoryController_0, results, out int clonedCount);
                            generatedItems += clonedCount;
                            if (appendResult.Failed)
                            {
                                break;
                            }
                        }
                        if (appendResult.Failed)
                        {
                            results.RollBack();
                            for (int i = 0; i < generatedItems; i++)
                            {
                                InventoryController_0.RollBack();
                            }
                            return;
                        }

                        method_5(results);
                    }
                    break;
                case InraidQuestPacket.InraidQuestType.Handover:
                    {
                        FikaGlobals.LogInfo($"Discarding {packet.ItemIdsToRemove.Count} items from {Profile.Info.MainProfileNickname}");
                        List<Item> itemsToRemove = [];
                        GameWorld gameWorld = Singleton<GameWorld>.Instance;
                        foreach (string itemId in packet.ItemIdsToRemove)
                        {
                            GStruct442<Item> result = gameWorld.FindItemById(itemId);
                            if (result.Failed)
                            {
                                FikaGlobals.LogError($"Could not find itemId {itemId}: {result.Error}");
                                continue;
                            }
                            itemsToRemove.Add(result.Value);
                        }

                        List<GStruct439> list = [];
                        GStruct439 discardResult = default;
                        for (int i = 0; i < itemsToRemove.Count; i++)
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
                default:
                    break;
            }
        }
    }
}
