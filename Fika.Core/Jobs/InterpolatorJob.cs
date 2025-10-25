using Comfort.Common;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.Player;
using Unity.Collections;
using Unity.Jobs;

namespace Fika.Core.Jobs;

internal readonly struct InterpolatorJob(float unscaledDeltaTime, double networkTime, int amount) : IJob
{
    [ReadOnly]
    public readonly float _unscaledDeltaTime = unscaledDeltaTime;
    [ReadOnly]
    private readonly double _networkTime = networkTime;
    [ReadOnly]
    private readonly int _amount = amount;

    public void Execute()
    {
        var netManager = Singleton<IFikaNetworkManager>.Instance;
        if (netManager != null)
        {
            for (var i = 0; i < _amount; i++)
            {
                var buffer = PlayerSnapshots.Snapshots[i];
                var packet = PlayerStatePacket.FromBuffer(in buffer);
                if (netManager.CoopHandler.Players.TryGetValue(packet.NetId, out var player))
                {
                    player.Snapshotter.Insert(ref packet, _networkTime);
                }
            }

            var amount = netManager.ObservedPlayers.Count;
            for (var i = 0; i < amount; i++)
            {
                netManager.ObservedPlayers[i].Snapshotter
                    .ManualUpdate(_unscaledDeltaTime);
            }
        }
    }
}
