using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets.Communication;

public struct FlareSuccessPacket(string profileId, bool success) : INetSerializable
{
    public string ProfileId = profileId;
    public bool Success = success;

    public void Deserialize(NetDataReader reader)
    {
        ProfileId = reader.GetString();
        Success = reader.GetBool();
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(ProfileId);
        writer.Put(Success);
    }
}
