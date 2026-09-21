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
            MineDirectional mineDirectional = null;
            foreach (var mine in Singleton<GameWorld>.Instance.MineManager.Mines)
            {
                if (Vector3.Distance(mine.transform.position, MinePosition) < Mathf.Epsilon)
                {
                    mineDirectional = mine;
                    break;
                }
            }
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
