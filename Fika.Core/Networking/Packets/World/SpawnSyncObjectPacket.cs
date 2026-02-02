using System;
using EFT.SynchronizableObjects;
using static Fika.Core.Networking.Packets.World.SpawnSyncObjectSubPackets;

namespace Fika.Core.Networking.Packets.World;

public class SpawnSyncObjectPacket : INetSerializable
{
    public SynchronizableObjectType ObjectType;
    public ISubPacket SubPacket;

    public void Deserialize(NetDataReader reader)
    {
        ObjectType = reader.GetEnum<SynchronizableObjectType>();
        SubPacket = GetSpawnSyncObjectSubPacket(reader);
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.PutEnum(ObjectType);
        SubPacket.Serialize(writer);
    }

    private ISubPacket GetSpawnSyncObjectSubPacket(NetDataReader reader)
    {
        return ObjectType switch
        {
            SynchronizableObjectType.AirDrop => new SpawnAirdrop(reader),
            //SynchronizableObjectType.AirPlane => new SpawnAirplane(reader),
            SynchronizableObjectType.Tripwire => new SpawnTripwire(reader),
            _ => throw new ArgumentOutOfRangeException(nameof(ObjectType)),
        };
    }
}
