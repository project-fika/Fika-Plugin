using Fika.Core.Coop.Players;
using LiteNetLib.Utils;
using static Fika.Core.Networking.SubPacket;

namespace Fika.Core.Networking
{
    public struct CommonPlayerPacket : INetSerializable
    {
        public int NetId;
        public ECommonSubPacketType Type;
        public ISubPacket SubPacket;

        public readonly void Execute(CoopPlayer player)
        {
            SubPacket.Execute(player);
        }

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
            Type = (ECommonSubPacketType)reader.GetByte();
            SubPacket = reader.GetCommonSubPacket(Type);
        }

        public readonly void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.Put((byte)Type);
            SubPacket?.Serialize(writer);
        }
    }
}
