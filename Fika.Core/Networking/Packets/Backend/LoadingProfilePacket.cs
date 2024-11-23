using EFT;
using LiteNetLib.Utils;
using System.Collections.Generic;

namespace Fika.Core.Networking
{
	public class LoadingProfilePacket : INetSerializable
	{
		public Dictionary<bool, Profile> Profiles;

		public void Deserialize(NetDataReader reader)
		{
			int count = reader.GetInt();
			if (count > 0)
			{
				Profiles = [];
				for (int i = 0; i < count; i++)
				{
					bool isLeader = reader.GetBool();
					Profile profile = reader.GetProfile();
					Profiles.Add(isLeader, profile);
				}
			}
		}

		public void Serialize(NetDataWriter writer)
		{
			if (Profiles != null)
			{
				int count = Profiles.Count;
				writer.Put(count);
				if (count > 0)
				{
					foreach (KeyValuePair<bool, Profile> kvp in Profiles)
					{
						writer.Put(kvp.Key);
						writer.PutProfile(kvp.Value);
					}
				}
				return;
			}

			writer.Put(0);
		}
	}
}
