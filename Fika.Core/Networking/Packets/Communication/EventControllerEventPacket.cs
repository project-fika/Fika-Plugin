using EFT;
using EFT.GlobalEvents;

namespace Fika.Core.Networking.Packets.Communication;

public struct EventControllerEventPacket : INetSerializable
{
    public int NetId;
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
            Event = stateEvent;
        }
        else if (Type == EEventType.MessageEvent)
        {
            Event = new RunddansMessagesEvent()
            {
                PlayerId = reader.GetInt(),
                Type = (RunddansMessagesEvent.EType)reader.GetByte()
            };
        }
        else if (Type == EEventType.RemoveItem)
        {
            NetId = reader.GetInt();
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
        else if (Type == EEventType.MessageEvent)
        {
            var messagesEvent = (RunddansMessagesEvent)Event;
            writer.Put(messagesEvent.PlayerId);
            writer.Put((byte)messagesEvent.Type);
        }
        else if (Type == EEventType.RemoveItem)
        {
            writer.Put(NetId);
        }
    }

    public enum EEventType
    {
        StartedEvent,
        RemoveItem,
        StateEvent,
        MessageEvent
    }
}
