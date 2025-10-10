namespace Fika.Core.Networking.Packets.Communication;

public struct TextMessagePacket(string nickname, string message) : INetSerializable
{
    public string Nickname = nickname;
    public string Message = message;

    public void Deserialize(NetDataReader reader)
    {
        Nickname = reader.GetString();
        Message = reader.GetString();
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(Nickname);
        writer.Put(Message);
    }
}
