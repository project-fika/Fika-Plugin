using Fika.Core.Coop.Players;
using LiteNetLib.Utils;
using static Fika.Core.Networking.SubPacket;

namespace Fika.Core.Networking
{
    public struct CommonPlayerPacket : INetSerializable
    {
        public ushort NetId;
        public ECommonSubPacketType Type;
        public ISubPacket SubPacket;

        public readonly void Execute(CoopPlayer player)
        {
            SubPacket.Execute(player);
        }

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetUShort();
            Type = reader.GetEnum<ECommonSubPacketType>();
            SubPacket = reader.GetCommonSubPacket(Type);
        }

        public readonly void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.PutEnum(Type);
            SubPacket?.Serialize(writer);
        }
    }
}
