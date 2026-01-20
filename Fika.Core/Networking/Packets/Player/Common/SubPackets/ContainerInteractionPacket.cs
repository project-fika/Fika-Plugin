using Comfort.Common;
using EFT;
using EFT.Interactive;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public sealed class ContainerInteractionPacket : IPoolSubPacket
{
    private ContainerInteractionPacket()
    {

    }

    public static ContainerInteractionPacket CreateInstance()
    {
        return new();
    }

    public static ContainerInteractionPacket FromValue(string interactiveId, EInteractionType interactionType)
    {
        var packet = CommonSubPacketPoolManager.Instance.GetPacket<ContainerInteractionPacket>(ECommonSubPacketType.ContainerInteraction);
        packet.InteractiveId = interactiveId;
        packet.InteractionType = interactionType;
        return packet;
    }

    public string InteractiveId;
    public EInteractionType InteractionType;

    public void Execute(FikaPlayer player)
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

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(InteractiveId);
        writer.PutEnum(InteractionType);
    }

    public void Deserialize(NetDataReader reader)
    {
        InteractiveId = reader.GetString();
        InteractionType = reader.GetEnum<EInteractionType>();
    }

    public void Dispose()
    {
        InteractiveId = null;
        InteractionType = default;
    }
}
