using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;
using System;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public class QuickReloadMagPacket : IPoolSubPacket
{
    private QuickReloadMagPacket()
    {

    }

    public static QuickReloadMagPacket FromValue(MongoID magId, bool reload)
    {
        QuickReloadMagPacket packet = FirearmSubPacketPoolManager.Instance.GetPacket<QuickReloadMagPacket>(EFirearmSubPacketType.QuickReloadMag);
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
                GStruct156<Item> result = player.FindItemById(MagId);
                if (!result.Succeeded)
                {
                    FikaPlugin.Instance.FikaLogger.LogError(result.Error);
                    return;
                }
                if (result.Value is MagazineItemClass magazine)
                {
                    controller.FastForwardCurrentState();
                    controller.QuickReloadMag(magazine, null);
                }
                else
                {
                    FikaPlugin.Instance.FikaLogger.LogError($"QuickReloadMagPacket: item was not of type MagazineClass, was {result.Value.GetType()}");
                }
            }
            catch (Exception ex)
            {
                FikaPlugin.Instance.FikaLogger.LogError(ex);
                FikaPlugin.Instance.FikaLogger.LogError($"QuickReloadMagPacket: There is no item {MagId} in profile {player.ProfileId}");
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
