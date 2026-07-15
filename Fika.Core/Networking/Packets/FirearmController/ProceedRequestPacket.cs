using EFT;
using Fika.Core.Networking.Packets.Player.Common;

namespace Fika.Core.Networking.Packets.FirearmController;

public struct ProceedRequestPacket : INetSerializable
{
    public int NetId;
    public uint CallbackId;
    public EProceedType ProceedType;
    public MongoID ItemId;

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put(CallbackId);
        writer.PutEnum(ProceedType);
        if (ProceedType is not EProceedType.EmptyHands)
        {
            writer.PutMongoID(ItemId);
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetInt();
        CallbackId = reader.GetUInt();
        ProceedType = reader.GetEnum<EProceedType>();
        if (ProceedType is not EProceedType.EmptyHands)
        {
            ItemId = reader.GetMongoID();
        }
    }
}
