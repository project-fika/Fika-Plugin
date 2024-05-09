using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets.Player
{
    public struct OperationCallbackPacket(int netId, uint callbackId, bool success) : INetSerializable
    {
        public int NetId = netId;
        public uint CallbackId = callbackId;
        public bool Success = success;

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
            CallbackId = reader.GetUInt();
            Success = reader.GetBool();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.Put(CallbackId);
            writer.Put(Success);
        }
    }
}
