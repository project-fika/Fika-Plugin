using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;
using RecorderHandsController = EFT.Player.RecorderHandsController;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public sealed class RecorderActionPacket : IPoolSubPacket
{
    private RecorderActionPacket()
    {

    }

    public static RecorderActionPacket CreateInstance()
    {
        return new();
    }

    public static RecorderActionPacket FromValue(MongoID recorderId, ERecorderHandsAction action)
    {
        var packet = CommonSubPacketPoolManager.Instance.GetPacket<RecorderActionPacket>(ECommonSubPacketType.RecorderAction);
        packet.RecorderId = recorderId;
        packet.Action = action;
        return packet;
    }

    public static void Send(FikaPlayer player, MongoID recorderId, RecorderHandler handler)
    {
        if (handler == null)
        {
            return;
        }

        player.CommonPacket.Type = ECommonSubPacketType.RecorderAction;
        player.CommonPacket.SubPacket = FromValue(recorderId, handler.CurrentAction);
        player.PacketSender.NetworkManager.SendNetReusable(ref player.CommonPacket, DeliveryMethod.ReliableOrdered, true);
    }

    public MongoID RecorderId;
    public ERecorderHandsAction Action;

    public void Execute(FikaPlayer player)
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

    public void Serialize(NetDataWriter writer)
    {
        writer.PutMongoID(RecorderId);
        writer.PutEnum(Action);
    }

    public void Deserialize(NetDataReader reader)
    {
        RecorderId = reader.GetMongoID();
        Action = reader.GetEnum<ERecorderHandsAction>();
    }

    public void Dispose()
    {
        RecorderId = default;
        Action = default;
    }
}
