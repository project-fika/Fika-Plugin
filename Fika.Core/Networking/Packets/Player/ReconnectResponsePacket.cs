using EFT;
using EFT.Interactive;
using LiteNetLib.Utils;
using UnityEngine;
using static Fika.Core.Networking.FikaSerialization;

namespace Fika.Core.Networking
{
    public struct ReconnectResponsePacket(int netId, bool isAlive, Vector3 position, Quaternion rotation, EPlayerPose playerPose, float poseLevel,
    bool isProne, WorldInteractiveObject[] interactiveObjects, WindowBreaker[] windows, LampController[] lights, Throwable[] smokes
    , PlayerInfoPacket profile, LootItemPositionClass[] items, int playerCount, bool initAirdrop, int airdropCount
    , AirdropPacket[] airdropPackets, AirdropLootPacket[] airdropLootPackets) : INetSerializable
    {
        public int NetId;
        public bool IsAlive;
        public Vector3 Position;
        public Quaternion Rotation;
        public EPlayerPose PlayerPose;
        public float PoseLevel;
        public bool IsProne = isProne;
        public int InteractiveObjectAmount;
        public WorldInteractiveObject.GStruct384[] InteractiveObjects;
        public int WindowBreakerAmount;
        public WindowBreaker[] Windows;
        public int LightAmount;
        public LampController[] Lights;
        public int SmokeAmount;
        public GStruct35[] Smokes;
        public PlayerInfoPacket Profile;
        public GClass1211 Items;
        public int PlayerCount;
        public bool InitAirdrop;
        public int AirdropCount;
        public AirdropPacket[] AirdropPackets;
        public AirdropLootPacket[] AirdropLootPackets;

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
            IsAlive = reader.GetBool();
            Position = reader.GetVector3();
            Rotation = reader.GetQuaternion();
            PlayerPose = (EPlayerPose)reader.GetByte();
            PoseLevel = reader.GetFloat();
            IsProne = reader.GetBool();
            InteractiveObjectAmount = reader.GetInt();

            if (InteractiveObjectAmount > 0)
            {
                InteractiveObjects = new WorldInteractiveObject.GStruct384[InteractiveObjectAmount];
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
                Smokes = new GStruct35[SmokeAmount];
                for (int i = 0; i < SmokeAmount; i++)
                {
                    Smokes[i] = reader.GetSmokeState();
                }
            }

            Profile = PlayerInfoPacket.Deserialize(reader);
            Items = reader.GetLocationItem();
            PlayerCount = reader.GetInt();
            InitAirdrop = reader.GetBool();
            AirdropCount = reader.GetInt();

            if (AirdropCount > 0)
            {
                AirdropPackets = new AirdropPacket[AirdropCount];
                for (int i = 0; i < AirdropCount; i++)
                {
                    AirdropPackets[i] = reader.GetAirdropPacket();
                }
            }

            if (AirdropCount > 0)
            {
                AirdropLootPackets = new AirdropLootPacket[AirdropCount];
                for (int i = 0; i < AirdropCount; i++)
                {
                    AirdropLootPackets[i] = reader.GetAirLootPacket();
                }
            }
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(netId);
            writer.Put(isAlive);
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

            PlayerInfoPacket.Serialize(writer, profile);

            if (items.Length > 0)
            {
                writer.PutLocationItem(items);
            }

            writer.Put(playerCount);
            writer.Put(initAirdrop);
            writer.Put(airdropCount);

            if (airdropCount > 0)
            {
                for (int i = 0; i < airdropCount; i++)
                {
                    writer.Put(airdropPackets[i]);
                }
            }

            if (airdropCount > 0)
            {
                for (int i = 0; i < airdropCount; i++)
                {
                    writer.Put(airdropLootPackets[i]);
                }
            }
        }
    }
}