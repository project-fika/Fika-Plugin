using EFT;
using LiteNetLib.Utils;
using System.Collections.Generic;

namespace Fika.Core.Networking
{
	public class LoadingProfilePacket : INetSerializable
	{
		public Dictionary<Profile, bool> Profiles;

		public void Deserialize(NetDataReader reader)
		{
			int count = reader.GetInt();
			if (count > 0)
			{
				Profiles = [];
				for (int i = 0; i < count; i++)
				{
					Profile profile = reader.GetProfile();
					bool isLeader = reader.GetBool();
					Profiles.Add(profile, isLeader);
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
					foreach (KeyValuePair<Profile, bool> kvp in Profiles)
					{
						writer.PutProfile(kvp.Key);
						writer.Put(kvp.Value);
					}
				}
				return;
			}

			writer.Put(0);
		}
	}
}
