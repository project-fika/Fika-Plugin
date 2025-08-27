using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;
using System;
using static Fika.Core.Main.Players.FikaPlayer;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public class WorldInteractionPacket : IPoolSubPacket
{
    private WorldInteractionPacket()
    {

    }

    public static WorldInteractionPacket CreateInstance()
    {
        return new();
    }

    public static WorldInteractionPacket FromValue(string interactiveId, EInteractionType interactionType, EInteractionStage interactionStage, string itemId = null)
    {
        WorldInteractionPacket packet = CommonSubPacketPoolManager.Instance.GetPacket<WorldInteractionPacket>(ECommonSubPacketType.WorldInteraction);
        packet.InteractiveId = interactiveId;
        packet.InteractionType = interactionType;
        packet.InteractionStage = interactionStage;
        packet.ItemId = itemId;
        return packet;
    }

    public string InteractiveId;
    public EInteractionType InteractionType;
    public EInteractionStage InteractionStage;
    public string ItemId;

    public void Execute(FikaPlayer player)
    {
        WorldInteractiveObject worldInteractiveObject = Singleton<GameWorld>.Instance.FindDoor(InteractiveId);
        if (worldInteractiveObject != null)
        {
            if (worldInteractiveObject.isActiveAndEnabled && !worldInteractiveObject.ForceLocalInteraction)
            {
                InteractionResult interactionResult;
                Action action;
                if (InteractionType == EInteractionType.Unlock)
                {
                    KeyHandler keyHandler = new(player);

                    if (string.IsNullOrEmpty(ItemId))
                    {
                        FikaPlugin.Instance.FikaLogger.LogWarning("WorldInteractionPacket: ItemID was null!");
                        return;
                    }

                    GStruct156<Item> result = player.FindItemById(ItemId, false, false);
                    if (!result.Succeeded)
                    {
                        FikaPlugin.Instance.FikaLogger.LogWarning("WorldInteractionPacket: Could not find item: " + ItemId);
                        return;
                    }

                    KeyComponent keyComponent = result.Value.GetItemComponent<KeyComponent>();
                    if (keyComponent == null)
                    {
                        FikaPlugin.Instance.FikaLogger.LogWarning("WorldInteractionPacket: keyComponent was null!");
                        return;
                    }

                    keyHandler.unlockResult = worldInteractiveObject.UnlockOperation(keyComponent, player, worldInteractiveObject);
                    if (keyHandler.unlockResult.Error != null)
                    {
                        FikaPlugin.Instance.FikaLogger.LogWarning("WorldInteractionPacket: Error when processing unlockResult: " + keyHandler.unlockResult.Error);
                        return;
                    }

                    interactionResult = keyHandler.unlockResult.Value;
                    keyHandler.unlockResult.Value.RaiseEvents(player.InventoryController, CommandStatus.Begin);
                    action = new(keyHandler.HandleKeyEvent);
                }
                else
                {
                    interactionResult = new InteractionResult(InteractionType);
                    action = null;
                }

                if (InteractionStage == EInteractionStage.Start)
                {
                    player.vmethod_0(worldInteractiveObject, interactionResult, action);
                    return;
                }

                if (InteractionStage != EInteractionStage.Execute)
                {
                    worldInteractiveObject.Interact(interactionResult);
                    return;
                }

                player.vmethod_1(worldInteractiveObject, interactionResult);
            }

        }
        else
        {
            FikaPlugin.Instance.FikaLogger.LogError("WorldInteractionPacket: WorldInteractiveObject was null or disabled!");
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(InteractiveId);
        writer.PutEnum(InteractionType);
        writer.PutEnum(InteractionStage);
        if (InteractionType == EInteractionType.Unlock)
        {
            writer.Put(ItemId);
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        InteractiveId = reader.GetString();
        InteractionType = reader.GetEnum<EInteractionType>();
        InteractionStage = reader.GetEnum<EInteractionStage>();
        if (InteractionType == EInteractionType.Unlock)
        {
            ItemId = reader.GetString();
        }
    }

    public void Dispose()
    {
        InteractiveId = null;
        InteractionType = default;
        InteractionStage = default;
        ItemId = null;
    }
}
