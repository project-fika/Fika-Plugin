using EFT.GlobalEvents;
using EFT.Interactive.SecretExfiltrations;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public readonly struct SecretExfilFoundPacket : IGenericPacket
{
    public EGenericPacketType Type => EGenericPacketType.SecretExfilFound;

    public SecretExfilFoundPacket(string groupId, string exitName)
    {
        GroupId = groupId;
        ExitName = exitName;
    }

    public SecretExfilFoundPacket(NetDataReader reader)
    {
        GroupId = reader.GetString();
        ExitName = reader.GetString();
    }

    public readonly string GroupId;
    public readonly string ExitName;

    public readonly void Execute()
    {
        GlobalEventsController.Instance
            .CreateCommonEvent<SecretExfiltrationPointFoundShareEvent>()
            .Invoke(GroupId, GroupId, ExitName);
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(GroupId);
        writer.Put(ExitName);
    }
}
