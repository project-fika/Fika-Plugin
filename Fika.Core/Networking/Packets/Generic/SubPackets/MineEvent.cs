using System.Linq;
using Comfort.Common;
using EFT;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public sealed class MineEvent : IPoolSubPacket
{
    public Vector3 MinePosition;

    private MineEvent() { }

    public static MineEvent CreateInstance()
    {
        return new MineEvent();
    }

    public static MineEvent FromValue(Vector3 minePosition)
    {
        var packet = GenericSubPacketPoolManager.Instance.GetPacket<MineEvent>(EGenericSubPacketType.Mine);
        packet.MinePosition = minePosition;
        return packet;
    }

    public void Execute(FikaPlayer player = null)
    {
        if (Singleton<GameWorld>.Instance.MineManager != null)
        {
            NetworkGame<EftGamePlayerOwner>.Class1656 mineSeeker = new()
            {
                minePosition = MinePosition
            };
            var mineDirectional = Singleton<GameWorld>.Instance.MineManager.Mines.FirstOrDefault(mineSeeker.method_0);
            if (mineDirectional == null)
            {
                return;
            }
            mineDirectional.Explosion();
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.PutUnmanaged(MinePosition);
    }

    public void Deserialize(NetDataReader reader)
    {
        MinePosition = reader.GetUnmanaged<Vector3>();
    }

    public void Dispose()
    {
        MinePosition = default;
    }
}
