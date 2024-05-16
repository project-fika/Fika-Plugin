using EFT;
using EFT.Interactive;
using LiteNetLib.Utils;
using UnityEngine;

namespace Fika.Core.Networking
{
    public struct ReconnectResponsePacket(int netId, Vector3 position, Quaternion rotation, EPlayerPose playerPose, float poseLevel, 
    bool isProne, WorldInteractiveObject[] interactiveObjects, WindowBreaker[] windows, LampController[] lights, Throwable[] smokes
    , EquipmentClass equipment, LootItemPositionClass[] items): INetSerializable
    {
        public int NetId;
        public Vector3 Position;
        public Quaternion Rotation;
        public EPlayerPose PlayerPose;
        public float PoseLevel;
        public bool IsProne = isProne;
        public int InteractiveObjectAmount;
        public WorldInteractiveObject.GStruct385[] InteractiveObjects;
        public int WindowBreakerAmount;
        public WindowBreaker[] Windows;
        public int LightAmount;
        public LampController[] Lights;
        public int SmokeAmount;
        public GStruct34[] Smokes;
        public EquipmentClass Equipment;
        public int ItemAmount;
        public GClass1202 Items;

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
            Position = reader.GetVector3();
            Rotation = reader.GetQuaternion();
            PlayerPose = (EPlayerPose)reader.GetByte();
            PoseLevel = reader.GetFloat();
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

            SmokeAmount = reader.GetInt();

            if (SmokeAmount > 0)
            {
                Smokes = new GStruct34[SmokeAmount];
                for (int i = 0; i < SmokeAmount; i++)
                {
                    Smokes[i] = reader.GetSmokeState();
                }
            }

            Equipment = (EquipmentClass)reader.GetItem();
            Items = reader.GetLocationItem();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(netId);
            writer.Put(position);
            writer.Put(rotation);
            writer.Put((byte)playerPose);
            writer.Put(poseLevel);
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

            writer.Put(smokes.Length);

            if (smokes.Length > 0)
            {
                for (int i = 0; i < smokes.Length; i++)
                {
                    writer.PutSmokeState(smokes[i]);
                }
            }

            writer.PutItem(equipment);

            if (items.Length > 0)
            {
                writer.PutLocationItem(items);
            }
        }
    }
}