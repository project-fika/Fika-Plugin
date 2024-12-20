using LiteNetLib.Utils;
using System.Collections.Generic;

namespace Fika.Core.Networking
{
	public class WorldPacket
	{
		public List<GStruct129> RagdollPackets { get; set; } = [];
		public List<GStruct130> ArtilleryPackets { get; set; } = [];
		public List<GStruct131> ThrowablePackets { get; set; } = [];

		public bool HasData
		{
			get
			{
				return RagdollPackets.Count > 0 || ArtilleryPackets.Count > 0 || ThrowablePackets.Count > 0;
			}
		}

		public void Flush()
		{
			RagdollPackets.Clear();
			ArtilleryPackets.Clear();
			ThrowablePackets.Clear();
		}

		/*public void Deserialize(NetDataReader reader)
		{
			Flush();

			int ragdollPackets = reader.GetInt();
			for (int i = 0; i < ragdollPackets; i++)
			{
				RagdollPackets.Add(reader.GetRagdollStruct());
			}

			int artilleryPackets = reader.GetInt();
			for (int i = 0; i < artilleryPackets; i++)
			{
				ArtilleryPackets.Add(reader.GetArtilleryStruct());
			}

			int throwablePackets = reader.GetInt();
			for (int i = 0; i < throwablePackets; i++)
			{
				ThrowablePackets.Add(reader.GetGrenadeStruct());
			}
		}

		public void Serialize(NetDataWriter writer)
		{
			int ragdollPackets = RagdollPackets.Count;
			writer.Put(ragdollPackets);
			for (int i = 0; i < ragdollPackets; i++)
			{
				writer.PutRagdollStruct(RagdollPackets[i]);
			}

			int artilleryPackets = ArtilleryPackets.Count;
			writer.Put(artilleryPackets);
			for (int i = 0; i < artilleryPackets; i++)
			{
				writer.PutArtilleryStruct(ArtilleryPackets[i]);
			}

			int throwablePackets = ThrowablePackets.Count;
			writer.Put(throwablePackets);
			for (int i = 0; i < throwablePackets; i++)
			{
				writer.PutGrenadeStruct(ThrowablePackets[i]);
			}

			Flush();
		}*/
	}
}
