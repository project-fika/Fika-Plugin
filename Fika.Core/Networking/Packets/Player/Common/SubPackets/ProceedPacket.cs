using EFT;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public sealed class ProceedPacket : IPoolSubPacket
{
    private ProceedPacket()
    {

    }

    public static ProceedPacket CreateInstance()
    {
        return new();
    }

    public static ProceedPacket FromValue(GStruct382<EBodyPart> bodyParts, MongoID itemId, float amount, int animationVariant, EProceedType proceedType, bool scheduled)
    {
        var packet = CommonSubPacketPoolManager.Instance.GetPacket<ProceedPacket>(ECommonSubPacketType.Proceed);
        packet.BodyParts = bodyParts;
        packet.ItemId = itemId;
        packet.Amount = amount;
        packet.AnimationVariant = animationVariant;
        packet.ProceedType = proceedType;
        packet.Scheduled = scheduled;
        return packet;
    }

    public GStruct382<EBodyPart> BodyParts;
    public MongoID ItemId;
    public float Amount;
    public int AnimationVariant;
    public EProceedType ProceedType;
    public bool Scheduled;

    public void Execute(FikaPlayer player)
    {
        if (player is ObservedPlayer observedPlayer)
        {
            observedPlayer.HandleProceedPacket(this);
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.PutEnum(ProceedType);
        if (ProceedType is not EProceedType.EmptyHands)
        {
            writer.PutMongoID(ItemId);
        }
        else
        {
            writer.Put(Scheduled);
        }
        if (ProceedType is EProceedType.FoodClass or EProceedType.MedsClass)
        {
            writer.Put(Amount);
            writer.Put(AnimationVariant);
            if (ProceedType is EProceedType.MedsClass)
            {
                var bodyPartsAmount = BodyParts.Length;
                writer.Put(bodyPartsAmount);
                for (var i = 0; i < bodyPartsAmount; i++)
                {
                    writer.PutEnum(BodyParts[i]);
                }
            }
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        ProceedType = reader.GetEnum<EProceedType>();
        if (ProceedType is not EProceedType.EmptyHands)
        {
            ItemId = reader.GetMongoID();
        }
        else
        {
            Scheduled = reader.GetBool();
        }
        if (ProceedType is EProceedType.FoodClass or EProceedType.MedsClass)
        {
            Amount = reader.GetFloat();
            AnimationVariant = reader.GetInt();
            if (ProceedType is EProceedType.MedsClass)
            {
                var bodyPartsAmount = reader.GetInt();
                for (var i = 0; i < bodyPartsAmount; i++)
                {
                    BodyParts.Add(reader.GetEnum<EBodyPart>());
                }
            }
        }
    }

    public void Dispose()
    {
        BodyParts = default;
        ItemId = default;
        Amount = 0f;
        AnimationVariant = 0;
        ProceedType = default;
        Scheduled = false;
    }
}
