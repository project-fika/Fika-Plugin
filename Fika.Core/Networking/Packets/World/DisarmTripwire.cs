using Comfort.Common;
using EFT;
using EFT.SynchronizableObjects;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Pooling;
using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets.World;

public class DisarmTripwire : IPoolSubPacket
{
    public AirplaneDataPacketStruct Data;

    private DisarmTripwire() { }

    public static DisarmTripwire CreateInstance()
    {
        return new DisarmTripwire();
    }

    public static DisarmTripwire FromValue(AirplaneDataPacketStruct data)
    {
        DisarmTripwire packet = GenericSubPacketPoolManager.Instance.GetPacket<DisarmTripwire>(EGenericSubPacketType.DisarmTripwire);
        packet.Data = data;
        return packet;
    }

    public void Execute(FikaPlayer player = null)
    {
        if (Data.ObjectType == SynchronizableObjectType.Tripwire)
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            TripwireSynchronizableObject tripwire = gameWorld.SynchronizableObjectLogicProcessor.TripwireManager.GetTripwireById(Data.ObjectId);
            if (tripwire != null)
            {
                gameWorld.DeActivateTripwire(tripwire);
                return;
            }

            FikaGlobals.LogError($"OnSyncObjectPacketReceived: Tripwire with id {Data.ObjectId} could not be found!");
        }

        FikaGlobals.LogWarning($"OnSyncObjectPacketReceived: Received a packet we shouldn't receive: {Data.ObjectType}");
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.PutAirplaneDataPacketStruct(Data);
    }

    public void Deserialize(NetDataReader reader)
    {
        Data = reader.GetAirplaneDataPacketStruct();
    }

    public void Dispose()
    {
        Data = default;
    }
}
