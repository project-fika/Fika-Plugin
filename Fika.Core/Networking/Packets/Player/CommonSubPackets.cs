using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.Vaulting;
using EFT.WeaponMounting;
using Fika.Core.Main.ObservedClasses;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using LiteNetLib.Utils;
using System;
using UnityEngine;
using static Fika.Core.Main.Players.CoopPlayer;
using static Fika.Core.Networking.SubPacket;

namespace Fika.Core.Networking
{
    public class CommonSubPackets
    {
        public struct PhrasePacket : ISubPacket
        {
            public EPhraseTrigger PhraseTrigger;
            public int PhraseIndex;

            public PhrasePacket(NetDataReader reader)
            {
                PhraseTrigger = (EPhraseTrigger)reader.GetByte();
                PhraseIndex = reader.GetInt();
            }

            public readonly void Execute(CoopPlayer player)
            {
                if (player.gameObject.activeSelf && player.HealthController.IsAlive)
                {
                    player.Speaker.PlayDirect(PhraseTrigger, PhraseIndex);
                }
            }

            public readonly void Serialize(NetDataWriter writer)
            {
                writer.Put((byte)PhraseTrigger);
                writer.Put(PhraseIndex);
            }
        }

        public struct WorldInteractionPacket : ISubPacket
        {
            public string InteractiveId;
            public EInteractionType InteractionType;
            public EInteractionStage InteractionStage;
            public string ItemId;

            public WorldInteractionPacket(NetDataReader reader)
            {
                InteractiveId = reader.GetString();
                InteractionType = (EInteractionType)reader.GetByte();
                InteractionStage = reader.GetEnum<EInteractionStage>();
                if (InteractionType == EInteractionType.Unlock)
                {
                    ItemId = reader.GetString();
                }
            }

            public readonly void Execute(CoopPlayer player)
            {
                WorldInteractiveObject worldInteractiveObject = Singleton<GameWorld>.Instance.FindDoor(InteractiveId);
                if (worldInteractiveObject != null)
                {
                    if (worldInteractiveObject.isActiveAndEnabled && !worldInteractiveObject.ForceLocalInteraction)
                    {
                        InteractionResult interactionResult;
                        Action action;
                        if (InteractionType == EInteractionType.Unlock)
                        {
                            KeyHandler keyHandler = new(player);

                            if (string.IsNullOrEmpty(ItemId))
                            {
                                FikaPlugin.Instance.FikaLogger.LogWarning("WorldInteractionPacket: ItemID was null!");
                                return;
                            }

                            GStruct461<Item> result = player.FindItemById(ItemId, false, false);
                            if (!result.Succeeded)
                            {
                                FikaPlugin.Instance.FikaLogger.LogWarning("WorldInteractionPacket: Could not find item: " + ItemId);
                                return;
                            }

                            KeyComponent keyComponent = result.Value.GetItemComponent<KeyComponent>();
                            if (keyComponent == null)
                            {
                                FikaPlugin.Instance.FikaLogger.LogWarning("WorldInteractionPacket: keyComponent was null!");
                                return;
                            }

                            keyHandler.unlockResult = worldInteractiveObject.UnlockOperation(keyComponent, player, worldInteractiveObject);
                            if (keyHandler.unlockResult.Error != null)
                            {
                                FikaPlugin.Instance.FikaLogger.LogWarning("WorldInteractionPacket: Error when processing unlockResult: " + keyHandler.unlockResult.Error);
                                return;
                            }

                            interactionResult = keyHandler.unlockResult.Value;
                            keyHandler.unlockResult.Value.RaiseEvents(player.InventoryController, CommandStatus.Begin);
                            action = new(keyHandler.HandleKeyEvent);
                        }
                        else
                        {
                            interactionResult = new InteractionResult(InteractionType);
                            action = null;
                        }

                        if (InteractionStage == EInteractionStage.Start)
                        {
                            player.vmethod_0(worldInteractiveObject, interactionResult, action);
                            return;
                        }

                        if (InteractionStage != EInteractionStage.Execute)
                        {
                            worldInteractiveObject.Interact(interactionResult);
                            return;
                        }

                        player.vmethod_1(worldInteractiveObject, interactionResult);
                    }

                }
                else
                {
                    FikaPlugin.Instance.FikaLogger.LogError("WorldInteractionPacket: WorldInteractiveObject was null or disabled!");
                }
            }

            public readonly void Serialize(NetDataWriter writer)
            {
                writer.Put(InteractiveId);
                writer.Put((byte)InteractionType);
                writer.PutEnum(InteractionStage);
                if (InteractionType == EInteractionType.Unlock)
                {
                    writer.Put(ItemId);
                }
            }
        }

        public struct ContainerInteractionPacket : ISubPacket
        {
            public string InteractiveId;
            public EInteractionType InteractionType;

            public ContainerInteractionPacket(NetDataReader reader)
            {
                InteractiveId = reader.GetString();
                InteractionType = (EInteractionType)reader.GetByte();
            }

            public readonly void Execute(CoopPlayer player)
            {
                WorldInteractiveObject lootableContainer = Singleton<GameWorld>.Instance.FindDoor(InteractiveId);
                if (lootableContainer != null)
                {
                    if (lootableContainer.isActiveAndEnabled)
                    {
                        InteractionResult result = new(InteractionType);
                        lootableContainer.Interact(result);
                    }
                }
                else
                {
                    FikaPlugin.Instance.FikaLogger.LogError("ContainerInteractionPacket: LootableContainer was null!");
                }
            }

            public readonly void Serialize(NetDataWriter writer)
            {
                writer.Put(InteractiveId);
                writer.Put((byte)InteractionType);
            }
        }

        public struct ProceedPacket : ISubPacket
        {
            public GStruct375<EBodyPart> BodyParts;
            public MongoID ItemId;
            public float Amount;
            public int AnimationVariant;
            public EProceedType ProceedType;
            public bool Scheduled;

            public ProceedPacket(NetDataReader reader)
            {
                ProceedType = reader.GetEnum<EProceedType>();
                if (ProceedType is not EProceedType.EmptyHands)
                {
                    ItemId = reader.GetMongoID();
                }
                else
                {
                    Scheduled = reader.GetBool();
                }
                if (ProceedType is EProceedType.FoodClass or EProceedType.MedsClass)
                {
                    Amount = reader.GetFloat();
                    AnimationVariant = reader.GetInt();
                    if (ProceedType is EProceedType.MedsClass)
                    {
                        int bodyPartsAmount = reader.GetInt();
                        for (int i = 0; i < bodyPartsAmount; i++)
                        {
                            BodyParts.Add((EBodyPart)reader.GetByte());
                        }
                    }
                }
            }

            public void Execute(CoopPlayer player)
            {
                if (player is ObservedCoopPlayer observedPlayer)
                {
                    observedPlayer.HandleProceedPacket(in this);
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.PutEnum(ProceedType);
                if (ProceedType is not EProceedType.EmptyHands)
                {
                    writer.PutMongoID(ItemId);
                }
                else
                {
                    writer.Put(Scheduled);
                }
                if (ProceedType is EProceedType.FoodClass or EProceedType.MedsClass)
                {
                    writer.Put(Amount);
                    writer.Put(AnimationVariant);
                    if (ProceedType is EProceedType.MedsClass)
                    {
                        int bodyPartsAmount = BodyParts.Length;
                        writer.Put(bodyPartsAmount);
                        for (int i = 0; i < bodyPartsAmount; i++)
                        {
                            writer.Put((byte)BodyParts[i]);
                        }
                    }
                }
            }
        }

        public struct HeadLightsPacket : ISubPacket
        {
            public int Amount;
            public bool IsSilent;
            public FirearmLightStateStruct[] LightStates;

            public HeadLightsPacket(NetDataReader reader)
            {
                Amount = reader.GetInt();
                IsSilent = reader.GetBool();
                if (Amount > 0)
                {
                    LightStates = new FirearmLightStateStruct[Amount];
                    for (int i = 0; i < Amount; i++)
                    {
                        LightStates[i] = new()
                        {
                            Id = reader.GetString(),
                            IsActive = reader.GetBool(),
                            LightMode = reader.GetInt()
                        };
                    }
                }
            }

            public readonly void Execute(CoopPlayer player)
            {
                player.HandleHeadLightsPacket(this);
            }

            public readonly void Serialize(NetDataWriter writer)
            {
                writer.Put(Amount);
                writer.Put(IsSilent);
                if (Amount > 0)
                {
                    for (int i = 0; i < Amount; i++)
                    {
                        writer.Put(LightStates[i].Id);
                        writer.Put(LightStates[i].IsActive);
                        writer.Put(LightStates[i].LightMode);
                    }
                }
            }
        }

        public struct InventoryChangedPacket : ISubPacket
        {
            public bool InventoryOpen;

            public InventoryChangedPacket(NetDataReader reader)
            {
                InventoryOpen = reader.GetBool();
            }

            public readonly void Execute(CoopPlayer player)
            {
                player.HandleInventoryOpenedPacket(InventoryOpen);
            }

            public readonly void Serialize(NetDataWriter writer)
            {
                writer.Put(InventoryOpen);
            }
        }

        public struct DropPacket : ISubPacket
        {
            public bool FastDrop;

            public DropPacket(NetDataReader reader)
            {
                FastDrop = reader.GetBool();
            }

            public readonly void Execute(CoopPlayer player)
            {
                player.HandleDropPacket(FastDrop);
            }

            public readonly void Serialize(NetDataWriter writer)
            {
                writer.Put(FastDrop);
            }
        }

        public struct StationaryPacket : ISubPacket
        {
            public EStationaryCommand Command;
            public string Id;

            public StationaryPacket(NetDataReader reader)
            {
                Command = (EStationaryCommand)reader.GetByte();
                if (Command == EStationaryCommand.Occupy)
                {
                    Id = reader.GetString();
                }
            }

            public readonly void Execute(CoopPlayer player)
            {
                StationaryWeapon stationaryWeapon = (Command == EStationaryCommand.Occupy)
                    ? Singleton<GameWorld>.Instance.FindStationaryWeapon(Id) : null;
                player.ObservedStationaryInteract(stationaryWeapon, (StationaryPacketStruct.EStationaryCommand)Command);
            }

            public readonly void Serialize(NetDataWriter writer)
            {
                writer.Put((byte)Command);
                if (Command == EStationaryCommand.Occupy)
                {
                    writer.Put(Id);
                }
            }
        }

        public struct InteractionPacket : ISubPacket
        {
            public EInteraction Interaction;

            public InteractionPacket(NetDataReader reader)
            {
                Interaction = (EInteraction)reader.GetByte();
            }

            public readonly void Execute(CoopPlayer player)
            {
                player.SetInteractInHands(Interaction);
            }

            public readonly void Serialize(NetDataWriter writer)
            {
                writer.Put((byte)Interaction);
            }
        }

        public struct VaultPacket : ISubPacket
        {
            public EVaultingStrategy VaultingStrategy;
            public Vector3 VaultingPoint;
            public float VaultingHeight;
            public float VaultingLength;
            public float VaultingSpeed;
            public float BehindObstacleHeight;
            public float AbsoluteForwardVelocity;

            public VaultPacket(NetDataReader reader)
            {
                VaultingStrategy = (EVaultingStrategy)reader.GetByte();
                VaultingPoint = reader.GetVector3();
                VaultingHeight = reader.GetFloat();
                VaultingLength = reader.GetFloat();
                VaultingSpeed = reader.GetFloat();
                BehindObstacleHeight = reader.GetFloat();
                AbsoluteForwardVelocity = reader.GetFloat();
            }

            public readonly void Execute(CoopPlayer player)
            {
                // A headless client can get stuck in permanent high-velocity states due to vaulting, skip it
                if (!FikaBackendUtils.IsHeadless)
                {
                    player.DoObservedVault(this);
                }
            }

            public readonly void Serialize(NetDataWriter writer)
            {
                writer.Put((byte)VaultingStrategy);
                writer.PutVector3(VaultingPoint);
                writer.Put(VaultingHeight);
                writer.Put(VaultingLength);
                writer.Put(VaultingSpeed);
                writer.Put(BehindObstacleHeight);
                writer.Put(AbsoluteForwardVelocity);
            }
        }

        public struct MountingPacket : ISubPacket
        {
            public MountingPacketStruct.EMountingCommand Command;
            public bool IsMounted;
            public Vector3 MountDirection;
            public Vector3 MountingPoint;
            public Vector3 TargetPos;
            public float TargetPoseLevel;
            public float TargetHandsRotation;
            public Vector2 PoseLimit;
            public Vector2 PitchLimit;
            public Vector2 YawLimit;
            public Quaternion TargetBodyRotation;
            public float CurrentMountingPointVerticalOffset;
            public short MountingDirection;
            public float TransitionTime;

            public MountingPacket(NetDataReader reader)
            {
                Command = (MountingPacketStruct.EMountingCommand)reader.GetByte();
                if (Command == MountingPacketStruct.EMountingCommand.Update)
                {
                    CurrentMountingPointVerticalOffset = reader.GetFloat();
                }
                if (Command is <= MountingPacketStruct.EMountingCommand.Exit)
                {
                    IsMounted = reader.GetBool();
                }
                ;
                if (Command == MountingPacketStruct.EMountingCommand.Enter)
                {
                    MountDirection = reader.GetVector3();
                    MountingPoint = reader.GetVector3();
                    MountingDirection = reader.GetShort();
                    TransitionTime = reader.GetFloat();
                    TargetPos = reader.GetVector3();
                    TargetPoseLevel = reader.GetFloat();
                    TargetHandsRotation = reader.GetFloat();
                    TargetBodyRotation = reader.GetQuaternion();
                    PoseLimit = reader.GetVector2();
                    PitchLimit = reader.GetVector2();
                    YawLimit = reader.GetVector2();
                }
            }

            public readonly void Execute(CoopPlayer player)
            {
                switch (Command)
                {
                    case MountingPacketStruct.EMountingCommand.Enter:
                        {
                            player.MovementContext.PlayerMountingPointData.SetData(new MountPointData(MountingPoint, MountDirection,
                                (EMountSideDirection)MountingDirection), TargetPos, TargetPoseLevel, TargetHandsRotation,
                                TransitionTime, TargetBodyRotation, PoseLimit, PitchLimit, YawLimit);
                            player.MovementContext.PlayerMountingPointData.CurrentMountingPointVerticalOffset = CurrentMountingPointVerticalOffset;
                            player.MovementContext.EnterMountedState();
                        }
                        break;
                    case MountingPacketStruct.EMountingCommand.Exit:
                        {
                            player.MovementContext.ExitMountedState();
                        }
                        break;
                    case MountingPacketStruct.EMountingCommand.Update:
                        {
                            player.MovementContext.PlayerMountingPointData.CurrentMountingPointVerticalOffset = CurrentMountingPointVerticalOffset;
                        }
                        break;
                    case MountingPacketStruct.EMountingCommand.StartLeaving:
                        {
                            if (player.MovementContext is ObservedMovementContext observedMovementContext)
                            {
                                observedMovementContext.ObservedStartExitingMountedState();
                            }
                        }
                        break;
                    default:
                        break;
                }
            }

            public readonly void Serialize(NetDataWriter writer)
            {
                writer.Put((byte)Command);
                if (Command == MountingPacketStruct.EMountingCommand.Update)
                {
                    writer.Put(CurrentMountingPointVerticalOffset);
                }
                if (Command is <= MountingPacketStruct.EMountingCommand.Exit)
                {
                    writer.Put(IsMounted);
                }
                if (Command == MountingPacketStruct.EMountingCommand.Enter)
                {
                    writer.PutVector3(MountDirection);
                    writer.PutVector3(MountingPoint);
                    writer.Put(MountingDirection);
                    writer.Put(TransitionTime);
                    writer.PutVector3(TargetPos);
                    writer.Put(TargetPoseLevel);
                    writer.Put(TargetHandsRotation);
                    writer.PutQuaternion(TargetBodyRotation);
                    writer.PutVector2(PoseLimit);
                    writer.PutVector2(PitchLimit);
                    writer.PutVector2(YawLimit);
                }
            }
        }
    }
}
