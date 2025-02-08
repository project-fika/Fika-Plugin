using Fika.Core.Coop.Players;
using Fika.Core.Networking.Packets;
using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public struct InventoryPacket : IQueuePacket
    {
        public int NetId { get; set; }
        public uint CallbackId;
        public byte[] OperationBytes;

        public void Execute(CoopPlayer player)
        {
            player.PacketReceiver.ConvertInventoryPacket(this);
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.Put(CallbackId);
            writer.PutByteArray(OperationBytes);
        }

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
            CallbackId = reader.GetUInt();
            OperationBytes = reader.GetByteArray();
        }
    }
}
