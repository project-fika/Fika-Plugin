using Comfort.Common;
using Fika.Core.Networking;
using Unity.Collections;
using Unity.Jobs;

namespace Fika.Core.Jobs
{
    internal struct UpdateInterpolators(float unscaledDeltaTime) : IJobParallelFor
    {
        [ReadOnly]
        public readonly float _unscaledDeltaTime = unscaledDeltaTime;

        public readonly void Execute(int index)
        {
            Singleton<IFikaNetworkManager>.Instance.ObservedCoopPlayers[index]
                .Snapshotter.ManualUpdate(_unscaledDeltaTime);
        }
    }
}
