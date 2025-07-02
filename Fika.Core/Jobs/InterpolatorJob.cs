using Comfort.Common;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using Unity.Collections;
using Unity.Jobs;

namespace Fika.Core.Jobs
{
    public readonly struct InterpolatorJob(float unscaledDeltaTime, double networkTime, NativeArray<PlayerStatePacket> snapshots) : IJob
    {
        [ReadOnly]
        public readonly float _unscaledDeltaTime = unscaledDeltaTime;
        [ReadOnly]
        private readonly double _networkTime = networkTime;
        [ReadOnly]
        private readonly NativeArray<PlayerStatePacket> _snapshots = snapshots;

        public void Execute()
        {
            int snapLength = _snapshots.Length;
            for (int i = 0; i < snapLength; i++)
            {
                IFikaNetworkManager manager = Singleton<IFikaNetworkManager>.Instance;
                PlayerStatePacket packet = _snapshots[i];
                if (manager.CoopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer player))
                {
                    player.Snapshotter.Insert(ref packet, _networkTime);
                }
            }

            IFikaNetworkManager netmanager = Singleton<IFikaNetworkManager>.Instance;
            int amount = netmanager.ObservedCoopPlayers.Count;
            for (int i = 0; i < amount; i++)
            {
                Singleton<IFikaNetworkManager>.Instance.ObservedCoopPlayers[i].Snapshotter
                    .ManualUpdate(_unscaledDeltaTime);
            }
        }
    }
}
