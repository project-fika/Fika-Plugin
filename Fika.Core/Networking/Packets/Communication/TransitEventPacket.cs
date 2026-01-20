using System.Collections.Generic;
using EFT.GlobalEvents;
using Fika.Core.Main.Utils;

namespace Fika.Core.Networking.Packets.Communication;

public class TransitEventPacket : INetSerializable
{
    public ETransitEventType EventType;
    public SyncEventFromServer TransitEvent;
    public int TransitId;
    public int PlayerId;

    public void Deserialize(NetDataReader reader)
    {
        EventType = (ETransitEventType)reader.GetByte();
        switch (EventType)
        {
            case ETransitEventType.Init:
                {
                    /*TransitInitEvent initEvent = new()
						{
							PlayerId = GamePlayerOwner.MyPlayer.Id
						};
						int pointAmount = reader.GetInt();
						Dictionary<int, ushort> points = [];
						for (int i = 0; i < pointAmount; i++)
						{
							int key = reader.GetInt();
							ushort value = reader.GetUShort();
							points.Add(key, value);
						}
						initEvent.TransitionCount = reader.GetUShort();
						TransitEvent = initEvent;*/
                }
                break;
            case ETransitEventType.GroupTimer:
                {
                    TransitGroupTimerEvent timerEvent = new()
                    {
                        PointId = reader.GetInt()
                    };
                    var timerAmount = reader.GetInt();
                    Dictionary<int, ushort> timers = [];
                    for (var i = 0; i < timerAmount; i++)
                    {
                        var key = reader.GetInt();
                        var value = reader.GetUShort();
#if DEBUG
                        FikaGlobals.LogWarning($"GroupTimer: int: {key}, ushort: {value}");
#endif
                        timers.Add(key, value);
                    }
                    timerEvent.Timers = timers;
                    TransitEvent = timerEvent;
                }
                break;
            case ETransitEventType.GroupSize:
                {
                    TransitGroupSizeEvent sizeEvent = new();
                    var sizeAmount = reader.GetInt();
                    Dictionary<int, byte> sizes = [];
                    for (var i = 0; i < sizeAmount; i++)
                    {
                        var key = reader.GetInt();
                        var value = reader.GetByte();
#if DEBUG
                        FikaGlobals.LogWarning($"GroupSize: int: {key}, byte: {value}");
#endif
                        sizes.Add(key, value);
                    }
                    sizeEvent.Sizes = sizes;
                    TransitEvent = sizeEvent;
                }
                break;
            case ETransitEventType.Interaction:
                {
                    TransitEvent = (TransitInteractionEvent)new()
                    {
                        PlayerId = reader.GetInt(),
                        PointId = reader.GetInt(),
                        Type = (TransitInteractionEvent.EType)reader.GetByte()
                    };
                }
                break;
            case ETransitEventType.Messages:
                {
                    TransitMessagesEvent messagesEvent = new();
                    var messagesAmount = reader.GetInt();
                    Dictionary<int, TransitMessagesEvent.EType> messages = [];
                    for (var i = 0; i < messagesAmount; i++)
                    {
                        var key = reader.GetInt();
                        var value = (TransitMessagesEvent.EType)reader.GetByte();
                        messages.Add(key, value);
                    }
                    messagesEvent.Messages = messages;
                    TransitEvent = messagesEvent;
                }
                break;
            case ETransitEventType.Extract:
                TransitId = reader.GetInt();
                PlayerId = reader.GetInt();
                break;
            default:
                break;
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)EventType);
        switch (EventType)
        {
            case ETransitEventType.Init:
                {
                    /*if (TransitEvent is TransitInitEvent initEvent)
						{
							writer.Put(initEvent.Points.Count);
							foreach (KeyValuePair<int, ushort> point in initEvent.Points)
							{
								writer.Put(point.Key);
								writer.Put(point.Value);
							}
							writer.Put(initEvent.TransitionCount);
						}*/
                }
                break;
            case ETransitEventType.GroupTimer:
                {
                    if (TransitEvent is TransitGroupTimerEvent timerEvent)
                    {
                        writer.Put(timerEvent.PointId);
                        writer.Put(timerEvent.Timers.Count);
                        foreach (var timer in timerEvent.Timers)
                        {
                            writer.Put(timer.Key);
                            writer.Put(timer.Value);
                        }
                    }
                }
                break;
            case ETransitEventType.GroupSize:
                {
                    if (TransitEvent is TransitGroupSizeEvent sizeEvent)
                    {
                        writer.Put(sizeEvent.Sizes.Count);
                        foreach (var size in sizeEvent.Sizes)
                        {
                            writer.Put(size.Key);
                            writer.Put(size.Value);
                        }
                    }
                }
                break;
            case ETransitEventType.Interaction:
                {
                    if (TransitEvent is TransitInteractionEvent interactionEvent)
                    {
                        writer.Put(interactionEvent.PlayerId);
                        writer.Put(interactionEvent.PointId);
                        writer.Put((byte)interactionEvent.Type);
                    }
                }
                break;
            case ETransitEventType.Messages:
                {
                    if (TransitEvent is TransitMessagesEvent messagesEvent)
                    {
                        writer.Put(messagesEvent.Messages.Count);
                        foreach (var message in messagesEvent.Messages)
                        {
                            writer.Put(message.Key);
                            writer.Put((byte)message.Value);
                        }
                    }
                }
                break;
            case ETransitEventType.Extract:
                writer.Put(TransitId);
                writer.Put(PlayerId);
                break;
            default:
                break;
        }
    }

    public enum ETransitEventType
    {
        Init,
        GroupTimer,
        GroupSize,
        Interaction,
        Messages,
        Extract
    }
}
