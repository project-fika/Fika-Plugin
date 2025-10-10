using Comfort.Common;
using Fika.Core.Main.Players;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.Player;
using System;
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
        IFikaNetworkManager netManager = Singleton<IFikaNetworkManager>.Instance;
        for (int i = 0; i < _amount; i++)
        {
            ArraySegment<byte> buffer = PlayerSnapshots.Snapshots[i];
            PlayerStatePacket packet = PlayerStatePacket.FromBuffer(in buffer);
            if (netManager.CoopHandler.Players.TryGetValue(packet.NetId, out FikaPlayer player))
            {
                player.Snapshotter.Insert(ref packet, _networkTime);
            }
        }

        int amount = netManager.ObservedCoopPlayers.Count;
        for (int i = 0; i < amount; i++)
        {
            netManager.ObservedCoopPlayers[i].Snapshotter
                .ManualUpdate(_unscaledDeltaTime);
        }
    }
}
