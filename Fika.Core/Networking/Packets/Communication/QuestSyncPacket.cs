using EFT;
using EFT.Quests;

namespace Fika.Core.Networking.Packets.Communication;

public struct QuestSyncPacket : INetSerializable
{
    public int NetId;
    public EQuestSyncType Type;
    public int QuestId;
    public MongoID ConditionId;
    public int Value;
    public MongoID? ItemId;
    public string ZoneId;

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.PutEnum(Type);
        switch (Type)
        {
            case EQuestSyncType.Conditional:
                writer.Put(QuestId);
                writer.PutMongoID(ConditionId);
                writer.Put(Value);
                break;
            case EQuestSyncType.ItemDrop:
                writer.PutNullableMongoID(ItemId);
                writer.Put(ZoneId);
                break;
            case EQuestSyncType.PlaceVisited:
                writer.Put(ZoneId);
                break;
            case EQuestSyncType.PickUpQuestItem:
                writer.PutNullableMongoID(ItemId);
                break;
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetInt();
        Type = reader.GetEnum<EQuestSyncType>();
        switch (Type)
        {
            case EQuestSyncType.Conditional:
                QuestId = reader.GetInt();
                ConditionId = reader.GetMongoID();
                Value = reader.GetInt();
                break;
            case EQuestSyncType.ItemDrop:
                ItemId = reader.GetNullableMongoID();
                ZoneId = reader.GetString();
                break;
            case EQuestSyncType.PlaceVisited:
                ZoneId = reader.GetString();
                break;
            case EQuestSyncType.PickUpQuestItem:
                ItemId = reader.GetNullableMongoID();
                break;
        }
    }

    public enum EQuestSyncType
    {
        Conditional,
        ItemDrop,
        PlaceVisited,
        PickUpQuestItem
    }
}
