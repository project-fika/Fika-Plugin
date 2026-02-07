using System.Collections.Generic;
using EFT;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Players;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.Player;

namespace Fika.Core.Main.Components;

public class BotStateManager : MonoBehaviour
{
    private List<FikaBot> _bots;
    private HostGameController _controller;
    private BotsController _botsController;
    private FikaServer _server;
    private NetDataWriter _writer;

    private float _updateCount;
    private float _updatesPerTick;
    private byte _writtenPackets;

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

    protected void Update()
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
        for (var i = _bots.Count - 1; i >= 0; i--)
        {
            var bot = _bots[i];
            if (!bot.HealthController.IsAlive)
            {
                _bots.Remove(bot);
                continue;
            }

            if ((_writer.Length + PlayerStatePacket.PacketSize) > _server.MaxMTU)
            {
                SendAndReset();
            }

            if (bot.BotPacketSender.WriteState(_writer))
            {
                _writtenPackets++;
            }
        }
        SendAndReset();
    }

    private void SendAndReset()
    {
        if (_writtenPackets > 0)
        {
            _server.BatchSendStates(_writer, _writtenPackets);
            _writtenPackets = 0;
            _writer.Reset();
        }
    }

    protected void OnDestroy()
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
