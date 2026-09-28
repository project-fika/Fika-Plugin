using System;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public readonly struct QuickReloadMagPacket : IFirearmPacket
{
    public QuickReloadMagPacket(MongoID magId, bool reload)
    {
        MagId = magId;
        Reload = reload;
    }

    public QuickReloadMagPacket(NetDataReader reader)
    {
        Reload = reader.GetBool();
        if (Reload)
        {
            MagId = reader.GetMongoID();
        }
    }

    public readonly MongoID MagId;
    public readonly bool Reload;

    public readonly void Execute(FikaPlayer player)
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
                if (result.Value is Magazine magazine)
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

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(Reload);
        if (Reload)
        {
            writer.PutMongoID(MagId);
        }
    }

    public EFirearmPacketType Type => EFirearmPacketType.QuickReloadMag;
}
