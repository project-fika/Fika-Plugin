using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public struct BotStatePacket : INetSerializable
    {
        public ushort NetId;
        public EStateType Type;

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetUShort();
            Type = (EStateType)reader.GetByte();
        }

        public readonly void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.Put((byte)Type);
        }

        public enum EStateType
        {
            LoadBot,
            DisposeBot,
            EnableBot,
            DisableBot
        }
    }
}
