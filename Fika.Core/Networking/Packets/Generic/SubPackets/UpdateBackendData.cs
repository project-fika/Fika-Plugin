using Comfort.Common;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;
using Fika.Core.Main.Utils;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public sealed class UpdateBackendData : IPoolSubPacket
{
    public int PlayerAmount;

    private UpdateBackendData() { }

    public static UpdateBackendData CreateInstance()
    {
        return new();
    }

    public static UpdateBackendData FromValue(int playerAmount)
    {
        var packet = GenericSubPacketPoolManager.Instance.GetPacket<UpdateBackendData>(EGenericSubPacketType.UpdateBackendData);
        packet.PlayerAmount = playerAmount;
        return packet;
    }

    public void Execute(FikaPlayer player = null)
    {
        FikaGlobals.NetworkManager.PlayerAmount = PlayerAmount;
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(PlayerAmount);
    }

    public void Deserialize(NetDataReader reader)
    {
        PlayerAmount = reader.GetInt();
    }

    public void Dispose()
    {
        PlayerAmount = 0;
    }
}
