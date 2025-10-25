using Comfort.Common;
using Fika.Core.Networking;
using Unity.Collections;
using Unity.Jobs;

namespace Fika.Core.Jobs;

internal readonly struct UpdateInterpolators(float unscaledDeltaTime) : IJobFor
{
    [ReadOnly]
    public readonly float _unscaledDeltaTime = unscaledDeltaTime;

    public readonly void Execute(int index)
    {
        var netManager = Singleton<IFikaNetworkManager>.Instance;
        if (netManager != null)
        {
            var players = netManager.ObservedPlayers;
            if ((uint)index < (uint)players.Count) // single unsigned bounds check
            {
                var player = players[index];
                player?.Snapshotter.ManualUpdate(_unscaledDeltaTime);
            }
        }
    }
}
