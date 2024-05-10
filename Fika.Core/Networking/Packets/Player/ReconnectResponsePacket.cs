using EFT.Interactive;
using LiteNetLib.Utils;
using UnityEngine;

namespace Fika.Core.Networking
{
    public struct ReconnectResponsePacket(int netId, Vector3 position, Quaternion rotation, bool isProne, 
    WorldInteractiveObject[] interactiveObjects, WindowBreaker[] windows, LampController[] lights): INetSerializable
    {
        public int NetId = netId;
        public Vector3 Position = position;
        public Quaternion Rotation = rotation;
        public bool IsProne = isProne;
        public int InteractiveObjectAmount;
        public WorldInteractiveObject.GStruct385[] InteractiveObjects;
        public int WindowBreakerAmount;
        public WindowBreaker[] Windows;
        public int LightAmount;
        public LampController[] Lights;

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
            Position = reader.GetVector3();
            Rotation = reader.GetQuaternion();
            IsProne = reader.GetBool();
            InteractiveObjectAmount = reader.GetInt();
            
            if (InteractiveObjectAmount > 0)
            {
                InteractiveObjects = new WorldInteractiveObject.GStruct385[InteractiveObjectAmount];
                for (int i = 0; i < InteractiveObjectAmount; i++)
                {
                    InteractiveObjects[i] = reader.GetInteractiveObjectState();
                }
            }

            WindowBreakerAmount = reader.GetInt();

            if (WindowBreakerAmount > 0)
            {
                Windows = new WindowBreaker[WindowBreakerAmount];
                for (int i = 0; i < WindowBreakerAmount; i++)
                {
                    Windows[i] = reader.GetWindowBreakerState();
                }
            }

            LightAmount = reader.GetInt();

            if (LightAmount > 0)
            {
                Lights = new LampController[LightAmount];
                for (int i = 0; i < LightAmount; i++)
                {
                    Lights[i] = reader.GetLightState();
                }
            }
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.Put(Position);
            writer.Put(Rotation);
            writer.Put(IsProne);
            writer.Put(interactiveObjects.Length);

            if (interactiveObjects.Length > 0)
            {
                for (int i = 0; i < interactiveObjects.Length; i++)
                {
                    writer.PutInteractiveObjectState(interactiveObjects[i]);
                }
            }

            writer.Put(windows.Length);

            if (windows.Length > 0)
            {
                for (int i = 0; i < windows.Length; i++)
                {
                    writer.PutWindowBreakerState(windows[i]);
                }
            }

            writer.Put(lights.Length);

            if (lights.Length > 0)
            {
                for (int i = 0; i < lights.Length; i++)
                {
                    writer.PutLightState(lights[i]);
                }
            }
        }
    }
}