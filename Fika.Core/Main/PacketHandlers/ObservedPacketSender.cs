// © 2026 Lacyway All Rights Reserved

using Comfort.Common;
using Fika.Core.Networking;
using System;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.PacketHandlers;

public sealed class ObservedPacketSender : MonoBehaviour, IPacketSender
{
    public ObservedPacketSender(IntPtr pointer) : base(pointer)
    {
    }

    public bool SendState { get; set; }
    public IFikaNetworkManager NetworkManager { get; set; }

    private void Awake()
    {
        NetworkManager = FikaGlobals.NetworkManager;
    }

    public void Init()
    {

    }

    public void DestroyThis()
    {
        NetworkManager = null;
        Destroy(this);
    }
}
