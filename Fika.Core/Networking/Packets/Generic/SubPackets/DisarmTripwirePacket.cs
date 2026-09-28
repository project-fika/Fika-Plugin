using Comfort.Common;
using EFT;
using EFT.SynchronizableObjects;
using Fika.Core.Main.Utils;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public readonly struct DisarmTripwirePacket : IGenericPacket
{
    public EGenericPacketType Type => EGenericPacketType.DisarmTripwire;

    public DisarmTripwirePacket(SynchronizableObjectPacket data)
    {
        Data = data;
    }

    public DisarmTripwirePacket(NetDataReader reader)
    {
        Data = reader.GetAirplaneDataPacketStruct();
    }

    public readonly SynchronizableObjectPacket Data;

    public readonly void Execute()
    {
        if (Data.ObjectType == SynchronizableObjectType.Tripwire)
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            var tripwire = gameWorld.SynchronizableObjectLogicProcessor.TripwireManager.GetTripwireById(Data.ObjectId);
            if (tripwire != null)
            {
                gameWorld.DeActivateTripwire(tripwire);
                return;
            }

            FikaGlobals.LogError($"OnSyncObjectPacketReceived: Tripwire with id {Data.ObjectId} could not be found!");
            return;
        }

        FikaGlobals.LogWarning($"OnSyncObjectPacketReceived: Received a packet we shouldn't receive: {Data.ObjectType}");
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.PutAirplaneDataPacketStruct(Data);
    }
}
