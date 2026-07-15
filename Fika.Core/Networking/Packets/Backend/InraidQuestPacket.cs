using System.Collections.Generic;
using EFT;
using Newtonsoft.Json;

namespace Fika.Core.Networking.Packets.Backend;

public struct InRaidQuestPacket : INetSerializable
{
    public int NetId;
    public InraidQuestType Type;
    public List<FlatItemsDataClass[]> Items;
    public List<MongoID> ItemIdsToRemove;

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetInt();
        Type = (InraidQuestType)reader.GetByte();
        switch (Type)
        {
            case InraidQuestType.Finish:
                {
                    var length = reader.GetInt();
                    Items = new(length);
                    for (var i = 0; i < length; i++)
                    {
                        Items.Add(JsonConvert.DeserializeObject<FlatItemsDataClass[]>(reader.GetString()));
                    }
                }
                break;
            case InraidQuestType.Handover:
                {
                    if (reader.GetBool())
                    {
                        var length = reader.GetInt();
                        ItemIdsToRemove = new(length);
                        for (var i = 0; i < length; i++)
                        {
                            ItemIdsToRemove.Add(reader.GetMongoID());
                        }
                    }
                }
                break;
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put((byte)Type);
        switch (Type)
        {
            case InraidQuestType.Finish:
                writer.Put(Items.Count);
                for (var i = 0; i < Items.Count; i++)
                {
                    writer.Put(JsonConvert.SerializeObject(Items[i]));
                }
                break;
            case InraidQuestType.Handover:
                if (ItemIdsToRemove != null)
                {
                    writer.Put(true);
                    writer.Put(ItemIdsToRemove.Count);
                    for (var i = 0; i < ItemIdsToRemove.Count; i++)
                    {
                        writer.PutMongoID(ItemIdsToRemove[i]);
                    }
                }
                break;
        }
    }

    public enum InraidQuestType
    {
        Finish,
        Handover
    }
}
