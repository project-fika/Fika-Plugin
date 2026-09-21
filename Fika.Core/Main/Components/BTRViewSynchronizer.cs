using Comfort.Common;
using EFT.Vehicle;
using Fika.Core.Main.Custom;
using Fika.Core.Networking;
using System;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.Components;

/// <summary>
/// Adds the <see cref="BTRViewSynchronizer"/> to the <see cref="BTRView"/> GameObject so that the position is synced
/// </summary>
internal class BTRViewSynchronizer : ThrottledMono
{
    public BTRViewSynchronizer(IntPtr pointer) : base(pointer)
    {
    }

    private FikaServer _server;

    public override float UpdateRate
    {
        get
        {
            return 20f;
        }
    }

    public static void CreateInstance(BTRView btrView)
    {
        var syncComp = btrView.gameObject.AddComponent<BTRViewSynchronizer>();
        syncComp._server = Singleton<FikaServer>.Instance;
    }

    public override void Tick()
    {
        if (_server != null)
        {
            var packet = BtrController.Instance._offlineSyncPacket;
            _server.SendBTRPacket(ref packet);
        }
    }
}
