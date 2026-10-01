using EFT;
using EFT.NetworkPackets;
using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public readonly struct ProceedPacket : IPlayerPacket
{
    public EPlayerPacketType Type => EPlayerPacketType.Proceed;

    public ProceedPacket(OneAndList<EBodyPart> bodyParts, MongoID itemId, float amount, int animationVariant, EProceedType proceedType, bool scheduled)
    {
        BodyParts = bodyParts;
        ItemId = itemId;
        Amount = amount;
        AnimationVariant = animationVariant;
        ProceedType = proceedType;
        Scheduled = scheduled;
    }

    public ProceedPacket(NetDataReader reader)
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

    public readonly OneAndList<EBodyPart> BodyParts;
    public readonly MongoID ItemId;
    public readonly float Amount;
    public readonly int AnimationVariant;
    public readonly EProceedType ProceedType;
    public readonly bool Scheduled;

    public readonly void Execute(FikaPlayer player)
    {
        if (player is ObservedPlayer observedPlayer)
        {
            observedPlayer.HandleProceedPacket(this);
        }
    }

    public readonly void Serialize(NetDataWriter writer)
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
}
