using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EFT;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Players;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.Player;
using Fika.Core.Networking.Snapshotting;

namespace Fika.Core.Main.Components;

public sealed class BotStateManager : MonoBehaviour
{
    private List<FikaBot> _bots;
    private HostGameController _controller;
    private BotsController _botsController;
    private FikaServer _server;
    private NetDataWriter _writer;

    private float _updateCount;
    private float _updatesPerTick;
    private uint _writtenPackets;
    private bool _timeWritten;
    private readonly byte _maxSize = PlayerStateData.PacketSize;

    public void AddBot(FikaBot bot)
    {
        if (_bots.Contains(bot))
        {
            return;
        }

        _bots.Add(bot);
    }

    public bool RemoveBot(FikaBot bot)
    {
        return _bots.Remove(bot);
    }

    public static BotStateManager Create(AbstractGame game, FikaServer server, HostGameController hostGameController)
    {
        var component = game.gameObject.AddComponent<BotStateManager>();
        component._controller = hostGameController;
        component._updateCount = 0;
        component._updatesPerTick = 1f / server.SendRate;
        component._bots = [];
        component._server = server;
        component._writer = new NetDataWriter(true, 1024);
        return component;
    }

    private void Update()
    {
        _controller.Update?.Invoke();
        _botsController?.method_0();

        _updateCount += Time.unscaledDeltaTime;
        if (_updateCount >= _updatesPerTick)
        {
            SendBatchStates();
            _updateCount -= _updatesPerTick;
        }
    }

    private void SendBatchStates()
    {
        CheckAndWriteNetworkTime();

        var count = _bots.Count;
        for (var i = count - 1; i >= 0; i--)
        {
            var bot = _bots[i];
            if (!bot.HealthController.IsAlive)
            {
                _bots.RemoveAt(i);
                continue;
            }

            if ((_writer.Length + _maxSize) > _server.MaxMTU)
            {
                SendAndReset();
                CheckAndWriteNetworkTime();
            }

            if (bot.BotPacketSender.WriteState(_writer))
            {
                _writtenPackets++;
            }
        }

        SendAndReset();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckAndWriteNetworkTime()
    {
        if (!_timeWritten)
        {
            _writer.Put(NetworkTimeSync.NetworkTime);
            _timeWritten = true;
        }
    }

    private void SendAndReset()
    {
        if (_writtenPackets != 0)
        {
            _server.BatchSendStates(_writer);
            _writtenPackets = 0;
            _timeWritten = false;
            _writer.Reset();
        }
    }

    private void OnDestroy()
    {
        _bots.Clear();
    }

    public void AssignBotsController(BotsController botsController)
    {
        _botsController = botsController;
    }

    public void UnassignBotsController()
    {
        _botsController = null;
    }
}
