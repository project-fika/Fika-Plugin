using System;
using EFT;
using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public sealed class QuickReloadMagPacket : IPoolSubPacket
{
    private QuickReloadMagPacket()
    {

    }

    public static QuickReloadMagPacket FromValue(MongoID magId, bool reload)
    {
        var packet = FirearmSubPacketPoolManager.Instance.GetPacket<QuickReloadMagPacket>(EFirearmSubPacketType.QuickReloadMag);
        packet.MagId = magId;
        packet.Reload = reload;
        return packet;
    }

    public static QuickReloadMagPacket CreateInstance()
    {
        return new();
    }

    public MongoID MagId;
    public bool Reload;

    public void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            try
            {
                var result = player.FindItemById(MagId);
                if (!result.Succeeded)
                {
                    FikaGlobals.LogError(result.Error);
                    return;
                }
                if (result.Value is MagazineItemClass magazine)
                {
                    controller.FastForwardCurrentState();
                    controller.QuickReloadMag(magazine, null);
                }
                else
                {
                    FikaGlobals.LogError($"QuickReloadMagPacket: item was not of type MagazineClass, was {result.Value.GetType()}");
                }
            }
            catch (Exception ex)
            {
                FikaGlobals.LogError(ex);
                FikaGlobals.LogError($"QuickReloadMagPacket: There is no item {MagId} in profile {player.ProfileId}");
                throw;
            }
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Reload);
        if (Reload)
        {
            writer.PutMongoID(MagId);
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        Reload = reader.GetBool();
        if (Reload)
        {
            MagId = reader.GetMongoID();
        }
    }

    public void Dispose()
    {
        MagId = default;
        Reload = false;
    }
}
