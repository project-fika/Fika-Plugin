using System;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using static Fika.Core.Main.Players.FikaPlayer;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public readonly struct WorldInteractionPacket
{
    public WorldInteractionPacket(string interactiveId, EInteractionType interactionType, EInteractionStage interactionStage, string itemId = null)
    {
        InteractiveId = interactiveId;
        InteractionType = interactionType;
        InteractionStage = interactionStage;
        ItemId = itemId;
    }

    public WorldInteractionPacket(NetDataReader reader)
    {
        InteractiveId = reader.GetString();
        InteractionType = reader.GetEnum<EInteractionType>();
        InteractionStage = reader.GetEnum<EInteractionStage>();
        if (InteractionType == EInteractionType.Unlock)
        {
            ItemId = reader.GetString();
        }
    }

    public readonly string InteractiveId;
    public readonly EInteractionType InteractionType;
    public readonly EInteractionStage InteractionStage;
    public readonly string ItemId;

    public readonly void Execute(FikaPlayer player)
    {
        var worldInteractiveObject = Singleton<GameWorld>.Instance.FindDoor(InteractiveId);
        if (worldInteractiveObject != null)
        {
            if (worldInteractiveObject.isActiveAndEnabled && !worldInteractiveObject.ForceLocalInteraction)
            {
#if DEBUG
                FikaGlobals.LogInfo($"Interacting with door, type: {InteractionType}, stage: {InteractionStage}");
#endif

                InteractionResult interactionResult;
                Action action;
                if (InteractionType == EInteractionType.Unlock && InteractionStage == EInteractionStage.Start)
                {
                    var success = GetKeyHandler(player, worldInteractiveObject, out var keyHandler);
                    if (!success)
                    {
                        return;
                    }

                    interactionResult = keyHandler.UnlockResult.Value;
                    keyHandler.UnlockResult.Value.RaiseEvents(player.InventoryController, CommandStatus.Begin);
                    action = new(keyHandler.HandleKeyEvent);
                }
                else if (worldInteractiveObject is Switch && InteractionStage is EInteractionStage.Execute && !string.IsNullOrWhiteSpace(ItemId)) // edge case for switches, e.g. labyrinth puzzles
                {
                    var success = GetKeyHandler(player, worldInteractiveObject, out var keyHandler);
                    if (!success)
                    {
                        return;
                    }

                    keyHandler.UnlockResult.Value.RaiseEvents(player.InventoryController, CommandStatus.Begin);
                    keyHandler.UnlockResult.Value.RaiseEvents(player.InventoryController, CommandStatus.Succeed);
                    player.ExecuteInteraction(worldInteractiveObject, keyHandler.UnlockResult.Value);
                    return;
                }
                else
                {
                    interactionResult = new InteractionResult(InteractionType);
                    action = null;
                }

                if (InteractionStage == EInteractionStage.Start)
                {
                    player.StartInteraction(worldInteractiveObject, interactionResult, action);
                    return;
                }

                if (InteractionStage != EInteractionStage.Execute)
                {
                    worldInteractiveObject.Interact(interactionResult);
                    return;
                }

                player.ExecuteInteraction(worldInteractiveObject, interactionResult);
            }

        }
        else
        {
            FikaGlobals.LogError("WorldInteractionPacket: WorldInteractiveObject was null or disabled!");
        }
    }

    private readonly bool GetKeyHandler(FikaPlayer player, WorldInteractiveObject worldInteractiveObject, out KeyHandler keyHandler)
    {
        keyHandler = new(player);
        if (string.IsNullOrWhiteSpace(ItemId))
        {
            FikaGlobals.LogError("WorldInteractionPacket: ItemID was null!");
            return false;
        }

        var result = player.FindItemById(ItemId, false, false);
        if (!result.Succeeded)
        {
            FikaGlobals.LogError("WorldInteractionPacket: Could not find item: " + ItemId);
            return false;
        }

        var keyComponent = result.Value.GetItemComponent<KeyComponent>();
        if (keyComponent == null)
        {
            FikaGlobals.LogError("WorldInteractionPacket: keyComponent was null!");
            return false;
        }

#if DEBUG
        FikaGlobals.LogInfo($"Using key with {keyComponent.NumberOfUsages} current uses, {keyComponent.Template.MaximumNumberOfUsage} maximum");
#endif

        keyHandler.UnlockResult = worldInteractiveObject.UnlockOperation(keyComponent, player, worldInteractiveObject);
        if (keyHandler.UnlockResult.Error != null)
        {
            FikaGlobals.LogError("WorldInteractionPacket: Error when processing unlockResult: " + keyHandler.UnlockResult.Error);
            return false;
        }

        return true;
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(InteractiveId);
        writer.PutEnum(InteractionType);
        writer.PutEnum(InteractionStage);
        if (InteractionType == EInteractionType.Unlock)
        {
            writer.Put(ItemId);
        }
    }
}
