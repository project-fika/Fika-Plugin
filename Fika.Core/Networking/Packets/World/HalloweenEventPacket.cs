using EFT;
using EFT.GlobalEvents;
using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets.World
{
    public struct HalloweenEventPacket : INetSerializable
    {
        public EHalloweenPacketType PacketType;
        public SyncEventFromServer SyncEvent;

        public void Deserialize(NetDataReader reader)
        {
            PacketType = (EHalloweenPacketType)reader.GetInt();
            switch (PacketType)
            {
                case EHalloweenPacketType.Summon:
                    SyncEvent = new HalloweenSummonStartedEvent()
                    {
                        PointPosition = reader.GetUnmanaged<Vector3>()
                    };
                    break;
                case EHalloweenPacketType.Sync:
                    SyncEvent = new HalloweenSyncStateEvent()
                    {
                        EventState = (EEventState)reader.GetByte()
                    };
                    break;
                case EHalloweenPacketType.Exit:
                    SyncEvent = new HalloweenSyncExitsEvent()
                    {
                        ExitName = reader.GetString()
                    };
                    break;
            }
        }

        public readonly void Serialize(NetDataWriter writer)
        {
            writer.Put((int)PacketType);
            switch (PacketType)
            {
                case EHalloweenPacketType.Summon:
                    if (SyncEvent is HalloweenSummonStartedEvent startedEvent)
                    {
                        writer.PutUnmanaged(startedEvent.PointPosition);
                    }
                    break;
                case EHalloweenPacketType.Sync:
                    if (SyncEvent is HalloweenSyncStateEvent stateEvent)
                    {
                        writer.Put((byte)stateEvent.EventState);
                    }
                    break;
                case EHalloweenPacketType.Exit:
                    if (SyncEvent is HalloweenSyncExitsEvent exitsEvent)
                    {
                        writer.Put(exitsEvent.ExitName);
                    }
                    break;
            }
        }
    }

    public enum EHalloweenPacketType
    {
        Summon,
        Sync,
        Exit
    }
}
