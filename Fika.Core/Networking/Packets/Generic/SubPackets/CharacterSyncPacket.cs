using System.Collections.Generic;
using Comfort.Common;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public readonly struct CharacterSyncPacket : IGenericPacket
{
    public EGenericPacketType Type => EGenericPacketType.CharacterSync;

    public CharacterSyncPacket(Dictionary<int, FikaPlayer> players)
    {
        var playerIds = new int[players.Count];
        players.Keys.CopyTo(playerIds, 0);
        PlayerIds = playerIds;
    }

    public CharacterSyncPacket(NetDataReader reader)
    {
        PlayerIds = reader.GetIntArray();
    }

    public readonly int[] PlayerIds;

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.PutArray(PlayerIds);
    }

    public readonly void Execute()
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
}
