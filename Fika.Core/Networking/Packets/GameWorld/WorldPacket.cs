using System.Collections.Generic;

namespace Fika.Core.Networking
{
	public class WorldPacket
	{
		public List<GStruct132> RagdollPackets { get; set; } = [];
		public List<GStruct133> ArtilleryPackets { get; set; } = [];
		public List<GStruct134> ThrowablePackets { get; set; } = [];

		public bool HasData
		{
			get
			{
				return RagdollPackets.Count > 0
					|| ArtilleryPackets.Count > 0
					|| ThrowablePackets.Count > 0;
			}
		}

		public void Flush()
		{
			RagdollPackets.Clear();
			ArtilleryPackets.Clear();
			ThrowablePackets.Clear();
		}
	}
}
