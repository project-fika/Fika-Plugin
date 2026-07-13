namespace Fika.Core.Networking.Packets.FirearmController;

public struct ProceedResponsePacket : INetSerializable
{
    public uint CallbackId;
    public string Error;

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(CallbackId);
        writer.Put(Error);
    }

    public void Deserialize(NetDataReader reader)
    {
        CallbackId = reader.GetUInt();
        Error = reader.GetString();
    }
}
