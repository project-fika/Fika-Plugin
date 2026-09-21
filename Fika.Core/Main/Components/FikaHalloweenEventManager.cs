using System;
using BepInEx.Logging;
using Comfort.Common;
using EFT.GlobalEvents;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.World;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.Components;

internal class FikaHalloweenEventManager : MonoBehaviour
{
    public FikaHalloweenEventManager(IntPtr pointer) : base(pointer)
    {
    }

    private ManualLogSource _logger;

    private Il2CppSystem.Action _summonStartedAction;
    private Il2CppSystem.Action _syncStateEvent;
    private Il2CppSystem.Action _syncExitsEvent;

    private FikaServer _server;

    protected void Awake()
    {
        _logger = BepInEx.Logging.Logger.CreateLogSource("CoopHalloweenEventManager");
    }

    protected void Start()
    {
        _logger.LogInfo("Initializing CoopHalloweenEventManager");

        _server = Singleton<FikaServer>.Instance;

        _summonStartedAction = GlobalEventsController.Instance.SubscribeOnEvent<HalloweenSummonStartedEvent>(new System.Action<EFT.GlobalEvents.HalloweenSummonStartedEvent>(OnHalloweenSummonStarted));
        _syncStateEvent = GlobalEventsController.Instance.SubscribeOnEvent<HalloweenSyncStateEvent>(new System.Action<EFT.GlobalEvents.HalloweenSyncStateEvent>(OnHalloweenSyncStateEvent));
        _syncExitsEvent = GlobalEventsController.Instance.SubscribeOnEvent<HalloweenSyncExitsEvent>(new System.Action<EFT.GlobalEvents.HalloweenSyncExitsEvent>(OnHalloweenSyncExitsEvent));
    }

    protected void OnDestroy()
    {
        _logger.LogInfo("Destroying CoopHalloweenEventManager");

        _summonStartedAction?.Invoke();
        _syncStateEvent?.Invoke();
        _syncExitsEvent?.Invoke();

        _summonStartedAction = null;
        _syncStateEvent = null;
        _syncExitsEvent = null;
    }

    private void OnHalloweenSummonStarted(HalloweenSummonStartedEvent summonStartedEvent)
    {
#if DEBUG
        _logger.LogWarning("OnHalloweenSummonStarted");
#endif

        HalloweenEventPacket packet = new()
        {
            PacketType = EHalloweenPacketType.Summon,
            SyncEvent = summonStartedEvent
        };

        _server.SendData(ref packet, DeliveryMethod.ReliableOrdered);
    }

    private void OnHalloweenSyncStateEvent(HalloweenSyncStateEvent syncStateEvent)
    {
#if DEBUG
        _logger.LogWarning("OnHalloweenSyncStateEvent");
#endif

        HalloweenEventPacket packet = new()
        {
            PacketType = EHalloweenPacketType.Sync,
            SyncEvent = syncStateEvent
        };

        _server.SendData(ref packet, DeliveryMethod.ReliableOrdered);
    }

    private void OnHalloweenSyncExitsEvent(HalloweenSyncExitsEvent syncStateEvent)
    {
#if DEBUG
        _logger.LogWarning("OnHalloweenSyncExitsEvent");
#endif

        HalloweenEventPacket packet = new()
        {
            PacketType = EHalloweenPacketType.Exit,
            SyncEvent = syncStateEvent
        };

        _server.SendData(ref packet, DeliveryMethod.ReliableOrdered);
    }
}
