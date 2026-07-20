using System.Collections.Generic;
using ComponentAce.Compression.Libs.zlib;
using EFT;
using EFT.Interactive;
using Fika.Core.Networking.Packets.Communication;

namespace Fika.Core.Networking.Packets.World;

public sealed class ReconnectPacket : INetSerializable
{
    public bool IsRequest;
    public bool InitialRequest;
    public EReconnectDataType Type;

    public string ProfileId;
    public Profile Profile;
    public Profile.HealthInfo ProfileHealthClass;
    public Vector3 PlayerPosition;
    public Vector2 PlayerRotation;

    public List<SmokeGrenadeNetworkData> ThrowableData;
    public List<WorldInteractiveObject.InteractiveObjectStatusInfo> InteractivesData;
    public Dictionary<int, byte> LampStates;
    public Dictionary<int, Vector3> WindowBreakerStates;
    public List<QuestSyncPacket> QuestSyncPackets;

    public void Deserialize(NetDataReader reader)
    {
        IsRequest = reader.GetBool();
        InitialRequest = reader.GetBool();
        ProfileId = reader.GetString();
        if (!IsRequest)
        {
            Type = reader.GetEnum<EReconnectDataType>();
            switch (Type)
            {
                case EReconnectDataType.Throwable:
                    ThrowableData = reader.GetThrowableData();
                    break;
                case EReconnectDataType.Interactives:
                    InteractivesData = reader.GetInteractivesStates();
                    break;
                case EReconnectDataType.LampControllers:
                    LampStates = reader.GetLampStates();
                    break;
                case EReconnectDataType.Windows:
                    WindowBreakerStates = reader.GetWindowBreakerStates();
                    break;
                case EReconnectDataType.OwnCharacter:
                    Profile = reader.GetProfile();
                    ProfileHealthClass = SimpleZlib.Decompress(reader.GetByteArray()).ParseJsonTo<Profile.HealthInfo>();
                    PlayerPosition = reader.GetUnmanaged<Vector3>();
                    PlayerRotation = reader.GetUnmanaged<Vector2>();
                    break;
                case EReconnectDataType.Quests:
                    var count = reader.GetUShort();
                    QuestSyncPackets = new List<QuestSyncPacket>(count);
                    for (var i = 0; i < count; i++)
                    {
                        QuestSyncPackets.Add(reader.Get<QuestSyncPacket>());
                    }
                    break;
            }
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(IsRequest);
        writer.Put(InitialRequest);
        writer.Put(ProfileId);
        if (!IsRequest)
        {
            writer.PutEnum(Type);
            switch (Type)
            {
                case EReconnectDataType.Throwable:
                    writer.PutThrowableData(ThrowableData);
                    break;
                case EReconnectDataType.Interactives:
                    writer.PutInteractivesStates(InteractivesData);
                    break;
                case EReconnectDataType.LampControllers:
                    writer.PutLampStates(LampStates);
                    break;
                case EReconnectDataType.Windows:
                    writer.PutWindowBreakerStates(WindowBreakerStates);
                    break;
                case EReconnectDataType.OwnCharacter:
                    writer.PutProfile(Profile);
                    writer.PutByteArray(SimpleZlib.CompressToBytes(ProfileHealthClass.ToJson(), 4));
                    writer.PutUnmanaged(PlayerPosition);
                    writer.PutUnmanaged(PlayerRotation);
                    break;
                case EReconnectDataType.Quests:
                    var count = QuestSyncPackets.Count;
                    writer.Put((ushort)count);
                    for (var i = 0; i < count; i++)
                    {
                        writer.Put(QuestSyncPackets[i]);
                    }
                    break;
            }
        }
    }

    public enum EReconnectDataType
    {
        Throwable,
        Interactives,
        LampControllers,
        Windows,
        OwnCharacter,
        Quests,
        Finished
    }
}
