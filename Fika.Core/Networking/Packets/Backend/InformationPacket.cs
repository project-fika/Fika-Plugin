// © 2024 Lacyway All Rights Reserved

using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public struct InformationPacket(bool isRequest) : INetSerializable
    {
        public bool IsRequest = isRequest;
        public int Connected = 0;
        public int Ready = 0;
        public string GroupId = "";
        
        public void Deserialize(NetDataReader reader)
        {
            IsRequest = reader.GetBool();
            Connected = reader.GetInt();
            Ready = reader.GetInt();
            GroupId = reader.GetString();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(IsRequest);
            writer.Put(Connected);
            writer.Put(Ready);
            writer.Put(GroupId);
        }
    }
}
