using System.Linq;
using Comfort.Common;
using EFT;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public readonly struct MineEventPacket(Vector3 minePosition) : IGenericPacket
{
    public EGenericPacketType Type => EGenericPacketType.MineEvent;

    public readonly Vector3 MinePosition = minePosition;

    public readonly void Execute()
    {
        if (Singleton<GameWorld>.Instance.MineManager != null)
        {
            NetworkGame<EftGamePlayerOwner>.CG_ParseMineExplosionData mineSeeker = new()
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

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.PutUnmanaged(MinePosition);
    }
}
