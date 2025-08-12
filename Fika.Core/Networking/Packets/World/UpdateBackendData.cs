using Comfort.Common;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.World;

public class UpdateBackendData : IPoolSubPacket
{
    public int PlayerAmount;

    private UpdateBackendData() { }

    public static UpdateBackendData CreateInstance()
    {
        return new();
    }

    public static UpdateBackendData FromValue(int playerAmount)
    {
        UpdateBackendData packet = GenericSubPacketPoolManager.Instance.GetPacket<UpdateBackendData>(EGenericSubPacketType.UpdateBackendData);
        packet.PlayerAmount = playerAmount;
        return packet;
    }

    public void Execute(FikaPlayer player = null)
    {
        Singleton<IFikaNetworkManager>.Instance.PlayerAmount = PlayerAmount;
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
