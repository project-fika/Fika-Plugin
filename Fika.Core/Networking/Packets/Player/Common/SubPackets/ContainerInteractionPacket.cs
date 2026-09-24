using Comfort.Common;
using EFT;
using EFT.Interactive;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public readonly struct ContainerInteractionPacket
{
    public ContainerInteractionPacket(string interactiveId, EInteractionType interactionType)
    {
        InteractiveId = interactiveId;
        InteractionType = interactionType;
    }

    public ContainerInteractionPacket(NetDataReader reader)
    {
        InteractiveId = reader.GetString();
        InteractionType = reader.GetEnum<EInteractionType>();
    }

    public readonly string InteractiveId;
    public readonly EInteractionType InteractionType;

    public readonly void Execute(FikaPlayer player)
    {
        var lootableContainer = Singleton<GameWorld>.Instance.FindDoor(InteractiveId);
        if (lootableContainer != null)
        {
            if (lootableContainer.isActiveAndEnabled)
            {
                InteractionResult result = new(InteractionType);
                lootableContainer.Interact(result);
            }
        }
        else
        {
            FikaGlobals.LogError("ContainerInteractionPacket: LootableContainer was null!");
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(InteractiveId);
        writer.PutEnum(InteractionType);
    }
}
