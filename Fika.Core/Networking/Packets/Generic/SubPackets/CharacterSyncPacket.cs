using System.Collections.Generic;
using Comfort.Common;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public sealed class CharacterSyncPacket : IPoolSubPacket
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
        var packet = GenericSubPacketPoolManager.Instance.GetPacket<CharacterSyncPacket>(EGenericSubPacketType.CharacterSync);
        foreach ((var netid, _) in players)
        {
            packet.PlayerIds.Add(netid);
        }
        return packet;
    }

    public List<int> PlayerIds = new(32);

    public void Deserialize(NetDataReader reader)
    {
        var amount = reader.GetUShort();
        if (amount > 0)
        {
            for (var i = 0; i < amount; i++)
            {
                PlayerIds.Add(reader.GetInt());
            }
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((ushort)PlayerIds.Count);
        foreach (var netId in PlayerIds)
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
