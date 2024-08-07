using EFT.Interactive;
using LiteNetLib.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace Fika.Core.Networking.Packets.GameWorld
{
	public struct ReconnectPacket : INetSerializable
	{
		public bool IsRequest;
		public EReconnectDataType Type;

		public List<GStruct35> ThrowableData;
		public List<WorldInteractiveObject.GStruct384> InteractivesData;
		public Dictionary<int, byte> LampStates;
		public Dictionary<int, Vector3> WindowBreakerStates;

		public void Deserialize(NetDataReader reader)
		{
			IsRequest = reader.GetBool();
			Type = (EReconnectDataType)reader.GetByte();
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
				default:
					break;
			}
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(IsRequest);
			writer.Put((byte)Type);
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
				default:
					break;
			}
		}

		public enum EReconnectDataType
		{
			Throwable,
			Interactives,
			LampControllers,
			Windows
		}
	}
}
