using LiteNetLib.Utils;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Fika.Core.Networking.Packets.Backend
{
    public class InraidQuestPacket : INetSerializable
    {
        public ushort NetId;
        public InraidQuestType Type;
        public List<FlatItemsDataClass[]> Items;
        public List<string> ItemIdsToRemove;

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetUShort();
            Type = reader.GetEnum<InraidQuestType>();
            switch (Type)
            {
                case InraidQuestType.Finish:
                    {
                        int length = reader.GetInt();
                        Items = new(length);
                        for (int i = 0; i < length; i++)
                        {
                            Items.Add(JsonConvert.DeserializeObject<FlatItemsDataClass[]>(reader.GetString()));
                        }
                    }
                    break;
                case InraidQuestType.Handover:
                    {
                        if (reader.GetBool())
                        {
                            int length = reader.GetInt();
                            ItemIdsToRemove = new(length);
                            for (int i = 0; i < length; i++)
                            {
                                ItemIdsToRemove.Add(reader.GetString());
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.PutEnum(Type);
            switch (Type)
            {
                case InraidQuestType.Finish:
                    writer.Put(Items.Count);
                    for (int i = 0; i < Items.Count; i++)
                    {
                        writer.Put(JsonConvert.SerializeObject(Items[i]));
                    }
                    break;
                case InraidQuestType.Handover:
                    if (ItemIdsToRemove != null)
                    {
                        writer.Put(true);
                        writer.Put(ItemIdsToRemove.Count);
                        for (int i = 0; i < ItemIdsToRemove.Count; i++)
                        {
                            writer.Put(ItemIdsToRemove[i]);
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        public enum InraidQuestType : byte
        {
            Finish,
            Handover
        }
    }
}
