using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;
using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets.FirearmController
{
    public class WeaponPacket : INetReusable
    {
        public int NetId;
        public EFirearmSubPacketType Type;
        public IPoolSubPacket SubPacket;

        public void Execute(FikaPlayer player = null)
        {
            SubPacket.Execute(player);
            FirearmSubPacketPoolManager.Instance.ReturnPacket(Type, SubPacket);
        }

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
            Type = reader.GetEnum<EFirearmSubPacketType>();
            SubPacket = FirearmSubPacketPoolManager.Instance.GetPacket<IPoolSubPacket>(Type);
            SubPacket.Deserialize(reader);
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.PutEnum(Type);
            SubPacket?.Serialize(writer);
        }

        public void Clear()
        {
            if (SubPacket != null)
            {
                FirearmSubPacketPoolManager.Instance.ReturnPacket(Type, SubPacket);
                SubPacket = null;
            }
        }

        public void Flush()
        {
            SubPacket = null;
        }
    }
}
