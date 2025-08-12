namespace Fika.Core.Networking.LiteNetLib.Utils;

public interface INetSerializable
{
    void Serialize(NetDataWriter writer);
    void Deserialize(NetDataReader reader);
}
