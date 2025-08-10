using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.Vaulting;
using EFT.WeaponMounting;
using Fika.Core.Main.ObservedClasses;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Pools;
using LiteNetLib.Utils;
using System;
using static Fika.Core.Main.Players.FikaPlayer;
using static Fika.Core.Networking.Packets.SubPacket;

namespace Fika.Core.Networking.Packets.Player
{
    public class CommonSubPackets
    {
        public class PhrasePacket : IPoolSubPacket
        {
            private PhrasePacket()
            {

            }

            public static PhrasePacket CreateInstance()
            {
                return new();
            }

            public static PhrasePacket FromValue(EPhraseTrigger trigger, int index)
            {
                PhrasePacket packet = CommonSubPacketPoolManager.Instance.GetPacket<PhrasePacket>(ECommonSubPacketType.Phrase);
                packet.PhraseTrigger = trigger;
                packet.PhraseIndex = index;
                return packet;
            }

            public EPhraseTrigger PhraseTrigger;
            public int PhraseIndex;

            public void Execute(FikaPlayer player)
            {
                if (player.gameObject.activeSelf && player.HealthController.IsAlive)
                {
                    player.Speaker.PlayDirect(PhraseTrigger, PhraseIndex);
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.PutEnum(PhraseTrigger);
                writer.Put(PhraseIndex);
            }

            public void Deserialize(NetDataReader reader)
            {
                PhraseTrigger = reader.GetEnum<EPhraseTrigger>();
                PhraseIndex = reader.GetInt();
            }

            public void Dispose()
            {
                PhraseTrigger = EPhraseTrigger.None;
                PhraseIndex = 0;
            }
        }

        public class WorldInteractionPacket : IPoolSubPacket
        {
            private WorldInteractionPacket()
            {

            }

            public static WorldInteractionPacket CreateInstance()
            {
                return new();
            }

            public static WorldInteractionPacket FromValue(string interactiveId, EInteractionType interactionType, EInteractionStage interactionStage, string itemId = null)
            {
                WorldInteractionPacket packet = CommonSubPacketPoolManager.Instance.GetPacket<WorldInteractionPacket>(ECommonSubPacketType.WorldInteraction);
                packet.InteractiveId = interactiveId;
                packet.InteractionType = interactionType;
                packet.InteractionStage = interactionStage;
                packet.ItemId = itemId;
                return packet;
            }

            public string InteractiveId;
            public EInteractionType InteractionType;
            public EInteractionStage InteractionStage;
            public string ItemId;

            public void Execute(FikaPlayer player)
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

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(InteractiveId);
                writer.PutEnum(InteractionType);
                writer.PutEnum(InteractionStage);
                if (InteractionType == EInteractionType.Unlock)
                {
                    writer.Put(ItemId);
                }
            }

            public void Deserialize(NetDataReader reader)
            {
                InteractiveId = reader.GetString();
                InteractionType = reader.GetEnum<EInteractionType>();
                InteractionStage = reader.GetEnum<EInteractionStage>();
                if (InteractionType == EInteractionType.Unlock)
                {
                    ItemId = reader.GetString();
                }
            }

            public void Dispose()
            {
                InteractiveId = null;
                InteractionType = default;
                InteractionStage = default;
                ItemId = null;
            }
        }

        public class ContainerInteractionPacket : IPoolSubPacket
        {
            private ContainerInteractionPacket()
            {

            }

            public static ContainerInteractionPacket CreateInstance()
            {
                return new();
            }

            public static ContainerInteractionPacket FromValue(string interactiveId, EInteractionType interactionType)
            {
                ContainerInteractionPacket packet = CommonSubPacketPoolManager.Instance.GetPacket<ContainerInteractionPacket>(ECommonSubPacketType.ContainerInteraction);
                packet.InteractiveId = interactiveId;
                packet.InteractionType = interactionType;
                return packet;
            }

            public string InteractiveId;
            public EInteractionType InteractionType;

            public void Execute(FikaPlayer player)
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

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(InteractiveId);
                writer.PutEnum(InteractionType);
            }

            public void Deserialize(NetDataReader reader)
            {
                InteractiveId = reader.GetString();
                InteractionType = reader.GetEnum<EInteractionType>();
            }

            public void Dispose()
            {
                InteractiveId = null;
                InteractionType = default;
            }
        }

        public class ProceedPacket : IPoolSubPacket
        {
            private ProceedPacket()
            {

            }

            public static ProceedPacket CreateInstance()
            {
                return new();
            }

            public static ProceedPacket FromValue(GStruct375<EBodyPart> bodyParts, MongoID itemId, float amount, int animationVariant, EProceedType proceedType, bool scheduled)
            {
                ProceedPacket packet = CommonSubPacketPoolManager.Instance.GetPacket<ProceedPacket>(ECommonSubPacketType.Proceed);
                packet.BodyParts = bodyParts;
                packet.ItemId = itemId;
                packet.Amount = amount;
                packet.AnimationVariant = animationVariant;
                packet.ProceedType = proceedType;
                packet.Scheduled = scheduled;
                return packet;
            }

            public GStruct375<EBodyPart> BodyParts;
            public MongoID ItemId;
            public float Amount;
            public int AnimationVariant;
            public EProceedType ProceedType;
            public bool Scheduled;

            public void Execute(FikaPlayer player)
            {
                if (player is ObservedPlayer observedPlayer)
                {
                    observedPlayer.HandleProceedPacket(this);
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
                            writer.PutEnum(BodyParts[i]);
                        }
                    }
                }
            }

            public void Deserialize(NetDataReader reader)
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
                            BodyParts.Add(reader.GetEnum<EBodyPart>());
                        }
                    }
                }
            }

            public void Dispose()
            {
                BodyParts = default;
                ItemId = default;
                Amount = 0f;
                AnimationVariant = 0;
                ProceedType = default;
                Scheduled = false;
            }
        }

        public class HeadLightsPacket : IPoolSubPacket
        {
            private HeadLightsPacket()
            {

            }

            public static HeadLightsPacket CreateInstance()
            {
                return new();
            }

            public static HeadLightsPacket FromValue(int amount, bool isSilent, FirearmLightStateStruct[] lightStates)
            {
                HeadLightsPacket packet = CommonSubPacketPoolManager.Instance.GetPacket<HeadLightsPacket>(ECommonSubPacketType.HeadLights);
                packet.Amount = amount;
                packet.IsSilent = isSilent;
                packet.LightStates = lightStates;
                return packet;
            }

            public int Amount;
            public bool IsSilent;
            public FirearmLightStateStruct[] LightStates;

            public void Execute(FikaPlayer player)
            {
                player.HandleHeadLightsPacket(this);
            }

            public void Serialize(NetDataWriter writer)
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

            public void Deserialize(NetDataReader reader)
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

            public void Dispose()
            {
                Amount = 0;
                IsSilent = false;
                LightStates = null;
            }
        }

        public class InventoryChangedPacket : IPoolSubPacket
        {
            private InventoryChangedPacket()
            {

            }

            public static InventoryChangedPacket CreateInstance()
            {
                return new();
            }

            public static InventoryChangedPacket FromValue(bool inventoryOpen)
            {
                InventoryChangedPacket packet = CommonSubPacketPoolManager.Instance.GetPacket<InventoryChangedPacket>(ECommonSubPacketType.InventoryChanged);
                packet.InventoryOpen = inventoryOpen;
                return packet;
            }

            public bool InventoryOpen;

            public void Execute(FikaPlayer player)
            {
                player.HandleInventoryOpenedPacket(InventoryOpen);
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(InventoryOpen);
            }

            public void Deserialize(NetDataReader reader)
            {
                InventoryOpen = reader.GetBool();
            }

            public void Dispose()
            {
                InventoryOpen = false;
            }
        }

        public class DropPacket : IPoolSubPacket
        {
            private DropPacket()
            {

            }

            public static DropPacket CreateInstance()
            {
                return new();
            }

            public static DropPacket FromValue(bool fastDrop)
            {
                DropPacket packet = CommonSubPacketPoolManager.Instance.GetPacket<DropPacket>(ECommonSubPacketType.Drop);
                packet.FastDrop = fastDrop;
                return packet;
            }

            public bool FastDrop;

            public void Execute(FikaPlayer player)
            {
                player.HandleDropPacket(FastDrop);
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(FastDrop);
            }

            public void Deserialize(NetDataReader reader)
            {
                FastDrop = reader.GetBool();
            }

            public void Dispose()
            {
                FastDrop = false;
            }
        }

        public class StationaryPacket : IPoolSubPacket
        {
            private StationaryPacket()
            {

            }

            public static StationaryPacket CreateInstance()
            {
                return new();
            }

            public static StationaryPacket FromValue(EStationaryCommand command, string id = null)
            {
                StationaryPacket packet = CommonSubPacketPoolManager.Instance.GetPacket<StationaryPacket>(ECommonSubPacketType.Stationary);
                packet.Command = command;
                packet.Id = id;
                return packet;
            }

            public EStationaryCommand Command;
            public string Id;

            public void Execute(FikaPlayer player)
            {
                StationaryWeapon stationaryWeapon = (Command == EStationaryCommand.Occupy)
                    ? Singleton<GameWorld>.Instance.FindStationaryWeapon(Id) : null;
                player.ObservedStationaryInteract(stationaryWeapon, (StationaryPacketStruct.EStationaryCommand)Command);
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.PutEnum(Command);
                if (Command == EStationaryCommand.Occupy)
                {
                    writer.Put(Id);
                }
            }

            public void Deserialize(NetDataReader reader)
            {
                Command = reader.GetEnum<EStationaryCommand>();
                if (Command == EStationaryCommand.Occupy)
                {
                    Id = reader.GetString();
                }
            }

            public void Dispose()
            {
                Command = default;
                Id = null;
            }
        }

        public class InteractionPacket : IPoolSubPacket
        {
            private InteractionPacket()
            {

            }

            public static InteractionPacket CreateInstance()
            {
                return new();
            }

            public static InteractionPacket FromValue(EInteraction interaction)
            {
                InteractionPacket packet = CreateInstance();
                packet.Interaction = interaction;
                return packet;
            }

            public EInteraction Interaction;

            public void Execute(FikaPlayer player)
            {
                player.SetInteractInHands(Interaction);
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put((byte)Interaction);
            }

            public void Deserialize(NetDataReader reader)
            {
                Interaction = reader.GetEnum<EInteraction>();
            }

            public void Dispose()
            {
                Interaction = default;
            }
        }

        public class VaultPacket : IPoolSubPacket
        {
            private VaultPacket()
            {

            }

            public static VaultPacket CreateInstance()
            {
                return new();
            }

            public static VaultPacket FromValue(EVaultingStrategy vaultingStrategy, Vector3 vaultingPoint, float vaultingHeight, float vaultingLength, float vaultingSpeed, float behindObstacleHeight, float absoluteForwardVelocity)
            {
                VaultPacket packet = CommonSubPacketPoolManager.Instance.GetPacket<VaultPacket>(ECommonSubPacketType.Vault);
                packet.VaultingStrategy = vaultingStrategy;
                packet.VaultingPoint = vaultingPoint;
                packet.VaultingHeight = vaultingHeight;
                packet.VaultingLength = vaultingLength;
                packet.VaultingSpeed = vaultingSpeed;
                packet.BehindObstacleHeight = behindObstacleHeight;
                packet.AbsoluteForwardVelocity = absoluteForwardVelocity;
                return packet;
            }

            public EVaultingStrategy VaultingStrategy;
            public Vector3 VaultingPoint;
            public float VaultingHeight;
            public float VaultingLength;
            public float VaultingSpeed;
            public float BehindObstacleHeight;
            public float AbsoluteForwardVelocity;

            public void Execute(FikaPlayer player)
            {
                // A headless client can get stuck in permanent high-velocity states due to vaulting, skip it
                if (!FikaBackendUtils.IsHeadless)
                {
                    player.DoObservedVault(this);
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.PutEnum(VaultingStrategy);
                writer.PutStruct(VaultingPoint);
                writer.Put(VaultingHeight);
                writer.Put(VaultingLength);
                writer.Put(VaultingSpeed);
                writer.Put(BehindObstacleHeight);
                writer.Put(AbsoluteForwardVelocity);
            }

            public void Deserialize(NetDataReader reader)
            {
                VaultingStrategy = reader.GetEnum<EVaultingStrategy>();
                VaultingPoint = reader.GetStruct<Vector3>();
                VaultingHeight = reader.GetFloat();
                VaultingLength = reader.GetFloat();
                VaultingSpeed = reader.GetFloat();
                BehindObstacleHeight = reader.GetFloat();
                AbsoluteForwardVelocity = reader.GetFloat();
            }

            public void Dispose()
            {
                VaultingStrategy = default;
                VaultingPoint = default;
                VaultingHeight = 0f;
                VaultingLength = 0f;
                VaultingSpeed = 0f;
                BehindObstacleHeight = 0f;
                AbsoluteForwardVelocity = 0f;
            }
        }

        public class MountingPacket : IPoolSubPacket
        {
            private MountingPacket()
            {

            }

            public static MountingPacket CreateInstance()
            {
                return new();
            }

            public static MountingPacket FromValue(MountingPacketStruct.EMountingCommand command, bool isMounted,
                Vector3 mountDirection, Vector3 mountingPoint, float currentMountingPointVerticalOffset, short mountingDirection)
            {
                MountingPacket packet = CommonSubPacketPoolManager.Instance.GetPacket<MountingPacket>(ECommonSubPacketType.Mounting);
                packet.Command = command;
                packet.IsMounted = isMounted;
                packet.MountDirection = mountDirection;
                packet.MountingPoint = mountingPoint;
                packet.CurrentMountingPointVerticalOffset = currentMountingPointVerticalOffset;
                packet.MountingDirection = mountingDirection;
                return packet;
            }

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

            public void Execute(FikaPlayer player)
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

            public void Serialize(NetDataWriter writer)
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
                    writer.PutStruct(MountDirection);
                    writer.PutStruct(MountingPoint);
                    writer.Put(MountingDirection);
                    writer.Put(TransitionTime);
                    writer.PutStruct(TargetPos);
                    writer.Put(TargetPoseLevel);
                    writer.Put(TargetHandsRotation);
                    writer.PutStruct(TargetBodyRotation);
                    writer.PutStruct(PoseLimit);
                    writer.PutStruct(PitchLimit);
                    writer.PutStruct(YawLimit);
                }
            }

            public void Deserialize(NetDataReader reader)
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
                    MountDirection = reader.GetStruct<Vector3>();
                    MountingPoint = reader.GetStruct<Vector3>();
                    MountingDirection = reader.GetShort();
                    TransitionTime = reader.GetFloat();
                    TargetPos = reader.GetStruct<Vector3>();
                    TargetPoseLevel = reader.GetFloat();
                    TargetHandsRotation = reader.GetFloat();
                    TargetBodyRotation = reader.GetStruct<Quaternion>();
                    PoseLimit = reader.GetStruct<Vector2>();
                    PitchLimit = reader.GetStruct<Vector2>();
                    YawLimit = reader.GetStruct<Vector2>();
                }
            }

            public void Dispose()
            {
                Command = default;
                IsMounted = false;
                MountDirection = default;
                MountingPoint = default;
                TargetPos = default;
                TargetPoseLevel = 0f;
                TargetHandsRotation = 0f;
                PoseLimit = default;
                PitchLimit = default;
                YawLimit = default;
                TargetBodyRotation = default;
                CurrentMountingPointVerticalOffset = 0f;
                MountingDirection = 0;
                TransitionTime = 0f;
            }
        }
    }
}
