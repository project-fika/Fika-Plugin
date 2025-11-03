using Comfort.Common;
using EFT.Vehicle;
using Fika.Core.Main.Custom;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;

namespace Fika.Core.Main.Components;

/// <summary>
/// Adds the <see cref="BTRViewSynchronizer"/> to the <see cref="BTRView"/> GameObject so that the position is synced
/// </summary>
internal class BTRViewSynchronizer : ThrottledMono
{
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
            FikaGlobals.LogInfo("Sending packet");
            _server.SendBTRPacket(ref BTRControllerClass.Instance.BTRDataPacketStruct);
        }
    }
}
