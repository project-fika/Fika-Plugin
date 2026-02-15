/*using Comfort.Common;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.Player;
using Unity.Collections;
using Unity.Jobs;

namespace Fika.Core.Jobs;

internal readonly struct HandlePlayerStates(double networkTime) : IJobFor
{
    [ReadOnly]
    private readonly double _networkTime = networkTime;

    public readonly void Execute(int index)
    {
        var manager = Singleton<IFikaNetworkManager>.Instance;
        var buffer = PlayerSnapshots.Snapshots[index];
        var packet = PlayerStatePacket.FromBuffer(in buffer);
        if (manager.CoopHandler.Players.TryGetValue(packet.NetId, out var player))
        {
            player.Snapshotter.Insert(packet, _networkTime);
        }
    }
}
*/