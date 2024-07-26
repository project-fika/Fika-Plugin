using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public struct TextMessagePacket(string nickname, string message) : INetSerializable
    {
        public string Nickname = nickname;
        public string Message = message;

        public void Deserialize(NetDataReader reader)
        {
            Nickname = reader.GetString();
            Message = reader.GetString();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Nickname);
            writer.Put(Message);
        }
    }
}
