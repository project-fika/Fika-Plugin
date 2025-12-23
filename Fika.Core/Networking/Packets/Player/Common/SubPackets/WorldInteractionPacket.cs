using System;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Pooling;
using static Fika.Core.Main.Players.FikaPlayer;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public sealed class WorldInteractionPacket : IPoolSubPacket
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
        var packet = CommonSubPacketPoolManager.Instance.GetPacket<WorldInteractionPacket>(ECommonSubPacketType.WorldInteraction);
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
                    KeyHandler keyHandler = new(player);

                    if (string.IsNullOrEmpty(ItemId))
                    {
                        FikaGlobals.LogWarning("WorldInteractionPacket: ItemID was null!");
                        return;
                    }

                    var result = player.FindItemById(ItemId, false, false);
                    if (!result.Succeeded)
                    {
                        FikaGlobals.LogWarning("WorldInteractionPacket: Could not find item: " + ItemId);
                        return;
                    }

                    var keyComponent = result.Value.GetItemComponent<KeyComponent>();
                    if (keyComponent == null)
                    {
                        FikaGlobals.LogWarning("WorldInteractionPacket: keyComponent was null!");
                        return;
                    }

#if DEBUG
                    FikaGlobals.LogInfo($"Using key with {keyComponent.NumberOfUsages}/{keyComponent.Template.MaximumNumberOfUsage} uses");
#endif

                    keyHandler.UnlockResult = worldInteractiveObject.UnlockOperation(keyComponent, player, worldInteractiveObject);
                    if (keyHandler.UnlockResult.Error != null)
                    {
                        FikaGlobals.LogWarning("WorldInteractionPacket: Error when processing unlockResult: " + keyHandler.UnlockResult.Error);
                        return;
                    }

                    interactionResult = keyHandler.UnlockResult.Value;
                    keyHandler.UnlockResult.Value.RaiseEvents(player.InventoryController, CommandStatus.Begin);
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
            FikaGlobals.LogError("WorldInteractionPacket: WorldInteractiveObject was null or disabled!");
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
