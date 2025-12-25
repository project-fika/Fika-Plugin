using EFT;

namespace Fika.Core.Networking.Packets.Communication;

public struct EventControllerInteractPacket : INetSerializable
{
    public int NetId;
    public InteractPacketStruct Data;

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetInt();
        Data = new()
        {
            hasInteraction = reader.GetBool(),
            objectId = reader.GetInt(),
            interaction = (EventObject.EInteraction)reader.GetByte()
        };
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put(Data.hasInteraction);
        writer.Put(Data.objectId);
        writer.Put((byte)Data.interaction);
    }
}
