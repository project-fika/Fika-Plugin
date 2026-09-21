namespace Fika.Core.Networking.Packets.World;

public struct PasscodeAttemptPacket : INetSerializable
{
    public int NetId;
    public string PasscodeId;
    public int TerminalNetId;
    public string Input;

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetInt();
        PasscodeId = reader.GetString();
        TerminalNetId = reader.GetInt();
        Input = reader.GetString();
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put(PasscodeId);
        writer.Put(TerminalNetId);
        writer.Put(Input);
    }
}
