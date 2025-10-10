using Comfort.Common;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Pooling;
using System.Collections.Generic;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public class CharacterSyncPacket : IPoolSubPacket
{
    private CharacterSyncPacket()
    {

    }

    public static CharacterSyncPacket CreateInstance()
    {
        return new();
    }

    public static CharacterSyncPacket FromValue(Dictionary<int, FikaPlayer> players)
    {
        CharacterSyncPacket packet = GenericSubPacketPoolManager.Instance.GetPacket<CharacterSyncPacket>(EGenericSubPacketType.CharacterSync);
        foreach ((int netid, _) in players)
        {
            packet.PlayerIds.Add(netid);
        }
        return packet;
    }

    public List<int> PlayerIds = new(32);

    public void Deserialize(NetDataReader reader)
    {
        ushort amount = reader.GetUShort();
        if (amount > 0)
        {
            for (int i = 0; i < amount; i++)
            {
                PlayerIds.Add(reader.GetInt());
            }
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((ushort)PlayerIds.Count);
        foreach (int netId in PlayerIds)
        {
            writer.Put(netId);
        }
    }

    public void Execute(FikaPlayer player = null)
    {
        if (!FikaBackendUtils.IsClient)
        {
            FikaGlobals.LogError("Received CharacterSyncPacket as server");
            return;
        }

        if (Singleton<FikaClient>.Instantiated)
        {
            Singleton<FikaClient>.Instance.OnCharacterSyncPacketReceived(this);
        }
    }

    public void Dispose()
    {
        PlayerIds.Clear();
    }
}
