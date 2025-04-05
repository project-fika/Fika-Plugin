﻿using Comfort.Common;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using Unity.Jobs;

namespace Fika.Core.Jobs
{
    internal struct UpdateInterpolators : IJobParallelFor
    {
        public void Execute(int index)
        {
            IFikaNetworkManager manager = Singleton<IFikaNetworkManager>.Instance;
            Singleton<IFikaNetworkManager>.Instance.ObservedCoopPlayers[index].Snapshotter.ManualUpdate();
        }
    }
}
