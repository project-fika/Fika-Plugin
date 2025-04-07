using Comfort.Common;
using Fika.Core.Coop.ObservedClasses.Snapshotting;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using Unity.Jobs;

namespace Fika.Core.Jobs
{
    internal struct HandlePlayerStates : IJobParallelFor
    {
        public void Execute(int index)
        {
            IFikaNetworkManager manager = Singleton<IFikaNetworkManager>.Instance;
            PlayerStatePacket packet = manager.Snapshots[index];
            if (manager.CoopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer player))
            {
                player.Snapshotter.Insert((PlayerStatePacket)packet);
            }
        }
    }
}
