using System.Collections.Generic;
using ComponentAce.Compression.Libs.zlib;
using EFT;
using EFT.Interactive;

namespace Fika.Core.Networking.Packets.World;

public class ReconnectPacket : INetSerializable
{
    public bool IsRequest;
    public bool InitialRequest;
    public EReconnectDataType Type;

    public string ProfileId;
    public Profile Profile;
    public Profile.ProfileHealthClass ProfileHealthClass;
    public Vector3 PlayerPosition;

    public List<SmokeGrenadeDataPacketStruct> ThrowableData;
    public List<WorldInteractiveObject.WorldInteractiveDataPacketStruct> InteractivesData;
    public Dictionary<int, byte> LampStates;
    public Dictionary<int, Vector3> WindowBreakerStates;

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
                    ProfileHealthClass = SimpleZlib.Decompress(reader.GetByteArray()).ParseJsonTo<Profile.ProfileHealthClass>();
                    PlayerPosition = reader.GetUnmanaged<Vector3>();
                    break;
                case EReconnectDataType.Finished:
                default:
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
                    break;
                case EReconnectDataType.Finished:
                default:
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
        Finished
    }
}
