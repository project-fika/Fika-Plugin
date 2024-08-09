// © 2024 Lacyway All Rights Reserved

using EFT.Interactive;
using LiteNetLib.Utils;
using System.Collections.Generic;
using System.Linq;

namespace Fika.Core.Networking
{
	public struct ExfiltrationPacket(bool isRequest) : INetSerializable
	{
		public bool IsRequest = isRequest;
		public int ExfiltrationAmount;
		public Dictionary<string, EExfiltrationStatus> ExfiltrationPoints;
		public List<int> StartTimes;
		public bool HasScavExfils = false;
		public int ScavExfiltrationAmount;
		public Dictionary<string, EExfiltrationStatus> ScavExfiltrationPoints;
		public List<int> ScavStartTimes;

		public void Deserialize(NetDataReader reader)
		{
			IsRequest = reader.GetBool();
			if (!IsRequest)
			{
				ExfiltrationAmount = reader.GetInt();
				ExfiltrationPoints = [];
				StartTimes = [];
				for (int i = 0; i < ExfiltrationAmount; i++)
				{
					ExfiltrationPoints.Add(reader.GetString(), (EExfiltrationStatus)reader.GetInt());
					StartTimes.Add(reader.GetInt());
				}
				HasScavExfils = reader.GetBool();
				if (HasScavExfils)
				{
					ScavExfiltrationAmount = reader.GetInt();
					ScavExfiltrationPoints = [];
					ScavStartTimes = [];
					for (int i = 0; i < ScavExfiltrationAmount; i++)
					{
						ScavExfiltrationPoints.Add(reader.GetString(), (EExfiltrationStatus)reader.GetInt());
						ScavStartTimes.Add(reader.GetInt());
					}
				}
			}
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(IsRequest);
			if (!IsRequest)
			{
				writer.Put(ExfiltrationAmount);
				for (int i = 0; i < ExfiltrationPoints.Count; i++)
				{
					writer.Put(ExfiltrationPoints.ElementAt(i).Key);
					writer.Put((int)ExfiltrationPoints.ElementAt(i).Value);
					writer.Put(StartTimes[i]);
				}
				writer.Put(HasScavExfils);
				if (HasScavExfils)
				{
					writer.Put(ScavExfiltrationAmount);
					for (int i = 0; i < ScavExfiltrationPoints.Count; i++)
					{
						writer.Put(ScavExfiltrationPoints.ElementAt(i).Key);
						writer.Put((int)ScavExfiltrationPoints.ElementAt(i).Value);
						writer.Put(ScavStartTimes[i]);
					}
				}
			}
		}
	}
}
