using LiteNetLib.Utils;

namespace Fika.Core.Networking 
{
    // used for both quests and achievements
    public struct ConditionChangePacket(int netId, string conditionId, float conditionValue) : INetSerializable
    {
        public int NetId;
        public string ConditionId;
        public float ConditionValue;

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
            ConditionId = reader.GetString();
            ConditionValue = reader.GetFloat();            
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(netId);
            writer.Put(conditionId);
            writer.Put(conditionValue);
        }
    }
}