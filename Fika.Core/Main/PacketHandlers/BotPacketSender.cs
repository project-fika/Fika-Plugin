// © 2026 Lacyway All Rights Reserved

using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using Fika.Core.Main.Players;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.Player;

namespace Fika.Core.Main.PacketHandlers;

public class BotPacketSender : MonoBehaviour, IPacketSender
{
    public bool SendState { get; set; }
    public IFikaNetworkManager NetworkManager { get; set; }

    private FikaPlayer _player;
    private bool _sendPackets;
    private int _animHash;
    private bool IsMoving
    {
        get
        {
            return _player.MovementContext.PlayerAnimator.Animator.GetBool(_animHash);
        }
    }

    public static Task<BotPacketSender> Create(FikaBot bot)
    {
        var sender = bot.gameObject.AddComponent<BotPacketSender>();
        sender._player = bot;
        sender.NetworkManager = Singleton<FikaServer>.Instance;
        sender._animHash = PlayerAnimator.INERT_PARAM_HASH;
        sender.SendState = true;
        return Task.FromResult(sender);
    }

    public void Init()
    {

    }

    public void OnEnable()
    {
        _sendPackets = true;
    }

    public void OnDisable()
    {
        _sendPackets = false;
    }

    public void SendPlayerState()
    {
        if (!_sendPackets)
        {
            return;
        }

        var state = new PlayerStatePacket(_player, IsMoving);
        NetworkManager.SendPlayerState(ref state);
    }

    public bool WriteState(NetDataWriter writer)
    {
        if (!_sendPackets)
        {
            return false;
        }

        var state = new PlayerStatePacket(_player, IsMoving);
        writer.PutUnmanaged(state);
        return true;
    }

    public void DestroyThis()
    {
        NetworkManager = null;
        Destroy(this);
    }
}
