using Fika.Core.Main.Players;
using LiteNetLib.Utils;
using static Fika.Core.Networking.Packets.SubPacket;

namespace Fika.Core.Networking.Packets.FirearmController
{
    public struct WeaponPacket : INetSerializable
    {
        public int NetId;
        public EFirearmSubPacketType Type;
        public IPoolSubPacket SubPacket;

        public readonly void Execute(FikaPlayer player)
        {
            SubPacket.Execute(player);
            FirearmSubPacketPool.ReturnPacket(this);
        }

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
            Type = reader.GetEnum<EFirearmSubPacketType>();
            SubPacket = FirearmSubPacketPool.GetPacket<IPoolSubPacket>(Type);
            SubPacket.Deserialize(reader);
        }

        public readonly void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.PutEnum(Type);
            SubPacket?.Serialize(writer);
        }
    }
}
