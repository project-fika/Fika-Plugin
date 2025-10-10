namespace Fika.Core.Networking.Packets.Communication;

public struct BotStatePacket : INetSerializable
{
    public int NetId;
    public EStateType Type;

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetInt();
        Type = reader.GetEnum<EStateType>();
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.PutEnum(Type);
    }

    public enum EStateType
    {
        LoadBot,
        DisposeBot,
        EnableBot,
        DisableBot
    }
}
