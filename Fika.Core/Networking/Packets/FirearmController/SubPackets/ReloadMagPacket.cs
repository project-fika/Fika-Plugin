using System;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public sealed class ReloadMagPacket : IPoolSubPacket
{
    private ReloadMagPacket()
    {

    }

    public static ReloadMagPacket FromValue(MongoID magId, byte[] locationDescription, bool reload)
    {
        var packet = FirearmSubPacketPoolManager.Instance.GetPacket<ReloadMagPacket>(EFirearmSubPacketType.ReloadMag);
        packet.MagId = magId;
        packet.LocationDescription = locationDescription;
        packet.Reload = reload;
        return packet;
    }

    public static ReloadMagPacket CreateInstance()
    {
        return new();
    }

    public MongoID MagId;
    public byte[] LocationDescription;
    public bool Reload;

    public void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            MagazineItemClass magazine = null;
            try
            {
                var result = player.FindItemById(MagId);
                if (!result.Succeeded)
                {
                    FikaGlobals.LogError(result.Error.ToString());
                    return;
                }
                if (result.Value is MagazineItemClass magazineClass)
                {
                    magazine = magazineClass;
                }
                else
                {
                    FikaGlobals.LogError($"ReloadMagPacket: Item was not MagazineClass, it was {result.Value.GetType()}");
                }
            }
            catch (Exception ex)
            {
                FikaGlobals.LogError(ex);
                FikaGlobals.LogError($"ReloadMagPacket: There is no item {MagId} in profile {player.ProfileId}");
                throw;
            }
            ItemAddress gridItemAddress = null;
            if (LocationDescription != null)
            {
                try
                {
                    using var eftReader = PacketToEFTReaderAbstractClass.Get(LocationDescription);
                    if (LocationDescription.Length != 0)
                    {
                        var descriptor = eftReader.ReadPolymorph<GClass1950>();
                        gridItemAddress = player.InventoryController.ToItemAddress(descriptor);
                    }
                }
                catch (GException4 exception2)
                {
                    FikaGlobals.LogError(exception2);
                }
            }
            if (magazine != null)
            {
                controller.FastForwardCurrentState();
                controller.ReloadMag(magazine, gridItemAddress, null);
            }
            else
            {
                FikaGlobals.LogError($"ReloadMagPacket: final variables were null! Mag: {magazine}, Address: {gridItemAddress}");
            }
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Reload);
        if (Reload)
        {
            writer.PutMongoID(MagId);
            writer.PutByteArray(LocationDescription);
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        Reload = reader.GetBool();
        if (Reload)
        {
            MagId = reader.GetMongoID();
            LocationDescription = reader.GetByteArray();
        }
    }

    public void Dispose()
    {
        MagId = default;
        LocationDescription = null;
        Reload = false;
    }
}
