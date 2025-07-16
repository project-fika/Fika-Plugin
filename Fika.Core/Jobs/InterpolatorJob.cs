using Comfort.Common;
using Fika.Core.Main.Players;
using Fika.Core.Networking;
using Unity.Collections;
using Unity.Jobs;

namespace Fika.Core.Jobs
{
    internal readonly struct InterpolatorJob(float unscaledDeltaTime, double networkTime, NativeArray<PlayerStatePacket> snapshots, int amount) : IJob
    {
        [ReadOnly]
        public readonly float _unscaledDeltaTime = unscaledDeltaTime;
        [ReadOnly]
        private readonly double _networkTime = networkTime;
        [ReadOnly]
        private readonly NativeArray<PlayerStatePacket> _snapshots = snapshots;
        [ReadOnly]
        private readonly int _amount = amount;

        public void Execute()
        {
            IFikaNetworkManager netManager = Singleton<IFikaNetworkManager>.Instance;
            for (int i = 0; i < _amount; i++)
            {                
                PlayerStatePacket packet = _snapshots[i];
                if (netManager.CoopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer player))
                {
                    player.Snapshotter.Insert(ref packet, _networkTime);
                }
            }

            int amount = netManager.ObservedCoopPlayers.Count;
            for (int i = 0; i < amount; i++)
            {
                Singleton<IFikaNetworkManager>.Instance.ObservedCoopPlayers[i].Snapshotter
                    .ManualUpdate(_unscaledDeltaTime);
            }
        }
    }
}
