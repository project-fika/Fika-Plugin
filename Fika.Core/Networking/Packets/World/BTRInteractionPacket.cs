using EFT;
using EFT.Vehicle;

namespace Fika.Core.Networking.Packets.World;

public struct BTRInteractionPacket(int netId) : INetSerializable
{
    public int NetId = netId;
    public bool IsResponse;
    public EBtrInteractionStatus Status;
    public PlayerInteractPacket Data;

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetInt();
        IsResponse = reader.GetBool();
        if (IsResponse)
        {
            Status = reader.GetEnum<EBtrInteractionStatus>();
        }
        Data = new()
        {
            HasInteraction = reader.GetBool(),
            InteractionType = reader.GetEnum<EInteractionType>(),
            SideId = reader.GetByte(),
            SlotId = reader.GetByte(),
            Fast = reader.GetBool()
        };
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put(IsResponse);
        if (IsResponse)
        {
            writer.PutEnum(Status);
        }
        writer.Put(Data.HasInteraction);
        writer.PutEnum(Data.InteractionType);
        writer.Put(Data.SideId);
        writer.Put(Data.SlotId);
        writer.Put(Data.Fast);
    }
}
