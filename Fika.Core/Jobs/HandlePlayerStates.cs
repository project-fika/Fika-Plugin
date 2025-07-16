using Comfort.Common;
using Fika.Core.Main.Players;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.Player;
using Unity.Collections;
using Unity.Jobs;

namespace Fika.Core.Jobs
{
    internal readonly struct HandlePlayerStates(double networkTime, NativeArray<PlayerStatePacket> snapshots) : IJobFor
    {
        [ReadOnly]
        private readonly double _networkTime = networkTime;
        [ReadOnly]
        private readonly NativeArray<PlayerStatePacket> _snapshots = snapshots;

        public readonly void Execute(int index)
        {
            IFikaNetworkManager manager = Singleton<IFikaNetworkManager>.Instance;
            PlayerStatePacket packet = _snapshots[index];
            if (manager.CoopHandler.Players.TryGetValue(packet.NetId, out FikaPlayer player))
            {
                player.Snapshotter.Insert(ref packet, _networkTime);
            }
        }
    }
}
