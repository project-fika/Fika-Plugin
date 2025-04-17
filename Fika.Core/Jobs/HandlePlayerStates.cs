using Comfort.Common;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using Unity.Jobs;

namespace Fika.Core.Jobs
{
    internal struct HandlePlayerStates(double networkTime) : IJobParallelFor
    {
        private readonly double _networkTime = networkTime;

        public void Execute(int index)
        {
            IFikaNetworkManager manager = Singleton<IFikaNetworkManager>.Instance;
            PlayerStatePacket packet = manager.Snapshots[index];
            if (manager.CoopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer player))
            {
                player.Snapshotter.Insert(packet, _networkTime);
            }
        }
    }
}
