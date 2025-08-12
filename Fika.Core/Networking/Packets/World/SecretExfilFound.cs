using EFT.Interactive.SecretExfiltrations;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;
using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets.World;

public class SecretExfilFound : IPoolSubPacket
{
    public string GroupId;
    public string ExitName;

    private SecretExfilFound() { }

    public static SecretExfilFound CreateInstance()
    {
        return new SecretExfilFound();
    }

    public static SecretExfilFound FromValue(string groupId, string exitName)
    {
        SecretExfilFound packet = GenericSubPacketPoolManager.Instance.GetPacket<SecretExfilFound>(EGenericSubPacketType.SecretExfilFound);
        packet.GroupId = groupId;
        packet.ExitName = exitName;
        return packet;
    }

    public void Execute(FikaPlayer player = null)
    {
        GlobalEventHandlerClass.Instance
            .CreateCommonEvent<SecretExfiltrationPointFoundShareEvent>()
            .Invoke(GroupId, GroupId, ExitName);
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(GroupId);
        writer.Put(ExitName);
    }

    public void Deserialize(NetDataReader reader)
    {
        GroupId = reader.GetString();
        ExitName = reader.GetString();
    }

    public void Dispose()
    {
        GroupId = null;
        ExitName = null;
    }
}
