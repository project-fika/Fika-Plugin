using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;
using Fika.Core.Networking.LiteNetLib.Utils;
using Fika.Core.Networking.Pooling;
using System;

namespace Fika.Core.Networking.Packets.FirearmController;

public class ReloadMagPacket : IPoolSubPacket
{
    private ReloadMagPacket()
    {

    }

    public static ReloadMagPacket FromValue(MongoID magId, byte[] locationDescription, bool reload)
    {
        ReloadMagPacket packet = FirearmSubPacketPoolManager.Instance.GetPacket<ReloadMagPacket>(EFirearmSubPacketType.ReloadMag);
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
                GStruct461<Item> result = player.FindItemById(MagId);
                if (!result.Succeeded)
                {
                    FikaPlugin.Instance.FikaLogger.LogError(result.Error);
                    return;
                }
                if (result.Value is MagazineItemClass magazineClass)
                {
                    magazine = magazineClass;
                }
                else
                {
                    FikaPlugin.Instance.FikaLogger.LogError($"ReloadMagPacket: Item was not MagazineClass, it was {result.Value.GetType()}");
                }
            }
            catch (Exception ex)
            {
                FikaPlugin.Instance.FikaLogger.LogError(ex);
                FikaPlugin.Instance.FikaLogger.LogError($"ReloadMagPacket: There is no item {MagId} in profile {player.ProfileId}");
                throw;
            }
            ItemAddress gridItemAddress = null;
            if (LocationDescription != null)
            {
                try
                {
                    using GClass1278 eftReader = PacketToEFTReaderAbstractClass.Get(LocationDescription);
                    if (LocationDescription.Length != 0)
                    {
                        GClass1785 descriptor = eftReader.ReadPolymorph<GClass1785>();
                        gridItemAddress = player.InventoryController.ToItemAddress(descriptor);
                    }
                }
                catch (GException4 exception2)
                {
                    FikaPlugin.Instance.FikaLogger.LogError(exception2);
                }
            }
            if (magazine != null)
            {
                controller.FastForwardCurrentState();
                controller.ReloadMag(magazine, gridItemAddress, null);
            }
            else
            {
                FikaPlugin.Instance.FikaLogger.LogError($"ReloadMagPacket: final variables were null! Mag: {magazine}, Address: {gridItemAddress}");
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
