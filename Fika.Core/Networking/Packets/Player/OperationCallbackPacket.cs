using EFT;
using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets.Player
{
    public struct OperationCallbackPacket(int netId, uint callbackId, EOperationStatus operationStatus, string error = null) : INetSerializable
    {
        public int NetId = netId;
        public uint CallbackId = callbackId;
        public EOperationStatus OperationStatus = operationStatus;
        public string Error = error;

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
            CallbackId = reader.GetUInt();
            OperationStatus = (EOperationStatus)reader.GetInt();
            if (OperationStatus == EOperationStatus.Failed)
            {
                Error = reader.GetString();
            }
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.Put(CallbackId);
            writer.Put((int)OperationStatus);
            if (OperationStatus == EOperationStatus.Failed)
            {
                writer.Put(Error);
            }
        }
    }
}
