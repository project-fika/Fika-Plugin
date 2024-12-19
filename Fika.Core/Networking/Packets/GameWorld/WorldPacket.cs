using LiteNetLib.Utils;
using System.Collections.Generic;

namespace Fika.Core.Networking
{
	public class WorldPacket : INetSerializable
	{
		public List<CorpsePositionPacket> RagdollPackets = [];
		public List<ArtilleryPacket> ArtilleryPackets = [];
		public List<ThrowablePacket> ThrowablePackets = [];

		public void Flush()
		{
			RagdollPackets.Clear();
			ArtilleryPackets.Clear();
			ThrowablePackets.Clear();
		}

		public void Deserialize(NetDataReader reader)
		{
			Flush();

			int ragdollPackets = reader.GetInt();
			for (int i = 0; i < ragdollPackets; i++)
			{
				RagdollPackets.Add(reader.Get<CorpsePositionPacket>());
			}

			int artilleryPackets = reader.GetInt();
			for (int i = 0; i < artilleryPackets; i++)
			{
				ArtilleryPackets.Add(reader.Get<ArtilleryPacket>());
			}

			int throwablePackets = reader.GetInt();
			for (int i = 0; i < throwablePackets; i++)
			{
				ThrowablePackets.Add(reader.Get<ThrowablePacket>());
			}
		}

		public void Serialize(NetDataWriter writer)
		{
			int ragdollPackets = RagdollPackets.Count;
			writer.Put(ragdollPackets);
			for (int i = 0; i < ragdollPackets; i++)
			{
				writer.Put(RagdollPackets[i]);
			}

			int artilleryPackets = ArtilleryPackets.Count;
			for (int i = 0; i < artilleryPackets; i++)
			{
				writer.Put(ArtilleryPackets[i]);
			}

			int throwablePackets = ThrowablePackets.Count;
			for (int i = 0; i < throwablePackets; i++)
			{
				writer.Put(ThrowablePackets[i]);
			}

			Flush();
		}
	}
}
