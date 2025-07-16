using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets.Player
{
    public struct InventoryPacket : INetSerializable
    {
        public int NetId;
        public uint CallbackId;
        public byte[] OperationBytes;

        public readonly void Serialize(NetDataWriter writer)
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
