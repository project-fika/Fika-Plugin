using EFT;
using EFT.GlobalEvents;

namespace Fika.Core.Networking.Packets.Communication;

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
            var amount = reader.GetInt();
            stateEvent.Objects = new(amount);
            for (var i = 0; i < amount; i++)
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
            var stateEvent = (RunddansStateEvent)Event;
            writer.Put(stateEvent.PlayerId);
            writer.Put(stateEvent.Objects.Count);
            foreach ((var objectId, var state) in stateEvent.Objects)
            {
                writer.Put(objectId);
                writer.Put((byte)state);
            }
        }
        else
        {
            var messagesEvent = (RunddansMessagesEvent)Event;
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
