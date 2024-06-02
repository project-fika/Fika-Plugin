using UnityEngine;
using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public struct ReconnectAirdropPacket(Vector3 planePosition, Vector3 boxPosition, float distanceTravelled) : INetSerializable
    {
        public Vector3 PlanePosition;
        public Vector3 BoxPosition;
        public float DistanceTravelled;

        public void Deserialize(NetDataReader reader)
        {
            PlanePosition = reader.GetVector3();
            BoxPosition = reader.GetVector3();
            DistanceTravelled = reader.GetFloat();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(planePosition);
            writer.Put(boxPosition);
            writer.Put(distanceTravelled);
        }
    }
}