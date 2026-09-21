using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using RecorderHandsController = EFT.Player.RecorderHandsController;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public readonly struct RecorderActionPacket : IPlayerPacket
{
    public EPlayerPacketType Type => EPlayerPacketType.RecorderAction;

    public RecorderActionPacket(MongoID recorderId, ERecorderHandsAction action)
    {
        RecorderId = recorderId;
        Action = action;
    }

    public RecorderActionPacket(NetDataReader reader)
    {
        RecorderId = reader.GetMongoID();
        Action = reader.GetEnum<ERecorderHandsAction>();
    }

    public readonly MongoID RecorderId;
    public readonly ERecorderHandsAction Action;

    public static void Send(FikaPlayer player, MongoID recorderId, RecorderHandler handler)
    {
        if (handler == null)
        {
            return;
        }

        var packet = new RecorderActionPacket(recorderId, handler.CurrentAction);
        player.PacketSender.NetworkManager.SendPlayerPacket(in packet, player.NetId, DeliveryMethod.ReliableOrdered, true);
    }

    public readonly void Execute(FikaPlayer player)
    {
        if (player is not ObservedPlayer || player.HandsController is not RecorderHandsController controller)
        {
            return;
        }

        var recorder = controller._recorder;
        if (recorder == null || recorder.Id != RecorderId)
        {
            return;
        }

        var handler = controller._recorderHandler;
        if (handler == null)
        {
            return;
        }

        var animator = controller.FirearmsAnimator;
        if (animator == null)
        {
            return;
        }

        handler.CurrentAction = Action;
        animator.SetMagInWeapon(handler.InsertTape);
        animator.SetMagOutWeapon(handler.EjectTape);
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.PutMongoID(RecorderId);
        writer.PutEnum(Action);
    }
}
