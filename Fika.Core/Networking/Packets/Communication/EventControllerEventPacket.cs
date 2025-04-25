using EFT;
using EFT.GlobalEvents;
using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public class EventControllerEventPacket : INetSerializable
    {
        public EEventType Type;
        public SyncEventFromServer Event;

        public void Deserialize(NetDataReader reader)
        {
            Type = (EEventType)reader.GetByte();
            if (Type == EEventType.StateEvent)
            {
                RunddansStateEvent stateEvent = new()
                {
                    PlayerId = reader.GetInt()
                };
                int amount = reader.GetInt();
                stateEvent.Objects = new(amount);
                for (int i = 0; i < amount; i++)
                {
                    stateEvent.Objects.Add(reader.GetInt(),
                        (EventObject.EState)reader.GetByte());
                }
            }
            else
            {
                Event = new RunddansMessagesEvent()
                {
                    PlayerId = reader.GetInt(),
                    Type = (RunddansMessagesEvent.EType)reader.GetByte()
                };
            }
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put((byte)Type);
            if (Type == EEventType.StateEvent)
            {
                RunddansStateEvent stateEvent = (RunddansStateEvent)Event;
                writer.Put(stateEvent.PlayerId);
                writer.Put(stateEvent.Objects.Count);
                foreach ((int objectId, EventObject.EState state) in stateEvent.Objects)
                {
                    writer.Put(objectId);
                    writer.Put((byte)state);
                }
            }
            else
            {
                RunddansMessagesEvent messagesEvent = (RunddansMessagesEvent)Event;
                writer.Put(messagesEvent.PlayerId);
                writer.Put((byte)messagesEvent.Type);
            }
        }

        public enum EEventType
        {
            StateEvent,
            MessageEvent
        }
    }
}
