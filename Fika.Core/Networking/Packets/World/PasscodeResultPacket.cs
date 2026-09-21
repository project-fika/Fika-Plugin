namespace Fika.Core.Networking.Packets.World;

public struct PasscodeResultPacket : INetSerializable
{
    public int NetId;
    public string PasscodeId;
    public int TerminalNetId;
    public bool Successful;

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetInt();
        PasscodeId = reader.GetString();
        TerminalNetId = reader.GetInt();
        Successful = reader.GetBool();
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put(PasscodeId);
        writer.Put(TerminalNetId);
        writer.Put(Successful);
    }
}
