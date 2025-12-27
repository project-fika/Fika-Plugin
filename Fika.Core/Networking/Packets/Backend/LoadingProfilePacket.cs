using System.Collections.Generic;
using EFT;

namespace Fika.Core.Networking.Packets.Backend;

public class LoadingProfilePacket : INetSerializable
{
    public Dictionary<Profile, bool> Profiles;

    public void Deserialize(NetDataReader reader)
    {
        var count = reader.GetInt();
        if (count > 0)
        {
            Profiles = new(count);
            for (var i = 0; i < count; i++)
            {
                var profile = reader.GetProfile();
                var isLeader = reader.GetBool();
                Profiles.Add(profile, isLeader);
            }
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        if (Profiles != null)
        {
            var count = Profiles.Count;
            writer.Put(count);
            if (count > 0)
            {
                foreach (var kvp in Profiles)
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
