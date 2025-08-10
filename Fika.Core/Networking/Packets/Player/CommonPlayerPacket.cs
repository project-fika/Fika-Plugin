using Fika.Core.Main.Players;
using Fika.Core.Networking.Pools;
using LiteNetLib.Utils;
using static Fika.Core.Networking.Packets.SubPacket;

namespace Fika.Core.Networking.Packets.Player
{
    public class CommonPlayerPacket : INetReusable
    {
        public int NetId;
        public ECommonSubPacketType Type;
        public IPoolSubPacket SubPacket;

        public void Execute(FikaPlayer player)
        {
            SubPacket.Execute(player);
            CommonSubPacketPoolManager.Instance.ReturnPacket(Type, SubPacket);
        }

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
            Type = reader.GetEnum<ECommonSubPacketType>();
            SubPacket = CommonSubPacketPoolManager.Instance.GetPacket<IPoolSubPacket>(Type);
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
                CommonSubPacketPoolManager.Instance.ReturnPacket(Type, SubPacket);
                SubPacket = null;
            }
        }

        public void Flush()
        {
            SubPacket = null;
        }
    }
}
