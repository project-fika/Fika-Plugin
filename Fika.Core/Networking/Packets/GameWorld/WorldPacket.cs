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
