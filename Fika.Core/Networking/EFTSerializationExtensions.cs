using EFT.BinarySerialization;
using EFT.InventoryLogic.Operations;
using EFT.Notes;
using EFT.Prestige;
using JsonType;
using System;
using System.Collections.Generic;
using EFT;
using EFT.InventoryLogic;
using EFT.Quests;
using Fika.Core.Main.Utils;

namespace Fika.Core.Networking;

/// <summary>
/// Extension methods to write/read EFT classes
/// </summary>
public static class EFTSerializationExtensions
{
    private static readonly List<Type> _indexToType = BinarySerializationMirrorExtensions._types;
    private static readonly Dictionary<Type, byte> _typeToByte;
    private static readonly Dictionary<Type, Action<NetDataWriter, object>> _serializers;
    private static readonly Dictionary<byte, Func<NetDataReader, object>> _deserializers;

    static EFTSerializationExtensions()
    {
        var count = _indexToType.Count;
        _typeToByte = new Dictionary<Type, byte>(count);
        _serializers = new Dictionary<Type, Action<NetDataWriter, object>>(count);
        _deserializers = new Dictionary<byte, Func<NetDataReader, object>>(count);

        for (var i = 0; i < count; i++)
        {
            _typeToByte[_indexToType[i]] = (byte)i;
        }

        RegisterSerializer<AggressorStats>((w, t) => w.PutAggressorStats(t));
        RegisterSerializer<ClassQuaternion>((w, t) => w.PutClassQuaternion(t));
        RegisterSerializer<ClassTransformSync>((w, t) => w.PutClassTransformSync(t));
        RegisterSerializer<ClassVector3>((w, t) => w.PutClassVector3(t));
        RegisterSerializer<AddNoteOperationDescriptor>((w, t) => w.PutEFTAddNoteOperationDescriptor(t));
        RegisterSerializer<ApplyKeyOperationDescriptor>((w, t) => w.PutEFTApplyKeyOperationDescriptor(t));
        RegisterSerializer<BindItemOperationDescriptor>((w, t) => w.PutEFTBindItemOperationDescriptor(t));
        RegisterSerializer<BodyPartDamageHistoryDescriptor>((w, t) => w.PutEFTBodyPartDamageHistoryDescriptor(t));
        RegisterSerializer<BonusDescriptor>((w, t) => w.PutEFTBonusDescriptor(t));
        RegisterSerializer<CheckMagazineOperationDescriptor>((w, t) => w.PutEFTCheckMagazineOperationDescriptor(t));
        RegisterSerializer<ContainerDescriptor>((w, t) => w.PutEFTContainerDescriptor(t));
        RegisterSerializer<CounterCollectionDescriptor>((w, t) => w.PutEFTCounterCollectionDescriptor(t));
        RegisterSerializer<CounterCollectionItemDescriptor>((w, t) => w.PutEFTCounterCollectionItemDescriptor(t));
        RegisterSerializer<CreateMapMarkerOperationDescriptor>((w, t) => w.PutEFTCreateMapMarkerOperationDescriptor(t));
        RegisterSerializer<CultistAmuletComponentDescriptor>((w, t) => w.PutEFTCultistAmuletComponentDescriptor(t));
        RegisterSerializer<DamageHistoryDescriptor>((w, t) => w.PutEFTDamageHistoryDescriptor(t));
        RegisterSerializer<DamageStatsDescriptor>((w, t) => w.PutEFTDamageStatsDescriptor(t));
        RegisterSerializer<DeathCause>((w, t) => w.PutEFTDeathCause(t));
        RegisterSerializer<DeleteMapMarkerOperationDescriptor>((w, t) => w.PutEFTDeleteMapMarkerOperationDescriptor(t));
        RegisterSerializer<DeleteNoteOperationDescriptor>((w, t) => w.PutEFTDeleteNoteOperationDescriptor(t));
        RegisterSerializer<DestroyedItem>((w, t) => w.PutEFTDestroyedItem(t));
        RegisterSerializer<DogTagComponentDescriptor>((w, t) => w.PutEFTDogTagComponentDescriptor(t));
        RegisterSerializer<DroppedItem>((w, t) => w.PutEFTDroppedItem(t));
        RegisterSerializer<EditMapMarkerOperationDescriptor>((w, t) => w.PutEFTEditMapMarkerOperationDescriptor(t));
        RegisterSerializer<EditNoteOperationDescriptor>((w, t) => w.PutEFTEditNoteOperationDescriptor(t));
        RegisterSerializer<ExamineMalfTypeOperationDescriptor>((w, t) => w.PutEFTExamineMalfTypeOperationDescriptor(t));
        RegisterSerializer<ExamineMalfunctionOperationDescriptor>((w, t) => w.PutEFTExamineMalfunctionOperationDescriptor(t));
        RegisterSerializer<ExamineOperationDescriptor>((w, t) => w.PutEFTExamineOperationDescriptor(t));
        RegisterSerializer<FaceShieldComponentDescriptor>((w, t) => w.PutEFTFaceShieldComponentDescriptor(t));
        RegisterSerializer<FaceshieldMarkOperationDescriptor>((w, t) => w.PutEFTFaceshieldMarkOperationDescriptor(t));
        RegisterSerializer<FireModeComponentDescriptor>((w, t) => w.PutEFTFireModeComponentDescriptor(t));
        RegisterSerializer<FoldableComponentDescriptor>((w, t) => w.PutEFTFoldableComponentDescriptor(t));
        RegisterSerializer<FoldOperationDescriptor>((w, t) => w.PutEFTFoldOperationDescriptor(t));
        RegisterSerializer<FoodDrinkComponentDescriptor>((w, t) => w.PutEFTFoodDrinkComponentDescriptor(t));
        RegisterSerializer<FoundInRaidItem>((w, t) => w.PutEFTFoundInRaidItem(t));
        RegisterSerializer<GridDescriptor>((w, t) => w.PutEFTGridDescriptor(t));
        RegisterSerializer<GridItemAddressDescriptor>((w, t) => w.PutEFTGridItemAddressDescriptor(t));
        RegisterSerializer<InventoryDescriptor>((w, t) => w.PutEFTInventoryDescriptor(t));
        RegisterSerializer<InventoryEquipmentDescriptor>((w, t) => w.PutEFTInventoryEquipmentDescriptor(t));
        RegisterSerializer<MapMarker>((w, t) => w.PutEFTInventoryLogicMapMarker(t));
        RegisterSerializer<AddToWishlistOperationDescriptor>((w, t) => w.PutEFTInventoryLogicOperationsAddToWishlistOperationDescriptor(t));
        RegisterSerializer<ChangeItemsOperationDescriptor>((w, t) => w.PutEFTInventoryLogicOperationsChangeItemsOperationDescriptor(t));
        RegisterSerializer<ChangeWishlistItemCategoryOperationDescriptor>((w, t) => w.PutEFTInventoryLogicOperationsChangeWishlistItemCategoryOperationDescriptor(t));
        RegisterSerializer<PurchaseTraderServiceOperationDescriptor>((w, t) => w.PutEFTInventoryLogicOperationsPurchaseTraderServiceOperationDescriptor(t));
        RegisterSerializer<RemoveFromWishlistOperationDescriptor>((w, t) => w.PutEFTInventoryLogicOperationsRemoveFromWishlistOperationDescriptor(t));
        RegisterSerializer<SearchContentOperationDescriptor>((w, t) => w.PutEFTInventoryLogicOperationsSearchContentOperationDescriptor(t));
        RegisterSerializer<SearchSuboperationDescriptor>((w, t) => w.PutEFTInventoryLogicOperationsSearchSuboperationDescriptor(t));
        RegisterSerializer<SplitToNowhereDescriptor>((w, t) => w.PutEFTInventoryLogicOperationsSplitToNowhereDescriptor(t));
        RegisterSerializer<TransferFromNowhereDescriptor>((w, t) => w.PutEFTInventoryLogicOperationsTransferFromNowhereDescriptor(t));
        RegisterSerializer<ItemDescriptor>((w, t) => w.PutEFTItemDescriptor(t));
        RegisterSerializer<ItemInfoDescriptor>((w, t) => w.PutEFTItemInfoDescriptor(t));
        RegisterSerializer<ItemInGridDescriptor>((w, t) => w.PutEFTItemInGridDescriptor(t));
        RegisterSerializer<JsonCorpseDescriptor>((w, t) => w.PutEFTJsonCorpseDescriptor(t));
        RegisterSerializer<JsonLootItemDescriptor>((w, t) => w.PutEFTJsonLootItemDescriptor(t));
        RegisterSerializer<KeyComponentDescriptor>((w, t) => w.PutEFTKeyComponentDescriptor(t));
        RegisterSerializer<LightComponentDescriptor>((w, t) => w.PutEFTLightComponentDescriptor(t));
        RegisterSerializer<LoadMagOperationDescriptor>((w, t) => w.PutEFTLoadMagOperationDescriptor(t));
        RegisterSerializer<LockableComponentDescriptor>((w, t) => w.PutEFTLockableComponentDescriptor(t));
        RegisterSerializer<LootDataDescriptor>((w, t) => w.PutEFTLootDataDescriptor(t));
        RegisterSerializer<MalfunctionDescriptor>((w, t) => w.PutEFTMalfunctionDescriptor(t));
        RegisterSerializer<MapComponentDescriptor>((w, t) => w.PutEFTMapComponentDescriptor(t));
        RegisterSerializer<MedKitComponentDescriptor>((w, t) => w.PutEFTMedKitComponentDescriptor(t));
        RegisterSerializer<MergeOperationDescriptor>((w, t) => w.PutEFTMergeOperationDescriptor(t));
        RegisterSerializer<MoveOperationDescriptor>((w, t) => w.PutEFTMoveOperationDescriptor(t));
        RegisterSerializer<NestedItemDescriptor>((w, t) => w.PutEFTNestedItemDescriptor(t));
        RegisterSerializer<Note>((w, t) => w.PutEFTNotesNote(t));
        RegisterSerializer<NotesManager.NotesDescriptor>((w, t) => w.PutEFTNotesNotesManagerNotesDescriptor(t));
        RegisterSerializer<OperateStationaryWeaponOperationDescriptor>((w, t) => w.PutEFTOperateStationaryWeaponOperationDescriptor(t));
        RegisterSerializer<OwnerItselfDescriptor>((w, t) => w.PutEFTOwnerItselfDescriptor(t));
        RegisterSerializer<PlantTripwireOperationDescriptor>((w, t) => w.PutEFTPlantTripwireOperationDescriptor(t));
        RegisterSerializer<PlayerVisualRepresentationDescriptor>((w, t) => w.PutEFTPlayerVisualRepresentationDescriptor(t));
        RegisterSerializer<PoisonComponentDescriptor>((w, t) => w.PutEFTPoisonComponentDescriptor(t));
        RegisterSerializer<PrestigeStatusData>((w, t) => w.PutEFTPrestigePrestigeStatusData(t));
        RegisterSerializer<Profile.HealthInfo>((w, t) => w.PutEFTProfileHealthInfo(t));
        RegisterSerializer<Profile.HealthInfo.BodyPartInfo>((w, t) => w.PutEFTProfileHealthInfoBodyPartInfo(t));
        RegisterSerializer<Profile.HealthInfo.EffectInfo>((w, t) => w.PutEFTProfileHealthInfoEffectInfo(t));
        RegisterSerializer<Profile.HealthInfo.ValueInfo>((w, t) => w.PutEFTProfileHealthInfoValueInfo(t));
        RegisterSerializer<Profile.MoneyTransferLimitData>((w, t) => w.PutEFTProfileMoneyTransferLimitData(t));
        RegisterSerializer<Profile.UnlockedInfo>((w, t) => w.PutEFTProfileUnlockedInfo(t));
        RegisterSerializer<ProfileBanDescriptor>((w, t) => w.PutEFTProfileBanDescriptor(t));
        RegisterSerializer<ProfileDescriptor>((w, t) => w.PutEFTProfileDescriptor(t));
        RegisterSerializer<ProfileInfoDescriptor>((w, t) => w.PutEFTProfileInfoDescriptor(t));
        RegisterSerializer<ProfileSettings>((w, t) => w.PutEFTProfileSettings(t));
        RegisterSerializer<ProfileStatsDescriptor>((w, t) => w.PutEFTProfileStatsDescriptor(t));
        RegisterSerializer<ProfileStatsSeparatorDescriptor>((w, t) => w.PutEFTProfileStatsSeparatorDescriptor(t));
        RegisterSerializer<QuestAcceptDescriptor>((w, t) => w.PutEFTQuestAcceptDescriptor(t));
        RegisterSerializer<QuestFinishDescriptor>((w, t) => w.PutEFTQuestFinishDescriptor(t));
        RegisterSerializer<QuestHandoverDescriptor>((w, t) => w.PutEFTQuestHandoverDescriptor(t));
        RegisterSerializer<QuestDataClass>((w, t) => w.PutEFTQuestsQuestStatusData(t));
        RegisterSerializer<RecodableComponentDescriptor>((w, t) => w.PutEFTRecodableComponentDescriptor(t));
        RegisterSerializer<RemoveOperationDescriptor>((w, t) => w.PutEFTRemoveOperationDescriptor(t));
        RegisterSerializer<RepairableComponentDescriptor>((w, t) => w.PutEFTRepairableComponentDescriptor(t));
        RegisterSerializer<RepairEnhancementComponentDescriptor>((w, t) => w.PutEFTRepairEnhancementComponentDescriptor(t));
        RegisterSerializer<RepairKitComponentDescriptor>((w, t) => w.PutEFTRepairKitComponentDescriptor(t));
        RegisterSerializer<ResourceItemComponentDescriptor>((w, t) => w.PutEFTResourceItemComponentDescriptor(t));
        RegisterSerializer<SceneResourceKey>((w, t) => w.PutEFTSceneResourceKey(t));
        RegisterSerializer<ResourceKey>((w, t) => w.PutEFTResourceKey(t));
        RegisterSerializer<SetDialogProgressOperationDescriptor>((w, t) => w.PutEFTSetDialogProgressOperationDescriptor(t));
        RegisterSerializer<SetupItemOperationDescriptor>((w, t) => w.PutEFTSetupItemOperationDescriptor(t));
        RegisterSerializer<SetVariableOperationDescriptor>((w, t) => w.PutEFTSetVariableOperationDescriptor(t));
        RegisterSerializer<ShellTemplateDescriptor>((w, t) => w.PutEFTShellTemplateDescriptor(t));
        RegisterSerializer<SightComponentDescriptor>((w, t) => w.PutEFTSightComponentDescriptor(t));
        RegisterSerializer<SkillsDescriptor>((w, t) => w.PutEFTSkillsDescriptor(t));
        RegisterSerializer<SkillsDescriptor.MasteringInfoDescriptor>((w, t) => w.PutEFTSkillsDescriptorMasteringInfoDescriptor(t));
        RegisterSerializer<SkillsDescriptor.SkillInfoDescriptor>((w, t) => w.PutEFTSkillsDescriptorSkillInfoDescriptor(t));
        RegisterSerializer<SlotDescriptor>((w, t) => w.PutEFTSlotDescriptor(t));
        RegisterSerializer<SlotItemAddressDescriptor>((w, t) => w.PutEFTSlotItemAddressDescriptor(t));
        RegisterSerializer<SplitOperationDescriptor>((w, t) => w.PutEFTSplitOperationDescriptor(t));
        RegisterSerializer<StackSlotDescriptor>((w, t) => w.PutEFTStackSlotDescriptor(t));
        RegisterSerializer<StackSlotItemAddressDescriptor>((w, t) => w.PutEFTStackSlotItemAddressDescriptor(t));
        RegisterSerializer<SwapOperationDescriptor>((w, t) => w.PutEFTSwapOperationDescriptor(t));
        RegisterSerializer<TagComponentDescriptor>((w, t) => w.PutEFTTagComponentDescriptor(t));
        RegisterSerializer<TagOperationDescriptor>((w, t) => w.PutEFTTagOperationDescriptor(t));
        RegisterSerializer<TaskConditionCounterDescriptor>((w, t) => w.PutEFTTaskConditionCounterDescriptor(t));
        RegisterSerializer<ThrowOperationDescriptor>((w, t) => w.PutEFTThrowOperationDescriptor(t));
        RegisterSerializer<TogglableComponentDescriptor>((w, t) => w.PutEFTTogglableComponentDescriptor(t));
        RegisterSerializer<ToggleOperationDescriptor>((w, t) => w.PutEFTToggleOperationDescriptor(t));
        RegisterSerializer<TraderInfoDescriptor>((w, t) => w.PutEFTTraderInfoDescriptor(t));
        RegisterSerializer<TraderServiceAvailabilityData>((w, t) => w.PutEFTTraderServiceAvailabilityData(t));
        RegisterSerializer<TransferOperationDescriptor>((w, t) => w.PutEFTTransferOperationDescriptor(t));
        RegisterSerializer<UnbindItemOperationDescriptor>((w, t) => w.PutEFTUnbindItemOperationDescriptor(t));
        RegisterSerializer<UnloadMagOperationDescriptor>((w, t) => w.PutEFTUnloadMagOperationDescriptor(t));
        RegisterSerializer<VictimStats>((w, t) => w.PutEFTVictimStats(t));
        RegisterSerializer<WeaponRechamberOperationDescriptor>((w, t) => w.PutEFTWeaponRechamberOperationDescriptor(t));
        RegisterSerializer<InsuredProfileItems>((w, t) => w.PutJsonTypeInsuredProfileItems(t));
        RegisterSerializer<PlayerInfo>((w, t) => w.PutJsonTypePlayerInfo(t));
        RegisterSerializer<WeightedLootPointSpawnPosition>((w, t) => w.PutWeightedLootPointSpawnPosition(t));

        RegisterDeserializer<AggressorStats>(r => r.GetAggressorStats());
        RegisterDeserializer<ClassQuaternion>(r => r.GetClassQuaternion());
        RegisterDeserializer<ClassTransformSync>(r => r.GetClassTransformSync());
        RegisterDeserializer<ClassVector3>(r => r.GetClassVector3());
        RegisterDeserializer<AddNoteOperationDescriptor>(r => r.GetEFTAddNoteOperationDescriptor());
        RegisterDeserializer<ApplyKeyOperationDescriptor>(r => r.GetEFTApplyKeyOperationDescriptor());
        RegisterDeserializer<BindItemOperationDescriptor>(r => r.GetEFTBindItemOperationDescriptor());
        RegisterDeserializer<BodyPartDamageHistoryDescriptor>(r => r.GetEFTBodyPartDamageHistoryDescriptor());
        RegisterDeserializer<BonusDescriptor>(r => r.GetEFTBonusDescriptor());
        RegisterDeserializer<CheckMagazineOperationDescriptor>(r => r.GetEFTCheckMagazineOperationDescriptor());
        RegisterDeserializer<ContainerDescriptor>(r => r.GetEFTContainerDescriptor());
        RegisterDeserializer<CounterCollectionDescriptor>(r => r.GetEFTCounterCollectionDescriptor());
        RegisterDeserializer<CounterCollectionItemDescriptor>(r => r.GetEFTCounterCollectionItemDescriptor());
        RegisterDeserializer<CreateMapMarkerOperationDescriptor>(r => r.GetEFTCreateMapMarkerOperationDescriptor());
        RegisterDeserializer<CultistAmuletComponentDescriptor>(r => r.GetEFTCultistAmuletComponentDescriptor());
        RegisterDeserializer<DamageHistoryDescriptor>(r => r.GetEFTDamageHistoryDescriptor());
        RegisterDeserializer<DamageStatsDescriptor>(r => r.GetEFTDamageStatsDescriptor());
        RegisterDeserializer<DeathCause>(r => r.GetEFTDeathCause());
        RegisterDeserializer<DeleteMapMarkerOperationDescriptor>(r => r.GetEFTDeleteMapMarkerOperationDescriptor());
        RegisterDeserializer<DeleteNoteOperationDescriptor>(r => r.GetEFTDeleteNoteOperationDescriptor());
        RegisterDeserializer<DestroyedItem>(r => r.GetEFTDestroyedItem());
        RegisterDeserializer<DogTagComponentDescriptor>(r => r.GetEFTDogTagComponentDescriptor());
        RegisterDeserializer<DroppedItem>(r => r.GetEFTDroppedItem());
        RegisterDeserializer<EditMapMarkerOperationDescriptor>(r => r.GetEFTEditMapMarkerOperationDescriptor());
        RegisterDeserializer<EditNoteOperationDescriptor>(r => r.GetEFTEditNoteOperationDescriptor());
        RegisterDeserializer<ExamineMalfTypeOperationDescriptor>(r => r.GetEFTExamineMalfTypeOperationDescriptor());
        RegisterDeserializer<ExamineMalfunctionOperationDescriptor>(r => r.GetEFTExamineMalfunctionOperationDescriptor());
        RegisterDeserializer<ExamineOperationDescriptor>(r => r.GetEFTExamineOperationDescriptor());
        RegisterDeserializer<FaceShieldComponentDescriptor>(r => r.GetEFTFaceShieldComponentDescriptor());
        RegisterDeserializer<FaceshieldMarkOperationDescriptor>(r => r.GetEFTFaceshieldMarkOperationDescriptor());
        RegisterDeserializer<FireModeComponentDescriptor>(r => r.GetEFTFireModeComponentDescriptor());
        RegisterDeserializer<FoldableComponentDescriptor>(r => r.GetEFTFoldableComponentDescriptor());
        RegisterDeserializer<FoldOperationDescriptor>(r => r.GetEFTFoldOperationDescriptor());
        RegisterDeserializer<FoodDrinkComponentDescriptor>(r => r.GetEFTFoodDrinkComponentDescriptor());
        RegisterDeserializer<FoundInRaidItem>(r => r.GetEFTFoundInRaidItem());
        RegisterDeserializer<GridDescriptor>(r => r.GetEFTGridDescriptor());
        RegisterDeserializer<GridItemAddressDescriptor>(r => r.GetEFTGridItemAddressDescriptor());
        RegisterDeserializer<InventoryDescriptor>(r => r.GetEFTInventoryDescriptor());
        RegisterDeserializer<InventoryEquipmentDescriptor>(r => r.GetEFTInventoryEquipmentDescriptor());
        RegisterDeserializer<MapMarker>(r => r.GetEFTInventoryLogicMapMarker());
        RegisterDeserializer<AddToWishlistOperationDescriptor>(r => r.GetEFTInventoryLogicOperationsAddToWishlistOperationDescriptor());
        RegisterDeserializer<ChangeItemsOperationDescriptor>(r => r.GetEFTInventoryLogicOperationsChangeItemsOperationDescriptor());
        RegisterDeserializer<ChangeWishlistItemCategoryOperationDescriptor>(r => r.GetEFTInventoryLogicOperationsChangeWishlistItemCategoryOperationDescriptor());
        RegisterDeserializer<PurchaseTraderServiceOperationDescriptor>(r => r.GetEFTInventoryLogicOperationsPurchaseTraderServiceOperationDescriptor());
        RegisterDeserializer<RemoveFromWishlistOperationDescriptor>(r => r.GetEFTInventoryLogicOperationsRemoveFromWishlistOperationDescriptor());
        RegisterDeserializer<SearchContentOperationDescriptor>(r => r.GetEFTInventoryLogicOperationsSearchContentOperationDescriptor());
        RegisterDeserializer<SearchSuboperationDescriptor>(r => r.GetEFTInventoryLogicOperationsSearchSuboperationDescriptor());
        RegisterDeserializer<SplitToNowhereDescriptor>(r => r.GetEFTInventoryLogicOperationsSplitToNowhereDescriptor());
        RegisterDeserializer<TransferFromNowhereDescriptor>(r => r.GetEFTInventoryLogicOperationsTransferFromNowhereDescriptor());
        RegisterDeserializer<ItemDescriptor>(r => r.GetEFTItemDescriptor());
        RegisterDeserializer<ItemInfoDescriptor>(r => r.GetEFTItemInfoDescriptor());
        RegisterDeserializer<ItemInGridDescriptor>(r => r.GetEFTItemInGridDescriptor());
        RegisterDeserializer<JsonCorpseDescriptor>(r => r.GetEFTJsonCorpseDescriptor());
        RegisterDeserializer<JsonLootItemDescriptor>(r => r.GetEFTJsonLootItemDescriptor());
        RegisterDeserializer<KeyComponentDescriptor>(r => r.GetEFTKeyComponentDescriptor());
        RegisterDeserializer<LightComponentDescriptor>(r => r.GetEFTLightComponentDescriptor());
        RegisterDeserializer<LoadMagOperationDescriptor>(r => r.GetEFTLoadMagOperationDescriptor());
        RegisterDeserializer<LockableComponentDescriptor>(r => r.GetEFTLockableComponentDescriptor());
        RegisterDeserializer<LootDataDescriptor>(r => r.GetEFTLootDataDescriptor());
        RegisterDeserializer<MalfunctionDescriptor>(r => r.GetEFTMalfunctionDescriptor());
        RegisterDeserializer<MapComponentDescriptor>(r => r.GetEFTMapComponentDescriptor());
        RegisterDeserializer<MedKitComponentDescriptor>(r => r.GetEFTMedKitComponentDescriptor());
        RegisterDeserializer<MergeOperationDescriptor>(r => r.GetEFTMergeOperationDescriptor());
        RegisterDeserializer<MoveOperationDescriptor>(r => r.GetEFTMoveOperationDescriptor());
        RegisterDeserializer<NestedItemDescriptor>(r => r.GetEFTNestedItemDescriptor());
        RegisterDeserializer<Note>(r => r.GetEFTNotesNote());
        RegisterDeserializer<NotesManager.NotesDescriptor>(r => r.GetEFTNotesNotesManagerNotesDescriptor());
        RegisterDeserializer<OperateStationaryWeaponOperationDescriptor>(r => r.GetEFTOperateStationaryWeaponOperationDescriptor());
        RegisterDeserializer<OwnerItselfDescriptor>(r => r.GetEFTOwnerItselfDescriptor());
        RegisterDeserializer<PlantTripwireOperationDescriptor>(r => r.GetEFTPlantTripwireOperationDescriptor());
        RegisterDeserializer<PlayerVisualRepresentationDescriptor>(r => r.GetEFTPlayerVisualRepresentationDescriptor());
        RegisterDeserializer<PoisonComponentDescriptor>(r => r.GetEFTPoisonComponentDescriptor());
        RegisterDeserializer<PrestigeStatusData>(r => r.GetEFTPrestigePrestigeStatusData());
        RegisterDeserializer<Profile.HealthInfo>(r => r.GetEFTProfileHealthInfo());
        RegisterDeserializer<Profile.HealthInfo.BodyPartInfo>(r => r.GetEFTProfileHealthInfoBodyPartInfo());
        RegisterDeserializer<Profile.HealthInfo.EffectInfo>(r => r.GetEFTProfileHealthInfoEffectInfo());
        RegisterDeserializer<Profile.HealthInfo.ValueInfo>(r => r.GetEFTProfileHealthInfoValueInfo());
        RegisterDeserializer<Profile.MoneyTransferLimitData>(r => r.GetEFTProfileMoneyTransferLimitData());
        RegisterDeserializer<Profile.UnlockedInfo>(r => r.GetEFTProfileUnlockedInfo());
        RegisterDeserializer<ProfileBanDescriptor>(r => r.GetEFTProfileBanDescriptor());
        RegisterDeserializer<ProfileDescriptor>(r => r.GetEFTProfileDescriptor());
        RegisterDeserializer<ProfileInfoDescriptor>(r => r.GetEFTProfileInfoDescriptor());
        RegisterDeserializer<ProfileSettings>(r => r.GetEFTProfileSettings());
        RegisterDeserializer<ProfileStatsDescriptor>(r => r.GetEFTProfileStatsDescriptor());
        RegisterDeserializer<ProfileStatsSeparatorDescriptor>(r => r.GetEFTProfileStatsSeparatorDescriptor());
        RegisterDeserializer<QuestAcceptDescriptor>(r => r.GetEFTQuestAcceptDescriptor());
        RegisterDeserializer<QuestFinishDescriptor>(r => r.GetEFTQuestFinishDescriptor());
        RegisterDeserializer<QuestHandoverDescriptor>(r => r.GetEFTQuestHandoverDescriptor());
        RegisterDeserializer<QuestDataClass>(r => r.GetEFTQuestsQuestStatusData());
        RegisterDeserializer<RecodableComponentDescriptor>(r => r.GetEFTRecodableComponentDescriptor());
        RegisterDeserializer<RemoveOperationDescriptor>(r => r.GetEFTRemoveOperationDescriptor());
        RegisterDeserializer<RepairableComponentDescriptor>(r => r.GetEFTRepairableComponentDescriptor());
        RegisterDeserializer<RepairEnhancementComponentDescriptor>(r => r.GetEFTRepairEnhancementComponentDescriptor());
        RegisterDeserializer<RepairKitComponentDescriptor>(r => r.GetEFTRepairKitComponentDescriptor());
        RegisterDeserializer<ResourceItemComponentDescriptor>(r => r.GetEFTResourceItemComponentDescriptor());
        RegisterDeserializer<SceneResourceKey>(r => r.GetEFTSceneResourceKey());
        RegisterDeserializer<ResourceKey>(r => r.GetEFTResourceKey());
        RegisterDeserializer<SetDialogProgressOperationDescriptor>(r => r.GetEFTSetDialogProgressOperationDescriptor());
        RegisterDeserializer<SetupItemOperationDescriptor>(r => r.GetEFTSetupItemOperationDescriptor());
        RegisterDeserializer<SetVariableOperationDescriptor>(r => r.GetEFTSetVariableOperationDescriptor());
        RegisterDeserializer<ShellTemplateDescriptor>(r => r.GetEFTShellTemplateDescriptor());
        RegisterDeserializer<SightComponentDescriptor>(r => r.GetEFTSightComponentDescriptor());
        RegisterDeserializer<SkillsDescriptor>(r => r.GetEFTSkillsDescriptor());
        RegisterDeserializer<SkillsDescriptor.MasteringInfoDescriptor>(r => r.GetEFTSkillsDescriptorMasteringInfoDescriptor());
        RegisterDeserializer<SkillsDescriptor.SkillInfoDescriptor>(r => r.GetEFTSkillsDescriptorSkillInfoDescriptor());
        RegisterDeserializer<SlotDescriptor>(r => r.GetEFTSlotDescriptor());
        RegisterDeserializer<SlotItemAddressDescriptor>(r => r.GetEFTSlotItemAddressDescriptor());
        RegisterDeserializer<SplitOperationDescriptor>(r => r.GetEFTSplitOperationDescriptor());
        RegisterDeserializer<StackSlotDescriptor>(r => r.GetEFTStackSlotDescriptor());
        RegisterDeserializer<StackSlotItemAddressDescriptor>(r => r.GetEFTStackSlotItemAddressDescriptor());
        RegisterDeserializer<SwapOperationDescriptor>(r => r.GetEFTSwapOperationDescriptor());
        RegisterDeserializer<TagComponentDescriptor>(r => r.GetEFTTagComponentDescriptor());
        RegisterDeserializer<TagOperationDescriptor>(r => r.GetEFTTagOperationDescriptor());
        RegisterDeserializer<TaskConditionCounterDescriptor>(r => r.GetEFTTaskConditionCounterDescriptor());
        RegisterDeserializer<ThrowOperationDescriptor>(r => r.GetEFTThrowOperationDescriptor());
        RegisterDeserializer<TogglableComponentDescriptor>(r => r.GetEFTTogglableComponentDescriptor());
        RegisterDeserializer<ToggleOperationDescriptor>(r => r.GetEFTToggleOperationDescriptor());
        RegisterDeserializer<TraderInfoDescriptor>(r => r.GetEFTTraderInfoDescriptor());
        RegisterDeserializer<TraderServiceAvailabilityData>(r => r.GetEFTTraderServiceAvailabilityData());
        RegisterDeserializer<TransferOperationDescriptor>(r => r.GetEFTTransferOperationDescriptor());
        RegisterDeserializer<UnbindItemOperationDescriptor>(r => r.GetEFTUnbindItemOperationDescriptor());
        RegisterDeserializer<UnloadMagOperationDescriptor>(r => r.GetEFTUnloadMagOperationDescriptor());
        RegisterDeserializer<VictimStats>(r => r.GetEFTVictimStats());
        RegisterDeserializer<WeaponRechamberOperationDescriptor>(r => r.GetEFTWeaponRechamberOperationDescriptor());
        RegisterDeserializer<InsuredProfileItems>(r => r.GetJsonTypeInsuredProfileItems());
        RegisterDeserializer<PlayerInfo>(r => r.GetJsonTypePlayerInfo());
        RegisterDeserializer<WeightedLootPointSpawnPosition>(r => r.GetWeightedLootPointSpawnPosition());
    }

    private static void RegisterSerializer<T>(Action<NetDataWriter, T> exactSerializer) where T : class
    {
        _serializers[typeof(T)] = (writer, target) => exactSerializer(writer, (T)target);
    }

    private static void RegisterDeserializer<T>(Func<NetDataReader, object> exactDeserializer) where T : class
    {
        var index = _indexToType.IndexOf(typeof(T));
        if (index != -1)
        {
            _deserializers[(byte)index] = exactDeserializer;
        }
    }

    public static void PutPolymorph<T>(this NetDataWriter writer, T target) where T : class
    {
        if (target == null)
        {
            writer.Put((byte)_indexToType.Count);
            return;
        }

        var targetType = target.GetType();

        if (!_typeToByte.TryGetValue(targetType, out var typeIndex))
        {
            FikaGlobals.LogError($"Type for serialization not found: {targetType}");
            return;
        }

        writer.Put(typeIndex);

        if (_serializers.TryGetValue(targetType, out var serializeAction))
        {
            serializeAction(writer, target);
        }
        else
        {
            FikaGlobals.LogError($"Serializer delegate missing for registered type: {targetType}");
        }
    }

    public static T GetPolymorph<T>(this NetDataReader reader) where T : class
    {
        var typeIndex = reader.GetByte();

        if (typeIndex == (byte)_indexToType.Count)
        {
            return null;
        }

        if (_deserializers.TryGetValue(typeIndex, out var deserializeFunc))
        {
            return deserializeFunc(reader) as T;
        }

        FikaGlobals.LogError($"Type index {typeIndex} for deserialization not found.");
        return null;
    }

    /// <summary>
    /// Serializes the aggressor statistics from a AggressorStats object into the writer stream.
    /// </summary>
    /// <param name="target">The AggressorStats instance containing the statistics to write.</param>
    public static void PutAggressorStats(this NetDataWriter writer, AggressorStats target)
    {
        writer.Put(target.AccountId);
        writer.Put(target.ProfileId);
        writer.Put(target.MainProfileNickname);
        writer.Put(target.Name);
        writer.PutEnum(target.Side);
        writer.Put(target.PrestigeLevel);
        writer.PutEnum(target.ColliderType);
        writer.Put(target.WeaponName);
        writer.PutEnum(target.Category);
        writer.PutEnum(target.Role);
    }

    /// <summary>
    /// Deserializes and reconstructs a AggressorStats object from the reader stream.
    /// </summary>
    /// <returns>A new instance of AggressorStats populated with the stream data.</returns>
    public static AggressorStats GetAggressorStats(this NetDataReader reader)
    {
        return new AggressorStats
        {
            AccountId = reader.GetString(),
            ProfileId = reader.GetString(),
            MainProfileNickname = reader.GetString(),
            Name = reader.GetString(),
            Side = reader.GetEnum<EPlayerSide>(),
            PrestigeLevel = reader.GetInt(),
            ColliderType = reader.GetEnum<EBodyPartColliderType>(),
            WeaponName = reader.GetString(),
            Category = reader.GetEnum<EMemberCategory>(),
            Role = reader.GetEnum<WildSpawnType>()
        };
    }

    /// <summary>
    /// Serializes the add note operation descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The AddNoteOperationDescriptor instance containing the descriptor data to write.</param>
    public static void PutEFTAddNoteOperationDescriptor(this NetDataWriter writer, AddNoteOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutEFTNotesNote(target.Note);
    }

    /// <summary>
    /// Deserializes and reconstructs an AddNoteOperationDescriptor object from the reader stream.
    /// </summary>
    /// <returns>A new instance of AddNoteOperationDescriptor populated with the stream data.</returns>
    public static AddNoteOperationDescriptor GetEFTAddNoteOperationDescriptor(this NetDataReader reader)
    {
        return new AddNoteOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            Note = reader.GetEFTNotesNote()
        };
    }

    /// <summary>
    /// Serializes a Note note object into the writer stream.
    /// </summary>
    /// <param name="target">The Note instance containing the note data to write.</param>
    public static void PutEFTNotesNote(this NetDataWriter writer, Note target)
    {
        writer.Put(target.Time);
        writer.Put(target.Text);
    }

    /// <summary>
    /// Deserializes and reconstructs a Note note object from the reader stream.
    /// </summary>
    /// <param name="reader">The reader instance.</param>
    /// <returns>A new instance of Note populated with the stream data.</returns>
    public static Note GetEFTNotesNote(this NetDataReader reader)
    {
        return new Note
        {
            Time = reader.GetFloat(),
            Text = reader.GetString()
        };
    }

    /// <summary>
    /// Serializes the key application operation descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The ApplyKeyOperationDescriptor instance containing the descriptor data to write.</param>
    public static void PutEFTApplyKeyOperationDescriptor(this NetDataWriter writer, ApplyKeyOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.ItemId);
        writer.Put(target.TargetItemId);
    }

    /// <summary>
    /// Deserializes and reconstructs an ApplyKeyOperationDescriptor object from the reader stream.
    /// </summary>
    /// <returns>A new instance of ApplyKeyOperationDescriptor populated with the stream data.</returns>
    public static ApplyKeyOperationDescriptor GetEFTApplyKeyOperationDescriptor(this NetDataReader reader)
    {
        return new ApplyKeyOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            ItemId = reader.GetString(),
            TargetItemId = reader.GetString()
        };
    }

    /// <summary>
    /// Serializes the item binding operation descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The BindItemOperationDescriptor instance containing the descriptor data to write.</param>
    public static void PutEFTBindItemOperationDescriptor(this NetDataWriter writer, BindItemOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.ItemId);
        writer.PutEnum(target.Index);
    }

    /// <summary>
    /// Deserializes and reconstructs a BindItemOperationDescriptor object from the reader stream.
    /// </summary>
    /// <returns>A new instance of BindItemOperationDescriptor populated with the stream data.</returns>
    public static BindItemOperationDescriptor GetEFTBindItemOperationDescriptor(this NetDataReader reader)
    {
        return new BindItemOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            ItemId = reader.GetString(),
            Index = reader.GetEnum<EBoundItem>()
        };
    }

    /// <summary>
    /// Serializes the body part damage history descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The BodyPartDamageHistoryDescriptor instance containing the damage history data to write.</param>
    public static void PutEFTBodyPartDamageHistoryDescriptor(this NetDataWriter writer, BodyPartDamageHistoryDescriptor target)
    {
        writer.Put(target.DamageList.Count);
        for (var i = 0; i < target.DamageList.Count; i++)
        {
            writer.PutEFTDamageStatsDescriptor(target.DamageList[i]);
        }
    }

    /// <summary>
    /// Deserializes and reconstructs a BodyPartDamageHistoryDescriptor object from the reader stream.
    /// </summary>
    /// <returns>A new instance of BodyPartDamageHistoryDescriptor populated with the stream data.</returns>
    public static BodyPartDamageHistoryDescriptor GetEFTBodyPartDamageHistoryDescriptor(this NetDataReader reader)
    {
        var gclass = new BodyPartDamageHistoryDescriptor();
        var num = reader.GetInt();
        gclass.DamageList = new List<DamageStatsDescriptor>(num);
        for (var i = 0; i < num; i++)
        {
            gclass.DamageList.Add(reader.GetEFTDamageStatsDescriptor());
        }
        return gclass;
    }

    /// <summary>
    /// Serializes the damage statistics descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The DamageStatsDescriptor instance containing the damage stats data to write.</param>
    public static void PutEFTDamageStatsDescriptor(this NetDataWriter writer, DamageStatsDescriptor target)
    {
        writer.Put(target.Amount);
        writer.PutEnum(target.Type);
        writer.Put(target.SourceId);
        if (target.OverDamageFrom != null)
        {
            writer.Put(true);
            writer.PutEnum(target.OverDamageFrom.Value);
        }
        else
        {
            writer.Put(false);
        }
        writer.Put(target.Blunt);
        writer.Put(target.ImpactsCount);
    }

    /// <summary>
    /// Deserializes and reconstructs a DamageStatsDescriptor object from the reader stream.
    /// </summary>
    /// <returns>A new instance of DamageStatsDescriptor populated with the stream data.</returns>
    public static DamageStatsDescriptor GetEFTDamageStatsDescriptor(this NetDataReader reader)
    {
        var gclass = new DamageStatsDescriptor
        {
            Amount = reader.GetFloat(),
            Type = reader.GetEnum<EDamageType>(),
            SourceId = reader.GetString()
        };
        if (reader.GetBool())
        {
            gclass.OverDamageFrom = new EBodyPart?(reader.GetEnum<EBodyPart>());
        }
        gclass.Blunt = reader.GetBool();
        gclass.ImpactsCount = reader.GetFloat();
        return gclass;
    }

    /// <summary>
    /// Serializes the profile bonus descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The BonusDescriptor instance containing the bonus data to write.</param>
    public static void PutEFTBonusDescriptor(this NetDataWriter writer, BonusDescriptor target)
    {
        writer.PutEnum(target.BonusType);
        writer.PutMongoID(target.Id);
        writer.Put(target.Value);
        writer.Put(target.IsVisible);
        writer.Put(target.Passive);
        writer.Put(target.Production);
        writer.Put(target.Icon);
        if (target.Filters != null)
        {
            writer.Put(true);
            writer.Put(target.Filters.Count);
            for (var i = 0; i < target.Filters.Count; i++)
            {
                writer.PutMongoID(target.Filters[i]);
            }
        }
        else
        {
            writer.Put(false);
        }
        if (target.SkillType != null)
        {
            writer.Put(true);
            writer.PutEnum(target.SkillType.Value);
        }
        else
        {
            writer.Put(false);
        }
        if (target.SkillName != null)
        {
            writer.Put(true);
            writer.PutEnum(target.SkillName.Value);
        }
        else
        {
            writer.Put(false);
        }
        if (target.TemplateId != null)
        {
            writer.Put(true);
            writer.PutMongoID(target.TemplateId.Value);
            return;
        }
        writer.Put(false);
    }

    /// <summary>
    /// Deserializes and reconstructs a BonusDescriptor object from the reader stream.
    /// </summary>
    /// <returns>A new instance of BonusDescriptor populated with the stream data.</returns>
    public static BonusDescriptor GetEFTBonusDescriptor(this NetDataReader reader)
    {
        var profileBonusesClass = new BonusDescriptor
        {
            BonusType = reader.GetEnum<EBonusType>(),
            Id = reader.GetMongoID(),
            Value = reader.GetDouble(),
            IsVisible = reader.GetBool(),
            Passive = reader.GetBool(),
            Production = reader.GetBool(),
            Icon = reader.GetString()
        };
        if (reader.GetBool())
        {
            var num = reader.GetInt();
            profileBonusesClass.Filters = new List<MongoID>(num);
            for (var i = 0; i < num; i++)
            {
                profileBonusesClass.Filters.Add(reader.GetMongoID());
            }
        }
        if (reader.GetBool())
        {
            profileBonusesClass.SkillType = new ESkillClass?(reader.GetEnum<ESkillClass>());
        }
        if (reader.GetBool())
        {
            profileBonusesClass.SkillName = new ESkillId?(reader.GetEnum<ESkillId>());
        }
        if (reader.GetBool())
        {
            profileBonusesClass.TemplateId = new MongoID?(reader.GetMongoID());
        }
        return profileBonusesClass;
    }

    /// <summary>
    /// Serializes the magazine check operation descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The CheckMagazineOperationDescriptor instance containing the descriptor data to write.</param>
    public static void PutEFTCheckMagazineOperationDescriptor(this NetDataWriter writer, CheckMagazineOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutMongoID(target.ItemId);
        writer.Put(target.CheckStatus);
        writer.Put(target.SkillLevel);
    }

    /// <summary>
    /// Deserializes and reconstructs a CheckMagazineOperationDescriptor object from the reader stream.
    /// </summary>
    /// <returns>A new instance of CheckMagazineOperationDescriptor populated with the stream data.</returns>
    public static CheckMagazineOperationDescriptor GetEFTCheckMagazineOperationDescriptor(this NetDataReader reader)
    {
        return new CheckMagazineOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            ItemId = reader.GetMongoID(),
            CheckStatus = reader.GetBool(),
            SkillLevel = reader.GetByte()
        };
    }

    /// <summary>
    /// Serializes the container descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The ContainerDescriptor instance containing the container data to write.</param>
    public static void PutEFTContainerDescriptor(this NetDataWriter writer, ContainerDescriptor target)
    {
        if (target.ParentId != null)
        {
            writer.Put(true);
            writer.PutMongoID(target.ParentId.Value);
        }
        else
        {
            writer.Put(false);
        }
        writer.Put(target.ContainerId);
    }

    /// <summary>
    /// Deserializes and reconstructs a ContainerDescriptor object from the reader stream.
    /// </summary>
    /// <returns>A new instance of ContainerDescriptor populated with the stream data.</returns>
    public static ContainerDescriptor GetEFTContainerDescriptor(this NetDataReader reader)
    {
        var gclass = new ContainerDescriptor();
        if (reader.GetBool())
        {
            gclass.ParentId = new MongoID?(reader.GetMongoID());
        }
        gclass.ContainerId = reader.GetString();
        return gclass;
    }

    /// <summary>
    /// Serializes the counter collection descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The CounterCollectionDescriptor instance containing the collection items to write.</param>
    public static void PutEFTCounterCollectionDescriptor(this NetDataWriter writer, CounterCollectionDescriptor target)
    {
        writer.Put(target.Items.Count);
        for (var i = 0; i < target.Items.Count; i++)
        {
            writer.PutEFTCounterCollectionItemDescriptor(target.Items[i]);
        }
    }

    /// <summary>
    /// Deserializes and reconstructs a CounterCollectionDescriptor object from the reader stream.
    /// </summary>
    /// <returns>A new instance of CounterCollectionDescriptor populated with the stream data.</returns>
    public static CounterCollectionDescriptor GetEFTCounterCollectionDescriptor(this NetDataReader reader)
    {
        var gclass = new CounterCollectionDescriptor();
        var num = reader.GetInt();
        gclass.Items = new List<CounterCollectionItemDescriptor>(num);
        for (var i = 0; i < num; i++)
        {
            gclass.Items.Add(reader.GetEFTCounterCollectionItemDescriptor());
        }
        return gclass;
    }

    /// <summary>
    /// Serializes a counter collection item descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The CounterCollectionItemDescriptor instance containing the item data to write.</param>
    public static void PutEFTCounterCollectionItemDescriptor(this NetDataWriter writer, CounterCollectionItemDescriptor target)
    {
        writer.Put(target.Key.Count);
        for (var i = 0; i < target.Key.Count; i++)
        {
            writer.Put(target.Key[i]);
        }
        writer.Put(target.Value);
    }

    /// <summary>
    /// Deserializes and reconstructs a CounterCollectionItemDescriptor object from the reader stream.
    /// </summary>
    /// <returns>A new instance of CounterCollectionItemDescriptor populated with the stream data.</returns>
    public static CounterCollectionItemDescriptor GetEFTCounterCollectionItemDescriptor(this NetDataReader reader)
    {
        var gclass = new CounterCollectionItemDescriptor();
        var num = reader.GetInt();
        gclass.Key = new List<string>(num);
        for (var i = 0; i < num; i++)
        {
            gclass.Key.Add(reader.GetString());
        }
        gclass.Value = reader.GetLong();
        return gclass;
    }

    /// <summary>
    /// Serializes the map marker creation operation descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The CreateMapMarkerOperationDescriptor instance containing the descriptor data to write.</param>
    public static void PutEFTCreateMapMarkerOperationDescriptor(this NetDataWriter writer, CreateMapMarkerOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.MapItemId);
        writer.PutEFTInventoryLogicMapMarker(target.MapMarker);
    }

    /// <summary>
    /// Deserializes and reconstructs a CreateMapMarkerOperationDescriptor object from the reader stream.
    /// </summary>
    /// <returns>A new instance of CreateMapMarkerOperationDescriptor populated with the stream data.</returns>
    public static CreateMapMarkerOperationDescriptor GetEFTCreateMapMarkerOperationDescriptor(this NetDataReader reader)
    {
        return new CreateMapMarkerOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            MapItemId = reader.GetString(),
            MapMarker = reader.GetEFTInventoryLogicMapMarker()
        };
    }

    /// <summary>
    /// Serializes a map marker object into the writer stream.
    /// </summary>
    /// <param name="target">The MapMarker instance containing the map marker data to write.</param>
    public static void PutEFTInventoryLogicMapMarker(this NetDataWriter writer, MapMarker target)
    {
        writer.PutEnum(target.Type);
        writer.Put(target.X);
        writer.Put(target.Y);
        writer.Put(target.Note);
    }

    /// <summary>
    /// Deserializes and reconstructs a MapMarker object from the reader stream.
    /// </summary>
    /// <returns>A new instance of MapMarker populated with the stream data.</returns>
    public static MapMarker GetEFTInventoryLogicMapMarker(this NetDataReader reader)
    {
        return new MapMarker
        {
            Type = reader.GetEnum<MapMarkerType>(),
            X = reader.GetInt(),
            Y = reader.GetInt(),
            Note = reader.GetString()
        };
    }

    /// <summary>
    /// Serializes the cultist amulet component descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The CultistAmuletComponentDescriptor instance containing the component data to write.</param>
    public static void PutEFTCultistAmuletComponentDescriptor(this NetDataWriter writer, CultistAmuletComponentDescriptor target)
    {
        writer.Put(target.NumberOfUsages);
    }

    /// <summary>
    /// Deserializes and reconstructs a CultistAmuletComponentDescriptor object from the reader stream.
    /// </summary>
    /// <returns>A new instance of CultistAmuletComponentDescriptor populated with the stream data.</returns>
    public static CultistAmuletComponentDescriptor GetEFTCultistAmuletComponentDescriptor(this NetDataReader reader)
    {
        return new CultistAmuletComponentDescriptor
        {
            NumberOfUsages = reader.GetInt()
        };
    }

    /// <summary>
    /// Serializes the total damage history descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The DamageHistoryDescriptor instance containing the entire damage history data to write.</param>
    public static void PutEFTDamageHistoryDescriptor(this NetDataWriter writer, DamageHistoryDescriptor target)
    {
        writer.PutEnum(target.LethalDamagePart);
        if (target.LethalDamage != null)
        {
            writer.Put(true);
            writer.PutEFTDamageStatsDescriptor(target.LethalDamage);
        }
        else
        {
            writer.Put(false);
        }
        writer.Put(target.BodyParts.Count);
        foreach (var keyValuePair in target.BodyParts)
        {
            writer.PutEnum(keyValuePair.Key);
            writer.PutEFTBodyPartDamageHistoryDescriptor(keyValuePair.Value);
        }
    }

    /// <summary>
    /// Deserializes and reconstructs a DamageHistoryDescriptor object from the reader stream.
    /// </summary>
    /// <returns>A new instance of DamageHistoryDescriptor populated with the stream data.</returns>
    public static DamageHistoryDescriptor GetEFTDamageHistoryDescriptor(this NetDataReader reader)
    {
        var gclass = new DamageHistoryDescriptor
        {
            LethalDamagePart = reader.GetEnum<EBodyPart>()
        };
        if (reader.GetBool())
        {
            gclass.LethalDamage = reader.GetEFTDamageStatsDescriptor();
        }
        var num = reader.GetInt();
        gclass.BodyParts = new Dictionary<EBodyPart, BodyPartDamageHistoryDescriptor>();
        for (var i = 0; i < num; i++)
        {
            gclass.BodyParts[reader.GetEnum<EBodyPart>()] = reader.GetEFTBodyPartDamageHistoryDescriptor();
        }
        return gclass;
    }

    /// <summary>
    /// Serializes combat health depletion logs and causal weapon attributes into the writer stream.
    /// </summary>
    public static void PutEFTDeathCause(this NetDataWriter writer, DeathCause target)
    {
        writer.PutEnum(target.DamageType);
        writer.PutEnum(target.Side);
        writer.PutEnum(target.Role);
        writer.Put(target.WeaponId);
    }

    /// <summary>
    /// Deserializes and reconstructs a DeathCause object from the reader stream.
    /// </summary>
    public static DeathCause GetEFTDeathCause(this NetDataReader reader)
    {
        return new DeathCause
        {
            DamageType = reader.GetEnum<EDamageType>(),
            Side = reader.GetEnum<EPlayerSide>(),
            Role = reader.GetEnum<WildSpawnType>(),
            WeaponId = reader.GetString()
        };
    }

    /// <summary>
    /// Serializes the map marker deletion operation descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The DeleteMapMarkerOperationDescriptor instance containing the descriptor data to write.</param>
    public static void PutEFTDeleteMapMarkerOperationDescriptor(this NetDataWriter writer, DeleteMapMarkerOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.MapItemId);
        writer.Put(target.X);
        writer.Put(target.Y);
    }

    /// <summary>
    /// Deserializes and reconstructs a DeleteMapMarkerOperationDescriptor object from the reader stream.
    /// </summary>
    /// <returns>A new instance of DeleteMapMarkerOperationDescriptor populated with the stream data.</returns>
    public static DeleteMapMarkerOperationDescriptor GetEFTDeleteMapMarkerOperationDescriptor(this NetDataReader reader)
    {
        return new DeleteMapMarkerOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            MapItemId = reader.GetString(),
            X = reader.GetInt(),
            Y = reader.GetInt()
        };
    }

    /// <summary>
    /// Serializes the note deletion operation descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The DeleteNoteOperationDescriptor instance containing the descriptor data to write.</param>
    public static void PutEFTDeleteNoteOperationDescriptor(this NetDataWriter writer, DeleteNoteOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.Index);
    }

    /// <summary>
    /// Deserializes and reconstructs a DeleteNoteOperationDescriptor object from the reader stream.
    /// </summary>
    /// <returns>A new instance of DeleteNoteOperationDescriptor populated with the stream data.</returns>
    public static DeleteNoteOperationDescriptor GetEFTDeleteNoteOperationDescriptor(this NetDataReader reader)
    {
        return new DeleteNoteOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            Index = reader.GetInt()
        };
    }

    /// <summary>
    /// Serializes the destroyed item details into the writer stream.
    /// </summary>
    /// <param name="target">The DestroyedItem instance containing the item destruction data to write.</param>
    public static void PutEFTDestroyedItem(this NetDataWriter writer, DestroyedItem target)
    {
        writer.PutMongoID(target.ItemId);
        writer.Put(target.NumberToDestroy);
        writer.Put(target.NumberToPreserve);
    }

    /// <summary>
    /// Deserializes and reconstructs a DestroyedItem object from the reader stream.
    /// </summary>
    /// <returns>A new instance of DestroyedItem populated with the stream data.</returns>
    public static DestroyedItem GetEFTDestroyedItem(this NetDataReader reader)
    {
        return new DestroyedItem
        {
            ItemId = reader.GetMongoID(),
            NumberToDestroy = reader.GetInt(),
            NumberToPreserve = reader.GetInt()
        };
    }

    /// <summary>
    /// Serializes the dog tag component descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The DogTagComponentDescriptor instance containing the dog tag component data to write.</param>
    public static void PutEFTDogTagComponentDescriptor(this NetDataWriter writer, DogTagComponentDescriptor target)
    {
        writer.Put(target.AccountId);
        writer.Put(target.ProfileId);
        writer.Put(target.Nickname);
        writer.PutEnum(target.Side);
        writer.Put(target.Level);
        writer.Put(target.Time);
        writer.Put(target.Status);
        writer.Put(target.KillerAccountId);
        writer.Put(target.KillerProfileId);
        writer.Put(target.KillerName);
        writer.Put(target.WeaponName);
        writer.Put(target.CarriedByGroupMember);
    }

    /// <summary>
    /// Deserializes and reconstructs a DogTagComponentDescriptor object from the reader stream.
    /// </summary>
    /// <returns>A new instance of DogTagComponentDescriptor populated with the stream data.</returns>
    public static DogTagComponentDescriptor GetEFTDogTagComponentDescriptor(this NetDataReader reader)
    {
        return new DogTagComponentDescriptor
        {
            AccountId = reader.GetString(),
            ProfileId = reader.GetString(),
            Nickname = reader.GetString(),
            Side = reader.GetEnum<EPlayerSide>(),
            Level = reader.GetInt(),
            Time = reader.GetDouble(),
            Status = reader.GetString(),
            KillerAccountId = reader.GetString(),
            KillerProfileId = reader.GetString(),
            KillerName = reader.GetString(),
            WeaponName = reader.GetString(),
            CarriedByGroupMember = reader.GetBool()
        };
    }

    /// <summary>
    /// Serializes the dropped item information into the writer stream.
    /// </summary>
    /// <param name="target">The DroppedItem instance containing the dropped item data to write.</param>
    public static void PutEFTDroppedItem(this NetDataWriter writer, DroppedItem target)
    {
        writer.PutMongoID(target.QuestId);
        writer.PutMongoID(target.ItemId);
        writer.Put(target.ZoneId);
    }

    /// <summary>
    /// Deserializes and reconstructs a DroppedItem object from the reader stream.
    /// </summary>
    /// <returns>A new instance of DroppedItem populated with the stream data.</returns>
    public static DroppedItem GetEFTDroppedItem(this NetDataReader reader)
    {
        return new DroppedItem
        {
            QuestId = reader.GetMongoID(),
            ItemId = reader.GetMongoID(),
            ZoneId = reader.GetString()
        };
    }

    /// <summary>
    /// Serializes the map marker edit operation descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The EditMapMarkerOperationDescriptor instance containing the descriptor data to write.</param>
    public static void PutEFTEditMapMarkerOperationDescriptor(this NetDataWriter writer, EditMapMarkerOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.MapItemId);
        writer.Put(target.X);
        writer.Put(target.Y);
        writer.PutEFTInventoryLogicMapMarker(target.MapMarker);
    }

    /// <summary>
    /// Deserializes and reconstructs a EditMapMarkerOperationDescriptor object from the reader stream.
    /// </summary>
    /// <returns>A new instance of EditMapMarkerOperationDescriptor populated with the stream data.</returns>
    public static EditMapMarkerOperationDescriptor GetEFTEditMapMarkerOperationDescriptor(this NetDataReader reader)
    {
        return new EditMapMarkerOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            MapItemId = reader.GetString(),
            X = reader.GetInt(),
            Y = reader.GetInt(),
            MapMarker = reader.GetEFTInventoryLogicMapMarker()
        };
    }

    /// <summary>
    /// Serializes the note editing operation descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The EditNoteOperationDescriptor instance containing the descriptor data to write.</param>
    public static void PutEFTEditNoteOperationDescriptor(this NetDataWriter writer, EditNoteOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.Index);
        writer.PutEFTNotesNote(target.Note);
    }

    /// <summary>
    /// Deserializes and reconstructs an EditNoteOperationDescriptor object from the reader stream.
    /// </summary>
    /// <returns>A new instance of EditNoteOperationDescriptor populated with the stream data.</returns>
    public static EditNoteOperationDescriptor GetEFTEditNoteOperationDescriptor(this NetDataReader reader)
    {
        return new EditNoteOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            Index = reader.GetInt(),
            Note = reader.GetEFTNotesNote()
        };
    }

    /// <summary>
    /// Serializes the malfunction type examination operation descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The ExamineMalfTypeOperationDescriptor instance containing the descriptor data to write.</param>
    public static void PutEFTExamineMalfTypeOperationDescriptor(this NetDataWriter writer, ExamineMalfTypeOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutMongoID(target.ItemId);
    }

    /// <summary>
    /// Deserializes and reconstructs a ExamineMalfTypeOperationDescriptor object from the reader stream.
    /// </summary>
    /// <returns>A new instance of ExamineMalfTypeOperationDescriptor populated with the stream data.</returns>
    public static ExamineMalfTypeOperationDescriptor GetEFTExamineMalfTypeOperationDescriptor(this NetDataReader reader)
    {
        return new ExamineMalfTypeOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            ItemId = reader.GetMongoID()
        };
    }

    /// <summary>
    /// Serializes the malfunction examination operation descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The ExamineMalfunctionOperationDescriptor instance containing the descriptor data to write.</param>
    public static void PutEFTExamineMalfunctionOperationDescriptor(this NetDataWriter writer, ExamineMalfunctionOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutMongoID(target.ItemId);
    }

    /// <summary>
    /// Deserializes and reconstructs a ExamineMalfunctionOperationDescriptor object from the reader stream.
    /// </summary>
    /// <returns>A new instance of ExamineMalfunctionOperationDescriptor populated with the stream data.</returns>
    public static ExamineMalfunctionOperationDescriptor GetEFTExamineMalfunctionOperationDescriptor(this NetDataReader reader)
    {
        return new ExamineMalfunctionOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            ItemId = reader.GetMongoID()
        };
    }

    /// <summary>
    /// Serializes the item examination operation descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The ExamineOperationDescriptor instance containing the descriptor data to write.</param>
    public static void PutEFTExamineOperationDescriptor(this NetDataWriter writer, ExamineOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.ItemId);
    }

    /// <summary>
    /// Deserializes and reconstructs a ExamineOperationDescriptor object from the reader stream.
    /// </summary>
    /// <returns>A new instance of ExamineOperationDescriptor populated with the stream data.</returns>
    public static ExamineOperationDescriptor GetEFTExamineOperationDescriptor(this NetDataReader reader)
    {
        return new ExamineOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            ItemId = reader.GetString()
        };
    }

    /// <summary>
    /// Serializes the face shield component descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The FaceShieldComponentDescriptor instance containing the face shield status data to write.</param>
    public static void PutEFTFaceShieldComponentDescriptor(this NetDataWriter writer, FaceShieldComponentDescriptor target)
    {
        writer.Put(target.Hits);
        writer.Put(target.HitSeed);
    }

    /// <summary>
    /// Deserializes and reconstructs a FaceShieldComponentDescriptor object from the reader stream.
    /// </summary>
    /// <returns>A new instance of FaceShieldComponentDescriptor populated with the stream data.</returns>
    public static FaceShieldComponentDescriptor GetEFTFaceShieldComponentDescriptor(this NetDataReader reader)
    {
        return new FaceShieldComponentDescriptor
        {
            Hits = reader.GetByte(),
            HitSeed = reader.GetByte()
        };
    }

    /// <summary>
    /// Serializes the face shield marking operation descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The FaceshieldMarkOperationDescriptor instance containing the descriptor data to write.</param>
    public static void PutEFTFaceshieldMarkOperationDescriptor(this NetDataWriter writer, FaceshieldMarkOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutMongoID(target.ItemId);
    }

    /// <summary>
    /// Deserializes and reconstructs a FaceshieldMarkOperationDescriptor object from the reader stream.
    /// </summary>
    /// <returns>A new instance of FaceshieldMarkOperationDescriptor populated with the stream data.</returns>
    public static FaceshieldMarkOperationDescriptor GetEFTFaceshieldMarkOperationDescriptor(this NetDataReader reader)
    {
        return new FaceshieldMarkOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            ItemId = reader.GetMongoID()
        };
    }

    /// <summary>
    /// Serializes the fire mode component descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The FireModeComponentDescriptor instance containing the fire mode status data to write.</param>
    public static void PutEFTFireModeComponentDescriptor(this NetDataWriter writer, FireModeComponentDescriptor target)
    {
        writer.PutEnum(target.FireMode);
    }

    /// <summary>
    /// Deserializes and reconstructs a FireModeComponentDescriptor object from the reader stream.
    /// </summary>
    /// <returns>A new instance of FireModeComponentDescriptor populated with the stream data.</returns>
    public static FireModeComponentDescriptor GetEFTFireModeComponentDescriptor(this NetDataReader reader)
    {
        return new FireModeComponentDescriptor
        {
            FireMode = reader.GetEnum<Weapon.EFireMode>()
        };
    }

    /// <summary>
    /// Serializes the foldable component descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The FoldableComponentDescriptor instance containing the folded status data to write.</param>
    public static void PutEFTFoldableComponentDescriptor(this NetDataWriter writer, FoldableComponentDescriptor target)
    {
        writer.Put(target.Folded);
    }

    /// <summary>
    /// Deserializes and reconstructs a FoldableComponentDescriptor object from the reader stream.
    /// </summary>
    /// <returns>A new instance of FoldableComponentDescriptor populated with the stream data.</returns>
    public static FoldableComponentDescriptor GetEFTFoldableComponentDescriptor(this NetDataReader reader)
    {
        return new FoldableComponentDescriptor
        {
            Folded = reader.GetBool()
        };
    }

    /// <summary>
    /// Serializes the item folding operation descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The FoldOperationDescriptor instance containing the descriptor data to write.</param>
    public static void PutEFTFoldOperationDescriptor(this NetDataWriter writer, FoldOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.ItemId);
        writer.Put(target.Value);
    }

    /// <summary>
    /// Deserializes and reconstructs a FoldOperationDescriptor object from the reader stream.
    /// </summary>
    /// <returns>A new instance of FoldOperationDescriptor populated with the stream data.</returns>
    public static FoldOperationDescriptor GetEFTFoldOperationDescriptor(this NetDataReader reader)
    {
        return new FoldOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            ItemId = reader.GetString(),
            Value = reader.GetBool()
        };
    }

    /// <summary>
    /// Serializes the food and drink component descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The FoodDrinkComponentDescriptor instance containing the food/drink status data to write.</param>
    public static void PutEFTFoodDrinkComponentDescriptor(this NetDataWriter writer, FoodDrinkComponentDescriptor target)
    {
        writer.Put(target.HpPercent);
    }

    /// <summary>
    /// Deserializes and reconstructs a FoodDrinkComponentDescriptor object from the reader stream.
    /// </summary>
    /// <returns>A new instance of FoodDrinkComponentDescriptor populated with the stream data.</returns>
    public static FoodDrinkComponentDescriptor GetEFTFoodDrinkComponentDescriptor(this NetDataReader reader)
    {
        return new FoodDrinkComponentDescriptor
        {
            HpPercent = reader.GetFloat()
        };
    }

    /// <summary>
    /// Serializes the found in raid item details into the writer stream.
    /// </summary>
    /// <param name="target">The FoundInRaidItem instance containing the item raid data to write.</param>
    public static void PutEFTFoundInRaidItem(this NetDataWriter writer, FoundInRaidItem target)
    {
        writer.PutMongoID(target.QuestId);
        writer.PutMongoID(target.ItemId);
    }

    /// <summary>
    /// Deserializes and reconstructs a FoundInRaidItem object from the reader stream.
    /// </summary>
    /// <returns>A new instance of FoundInRaidItem populated with the stream data.</returns>
    public static FoundInRaidItem GetEFTFoundInRaidItem(this NetDataReader reader)
    {
        return new FoundInRaidItem
        {
            QuestId = reader.GetMongoID(),
            ItemId = reader.GetMongoID()
        };
    }

    /// <summary>
    /// Serializes a grid descriptor object into the writer stream.
    /// </summary>
    public static void PutEFTGridDescriptor(this NetDataWriter writer, GridDescriptor target)
    {
        writer.Put(target.GridNumber);
        writer.Put(target.ContainedItems.Count);
        for (var i = 0; i < target.ContainedItems.Count; i++)
        {
            writer.PutEFTItemInGridDescriptor(target.ContainedItems[i]);
        }
    }

    /// <summary>
    /// Deserializes and reconstructs a GridDescriptor object from the reader stream.
    /// </summary>
    public static GridDescriptor GetEFTGridDescriptor(this NetDataReader reader)
    {
        var gclass = new GridDescriptor
        {
            GridNumber = reader.GetByte()
        };
        var num = reader.GetInt();
        gclass.ContainedItems = new List<ItemInGridDescriptor>(num);
        for (var i = 0; i < num; i++)
        {
            gclass.ContainedItems.Add(reader.GetEFTItemInGridDescriptor());
        }
        return gclass;
    }

    /// <summary>
    /// Serializes a grid item address descriptor object into the writer stream.
    /// </summary>
    public static void PutEFTGridItemAddressDescriptor(this NetDataWriter writer, GridItemAddressDescriptor target)
    {
        writer.Put(target.X);
        writer.Put(target.Y);
        writer.Put(target.Horizontal);
        writer.PutEFTContainerDescriptor(target.Container);
    }

    /// <summary>
    /// Deserializes and reconstructs a GridItemAddressDescriptor object from the reader stream.
    /// </summary>
    public static GridItemAddressDescriptor GetEFTGridItemAddressDescriptor(this NetDataReader reader)
    {
        return new GridItemAddressDescriptor
        {
            X = reader.GetByte(),
            Y = reader.GetByte(),
            Horizontal = reader.GetBool(),
            Container = reader.GetEFTContainerDescriptor()
        };
    }

    /// <summary>
    /// Serializes the inventory descriptor state into the writer stream.
    /// </summary>
    public static void PutEFTInventoryDescriptor(this NetDataWriter writer, InventoryDescriptor target)
    {
        writer.PutEFTItemDescriptor(target.Equipment);
        if (target.Stash != null)
        {
            writer.Put(true);
            writer.PutEFTItemDescriptor(target.Stash);
        }
        else
        {
            writer.Put(false);
        }
        if (target.QuestRaidItems != null)
        {
            writer.Put(true);
            writer.PutEFTItemDescriptor(target.QuestRaidItems);
        }
        else
        {
            writer.Put(false);
        }
        if (target.QuestStashItems != null)
        {
            writer.Put(true);
            writer.PutEFTItemDescriptor(target.QuestStashItems);
        }
        else
        {
            writer.Put(false);
        }
        if (target.SortingTable != null)
        {
            writer.Put(true);
            writer.PutEFTItemDescriptor(target.SortingTable);
        }
        else
        {
            writer.Put(false);
        }
        if (target.HideoutCustomizationStash != null)
        {
            writer.Put(true);
            writer.PutEFTItemDescriptor(target.HideoutCustomizationStash);
        }
        else
        {
            writer.Put(false);
        }
        if (target.HideoutAreaStashes != null)
        {
            writer.Put(true);
            writer.Put(target.HideoutAreaStashes.Count);
            foreach (var keyValuePair in target.HideoutAreaStashes)
            {
                writer.PutEnum(keyValuePair.Key);
                writer.PutEFTItemDescriptor(keyValuePair.Value);
            }
        }
        else
        {
            writer.Put(false);
        }
        writer.Put(target.FastAccess.Count);
        foreach (var keyValuePair2 in target.FastAccess)
        {
            writer.PutEnum(keyValuePair2.Key);
            writer.PutMongoID(keyValuePair2.Value);
        }
        writer.Put(target.FavoriteItemsStorage.Count);
        for (var i = 0; i < target.FavoriteItemsStorage.Count; i++)
        {
            writer.PutMongoID(target.FavoriteItemsStorage[i]);
        }
        writer.Put(target.CheckInventoryHash);
        writer.Put(target.DiscardLimits.Count);
        foreach (var keyValuePair3 in target.DiscardLimits)
        {
            writer.PutMongoID(keyValuePair3.Key);
            writer.Put(keyValuePair3.Value);
        }
    }

    /// <summary>
    /// Deserializes and reconstructs an InventoryDescriptor object from the reader stream.
    /// </summary>
    public static InventoryDescriptor GetEFTInventoryDescriptor(this NetDataReader reader)
    {
        var eftinventoryClass = new InventoryDescriptor
        {
            Equipment = reader.GetEFTItemDescriptor()
        };
        if (reader.GetBool())
        {
            eftinventoryClass.Stash = reader.GetEFTItemDescriptor();
        }
        if (reader.GetBool())
        {
            eftinventoryClass.QuestRaidItems = reader.GetEFTItemDescriptor();
        }
        if (reader.GetBool())
        {
            eftinventoryClass.QuestStashItems = reader.GetEFTItemDescriptor();
        }
        if (reader.GetBool())
        {
            eftinventoryClass.SortingTable = reader.GetEFTItemDescriptor();
        }
        if (reader.GetBool())
        {
            eftinventoryClass.HideoutCustomizationStash = reader.GetEFTItemDescriptor();
        }
        if (reader.GetBool())
        {
            var num = reader.GetInt();
            eftinventoryClass.HideoutAreaStashes = new Dictionary<EAreaType, ItemDescriptor>();
            for (var i = 0; i < num; i++)
            {
                eftinventoryClass.HideoutAreaStashes[reader.GetEnum<EAreaType>()] = reader.GetEFTItemDescriptor();
            }
        }
        var num2 = reader.GetInt();
        eftinventoryClass.FastAccess = new Dictionary<EBoundItem, MongoID>();
        for (var j = 0; j < num2; j++)
        {
            eftinventoryClass.FastAccess[reader.GetEnum<EBoundItem>()] = reader.GetMongoID();
        }
        var num3 = reader.GetInt();
        eftinventoryClass.FavoriteItemsStorage = new List<MongoID>(num3);
        for (var k = 0; k < num3; k++)
        {
            eftinventoryClass.FavoriteItemsStorage.Add(reader.GetMongoID());
        }
        eftinventoryClass.CheckInventoryHash = reader.GetBool();
        var num4 = reader.GetInt();
        eftinventoryClass.DiscardLimits = new Dictionary<MongoID, int>();
        for (var l = 0; l < num4; l++)
        {
            eftinventoryClass.DiscardLimits[reader.GetMongoID()] = reader.GetInt();
        }
        return eftinventoryClass;
    }

    /// <summary>
    /// Serializes the inventory equipment descriptor into the writer stream.
    /// </summary>
    public static void PutEFTInventoryEquipmentDescriptor(this NetDataWriter writer, InventoryEquipmentDescriptor target)
    {
        writer.PutEFTItemDescriptor(target.Items);
    }

    /// <summary>
    /// Deserializes and reconstructs a InventoryEquipmentDescriptor object from the reader stream.
    /// </summary>
    public static InventoryEquipmentDescriptor GetEFTInventoryEquipmentDescriptor(this NetDataReader reader)
    {
        return new InventoryEquipmentDescriptor
        {
            Items = reader.GetEFTItemDescriptor()
        };
    }

    /// <summary>
    /// Serializes the wishlist addition operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTInventoryLogicOperationsAddToWishlistOperationDescriptor(this NetDataWriter writer, AddToWishlistOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutMongoID(target.TemplateId);
        writer.PutEnum(target.GroupType);
    }

    /// <summary>
    /// Deserializes and reconstructs a AddToWishlistOperationDescriptor object from the reader stream.
    /// </summary>
    public static AddToWishlistOperationDescriptor GetEFTInventoryLogicOperationsAddToWishlistOperationDescriptor(this NetDataReader reader)
    {
        return new AddToWishlistOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            TemplateId = reader.GetMongoID(),
            GroupType = reader.GetEnum<EWishlistGroup>()
        };
    }

    /// <summary>
    /// Serializes the item modification and change operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTInventoryLogicOperationsChangeItemsOperationDescriptor(this NetDataWriter writer, ChangeItemsOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        if (target.ChangedItems != null)
        {
            writer.Put(true);
            writer.Put(target.ChangedItems.Count);
            for (var i = 0; i < target.ChangedItems.Count; i++)
            {
                writer.PutEFTItemInfoDescriptor(target.ChangedItems[i]);
            }
        }
        else
        {
            writer.Put(false);
        }
        if (target.RemovedItems != null)
        {
            writer.Put(true);
            writer.Put(target.RemovedItems.Count);
            for (var j = 0; j < target.RemovedItems.Count; j++)
            {
                writer.PutMongoID(target.RemovedItems[j]);
            }
        }
        else
        {
            writer.Put(false);
        }
        if (target.MovedItems != null)
        {
            writer.Put(true);
            writer.Put(target.MovedItems.Count);
            foreach (var keyValuePair in target.MovedItems)
            {
                writer.PutMongoID(keyValuePair.Key);
                writer.PutPolymorph(keyValuePair.Value);
            }
        }
        else
        {
            writer.Put(false);
        }
        if (target.NewItems != null)
        {
            writer.Put(true);
            writer.Put(target.NewItems.Count);
            for (var k = 0; k < target.NewItems.Count; k++)
            {
                writer.PutEFTNestedItemDescriptor(target.NewItems[k]);
            }
        }
        else
        {
            writer.Put(false);
        }
        if (target.InternalOperationDescriptor != null)
        {
            writer.Put(true);
            writer.PutPolymorph(target.InternalOperationDescriptor);
            return;
        }
        writer.Put(false);
    }

    /// <summary>
    /// Deserializes and reconstructs a ChangeItemsOperationDescriptor object from the reader stream.
    /// </summary>
    public static ChangeItemsOperationDescriptor GetEFTInventoryLogicOperationsChangeItemsOperationDescriptor(this NetDataReader reader)
    {
        var gclass = new ChangeItemsOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID()
        };
        if (reader.GetBool())
        {
            var num = reader.GetInt();
            gclass.ChangedItems = new List<ItemInfoDescriptor>(num);
            for (var i = 0; i < num; i++)
            {
                gclass.ChangedItems.Add(reader.GetEFTItemInfoDescriptor());
            }
        }
        if (reader.GetBool())
        {
            var num2 = reader.GetInt();
            gclass.RemovedItems = new List<MongoID>(num2);
            for (var j = 0; j < num2; j++)
            {
                gclass.RemovedItems.Add(reader.GetMongoID());
            }
        }
        if (reader.GetBool())
        {
            var num3 = reader.GetInt();
            gclass.MovedItems = new Dictionary<MongoID, ItemAddressDescriptor>();
            for (var k = 0; k < num3; k++)
            {
                gclass.MovedItems[reader.GetMongoID()] = reader.GetPolymorph<ItemAddressDescriptor>();
            }
        }
        if (reader.GetBool())
        {
            var num4 = reader.GetInt();
            gclass.NewItems = new List<NestedItemDescriptor>(num4);
            for (var l = 0; l < num4; l++)
            {
                gclass.NewItems.Add(reader.GetEFTNestedItemDescriptor());
            }
        }
        if (reader.GetBool())
        {
            gclass.InternalOperationDescriptor = reader.GetPolymorph<InventoryOperationDescriptor>();
        }
        return gclass;
    }

    /// <summary>
    /// Serializes the wishlist item category change operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTInventoryLogicOperationsChangeWishlistItemCategoryOperationDescriptor(this NetDataWriter writer, ChangeWishlistItemCategoryOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutMongoID(target.TemplateId);
        writer.PutEnum(target.GroupType);
    }

    /// <summary>
    /// Deserializes and reconstructs a ChangeWishlistItemCategoryOperationDescriptor object from the reader stream.
    /// </summary>
    public static ChangeWishlistItemCategoryOperationDescriptor GetEFTInventoryLogicOperationsChangeWishlistItemCategoryOperationDescriptor(this NetDataReader reader)
    {
        return new ChangeWishlistItemCategoryOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            TemplateId = reader.GetMongoID(),
            GroupType = reader.GetEnum<EWishlistGroup>()
        };
    }

    /// <summary>
    /// Serializes the trader service purchase operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTInventoryLogicOperationsPurchaseTraderServiceOperationDescriptor(this NetDataWriter writer, PurchaseTraderServiceOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutEnum(target.ServiceType);
        if (target.SubServiceId != null)
        {
            writer.Put(true);
            writer.Put(target.SubServiceId);
            return;
        }
        writer.Put(false);
    }

    /// <summary>
    /// Deserializes and reconstructs a PurchaseTraderServiceOperationDescriptor object from the reader stream.
    /// </summary>
    public static PurchaseTraderServiceOperationDescriptor GetEFTInventoryLogicOperationsPurchaseTraderServiceOperationDescriptor(this NetDataReader reader)
    {
        var gclass = new PurchaseTraderServiceOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            ServiceType = reader.GetEnum<ETraderServiceType>()
        };
        if (reader.GetBool())
        {
            gclass.SubServiceId = reader.GetString();
        }
        return gclass;
    }

    /// <summary>
    /// Serializes the wishlist removal operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTInventoryLogicOperationsRemoveFromWishlistOperationDescriptor(this NetDataWriter writer, RemoveFromWishlistOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutMongoID(target.TemplateId);
        writer.PutEnum(target.GroupType);
    }

    /// <summary>
    /// Deserializes and reconstructs a RemoveFromWishlistOperationDescriptor object from the reader stream.
    /// </summary>
    public static RemoveFromWishlistOperationDescriptor GetEFTInventoryLogicOperationsRemoveFromWishlistOperationDescriptor(this NetDataReader reader)
    {
        return new RemoveFromWishlistOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            TemplateId = reader.GetMongoID(),
            GroupType = reader.GetEnum<EWishlistGroup>()
        };
    }

    /// <summary>
    /// Serializes the container content search operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTInventoryLogicOperationsSearchContentOperationDescriptor(this NetDataWriter writer, SearchContentOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutMongoID(target.ItemId);
    }

    /// <summary>
    /// Deserializes and reconstructs a SearchContentOperationDescriptor object from the reader stream.
    /// </summary>
    public static SearchContentOperationDescriptor GetEFTInventoryLogicOperationsSearchContentOperationDescriptor(this NetDataReader reader)
    {
        return new SearchContentOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            ItemId = reader.GetMongoID()
        };
    }

    /// <summary>
    /// Serializes the container content search suboperation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTInventoryLogicOperationsSearchSuboperationDescriptor(this NetDataWriter writer, SearchSuboperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutMongoID(target.SearchedItem);
        if (target.Content != null)
        {
            writer.Put(true);
            writer.Put(target.Content.Count);
            for (var i = 0; i < target.Content.Count; i++)
            {
                writer.PutEFTNestedItemDescriptor(target.Content[i]);
            }
        }
        else
        {
            writer.Put(false);
        }
        writer.Put(target.Instant);
    }

    /// <summary>
    /// Deserializes and reconstructs a SearchSuboperationDescriptor object from the reader stream.
    /// </summary>
    public static SearchSuboperationDescriptor GetEFTInventoryLogicOperationsSearchSuboperationDescriptor(this NetDataReader reader)
    {
        var gclass = new SearchSuboperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            SearchedItem = reader.GetMongoID()
        };
        if (reader.GetBool())
        {
            var num = reader.GetInt();
            gclass.Content = new List<NestedItemDescriptor>(num);
            for (var i = 0; i < num; i++)
            {
                gclass.Content.Add(reader.GetEFTNestedItemDescriptor());
            }
        }
        gclass.Instant = reader.GetBool();
        return gclass;
    }

    /// <summary>
    /// Serializes the item split to nowhere operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTInventoryLogicOperationsSplitToNowhereDescriptor(this NetDataWriter writer, SplitToNowhereDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutMongoID(target.ItemId);
        writer.Put(target.Count);
    }

    /// <summary>
    /// Deserializes and reconstructs a SplitToNowhereDescriptor object from the reader stream.
    /// </summary>
    public static SplitToNowhereDescriptor GetEFTInventoryLogicOperationsSplitToNowhereDescriptor(this NetDataReader reader)
    {
        return new SplitToNowhereDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            ItemId = reader.GetMongoID(),
            Count = reader.GetUInt()
        };
    }

    /// <summary>
    /// Serializes the item transfer from nowhere operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTInventoryLogicOperationsTransferFromNowhereDescriptor(this NetDataWriter writer, TransferFromNowhereDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutMongoID(target.ItemId);
        writer.Put(target.Count);
        writer.Put(target.SpawnedInSession);
    }

    /// <summary>
    /// Deserializes and reconstructs a TransferFromNowhereDescriptor object from the reader stream.
    /// </summary>
    public static TransferFromNowhereDescriptor GetEFTInventoryLogicOperationsTransferFromNowhereDescriptor(this NetDataReader reader)
    {
        return new TransferFromNowhereDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            ItemId = reader.GetMongoID(),
            Count = reader.GetUInt(),
            SpawnedInSession = reader.GetBool()
        };
    }

    /// <summary>
    /// Serializes an inventory item descriptor into the writer stream.
    /// </summary>
    public static void PutEFTItemDescriptor(this NetDataWriter writer, ItemDescriptor target)
    {
        writer.PutMongoID(target.Id);
        writer.PutMongoID(target.TemplateId);
        writer.Put(target.StackCount);
        writer.Put(target.SpawnedInSession);
        writer.Put(target.ActiveCamora);
        writer.Put(target.IsUnderBarrelDeviceActive);
        if (target.Malfunction != null)
        {
            writer.Put(true);
            writer.PutEFTMalfunctionDescriptor(target.Malfunction);
        }
        else
        {
            writer.Put(false);
        }
        if (target.UnsearchedInfo != null)
        {
            writer.Put(true);
            writer.PutEFTItemInfoDescriptor(target.UnsearchedInfo);
        }
        else
        {
            writer.Put(false);
        }
        if (target.Components != null)
        {
            writer.Put(true);
            writer.Put(target.Components.Count);
            for (var i = 0; i < target.Components.Count; i++)
            {
                writer.PutPolymorph(target.Components[i]);
            }
        }
        else
        {
            writer.Put(false);
        }
        if (target.Slots != null)
        {
            writer.Put(true);
            writer.Put(target.Slots.Count);
            for (var j = 0; j < target.Slots.Count; j++)
            {
                writer.PutEFTSlotDescriptor(target.Slots[j]);
            }
        }
        else
        {
            writer.Put(false);
        }
        if (target.ShellsInWeapon != null)
        {
            writer.Put(true);
            writer.Put(target.ShellsInWeapon.Count);
            for (var k = 0; k < target.ShellsInWeapon.Count; k++)
            {
                writer.PutEFTShellTemplateDescriptor(target.ShellsInWeapon[k]);
            }
        }
        else
        {
            writer.Put(false);
        }
        if (target.Grids != null)
        {
            writer.Put(true);
            writer.Put(target.Grids.Count);
            for (var l = 0; l < target.Grids.Count; l++)
            {
                writer.PutEFTGridDescriptor(target.Grids[l]);
            }
        }
        else
        {
            writer.Put(false);
        }
        if (target.StackSlots != null)
        {
            writer.Put(true);
            writer.Put(target.StackSlots.Count);
            for (var m = 0; m < target.StackSlots.Count; m++)
            {
                writer.PutEFTStackSlotDescriptor(target.StackSlots[m]);
            }
            return;
        }
        writer.Put(false);
    }

    /// <summary>
    /// Deserializes and reconstructs an ItemDescriptor object from the reader stream.
    /// </summary>
    public static ItemDescriptor GetEFTItemDescriptor(this NetDataReader reader)
    {
        var inventoryDescriptorClass = new ItemDescriptor
        {
            Id = reader.GetMongoID(),
            TemplateId = reader.GetMongoID(),
            StackCount = reader.GetInt(),
            SpawnedInSession = reader.GetBool(),
            ActiveCamora = reader.GetByte(),
            IsUnderBarrelDeviceActive = reader.GetBool()
        };
        if (reader.GetBool())
        {
            inventoryDescriptorClass.Malfunction = reader.GetEFTMalfunctionDescriptor();
        }
        if (reader.GetBool())
        {
            inventoryDescriptorClass.UnsearchedInfo = reader.GetEFTItemInfoDescriptor();
        }
        if (reader.GetBool())
        {
            var num = reader.GetInt();
            inventoryDescriptorClass.Components = new List<ItemComponentDescriptor>(num);
            for (var i = 0; i < num; i++)
            {
                inventoryDescriptorClass.Components.Add(reader.GetPolymorph<ItemComponentDescriptor>());
            }
        }
        if (reader.GetBool())
        {
            var num2 = reader.GetInt();
            inventoryDescriptorClass.Slots = new List<SlotDescriptor>(num2);
            for (var j = 0; j < num2; j++)
            {
                inventoryDescriptorClass.Slots.Add(reader.GetEFTSlotDescriptor());
            }
        }
        if (reader.GetBool())
        {
            var num3 = reader.GetInt();
            inventoryDescriptorClass.ShellsInWeapon = new List<ShellTemplateDescriptor>(num3);
            for (var k = 0; k < num3; k++)
            {
                inventoryDescriptorClass.ShellsInWeapon.Add(reader.GetEFTShellTemplateDescriptor());
            }
        }
        if (reader.GetBool())
        {
            var num4 = reader.GetInt();
            inventoryDescriptorClass.Grids = new List<GridDescriptor>(num4);
            for (var l = 0; l < num4; l++)
            {
                inventoryDescriptorClass.Grids.Add(reader.GetEFTGridDescriptor());
            }
        }
        if (reader.GetBool())
        {
            var num5 = reader.GetInt();
            inventoryDescriptorClass.StackSlots = new List<StackSlotDescriptor>(num5);
            for (var m = 0; m < num5; m++)
            {
                inventoryDescriptorClass.StackSlots.Add(reader.GetEFTStackSlotDescriptor());
            }
        }
        return inventoryDescriptorClass;
    }

    /// <summary>
    /// Serializes basic item informational parameters into the writer stream.
    /// </summary>
    public static void PutEFTItemInfoDescriptor(this NetDataWriter writer, ItemInfoDescriptor target)
    {
        writer.PutMongoID(target.Id);
        writer.Put(target.Hash);
        writer.Put(target.Width);
        writer.Put(target.Height);
        writer.Put(target.TotalWeight);
    }

    /// <summary>
    /// Deserializes and reconstructs a ItemInfoDescriptor object from the reader stream.
    /// </summary>
    public static ItemInfoDescriptor GetEFTItemInfoDescriptor(this NetDataReader reader)
    {
        return new ItemInfoDescriptor
        {
            Id = reader.GetMongoID(),
            Hash = reader.GetInt(),
            Width = reader.GetShort(),
            Height = reader.GetShort(),
            TotalWeight = reader.GetFloat()
        };
    }

    /// <summary>
    /// Serializes a descriptor mapping an item inside a parent grid layout into the writer stream.
    /// </summary>
    public static void PutEFTItemInGridDescriptor(this NetDataWriter writer, ItemInGridDescriptor target)
    {
        writer.PutEFTItemDescriptor(target.Item);
        writer.Put(target.X);
        writer.Put(target.Y);
        writer.Put(target.Horizontal);
    }

    /// <summary>
    /// Deserializes and reconstructs a ItemInGridDescriptor object from the reader stream.
    /// </summary>
    public static ItemInGridDescriptor GetEFTItemInGridDescriptor(this NetDataReader reader)
    {
        return new ItemInGridDescriptor
        {
            Item = reader.GetEFTItemDescriptor(),
            X = reader.GetByte(),
            Y = reader.GetByte(),
            Horizontal = reader.GetBool()
        };
    }

    /// <summary>
    /// Serializes a detailed corpse item state and visual transform metadata mapping into the writer stream.
    /// </summary>
    public static void PutEFTJsonCorpseDescriptor(this NetDataWriter writer, JsonCorpseDescriptor target)
    {
        writer.Put(target.Customization.Count);
        foreach (var keyValuePair in target.Customization)
        {
            writer.Put(keyValuePair.Key);
            writer.PutMongoID(keyValuePair.Value);
        }
        writer.PutEnum(target.Side);
        writer.Put(target.Bones.Length);
        for (var i = 0; i < target.Bones.Length; i++)
        {
            writer.PutClassTransformSync(target.Bones[i]);
        }
        writer.Put(target.PlayerProfileID);
        writer.Put(target.IsZombieCorpse);
        writer.Put(target.Id);
        writer.PutClassVector3(target.Position);
        writer.PutClassVector3(target.Rotation);
        writer.PutEFTItemDescriptor(target.Item);
        if (target.ValidProfiles != null)
        {
            writer.Put(true);
            writer.Put(target.ValidProfiles.Length);
            for (var j = 0; j < target.ValidProfiles.Length; j++)
            {
                writer.PutMongoID(target.ValidProfiles[j]);
            }
        }
        else
        {
            writer.Put(false);
        }
        writer.Put(target.IsContainer);
        writer.Put(target.UseGravity);
        writer.Put(target.RandomRotation);
        writer.PutClassVector3(target.Shift);
        writer.Put(target.PlatformId);
    }

    /// <summary>
    /// Deserializes and reconstructs a JsonCorpseDescriptor object from the reader stream.
    /// </summary>
    public static JsonCorpseDescriptor GetEFTJsonCorpseDescriptor(this NetDataReader reader)
    {
        var gclass = new JsonCorpseDescriptor();
        var num = reader.GetInt();
        gclass.Customization = new Dictionary<int, MongoID>();
        for (var i = 0; i < num; i++)
        {
            gclass.Customization[reader.GetInt()] = reader.GetMongoID();
        }
        gclass.Side = reader.GetEnum<EPlayerSide>();
        var num2 = reader.GetInt();
        gclass.Bones = new ClassTransformSync[num2];
        for (var j = 0; j < num2; j++)
        {
            gclass.Bones[j] = reader.GetClassTransformSync();
        }
        gclass.PlayerProfileID = reader.GetString();
        gclass.IsZombieCorpse = reader.GetBool();
        gclass.Id = reader.GetString();
        gclass.Position = reader.GetClassVector3();
        gclass.Rotation = reader.GetClassVector3();
        gclass.Item = reader.GetEFTItemDescriptor();
        if (reader.GetBool())
        {
            var num3 = reader.GetInt();
            gclass.ValidProfiles = new MongoID[num3];
            for (var k = 0; k < num3; k++)
            {
                gclass.ValidProfiles[k] = reader.GetMongoID();
            }
        }
        gclass.IsContainer = reader.GetBool();
        gclass.UseGravity = reader.GetBool();
        gclass.RandomRotation = reader.GetBool();
        gclass.Shift = reader.GetClassVector3();
        gclass.PlatformId = reader.GetShort();
        return gclass;
    }

    /// <summary>
    /// Serializes a loose world loot item instance descriptor into the writer stream.
    /// </summary>
    public static void PutEFTJsonLootItemDescriptor(this NetDataWriter writer, JsonLootItemDescriptor target)
    {
        writer.Put(target.Id);
        writer.PutClassVector3(target.Position);
        writer.PutClassVector3(target.Rotation);
        writer.PutEFTItemDescriptor(target.Item);
        if (target.ValidProfiles != null)
        {
            writer.Put(true);
            writer.Put(target.ValidProfiles.Length);
            for (var i = 0; i < target.ValidProfiles.Length; i++)
            {
                writer.PutMongoID(target.ValidProfiles[i]);
            }
        }
        else
        {
            writer.Put(false);
        }
        writer.Put(target.IsContainer);
        writer.Put(target.UseGravity);
        writer.Put(target.RandomRotation);
        writer.PutClassVector3(target.Shift);
        writer.Put(target.PlatformId);
    }

    /// <summary>
    /// Deserializes and reconstructs a JsonLootItemDescriptor object from the reader stream.
    /// </summary>
    public static JsonLootItemDescriptor GetEFTJsonLootItemDescriptor(this NetDataReader reader)
    {
        var gclass = new JsonLootItemDescriptor
        {
            Id = reader.GetString(),
            Position = reader.GetClassVector3(),
            Rotation = reader.GetClassVector3(),
            Item = reader.GetEFTItemDescriptor()
        };
        if (reader.GetBool())
        {
            var num = reader.GetInt();
            gclass.ValidProfiles = new MongoID[num];
            for (var i = 0; i < num; i++)
            {
                gclass.ValidProfiles[i] = reader.GetMongoID();
            }
        }
        gclass.IsContainer = reader.GetBool();
        gclass.UseGravity = reader.GetBool();
        gclass.RandomRotation = reader.GetBool();
        gclass.Shift = reader.GetClassVector3();
        gclass.PlatformId = reader.GetShort();
        return gclass;
    }

    /// <summary>
    /// Serializes the key component descriptor into the writer stream.
    /// </summary>
    public static void PutEFTKeyComponentDescriptor(this NetDataWriter writer, KeyComponentDescriptor target)
    {
        writer.Put(target.NumberOfUsages);
    }

    /// <summary>
    /// Deserializes and reconstructs a KeyComponentDescriptor object from the reader stream.
    /// </summary>
    public static KeyComponentDescriptor GetEFTKeyComponentDescriptor(this NetDataReader reader)
    {
        return new KeyComponentDescriptor
        {
            NumberOfUsages = reader.GetInt()
        };
    }

    /// <summary>
    /// Serializes the light/tactical device component descriptor into the writer stream.
    /// </summary>
    public static void PutEFTLightComponentDescriptor(this NetDataWriter writer, LightComponentDescriptor target)
    {
        writer.Put(target.IsActive);
        writer.Put(target.SelectedMode);
    }

    /// <summary>
    /// Deserializes and reconstructs a LightComponentDescriptor object from the reader stream.
    /// </summary>
    public static LightComponentDescriptor GetEFTLightComponentDescriptor(this NetDataReader reader)
    {
        return new LightComponentDescriptor
        {
            IsActive = reader.GetBool(),
            SelectedMode = reader.GetInt()
        };
    }

    /// <summary>
    /// Serializes the magazine loading operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTLoadMagOperationDescriptor(this NetDataWriter writer, LoadMagOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutPolymorph(target.InternalOperationDescriptor);
    }

    /// <summary>
    /// Deserializes and reconstructs a LoadMagOperationDescriptor object from the reader stream.
    /// </summary>
    public static LoadMagOperationDescriptor GetEFTLoadMagOperationDescriptor(this NetDataReader reader)
    {
        return new LoadMagOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            InternalOperationDescriptor = reader.GetPolymorph<InventoryOperationDescriptor>()
        };
    }

    /// <summary>
    /// Serializes the lockable component descriptor into the writer stream.
    /// </summary>
    public static void PutEFTLockableComponentDescriptor(this NetDataWriter writer, LockableComponentDescriptor target)
    {
        writer.Put(target.Locked);
    }

    /// <summary>
    /// Deserializes and reconstructs a LockableComponentDescriptor object from the reader stream.
    /// </summary>
    public static LockableComponentDescriptor GetEFTLockableComponentDescriptor(this NetDataReader reader)
    {
        return new LockableComponentDescriptor
        {
            Locked = reader.GetBool()
        };
    }

    /// <summary>
    /// Serializes global loot data descriptors into the writer stream.
    /// </summary>
    public static void PutEFTLootDataDescriptor(this NetDataWriter writer, LootDataDescriptor target)
    {
        writer.Put(target.Items.Count);
        for (var i = 0; i < target.Items.Count; i++)
        {
            writer.PutPolymorph(target.Items[i]);
        }
    }

    /// <summary>
    /// Deserializes and reconstructs a LootDataDescriptor object from the reader stream.
    /// </summary>
    public static LootDataDescriptor GetEFTLootDataDescriptor(this NetDataReader reader)
    {
        var gclass = new LootDataDescriptor();
        var num = reader.GetInt();
        gclass.Items = new List<JsonLootItemDescriptor>(num);
        for (var i = 0; i < num; i++)
        {
            gclass.Items.Add(reader.GetPolymorph<JsonLootItemDescriptor>());
        }
        return gclass;
    }

    /// <summary>
    /// Serializes detailed weapon malfunction descriptor structures into the writer stream.
    /// </summary>
    public static void PutEFTMalfunctionDescriptor(this NetDataWriter writer, MalfunctionDescriptor target)
    {
        writer.Put(target.Malfunction);
        writer.Put(target.LastShotOverheat);
        writer.Put(target.LastShotTime);
        writer.Put(target.SlideOnOverheatReached);
        writer.Put(target.PlayersWhoKnowAboutMalfunction.Count);
        for (var i = 0; i < target.PlayersWhoKnowAboutMalfunction.Count; i++)
        {
            writer.PutMongoID(target.PlayersWhoKnowAboutMalfunction[i]);
        }
        writer.Put(target.PlayersWhoKnowMalfType.Count);
        for (var j = 0; j < target.PlayersWhoKnowMalfType.Count; j++)
        {
            writer.PutMongoID(target.PlayersWhoKnowMalfType[j]);
        }
        writer.Put(target.PlayersReducedMalfChances.Count);
        foreach (var keyValuePair in target.PlayersReducedMalfChances)
        {
            writer.PutMongoID(keyValuePair.Key);
            writer.Put(keyValuePair.Value);
        }
        if (target.AmmoToFireTemplateId != null)
        {
            writer.Put(true);
            writer.PutMongoID(target.AmmoToFireTemplateId.Value);
        }
        else
        {
            writer.Put(false);
        }
        if (target.AmmoWillBeLoadedToChamberTemplateId != null)
        {
            writer.Put(true);
            writer.PutMongoID(target.AmmoWillBeLoadedToChamberTemplateId.Value);
        }
        else
        {
            writer.Put(false);
        }
        if (target.AmmoMalfunctionedTemplateId != null)
        {
            writer.Put(true);
            writer.PutMongoID(target.AmmoMalfunctionedTemplateId.Value);
            return;
        }
        writer.Put(false);
    }

    /// <summary>
    /// Deserializes and reconstructs a MalfunctionDescriptor object from the reader stream.
    /// </summary>
    public static MalfunctionDescriptor GetEFTMalfunctionDescriptor(this NetDataReader reader)
    {
        var gclass = new MalfunctionDescriptor
        {
            Malfunction = reader.GetByte(),
            LastShotOverheat = reader.GetFloat(),
            LastShotTime = reader.GetFloat(),
            SlideOnOverheatReached = reader.GetBool()
        };
        var num = reader.GetInt();
        gclass.PlayersWhoKnowAboutMalfunction = new List<MongoID>(num);
        for (var i = 0; i < num; i++)
        {
            gclass.PlayersWhoKnowAboutMalfunction.Add(reader.GetMongoID());
        }
        var num2 = reader.GetInt();
        gclass.PlayersWhoKnowMalfType = new List<MongoID>(num2);
        for (var j = 0; j < num2; j++)
        {
            gclass.PlayersWhoKnowMalfType.Add(reader.GetMongoID());
        }
        var num3 = reader.GetInt();
        gclass.PlayersReducedMalfChances = new Dictionary<MongoID, byte>();
        for (var k = 0; k < num3; k++)
        {
            gclass.PlayersReducedMalfChances[reader.GetMongoID()] = reader.GetByte();
        }
        if (reader.GetBool())
        {
            gclass.AmmoToFireTemplateId = new MongoID?(reader.GetMongoID());
        }
        if (reader.GetBool())
        {
            gclass.AmmoWillBeLoadedToChamberTemplateId = new MongoID?(reader.GetMongoID());
        }
        if (reader.GetBool())
        {
            gclass.AmmoMalfunctionedTemplateId = new MongoID?(reader.GetMongoID());
        }
        return gclass;
    }

    /// <summary>
    /// Serializes the map item component markers into the writer stream.
    /// </summary>
    public static void PutEFTMapComponentDescriptor(this NetDataWriter writer, MapComponentDescriptor target)
    {
        writer.Put(target.Markers.Count);
        for (var i = 0; i < target.Markers.Count; i++)
        {
            writer.PutEFTInventoryLogicMapMarker(target.Markers[i]);
        }
    }

    /// <summary>
    /// Deserializes and reconstructs a MapComponentDescriptor object from the reader stream.
    /// </summary>
    public static MapComponentDescriptor GetEFTMapComponentDescriptor(this NetDataReader reader)
    {
        var gclass = new MapComponentDescriptor();
        var num = reader.GetInt();
        gclass.Markers = new List<MapMarker>(num);
        for (var i = 0; i < num; i++)
        {
            gclass.Markers.Add(reader.GetEFTInventoryLogicMapMarker());
        }
        return gclass;
    }

    /// <summary>
    /// Serializes the medical kit component resource state into the writer stream.
    /// </summary>
    public static void PutEFTMedKitComponentDescriptor(this NetDataWriter writer, MedKitComponentDescriptor target)
    {
        writer.Put(target.HpResource);
    }

    /// <summary>
    /// Deserializes and reconstructs a MedKitComponentDescriptor object from the reader stream.
    /// </summary>
    public static MedKitComponentDescriptor GetEFTMedKitComponentDescriptor(this NetDataReader reader)
    {
        return new MedKitComponentDescriptor
        {
            HpResource = reader.GetFloat()
        };
    }

    /// <summary>
    /// Serializes the item stack merge operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTMergeOperationDescriptor(this NetDataWriter writer, MergeOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.ItemId);
        writer.Put(target.Item1Id);
    }

    /// <summary>
    /// Deserializes and reconstructs a MergeOperationDescriptor object from the reader stream.
    /// </summary>
    public static MergeOperationDescriptor GetEFTMergeOperationDescriptor(this NetDataReader reader)
    {
        return new MergeOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            ItemId = reader.GetString(),
            Item1Id = reader.GetString()
        };
    }

    /// <summary>
    /// Serializes the item structural inventory move operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTMoveOperationDescriptor(this NetDataWriter writer, MoveOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.ItemId);
        writer.PutPolymorph(target.From);
        writer.PutPolymorph(target.To);
        if (target.DestroyedItems != null)
        {
            writer.Put(true);
            writer.Put(target.DestroyedItems.Count);
            for (var i = 0; i < target.DestroyedItems.Count; i++)
            {
                writer.PutEFTDestroyedItem(target.DestroyedItems[i]);
            }
            return;
        }
        writer.Put(false);
    }

    /// <summary>
    /// Deserializes and reconstructs a MoveOperationDescriptor object from the reader stream.
    /// </summary>
    public static MoveOperationDescriptor GetEFTMoveOperationDescriptor(this NetDataReader reader)
    {
        var moveDescriptorClass = new MoveOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            ItemId = reader.GetString(),
            From = reader.GetPolymorph<ItemAddressDescriptor>(),
            To = reader.GetPolymorph<ItemAddressDescriptor>()
        };
        if (reader.GetBool())
        {
            var num = reader.GetInt();
            moveDescriptorClass.DestroyedItems = new List<DestroyedItem>(num);
            for (var i = 0; i < num; i++)
            {
                moveDescriptorClass.DestroyedItems.Add(reader.GetEFTDestroyedItem());
            }
        }
        return moveDescriptorClass;
    }

    /// <summary>
    /// Serializes a nested item hierarchy reference mapping its parent address context into the writer stream.
    /// </summary>
    public static void PutEFTNestedItemDescriptor(this NetDataWriter writer, NestedItemDescriptor target)
    {
        writer.PutPolymorph(target.Address);
        writer.PutEFTItemDescriptor(target.Item);
    }

    /// <summary>
    /// Deserializes and reconstructs a NestedItemDescriptor object from the reader stream.
    /// </summary>
    public static NestedItemDescriptor GetEFTNestedItemDescriptor(this NetDataReader reader)
    {
        return new NestedItemDescriptor
        {
            Address = reader.GetPolymorph<ItemAddressDescriptor>(),
            Item = reader.GetEFTItemDescriptor()
        };
    }

    /// <summary>
    /// Serializes the player profile context notes manager data arrays into the writer stream.
    /// </summary>
    public static void PutEFTNotesNotesManagerNotesDescriptor(this NetDataWriter writer, NotesManager.NotesDescriptor target)
    {
        writer.Put(target.Notes.Length);
        for (var i = 0; i < target.Notes.Length; i++)
        {
            writer.PutEFTNotesNote(target.Notes[i]);
        }
    }

    /// <summary>
    /// Deserializes and reconstructs a NotesManager.NotesDescriptor object from the reader stream.
    /// </summary>
    public static NotesManager.NotesDescriptor GetEFTNotesNotesManagerNotesDescriptor(this NetDataReader reader)
    {
        var gclass = new NotesManager.NotesDescriptor();
        var num = reader.GetInt();
        gclass.Notes = new Note[num];
        for (var i = 0; i < num; i++)
        {
            gclass.Notes[i] = reader.GetEFTNotesNote();
        }
        return gclass;
    }

    /// <summary>
    /// Serializes the stationary weapon interaction/operation context descriptor into the writer stream.
    /// </summary>
    public static void PutEFTOperateStationaryWeaponOperationDescriptor(this NetDataWriter writer, OperateStationaryWeaponOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.WeaponId);
    }

    /// <summary>
    /// Deserializes and reconstructs a OperateStationaryWeaponOperationDescriptor object from the reader stream.
    /// </summary>
    public static OperateStationaryWeaponOperationDescriptor GetEFTOperateStationaryWeaponOperationDescriptor(this NetDataReader reader)
    {
        return new OperateStationaryWeaponOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            WeaponId = reader.GetString()
        };
    }

    /// <summary>
    /// Serializes an inventory base root container self-owner tracking descriptor reference into the writer stream.
    /// </summary>
    public static void PutEFTOwnerItselfDescriptor(this NetDataWriter writer, OwnerItselfDescriptor target)
    {
        writer.PutEFTContainerDescriptor(target.Container);
    }

    /// <summary>
    /// Deserializes and reconstructs a OwnerItselfDescriptor object from the reader stream.
    /// </summary>
    public static OwnerItselfDescriptor GetEFTOwnerItselfDescriptor(this NetDataReader reader)
    {
        return new OwnerItselfDescriptor
        {
            Container = reader.GetEFTContainerDescriptor()
        };
    }

    /// <summary>
    /// Serializes a localized interactive structural tripwire deployment operation into the writer stream.
    /// </summary>
    public static void PutEFTPlantTripwireOperationDescriptor(this NetDataWriter writer, PlantTripwireOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutMongoID(target.TripwireId);
        if (target.PlantingKitId != null)
        {
            writer.Put(true);
            writer.PutMongoID(target.PlantingKitId.Value);
        }
        else
        {
            writer.Put(false);
        }
        writer.PutClassVector3(target.FromPosition);
        writer.PutClassVector3(target.ToPosition);
    }

    /// <summary>
    /// Deserializes and reconstructs a PlantTripwireOperationDescriptor object from the reader stream.
    /// </summary>
    public static PlantTripwireOperationDescriptor GetEFTPlantTripwireOperationDescriptor(this NetDataReader reader)
    {
        var gclass = new PlantTripwireOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            TripwireId = reader.GetMongoID()
        };
        if (reader.GetBool())
        {
            gclass.PlantingKitId = new MongoID?(reader.GetMongoID());
        }
        gclass.FromPosition = reader.GetClassVector3();
        gclass.ToPosition = reader.GetClassVector3();
        return gclass;
    }

    /// <summary>
    /// Serializes entire localized dynamic player profile visual equipment layout representations into the writer stream.
    /// </summary>
    public static void PutEFTPlayerVisualRepresentationDescriptor(this NetDataWriter writer, PlayerVisualRepresentationDescriptor target)
    {
        writer.PutJsonTypePlayerInfo(target.Info);
        if (target.Customization != null)
        {
            writer.Put(true);
            writer.Put(target.Customization.Count);
            foreach (var keyValuePair in target.Customization)
            {
                writer.PutEnum(keyValuePair.Key);
                writer.PutMongoID(keyValuePair.Value);
            }
        }
        else
        {
            writer.Put(false);
        }
        if (target.Equipment != null)
        {
            writer.Put(true);
            writer.PutEFTInventoryEquipmentDescriptor(target.Equipment);
            return;
        }
        writer.Put(false);
    }

    /// <summary>
    /// Deserializes and reconstructs a PlayerVisualRepresentationDescriptor object from the reader stream.
    /// </summary>
    public static PlayerVisualRepresentationDescriptor GetEFTPlayerVisualRepresentationDescriptor(this NetDataReader reader)
    {
        var gclass = new PlayerVisualRepresentationDescriptor
        {
            Info = reader.GetJsonTypePlayerInfo()
        };
        if (reader.GetBool())
        {
            var num = reader.GetInt();
            gclass.Customization = new Dictionary<EBodyModelPart, MongoID>();
            for (var i = 0; i < num; i++)
            {
                gclass.Customization[reader.GetEnum<EBodyModelPart>()] = reader.GetMongoID();
            }
        }
        if (reader.GetBool())
        {
            gclass.Equipment = reader.GetEFTInventoryEquipmentDescriptor();
        }
        return gclass;
    }

    /// <summary>
    /// Serializes the poison component descriptor state into the writer stream.
    /// </summary>
    public static void PutEFTPoisonComponentDescriptor(this NetDataWriter writer, PoisonComponentDescriptor target)
    {
        writer.Put(target.Resource);
    }

    /// <summary>
    /// Deserializes and reconstructs a PoisonComponentDescriptor object from the reader stream.
    /// </summary>
    public static PoisonComponentDescriptor GetEFTPoisonComponentDescriptor(this NetDataReader reader)
    {
        return new PoisonComponentDescriptor
        {
            Resource = reader.GetFloat()
        };
    }

    /// <summary>
    /// Serializes prestige status validation data points into the writer stream.
    /// </summary>
    public static void PutEFTPrestigePrestigeStatusData(this NetDataWriter writer, PrestigeStatusData target)
    {
        writer.PutMongoID(target.TemplateId);
        writer.Put(target.Timestamp);
    }

    /// <summary>
    /// Deserializes and reconstructs a PrestigeStatusData object from the reader stream.
    /// </summary>
    public static PrestigeStatusData GetEFTPrestigePrestigeStatusData(this NetDataReader reader)
    {
        return new PrestigeStatusData
        {
            TemplateId = reader.GetMongoID(),
            Timestamp = reader.GetInt()
        };
    }

    /// <summary>
    /// Serializes comprehensive multi-zone structural profile health data matrices into the writer stream.
    /// </summary>
    public static void PutEFTProfileHealthInfo(this NetDataWriter writer, Profile.HealthInfo target)
    {
        writer.Put(target.BodyParts.Count);
        foreach (var keyValuePair in target.BodyParts)
        {
            writer.PutEnum(keyValuePair.Key);
            writer.PutEFTProfileHealthInfoBodyPartInfo(keyValuePair.Value);
        }
        writer.PutEFTProfileHealthInfoValueInfo(target.Energy);
        writer.PutEFTProfileHealthInfoValueInfo(target.Hydration);
        writer.PutEFTProfileHealthInfoValueInfo(target.Temperature);
        writer.PutEFTProfileHealthInfoValueInfo(target.Poison);
        if (target.UpdateTime != null)
        {
            writer.Put(true);
            writer.Put(target.UpdateTime.Value);
            return;
        }
        writer.Put(false);
    }

    /// <summary>
    /// Deserializes and reconstructs a Profile.HealthInfo object from the reader stream.
    /// </summary>
    public static Profile.HealthInfo GetEFTProfileHealthInfo(this NetDataReader reader)
    {
        var profileHealthClass = new Profile.HealthInfo();
        var num = reader.GetInt();
        profileHealthClass.BodyParts = new Dictionary<EBodyPart, Profile.HealthInfo.BodyPartInfo>();
        for (var i = 0; i < num; i++)
        {
            profileHealthClass.BodyParts[reader.GetEnum<EBodyPart>()] = reader.GetEFTProfileHealthInfoBodyPartInfo();
        }
        profileHealthClass.Energy = reader.GetEFTProfileHealthInfoValueInfo();
        profileHealthClass.Hydration = reader.GetEFTProfileHealthInfoValueInfo();
        profileHealthClass.Temperature = reader.GetEFTProfileHealthInfoValueInfo();
        profileHealthClass.Poison = reader.GetEFTProfileHealthInfoValueInfo();
        if (reader.GetBool())
        {
            profileHealthClass.UpdateTime = new int?(reader.GetInt());
        }
        return profileHealthClass;
    }

    /// <summary>
    /// Serializes structural health values and active status side effects for a targeted body zone into the writer stream.
    /// </summary>
    public static void PutEFTProfileHealthInfoBodyPartInfo(this NetDataWriter writer, Profile.HealthInfo.BodyPartInfo target)
    {
        writer.PutEFTProfileHealthInfoValueInfo(target.Health);
        writer.Put(target.Effects.Count);
        foreach (var keyValuePair in target.Effects)
        {
            writer.Put(keyValuePair.Key);
            writer.PutEFTProfileHealthInfoEffectInfo(keyValuePair.Value);
        }
    }

    /// <summary>
    /// Deserializes and reconstructs a Profile.HealthInfo.BodyPartInfo object from the reader stream.
    /// </summary>
    public static Profile.HealthInfo.BodyPartInfo GetEFTProfileHealthInfoBodyPartInfo(this NetDataReader reader)
    {
        var profileBodyPartHealthClass = new Profile.HealthInfo.BodyPartInfo
        {
            Health = reader.GetEFTProfileHealthInfoValueInfo()
        };
        var num = reader.GetInt();
        profileBodyPartHealthClass.Effects = new Dictionary<string, Profile.HealthInfo.EffectInfo>();
        for (var i = 0; i < num; i++)
        {
            profileBodyPartHealthClass.Effects[reader.GetString()] = reader.GetEFTProfileHealthInfoEffectInfo();
        }
        return profileBodyPartHealthClass;
    }

    /// <summary>
    /// Serializes a targeted health anomaly or buff context duration element into the writer stream.
    /// </summary>
    public static void PutEFTProfileHealthInfoEffectInfo(this NetDataWriter writer, Profile.HealthInfo.EffectInfo target)
    {
        writer.Put(target.Time);
    }

    /// <summary>
    /// Deserializes and reconstructs a Profile.HealthInfo.EffectInfo object from the reader stream.
    /// </summary>
    public static Profile.HealthInfo.EffectInfo GetEFTProfileHealthInfoEffectInfo(this NetDataReader reader)
    {
        return new Profile.HealthInfo.EffectInfo
        {
            Time = reader.GetFloat()
        };
    }

    /// <summary>
    /// Serializes precise structural attribute parameters including maximum thresholds and external damage scale coefficients into the writer stream.
    /// </summary>
    public static void PutEFTProfileHealthInfoValueInfo(this NetDataWriter writer, Profile.HealthInfo.ValueInfo target)
    {
        writer.Put(target.Current);
        writer.Put(target.Minimum);
        writer.Put(target.Maximum);
        writer.Put(target.OverDamageReceivedMultiplier);
        writer.Put(target.EnvironmentDamageMultiplier);
    }

    /// <summary>
    /// Deserializes and reconstructs a Profile.HealthInfo.ValueInfo object from the reader stream.
    /// </summary>
    public static Profile.HealthInfo.ValueInfo GetEFTProfileHealthInfoValueInfo(this NetDataReader reader)
    {
        return new Profile.HealthInfo.ValueInfo
        {
            Current = reader.GetFloat(),
            Minimum = reader.GetFloat(),
            Maximum = reader.GetFloat(),
            OverDamageReceivedMultiplier = reader.GetFloat(),
            EnvironmentDamageMultiplier = reader.GetFloat()
        };
    }

    /// <summary>
    /// Serializes safe-trade profile economy money transfer limits and remaining intervals into the writer stream.
    /// </summary>
    public static void PutEFTProfileMoneyTransferLimitData(this NetDataWriter writer, Profile.MoneyTransferLimitData target)
    {
        writer.Put(target.nextResetTime);
        writer.Put(target.remainingLimit);
        writer.Put(target.totalLimit);
        writer.Put(target.resetInterval);
    }

    /// <summary>
    /// Deserializes and reconstructs a Profile.MoneyTransferLimitData object from the reader stream.
    /// </summary>
    public static Profile.MoneyTransferLimitData GetEFTProfileMoneyTransferLimitData(this NetDataReader reader)
    {
        return new Profile.MoneyTransferLimitData
        {
            nextResetTime = reader.GetInt(),
            remainingLimit = reader.GetInt(),
            totalLimit = reader.GetInt(),
            resetInterval = reader.GetInt()
        };
    }

    /// <summary>
    /// Serializes profile recipe unlock structures into the writer stream.
    /// </summary>
    public static void PutEFTProfileUnlockedInfo(this NetDataWriter writer, Profile.UnlockedInfo target)
    {
        writer.Put(target.unlockedSchemeList.Count);
        for (var i = 0; i < target.unlockedSchemeList.Count; i++)
        {
            writer.PutMongoID(target.unlockedSchemeList[i]);
        }
    }

    /// <summary>
    /// Deserializes and reconstructs a Profile.UnlockedInfo object from the reader stream.
    /// </summary>
    public static Profile.UnlockedInfo GetEFTProfileUnlockedInfo(this NetDataReader reader)
    {
        var gclass = new Profile.UnlockedInfo();
        var num = reader.GetInt();
        gclass.unlockedSchemeList = new List<MongoID>(num);
        for (var i = 0; i < num; i++)
        {
            gclass.unlockedSchemeList.Add(reader.GetMongoID());
        }
        return gclass;
    }

    /// <summary>
    /// Serializes a profile account moderation ban entry state into the writer stream.
    /// </summary>
    public static void PutEFTProfileBanDescriptor(this NetDataWriter writer, ProfileBanDescriptor target)
    {
        writer.PutEnum(target.Type);
        writer.Put(target.ExpirationTime);
    }

    /// <summary>
    /// Deserializes and reconstructs a ProfileBanDescriptor object from the reader stream.
    /// </summary>
    public static ProfileBanDescriptor GetEFTProfileBanDescriptor(this NetDataReader reader)
    {
        return new ProfileBanDescriptor
        {
            Type = reader.GetEnum<EBanType>(),
            ExpirationTime = reader.GetLong()
        };
    }

    /// <summary>
    /// Serializes an entire complete server profile context descriptor containing all sub-systems into the writer stream.
    /// </summary>
    public static void PutEFTProfileDescriptor(this NetDataWriter writer, ProfileDescriptor target)
    {
        writer.PutMongoID(target.Id);
        writer.Put(target.AccountId);
        if (target.PetId != null)
        {
            writer.Put(true);
            writer.PutMongoID(target.PetId.Value);
        }
        else
        {
            writer.Put(false);
        }
        writer.Put(target.KarmaValue);
        writer.PutPolymorph(target.Info);
        writer.Put(target.Customization.Count);
        foreach (var keyValuePair in target.Customization)
        {
            writer.PutEnum(keyValuePair.Key);
            writer.PutMongoID(keyValuePair.Value);
        }
        if (target.Encyclopedia != null)
        {
            writer.Put(true);
            writer.Put(target.Encyclopedia.Count);
            foreach (var keyValuePair2 in target.Encyclopedia)
            {
                writer.PutMongoID(keyValuePair2.Key);
                writer.Put(keyValuePair2.Value);
            }
        }
        else
        {
            writer.Put(false);
        }
        if (target.Health != null)
        {
            writer.Put(true);
            writer.PutEFTProfileHealthInfo(target.Health);
        }
        else
        {
            writer.Put(false);
        }
        writer.PutEFTInventoryDescriptor(target.Inventory);
        writer.Put(target.InsuredItems.Length);
        for (var i = 0; i < target.InsuredItems.Length; i++)
        {
            writer.PutJsonTypeInsuredProfileItems(target.InsuredItems[i]);
        }
        writer.PutEFTSkillsDescriptor(target.Skills);
        writer.PutEFTNotesNotesManagerNotesDescriptor(target.Notes);
        writer.Put(target.TaskConditionCounters.Count);
        foreach (var keyValuePair3 in target.TaskConditionCounters)
        {
            writer.PutMongoID(keyValuePair3.Key);
            writer.PutEFTTaskConditionCounterDescriptor(keyValuePair3.Value);
        }
        writer.Put(target.QuestsData.Count);
        for (var j = 0; j < target.QuestsData.Count; j++)
        {
            writer.PutEFTQuestsQuestStatusData(target.QuestsData[j]);
        }
        writer.Put(target.AchievementsData.Count);
        foreach (var keyValuePair4 in target.AchievementsData)
        {
            writer.PutMongoID(keyValuePair4.Key);
            writer.Put(keyValuePair4.Value);
        }
        writer.Put(target.PrestigeData.Count);
        foreach (var keyValuePair5 in target.PrestigeData)
        {
            writer.PutMongoID(keyValuePair5.Key);
            writer.Put(keyValuePair5.Value);
        }
        writer.Put(target.VariableData.Count);
        foreach (var keyValuePair6 in target.VariableData)
        {
            writer.PutMongoID(keyValuePair6.Key);
            writer.Put(keyValuePair6.Value);
        }
        writer.PutEFTProfileUnlockedInfo(target.UnlockedRecipeInfo);
        writer.PutEFTProfileMoneyTransferLimitData(target.TransferLimitData);
        writer.Put(target.Bonuses.Length);
        for (var k = 0; k < target.Bonuses.Length; k++)
        {
            writer.PutEFTBonusDescriptor(target.Bonuses[k]);
        }
        writer.Put(target.WishList.Count);
        foreach (var keyValuePair7 in target.WishList)
        {
            writer.PutMongoID(keyValuePair7.Key);
            writer.Put(keyValuePair7.Value);
        }
        writer.PutEFTProfileStatsSeparatorDescriptor(target.Stats);
        writer.Put(target.CheckedMagazines.Count);
        foreach (var keyValuePair8 in target.CheckedMagazines)
        {
            writer.PutMongoID(keyValuePair8.Key);
            writer.Put(keyValuePair8.Value);
        }
        writer.Put(target.CheckedChambers.Count);
        for (var l = 0; l < target.CheckedChambers.Count; l++)
        {
            writer.PutMongoID(target.CheckedChambers[l]);
        }
        writer.Put(target.TradersInfo.Count);
        foreach (var keyValuePair9 in target.TradersInfo)
        {
            writer.PutMongoID(keyValuePair9.Key);
            writer.PutEFTTraderInfoDescriptor(keyValuePair9.Value);
        }
    }

    /// <summary>
    /// Deserializes and reconstructs a ProfileDescriptor object from the reader stream.
    /// </summary>
    public static ProfileDescriptor GetEFTProfileDescriptor(this NetDataReader reader)
    {
        var completeProfileDescriptorClass = new ProfileDescriptor
        {
            Id = reader.GetMongoID(),
            AccountId = reader.GetString()
        };
        if (reader.GetBool())
        {
            completeProfileDescriptorClass.PetId = new MongoID?(reader.GetMongoID());
        }
        completeProfileDescriptorClass.KarmaValue = reader.GetFloat();
        completeProfileDescriptorClass.Info = reader.GetPolymorph<ProfileInfoDescriptor>();
        var num = reader.GetInt();
        completeProfileDescriptorClass.Customization = new Dictionary<EBodyModelPart, MongoID>();
        for (var i = 0; i < num; i++)
        {
            completeProfileDescriptorClass.Customization[reader.GetEnum<EBodyModelPart>()] = reader.GetMongoID();
        }
        if (reader.GetBool())
        {
            var num2 = reader.GetInt();
            completeProfileDescriptorClass.Encyclopedia = new Dictionary<MongoID, bool>();
            for (var j = 0; j < num2; j++)
            {
                completeProfileDescriptorClass.Encyclopedia[reader.GetMongoID()] = reader.GetBool();
            }
        }
        if (reader.GetBool())
        {
            completeProfileDescriptorClass.Health = reader.GetEFTProfileHealthInfo();
        }
        completeProfileDescriptorClass.Inventory = reader.GetEFTInventoryDescriptor();
        var num3 = reader.GetInt();
        completeProfileDescriptorClass.InsuredItems = new InsuredProfileItems[num3];
        for (var k = 0; k < num3; k++)
        {
            completeProfileDescriptorClass.InsuredItems[k] = reader.GetJsonTypeInsuredProfileItems();
        }
        completeProfileDescriptorClass.Skills = reader.GetEFTSkillsDescriptor();
        completeProfileDescriptorClass.Notes = reader.GetEFTNotesNotesManagerNotesDescriptor();
        var num4 = reader.GetInt();
        completeProfileDescriptorClass.TaskConditionCounters = new Dictionary<MongoID, TaskConditionCounterDescriptor>();
        for (var l = 0; l < num4; l++)
        {
            completeProfileDescriptorClass.TaskConditionCounters[reader.GetMongoID()] = reader.GetEFTTaskConditionCounterDescriptor();
        }
        var num5 = reader.GetInt();
        completeProfileDescriptorClass.QuestsData = new List<QuestDataClass>(num5);
        for (var m = 0; m < num5; m++)
        {
            completeProfileDescriptorClass.QuestsData.Add(reader.GetEFTQuestsQuestStatusData());
        }
        var num6 = reader.GetInt();
        completeProfileDescriptorClass.AchievementsData = new Dictionary<MongoID, int>();
        for (var n = 0; n < num6; n++)
        {
            completeProfileDescriptorClass.AchievementsData[reader.GetMongoID()] = reader.GetInt();
        }
        var num7 = reader.GetInt();
        completeProfileDescriptorClass.PrestigeData = new Dictionary<MongoID, int>();
        for (var num8 = 0; num8 < num7; num8++)
        {
            completeProfileDescriptorClass.PrestigeData[reader.GetMongoID()] = reader.GetInt();
        }
        var num9 = reader.GetInt();
        completeProfileDescriptorClass.VariableData = new Dictionary<MongoID, int>();
        for (var num10 = 0; num10 < num9; num10++)
        {
            completeProfileDescriptorClass.VariableData[reader.GetMongoID()] = reader.GetInt();
        }
        completeProfileDescriptorClass.UnlockedRecipeInfo = reader.GetEFTProfileUnlockedInfo();
        completeProfileDescriptorClass.TransferLimitData = reader.GetEFTProfileMoneyTransferLimitData();
        var num11 = reader.GetInt();
        completeProfileDescriptorClass.Bonuses = new BonusDescriptor[num11];
        for (var num12 = 0; num12 < num11; num12++)
        {
            completeProfileDescriptorClass.Bonuses[num12] = reader.GetEFTBonusDescriptor();
        }
        var num13 = reader.GetInt();
        completeProfileDescriptorClass.WishList = new Dictionary<MongoID, byte>();
        for (var num14 = 0; num14 < num13; num14++)
        {
            completeProfileDescriptorClass.WishList[reader.GetMongoID()] = reader.GetByte();
        }
        completeProfileDescriptorClass.Stats = reader.GetEFTProfileStatsSeparatorDescriptor();
        var num15 = reader.GetInt();
        completeProfileDescriptorClass.CheckedMagazines = new Dictionary<MongoID, int>();
        for (var num16 = 0; num16 < num15; num16++)
        {
            completeProfileDescriptorClass.CheckedMagazines[reader.GetMongoID()] = reader.GetInt();
        }
        var num17 = reader.GetInt();
        completeProfileDescriptorClass.CheckedChambers = new List<MongoID>(num17);
        for (var num18 = 0; num18 < num17; num18++)
        {
            completeProfileDescriptorClass.CheckedChambers.Add(reader.GetMongoID());
        }
        var num19 = reader.GetInt();
        completeProfileDescriptorClass.TradersInfo = new Dictionary<MongoID, TraderInfoDescriptor>();
        for (var num20 = 0; num20 < num19; num20++)
        {
            completeProfileDescriptorClass.TradersInfo[reader.GetMongoID()] = reader.GetEFTTraderInfoDescriptor();
        }
        return completeProfileDescriptorClass;
    }

    /// <summary>
    /// Serializes fundamental metadata and setting parameters for a profile character entry into the writer stream.
    /// </summary>
    public static void PutEFTProfileInfoDescriptor(this NetDataWriter writer, ProfileInfoDescriptor target)
    {
        writer.Put(target.Nickname);
        writer.Put(target.MainProfileNickname);
        writer.PutEnum(target.Side);
        writer.Put(target.PrestigeLevel);
        writer.Put(target.RegistrationDate);
        writer.Put(target.SavageLockTime);
        writer.Put(target.GroupId);
        writer.Put(target.TeamId);
        writer.Put(target.EntryPoint);
        writer.Put(target.NicknameChangeDate);
        writer.Put(target.GameVersion);
        writer.PutEnum(target.Type);
        writer.Put(target.HasCoopExtension);
        writer.Put(target.HasPveGame);
        writer.Put(target.IsStreamerModeAvailable);
        writer.Put(target.GroupInviteRestriction);
        writer.Put(target.Bans.Count);
        for (var i = 0; i < target.Bans.Count; i++)
        {
            writer.PutEFTProfileBanDescriptor(target.Bans[i]);
        }
        writer.PutEFTProfileSettings(target.Settings);
        writer.PutEnum(target.MemberCategory);
        writer.PutEnum(target.SelectedMemberCategory);
        writer.Put(target.Experience);
        writer.Put(target.Level);
        writer.Put(target.LockedMoveCommands);
    }

    /// <summary>
    /// Deserializes and reconstructs a ProfileInfoDescriptor object from the reader stream.
    /// </summary>
    public static ProfileInfoDescriptor GetEFTProfileInfoDescriptor(this NetDataReader reader)
    {
        var profileInfoClass = new ProfileInfoDescriptor
        {
            Nickname = reader.GetString(),
            MainProfileNickname = reader.GetString(),
            Side = reader.GetEnum<EPlayerSide>(),
            PrestigeLevel = reader.GetInt(),
            RegistrationDate = reader.GetInt(),
            SavageLockTime = reader.GetDouble(),
            GroupId = reader.GetString(),
            TeamId = reader.GetString(),
            EntryPoint = reader.GetString(),
            NicknameChangeDate = reader.GetLong(),
            GameVersion = reader.GetString(),
            Type = reader.GetEnum<EProfileType>(),
            HasCoopExtension = reader.GetBool(),
            HasPveGame = reader.GetBool(),
            IsStreamerModeAvailable = reader.GetBool(),
            GroupInviteRestriction = reader.GetBool()
        };
        var num = reader.GetInt();
        profileInfoClass.Bans = new List<ProfileBanDescriptor>(num);
        for (var i = 0; i < num; i++)
        {
            profileInfoClass.Bans.Add(reader.GetEFTProfileBanDescriptor());
        }
        profileInfoClass.Settings = reader.GetEFTProfileSettings();
        profileInfoClass.MemberCategory = reader.GetEnum<EMemberCategory>();
        profileInfoClass.SelectedMemberCategory = reader.GetEnum<EMemberCategory>();
        profileInfoClass.Experience = reader.GetInt();
        profileInfoClass.Level = reader.GetInt();
        profileInfoClass.LockedMoveCommands = reader.GetBool();
        return profileInfoClass;
    }

    /// <summary>
    /// Serializes contextual internal AI bot matching structure configuration fields into the writer stream.
    /// </summary>
    public static void PutEFTProfileSettings(this NetDataWriter writer, ProfileSettings target)
    {
        writer.PutEnum(target.Role);
        writer.PutEnum(target.BotDifficulty);
        writer.Put(target.Experience);
        writer.Put(target.StandingForKill);
        writer.Put(target.AggressorBonus);
        writer.Put(target.UseSimpleAnimator);
    }

    /// <summary>
    /// Deserializes and reconstructs a ProfileSettings object from the reader stream.
    /// </summary>
    public static ProfileSettings GetEFTProfileSettings(this NetDataReader reader)
    {
        return new ProfileSettings
        {
            Role = reader.GetEnum<WildSpawnType>(),
            BotDifficulty = reader.GetEnum<BotDifficulty>(),
            Experience = reader.GetInt(),
            StandingForKill = reader.GetDouble(),
            AggressorBonus = reader.GetDouble(),
            UseSimpleAnimator = reader.GetBool()
        };
    }

    /// <summary>
    /// Serializes session persistent stats metrics, combat actions history records, and raid logs into the writer stream.
    /// </summary>
    public static void PutEFTProfileStatsDescriptor(this NetDataWriter writer, ProfileStatsDescriptor target)
    {
        writer.PutEFTCounterCollectionDescriptor(target.SessionCounters);
        writer.PutEFTCounterCollectionDescriptor(target.OverallCounters);
        writer.Put(target.SessionExperienceMult);
        writer.Put(target.ExperienceBonusMult);
        writer.Put(target.TotalSessionExperience);
        writer.Put(target.LastSessionDate);
        if (target.Aggressor != null)
        {
            writer.Put(true);
            writer.PutAggressorStats(target.Aggressor);
        }
        else
        {
            writer.Put(false);
        }
        writer.Put(target.DroppedItems.Count);
        for (var i = 0; i < target.DroppedItems.Count; i++)
        {
            writer.PutEFTDroppedItem(target.DroppedItems[i]);
        }
        writer.Put(target.FoundInRaidItems.Count);
        for (var j = 0; j < target.FoundInRaidItems.Count; j++)
        {
            writer.PutEFTFoundInRaidItem(target.FoundInRaidItems[j]);
        }
        writer.Put(target.Victims.Count);
        for (var k = 0; k < target.Victims.Count; k++)
        {
            writer.PutEFTVictimStats(target.Victims[k]);
        }
        writer.Put(target.CarriedQuestItems.Count);
        for (var l = 0; l < target.CarriedQuestItems.Count; l++)
        {
            writer.PutMongoID(target.CarriedQuestItems[l]);
        }
        if (target.DamageHistory != null)
        {
            writer.Put(true);
            writer.PutEFTDamageHistoryDescriptor(target.DamageHistory);
        }
        else
        {
            writer.Put(false);
        }
        if (target.DeathCause != null)
        {
            writer.Put(true);
            writer.PutEFTDeathCause(target.DeathCause);
        }
        else
        {
            writer.Put(false);
        }
        if (target.LastPlayerState != null)
        {
            writer.Put(true);
            writer.PutEFTPlayerVisualRepresentationDescriptor(target.LastPlayerState);
        }
        else
        {
            writer.Put(false);
        }
        writer.Put(target.TotalInGameTime);
        writer.PutEnum(target.SurvivorClass);
    }

    /// <summary>
    /// Deserializes and reconstructs a ProfileStatsDescriptor object from the reader stream.
    /// </summary>
    public static ProfileStatsDescriptor GetEFTProfileStatsDescriptor(this NetDataReader reader)
    {
        var profileEftStatsClass = new ProfileStatsDescriptor
        {
            SessionCounters = reader.GetEFTCounterCollectionDescriptor(),
            OverallCounters = reader.GetEFTCounterCollectionDescriptor(),
            SessionExperienceMult = reader.GetFloat(),
            ExperienceBonusMult = reader.GetFloat(),
            TotalSessionExperience = reader.GetInt(),
            LastSessionDate = reader.GetInt()
        };
        if (reader.GetBool())
        {
            profileEftStatsClass.Aggressor = reader.GetAggressorStats();
        }
        var num = reader.GetInt();
        profileEftStatsClass.DroppedItems = new List<DroppedItem>(num);
        for (var i = 0; i < num; i++)
        {
            profileEftStatsClass.DroppedItems.Add(reader.GetEFTDroppedItem());
        }
        var num2 = reader.GetInt();
        profileEftStatsClass.FoundInRaidItems = new List<FoundInRaidItem>(num2);
        for (var j = 0; j < num2; j++)
        {
            profileEftStatsClass.FoundInRaidItems.Add(reader.GetEFTFoundInRaidItem());
        }
        var num3 = reader.GetInt();
        profileEftStatsClass.Victims = new List<VictimStats>(num3);
        for (var k = 0; k < num3; k++)
        {
            profileEftStatsClass.Victims.Add(reader.GetEFTVictimStats());
        }
        var num4 = reader.GetInt();
        profileEftStatsClass.CarriedQuestItems = new List<MongoID>(num4);
        for (var l = 0; l < num4; l++)
        {
            profileEftStatsClass.CarriedQuestItems.Add(reader.GetMongoID());
        }
        if (reader.GetBool())
        {
            profileEftStatsClass.DamageHistory = reader.GetEFTDamageHistoryDescriptor();
        }
        if (reader.GetBool())
        {
            profileEftStatsClass.DeathCause = reader.GetEFTDeathCause();
        }
        if (reader.GetBool())
        {
            profileEftStatsClass.LastPlayerState = reader.GetEFTPlayerVisualRepresentationDescriptor();
        }
        profileEftStatsClass.TotalInGameTime = reader.GetLong();
        profileEftStatsClass.SurvivorClass = reader.GetEnum<ProfileStats.ESurvivorClass>();
        return profileEftStatsClass;
    }

    /// <summary>
    /// Serializes divided statistics profiles for separate game environments into the writer stream.
    /// </summary>
    public static void PutEFTProfileStatsSeparatorDescriptor(this NetDataWriter writer, ProfileStatsSeparatorDescriptor target)
    {
        if (target.Eft != null)
        {
            writer.Put(true);
            writer.PutEFTProfileStatsDescriptor(target.Eft);
        }
        else
        {
            writer.Put(false);
        }
        if (target.Arena != null)
        {
            writer.Put(true);
            writer.PutEFTProfileStatsDescriptor(target.Arena);
            return;
        }
        writer.Put(false);
    }

    /// <summary>
    /// Deserializes and reconstructs a ProfileStatsSeparatorDescriptor object from the reader stream.
    /// </summary>
    public static ProfileStatsSeparatorDescriptor GetEFTProfileStatsSeparatorDescriptor(this NetDataReader reader)
    {
        var profileStatsClass = new ProfileStatsSeparatorDescriptor();
        if (reader.GetBool())
        {
            profileStatsClass.Eft = reader.GetEFTProfileStatsDescriptor();
        }
        if (reader.GetBool())
        {
            profileStatsClass.Arena = reader.GetEFTProfileStatsDescriptor();
        }
        return profileStatsClass;
    }

    /// <summary>
    /// Serializes a quest acceptance operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTQuestAcceptDescriptor(this NetDataWriter writer, QuestAcceptDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutMongoID(target.QuestId);
    }

    /// <summary>
    /// Deserializes and reconstructs a QuestAcceptDescriptor object from the reader stream.
    /// </summary>
    public static QuestAcceptDescriptor GetEFTQuestAcceptDescriptor(this NetDataReader reader)
    {
        return new QuestAcceptDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            QuestId = reader.GetMongoID()
        };
    }

    /// <summary>
    /// Serializes a quest finalization/finish operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTQuestFinishDescriptor(this NetDataWriter writer, QuestFinishDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutMongoID(target.QuestId);
    }

    /// <summary>
    /// Deserializes and reconstructs a QuestFinishDescriptor object from the reader stream.
    /// </summary>
    public static QuestFinishDescriptor GetEFTQuestFinishDescriptor(this NetDataReader reader)
    {
        return new QuestFinishDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            QuestId = reader.GetMongoID()
        };
    }

    /// <summary>
    /// Serializes a quest condition items handover operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTQuestHandoverDescriptor(this NetDataWriter writer, QuestHandoverDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.ItemIds.Length);
        for (var i = 0; i < target.ItemIds.Length; i++)
        {
            writer.PutMongoID(target.ItemIds[i]);
        }
        writer.PutMongoID(target.ConditionId);
        writer.PutMongoID(target.QuestId);
    }

    /// <summary>
    /// Deserializes and reconstructs a QuestHandoverDescriptor object from the reader stream.
    /// </summary>
    public static QuestHandoverDescriptor GetEFTQuestHandoverDescriptor(this NetDataReader reader)
    {
        var gclass = new QuestHandoverDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID()
        };
        var num = reader.GetInt();
        gclass.ItemIds = new MongoID[num];
        for (var i = 0; i < num; i++)
        {
            gclass.ItemIds[i] = reader.GetMongoID();
        }
        gclass.ConditionId = reader.GetMongoID();
        gclass.QuestId = reader.GetMongoID();
        return gclass;
    }

    /// <summary>
    /// Serializes historical quest state timeline logs and objective progress mappings into the writer stream.
    /// </summary>
    public static void PutEFTQuestsQuestStatusData(this NetDataWriter writer, QuestDataClass target)
    {
        writer.Put(target.Id);
        writer.Put(target.StartTime);
        writer.PutEnum(target.Status);
        writer.Put(target.StatusStartTimestamps.Count);
        foreach (var keyValuePair in target.StatusStartTimestamps)
        {
            writer.PutEnum(keyValuePair.Key);
            writer.Put(keyValuePair.Value);
        }
        writer.Put(target.CompletedConditions.Count);
        foreach (var mongoID in target.CompletedConditions)
        {
            writer.PutMongoID(mongoID);
        }
        writer.Put(target.AvailableAfter);
    }

    /// <summary>
    /// Deserializes and reconstructs a QuestDataClass object from the reader stream.
    /// </summary>
    public static QuestDataClass GetEFTQuestsQuestStatusData(this NetDataReader reader)
    {
        var questDataClass = new QuestDataClass
        {
            Id = reader.GetString(),
            StartTime = reader.GetInt(),
            Status = reader.GetEnum<EQuestStatus>()
        };
        var num = reader.GetInt();
        questDataClass.StatusStartTimestamps = new Dictionary<EQuestStatus, double>();
        for (var i = 0; i < num; i++)
        {
            questDataClass.StatusStartTimestamps[reader.GetEnum<EQuestStatus>()] = reader.GetDouble();
        }
        var num2 = reader.GetInt();
        questDataClass.CompletedConditions = new HashSet<MongoID>();
        for (var j = 0; j < num2; j++)
        {
            questDataClass.CompletedConditions.Add(reader.GetMongoID());
        }
        questDataClass.AvailableAfter = reader.GetInt();
        return questDataClass;
    }

    /// <summary>
    /// Serializes the recodable/encoded item status state component into the writer stream.
    /// </summary>
    public static void PutEFTRecodableComponentDescriptor(this NetDataWriter writer, RecodableComponentDescriptor target)
    {
        writer.Put(target.IsEncoded);
    }

    /// <summary>
    /// Deserializes and reconstructs a RecodableComponentDescriptor object from the reader stream.
    /// </summary>
    public static RecodableComponentDescriptor GetEFTRecodableComponentDescriptor(this NetDataReader reader)
    {
        return new RecodableComponentDescriptor
        {
            IsEncoded = reader.GetBool()
        };
    }

    /// <summary>
    /// Serializes an inventory item removal operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTRemoveOperationDescriptor(this NetDataWriter writer, RemoveOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutMongoID(target.ItemId);
    }

    /// <summary>
    /// Deserializes and reconstructs a RemoveOperationDescriptor object from the reader stream.
    /// </summary>
    public static RemoveOperationDescriptor GetEFTRemoveOperationDescriptor(this NetDataReader reader)
    {
        return new RemoveOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            ItemId = reader.GetMongoID()
        };
    }

    /// <summary>
    /// Serializes the repairable item status parameters into the writer stream.
    /// </summary>
    public static void PutEFTRepairableComponentDescriptor(this NetDataWriter writer, RepairableComponentDescriptor target)
    {
        writer.Put(target.Durability);
        writer.Put(target.MaxDurability);
    }

    /// <summary>
    /// Deserializes and reconstructs a RepairableComponentDescriptor object from the reader stream.
    /// </summary>
    public static RepairableComponentDescriptor GetEFTRepairableComponentDescriptor(this NetDataReader reader)
    {
        return new RepairableComponentDescriptor
        {
            Durability = reader.GetFloat(),
            MaxDurability = reader.GetFloat()
        };
    }

    /// <summary>
    /// Serializes the repair enhancement state attributes and modifiers into the writer stream.
    /// </summary>
    public static void PutEFTRepairEnhancementComponentDescriptor(this NetDataWriter writer, RepairEnhancementComponentDescriptor target)
    {
        if (target.BuffType != null)
        {
            writer.Put(true);
            writer.PutEnum(target.BuffType.Value);
        }
        else
        {
            writer.Put(false);
        }
        if (target.BuffRarity != null)
        {
            writer.Put(true);
            writer.PutEnum(target.BuffRarity.Value);
        }
        else
        {
            writer.Put(false);
        }
        writer.Put(target.Value);
        writer.Put(target.ThresholdDurability);
    }

    /// <summary>
    /// Deserializes and reconstructs a RepairEnhancementComponentDescriptor object from the reader stream.
    /// </summary>
    public static RepairEnhancementComponentDescriptor GetEFTRepairEnhancementComponentDescriptor(this NetDataReader reader)
    {
        var gclass = new RepairEnhancementComponentDescriptor();
        if (reader.GetBool())
        {
            gclass.BuffType = new ERepairBuffType?(reader.GetEnum<ERepairBuffType>());
        }
        if (reader.GetBool())
        {
            gclass.BuffRarity = new EBuffRarity?(reader.GetEnum<EBuffRarity>());
        }
        gclass.Value = reader.GetFloat();
        gclass.ThresholdDurability = reader.GetFloat();
        return gclass;
    }

    /// <summary>
    /// Serializes the repair kit resource points component data into the writer stream.
    /// </summary>
    public static void PutEFTRepairKitComponentDescriptor(this NetDataWriter writer, RepairKitComponentDescriptor target)
    {
        writer.Put(target.Resource);
    }

    /// <summary>
    /// Deserializes and reconstructs a RepairKitComponentDescriptor object from the reader stream.
    /// </summary>
    public static RepairKitComponentDescriptor GetEFTRepairKitComponentDescriptor(this NetDataReader reader)
    {
        return new RepairKitComponentDescriptor
        {
            Resource = reader.GetFloat()
        };
    }

    /// <summary>
    /// Serializes general item resource usage levels into the writer stream.
    /// </summary>
    public static void PutEFTResourceItemComponentDescriptor(this NetDataWriter writer, ResourceItemComponentDescriptor target)
    {
        writer.Put(target.Resource);
    }

    /// <summary>
    /// Deserializes and reconstructs a ResourceItemComponentDescriptor object from the reader stream.
    /// </summary>
    public static ResourceItemComponentDescriptor GetEFTResourceItemComponentDescriptor(this NetDataReader reader)
    {
        return new ResourceItemComponentDescriptor
        {
            Resource = reader.GetFloat()
        };
    }

    /// <summary>
    /// Serializes the asset reference manifest indices for a scene context into the writer stream.
    /// </summary>
    public static void PutEFTSceneResourceKey(this NetDataWriter writer, SceneResourceKey target)
    {
        if (target.path != null)
        {
            writer.Put(true);
            writer.Put(target.path);
        }
        else
        {
            writer.Put(false);
        }
        if (target.rcid != null)
        {
            writer.Put(true);
            writer.Put(target.rcid);
            return;
        }
        writer.Put(false);
    }

    /// <summary>
    /// Deserializes and reconstructs a SceneResourceKey object from the reader stream.
    /// </summary>
    public static SceneResourceKey GetEFTSceneResourceKey(this NetDataReader reader)
    {
        var sceneResourceKey = new SceneResourceKey();
        if (reader.GetBool())
        {
            sceneResourceKey.path = reader.GetString();
        }
        if (reader.GetBool())
        {
            sceneResourceKey.rcid = reader.GetString();
        }
        return sceneResourceKey;
    }

    /// <summary>
    /// Serializes standard system asset dependency lookup keys into the writer stream.
    /// </summary>
    public static void PutEFTResourceKey(this NetDataWriter writer, ResourceKey target)
    {
        if (target.path != null)
        {
            writer.Put(true);
            writer.Put(target.path);
        }
        else
        {
            writer.Put(false);
        }
        if (target.rcid != null)
        {
            writer.Put(true);
            writer.Put(target.rcid);
            return;
        }
        writer.Put(false);
    }

    /// <summary>
    /// Deserializes and reconstructs a ResourceKey object from the reader stream.
    /// </summary>
    public static ResourceKey GetEFTResourceKey(this NetDataReader reader)
    {
        var resourceKey = new ResourceKey();
        if (reader.GetBool())
        {
            resourceKey.path = reader.GetString();
        }
        if (reader.GetBool())
        {
            resourceKey.rcid = reader.GetString();
        }
        return resourceKey;
    }

    /// <summary>
    /// Serializes a localized story dialog narrative advancement event into the writer stream.
    /// </summary>
    public static void PutEFTSetDialogProgressOperationDescriptor(this NetDataWriter writer, SetDialogProgressOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutMongoID(target.TraderId);
        writer.PutMongoID(target.DialogId);
        writer.PutMongoID(target.LineId);
    }

    /// <summary>
    /// Deserializes and reconstructs a SetDialogProgressOperationDescriptor object from the reader stream.
    /// </summary>
    public static SetDialogProgressOperationDescriptor GetEFTSetDialogProgressOperationDescriptor(this NetDataReader reader)
    {
        return new SetDialogProgressOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            TraderId = reader.GetMongoID(),
            DialogId = reader.GetMongoID(),
            LineId = reader.GetMongoID()
        };
    }

    /// <summary>
    /// Serializes an item placement/setup operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTSetupItemOperationDescriptor(this NetDataWriter writer, SetupItemOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.ItemId);
        writer.Put(target.ZoneId);
        writer.PutClassVector3(target.Position);
        writer.PutClassQuaternion(target.Rotation);
        writer.Put(target.SetupTime);
    }

    /// <summary>
    /// Deserializes and reconstructs a SetupItemOperationDescriptor object from the reader stream.
    /// </summary>
    public static SetupItemOperationDescriptor GetEFTSetupItemOperationDescriptor(this NetDataReader reader)
    {
        return new SetupItemOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            ItemId = reader.GetString(),
            ZoneId = reader.GetString(),
            Position = reader.GetClassVector3(),
            Rotation = reader.GetClassQuaternion(),
            SetupTime = reader.GetFloat()
        };
    }

    /// <summary>
    /// Serializes a dynamic variable assignment operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTSetVariableOperationDescriptor(this NetDataWriter writer, SetVariableOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutMongoID(target.VariableId);
        writer.Put(target.Value);
    }

    /// <summary>
    /// Deserializes and reconstructs a SetVariableOperationDescriptor object from the reader stream.
    /// </summary>
    public static SetVariableOperationDescriptor GetEFTSetVariableOperationDescriptor(this NetDataReader reader)
    {
        return new SetVariableOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            VariableId = reader.GetMongoID(),
            Value = reader.GetInt()
        };
    }

    /// <summary>
    /// Serializes a weapon cartridge shell template reference mapping into the writer stream.
    /// </summary>
    public static void PutEFTShellTemplateDescriptor(this NetDataWriter writer, ShellTemplateDescriptor target)
    {
        writer.PutMongoID(target.AmmoTemplateId);
    }

    /// <summary>
    /// Deserializes and reconstructs a ShellTemplateDescriptor object from the reader stream.
    /// </summary>
    public static ShellTemplateDescriptor GetEFTShellTemplateDescriptor(this NetDataReader reader)
    {
        return new ShellTemplateDescriptor
        {
            AmmoTemplateId = reader.GetMongoID()
        };
    }

    /// <summary>
    /// Serializes complex structural firearm sight calibration and scope multi-mode arrays into the writer stream.
    /// </summary>
    public static void PutEFTSightComponentDescriptor(this NetDataWriter writer, SightComponentDescriptor target)
    {
        writer.Put(target.SelectedSightScope);
        writer.Put(target.ScopesSelectedModes.Length);
        for (var i = 0; i < target.ScopesSelectedModes.Length; i++)
        {
            writer.Put(target.ScopesSelectedModes[i]);
        }
        writer.Put(target.ScopesSelectedCalibPoints.Length);
        for (var j = 0; j < target.ScopesSelectedCalibPoints.Length; j++)
        {
            writer.Put(target.ScopesSelectedCalibPoints[j]);
        }
        writer.Put(target.ScopeZoomValue);
    }

    /// <summary>
    /// Deserializes and reconstructs a SightComponentDescriptor object from the reader stream.
    /// </summary>
    public static SightComponentDescriptor GetEFTSightComponentDescriptor(this NetDataReader reader)
    {
        var gclass = new SightComponentDescriptor
        {
            SelectedSightScope = reader.GetInt()
        };
        var num = reader.GetInt();
        gclass.ScopesSelectedModes = new int[num];
        for (var i = 0; i < num; i++)
        {
            gclass.ScopesSelectedModes[i] = reader.GetInt();
        }
        var num2 = reader.GetInt();
        gclass.ScopesSelectedCalibPoints = new int[num2];
        for (var j = 0; j < num2; j++)
        {
            gclass.ScopesSelectedCalibPoints[j] = reader.GetInt();
        }
        gclass.ScopeZoomValue = reader.GetFloat();
        return gclass;
    }

    /// <summary>
    /// Serializes separate progression blocks covering both common attributes and mastering stats into the writer stream.
    /// </summary>
    public static void PutEFTSkillsDescriptor(this NetDataWriter writer, SkillsDescriptor target)
    {
        writer.Put(target.Common.Length);
        for (var i = 0; i < target.Common.Length; i++)
        {
            writer.PutEFTSkillsDescriptorSkillInfoDescriptor(target.Common[i]);
        }
        writer.Put(target.Mastering.Length);
        for (var j = 0; j < target.Mastering.Length; j++)
        {
            writer.PutEFTSkillsDescriptorMasteringInfoDescriptor(target.Mastering[j]);
        }
    }

    /// <summary>
    /// Deserializes and reconstructs a SkillsDescriptor object from the reader stream.
    /// </summary>
    public static SkillsDescriptor GetEFTSkillsDescriptor(this NetDataReader reader)
    {
        var skillsDescriptorClass = new SkillsDescriptor();
        var num = reader.GetInt();
        skillsDescriptorClass.Common = new SkillsDescriptor.SkillInfoDescriptor[num];
        for (var i = 0; i < num; i++)
        {
            skillsDescriptorClass.Common[i] = reader.GetEFTSkillsDescriptorSkillInfoDescriptor();
        }
        var num2 = reader.GetInt();
        skillsDescriptorClass.Mastering = new SkillsDescriptor.MasteringInfoDescriptor[num2];
        for (var j = 0; j < num2; j++)
        {
            skillsDescriptorClass.Mastering[j] = reader.GetEFTSkillsDescriptorMasteringInfoDescriptor();
        }
        return skillsDescriptorClass;
    }

    /// <summary>
    /// Serializes a specified weapon mastery identifier alongside its progression metric into the writer stream.
    /// </summary>
    public static void PutEFTSkillsDescriptorMasteringInfoDescriptor(this NetDataWriter writer, SkillsDescriptor.MasteringInfoDescriptor target)
    {
        writer.Put(target.Id);
        writer.Put(target.Progress);
    }

    /// <summary>
    /// Deserializes and reconstructs a SkillsDescriptor.MasteringInfoDescriptor object from the reader stream.
    /// </summary>
    public static SkillsDescriptor.MasteringInfoDescriptor GetEFTSkillsDescriptorMasteringInfoDescriptor(this NetDataReader reader)
    {
        return new SkillsDescriptor.MasteringInfoDescriptor
        {
            Id = reader.GetString(),
            Progress = reader.GetFloat()
        };
    }

    /// <summary>
    /// Serializes an isolated character skill item encompassing session gain records and temporal markers into the writer stream.
    /// </summary>
    public static void PutEFTSkillsDescriptorSkillInfoDescriptor(this NetDataWriter writer, SkillsDescriptor.SkillInfoDescriptor target)
    {
        writer.PutEnum(target.Id);
        writer.Put(target.Progress);
        writer.Put(target.PointsEarnedDuringSession);
        writer.Put(target.LastAccess);
    }

    /// <summary>
    /// Deserializes and reconstructs a SkillsDescriptor.SkillInfoDescriptor object from the reader stream.
    /// </summary>
    public static SkillsDescriptor.SkillInfoDescriptor GetEFTSkillsDescriptorSkillInfoDescriptor(this NetDataReader reader)
    {
        return new SkillsDescriptor.SkillInfoDescriptor
        {
            Id = reader.GetEnum<ESkillId>(),
            Progress = reader.GetFloat(),
            PointsEarnedDuringSession = reader.GetFloat(),
            LastAccess = reader.GetInt()
        };
    }

    /// <summary>
    /// Serializes a single inventory slot descriptor into the writer stream.
    /// </summary>
    public static void PutEFTSlotDescriptor(this NetDataWriter writer, SlotDescriptor target)
    {
        writer.Put(target.SlotNumber);
        writer.PutEFTItemDescriptor(target.ContainedItem);
    }

    /// <summary>
    /// Deserializes and reconstructs a SlotDescriptor object from the reader stream.
    /// </summary>
    public static SlotDescriptor GetEFTSlotDescriptor(this NetDataReader reader)
    {
        return new SlotDescriptor
        {
            SlotNumber = reader.GetByte(),
            ContainedItem = reader.GetEFTItemDescriptor()
        };
    }

    /// <summary>
    /// Serializes a slot item address container reference mapping into the writer stream.
    /// </summary>
    public static void PutEFTSlotItemAddressDescriptor(this NetDataWriter writer, SlotItemAddressDescriptor target)
    {
        writer.PutEFTContainerDescriptor(target.Container);
    }

    /// <summary>
    /// Deserializes and reconstructs a SlotItemAddressDescriptor object from the reader stream.
    /// </summary>
    public static SlotItemAddressDescriptor GetEFTSlotItemAddressDescriptor(this NetDataReader reader)
    {
        return new SlotItemAddressDescriptor
        {
            Container = reader.GetEFTContainerDescriptor()
        };
    }

    /// <summary>
    /// Serializes an item stack partition split operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTSplitOperationDescriptor(this NetDataWriter writer, SplitOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.ItemId);
        writer.Put(target.CloneId);
        writer.PutPolymorph(target.From);
        writer.PutPolymorph(target.To);
        writer.Put(target.Count);
    }

    /// <summary>
    /// Deserializes and reconstructs a SplitOperationDescriptor object from the reader stream.
    /// </summary>
    public static SplitOperationDescriptor GetEFTSplitOperationDescriptor(this NetDataReader reader)
    {
        return new SplitOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            ItemId = reader.GetString(),
            CloneId = reader.GetString(),
            From = reader.GetPolymorph<ItemAddressDescriptor>(),
            To = reader.GetPolymorph<ItemAddressDescriptor>(),
            Count = reader.GetInt()
        };
    }

    /// <summary>
    /// Serializes an inventory multi-item stack slot layout block into the writer stream.
    /// </summary>
    public static void PutEFTStackSlotDescriptor(this NetDataWriter writer, StackSlotDescriptor target)
    {
        writer.Put(target.SlotNumber);
        writer.Put(target.ContainedItems.Count);
        for (var i = 0; i < target.ContainedItems.Count; i++)
        {
            writer.PutEFTItemDescriptor(target.ContainedItems[i]);
        }
    }

    /// <summary>
    /// Deserializes and reconstructs a StackSlotDescriptor object from the reader stream.
    /// </summary>
    public static StackSlotDescriptor GetEFTStackSlotDescriptor(this NetDataReader reader)
    {
        var gclass = new StackSlotDescriptor
        {
            SlotNumber = reader.GetByte()
        };
        var num = reader.GetInt();
        gclass.ContainedItems = new List<ItemDescriptor>(num);
        for (var i = 0; i < num; i++)
        {
            gclass.ContainedItems.Add(reader.GetEFTItemDescriptor());
        }
        return gclass;
    }

    /// <summary>
    /// Serializes a stack slot structural layout item address mapping descriptor into the writer stream.
    /// </summary>
    public static void PutEFTStackSlotItemAddressDescriptor(this NetDataWriter writer, StackSlotItemAddressDescriptor target)
    {
        writer.PutEFTContainerDescriptor(target.Container);
    }

    /// <summary>
    /// Deserializes and reconstructs a StackSlotItemAddressDescriptor object from the reader stream.
    /// </summary>
    public static StackSlotItemAddressDescriptor GetEFTStackSlotItemAddressDescriptor(this NetDataReader reader)
    {
        return new StackSlotItemAddressDescriptor
        {
            Container = reader.GetEFTContainerDescriptor()
        };
    }

    /// <summary>
    /// Serializes a complex item layout positional swap exchange operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTSwapOperationDescriptor(this NetDataWriter writer, SwapOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.ItemId);
        writer.PutPolymorph(target.To);
        writer.Put(target.Item1Id);
        writer.PutPolymorph(target.To1);
        if (target.DestroyedItems != null)
        {
            writer.Put(true);
            writer.Put(target.DestroyedItems.Count);
            for (var i = 0; i < target.DestroyedItems.Count; i++)
            {
                writer.PutEFTDestroyedItem(target.DestroyedItems[i]);
            }
            return;
        }
        writer.Put(false);
    }

    /// <summary>
    /// Deserializes and reconstructs a SwapOperationDescriptor object from the reader stream.
    /// </summary>
    public static SwapOperationDescriptor GetEFTSwapOperationDescriptor(this NetDataReader reader)
    {
        var gclass = new SwapOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            ItemId = reader.GetString(),
            To = reader.GetPolymorph<ItemAddressDescriptor>(),
            Item1Id = reader.GetString(),
            To1 = reader.GetPolymorph<ItemAddressDescriptor>()
        };
        if (reader.GetBool())
        {
            var num = reader.GetInt();
            gclass.DestroyedItems = new List<DestroyedItem>(num);
            for (var i = 0; i < num; i++)
            {
                gclass.DestroyedItems.Add(reader.GetEFTDestroyedItem());
            }
        }
        return gclass;
    }

    /// <summary>
    /// Serializes an interactive item status tag component entry into the writer stream.
    /// </summary>
    public static void PutEFTTagComponentDescriptor(this NetDataWriter writer, TagComponentDescriptor target)
    {
        writer.Put(target.Name);
        writer.Put(target.Color);
    }

    /// <summary>
    /// Deserializes and reconstructs a TagComponentDescriptor object from the reader stream.
    /// </summary>
    public static TagComponentDescriptor GetEFTTagComponentDescriptor(this NetDataReader reader)
    {
        return new TagComponentDescriptor
        {
            Name = reader.GetString(),
            Color = reader.GetInt()
        };
    }

    /// <summary>
    /// Serializes an individual item label tag alteration operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTTagOperationDescriptor(this NetDataWriter writer, TagOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.ItemId);
        writer.Put(target.TagName);
        writer.Put(target.TagColor);
    }

    /// <summary>
    /// Deserializes and reconstructs a TagOperationDescriptor object from the reader stream.
    /// </summary>
    public static TagOperationDescriptor GetEFTTagOperationDescriptor(this NetDataReader reader)
    {
        return new TagOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            ItemId = reader.GetString(),
            TagName = reader.GetString(),
            TagColor = reader.GetInt()
        };
    }

    /// <summary>
    /// Serializes a targeted profile progression task condition monitoring tracker element into the writer stream.
    /// </summary>
    public static void PutEFTTaskConditionCounterDescriptor(this NetDataWriter writer, TaskConditionCounterDescriptor target)
    {
        writer.PutMongoID(target.Id);
        writer.Put(target.Value);
        writer.Put(target.SourceId);
        writer.Put(target.Type);
    }

    /// <summary>
    /// Deserializes and reconstructs a TaskConditionCounterDescriptor object from the reader stream.
    /// </summary>
    public static TaskConditionCounterDescriptor GetEFTTaskConditionCounterDescriptor(this NetDataReader reader)
    {
        return new TaskConditionCounterDescriptor
        {
            Id = reader.GetMongoID(),
            Value = reader.GetInt(),
            SourceId = reader.GetString(),
            Type = reader.GetString()
        };
    }

    /// <summary>
    /// Serializes an environmental item discard/throw physical physics operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTThrowOperationDescriptor(this NetDataWriter writer, ThrowOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.ItemId);
        writer.Put(target.DownDirection);
        if (target.DestroyedItems != null)
        {
            writer.Put(true);
            writer.Put(target.DestroyedItems.Count);
            for (var i = 0; i < target.DestroyedItems.Count; i++)
            {
                writer.PutEFTDestroyedItem(target.DestroyedItems[i]);
            }
            return;
        }
        writer.Put(false);
    }

    /// <summary>
    /// Deserializes and reconstructs a ThrowOperationDescriptor object from the reader stream.
    /// </summary>
    public static ThrowOperationDescriptor GetEFTThrowOperationDescriptor(this NetDataReader reader)
    {
        var throwDescriptorClass = new ThrowOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            ItemId = reader.GetString(),
            DownDirection = reader.GetBool()
        };
        if (reader.GetBool())
        {
            var num = reader.GetInt();
            throwDescriptorClass.DestroyedItems = new List<DestroyedItem>(num);
            for (var i = 0; i < num; i++)
            {
                throwDescriptorClass.DestroyedItems.Add(reader.GetEFTDestroyedItem());
            }
        }
        return throwDescriptorClass;
    }

    /// <summary>
    /// Serializes the togglable component descriptor state into the writer stream.
    /// </summary>
    public static void PutEFTTogglableComponentDescriptor(this NetDataWriter writer, TogglableComponentDescriptor target)
    {
        writer.Put(target.IsOn);
    }

    /// <summary>
    /// Deserializes and reconstructs a TogglableComponentDescriptor object from the reader stream.
    /// </summary>
    public static TogglableComponentDescriptor GetEFTTogglableComponentDescriptor(this NetDataReader reader)
    {
        return new TogglableComponentDescriptor
        {
            IsOn = reader.GetBool()
        };
    }

    /// <summary>
    /// Serializes an inventory item toggle action operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTToggleOperationDescriptor(this NetDataWriter writer, ToggleOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.ItemId);
        writer.Put(target.Value);
    }

    /// <summary>
    /// Deserializes and reconstructs a ToggleOperationDescriptor object from the reader stream.
    /// </summary>
    public static ToggleOperationDescriptor GetEFTToggleOperationDescriptor(this NetDataReader reader)
    {
        return new ToggleOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            ItemId = reader.GetString(),
            Value = reader.GetBool()
        };
    }

    /// <summary>
    /// Serializes the loyalty level, standing, and threshold metadata for a trader instance into the writer stream.
    /// </summary>
    public static void PutEFTTraderInfoDescriptor(this NetDataWriter writer, TraderInfoDescriptor target)
    {
        writer.Put(target.Unlocked);
        writer.Put(target.LoyaltyLevel);
        writer.Put(target.SalesSum);
        writer.Put(target.Standing);
        writer.Put(target.NextResupply);
        writer.Put(target.Disabled);
    }

    /// <summary>
    /// Deserializes and reconstructs a TraderInfoDescriptor object from the reader stream.
    /// </summary>
    public static TraderInfoDescriptor GetEFTTraderInfoDescriptor(this NetDataReader reader)
    {
        return new TraderInfoDescriptor
        {
            Unlocked = reader.GetBool(),
            LoyaltyLevel = reader.GetInt(),
            SalesSum = reader.GetLong(),
            Standing = reader.GetDouble(),
            NextResupply = reader.GetInt(),
            Disabled = reader.GetBool()
        };
    }

    /// <summary>
    /// Serializes commercial trader services cost listings and eligibility validation states into the writer stream.
    /// </summary>
    public static void PutEFTTraderServiceAvailabilityData(this NetDataWriter writer, TraderServiceAvailabilityData target)
    {
        writer.PutMongoID(target.TraderId);
        writer.PutEnum(target.ServiceType);
        writer.Put(target.CanAfford);
        writer.Put(target.WasPurchasedInThisRaid);
        writer.Put(target.ItemsToPay.Count);
        foreach (var keyValuePair in target.ItemsToPay)
        {
            writer.PutMongoID(keyValuePair.Key);
            writer.Put(keyValuePair.Value);
        }
        writer.Put(target.UniqueItems.Length);
        for (var i = 0; i < target.UniqueItems.Length; i++)
        {
            writer.PutMongoID(target.UniqueItems[i]);
        }
        writer.Put(target.SubServices.Count);
        foreach (var keyValuePair2 in target.SubServices)
        {
            writer.Put(keyValuePair2.Key);
            writer.Put(keyValuePair2.Value);
        }
    }

    /// <summary>
    /// Deserializes and reconstructs a TraderServiceAvailabilityData object from the reader stream.
    /// </summary>
    public static TraderServiceAvailabilityData GetEFTTraderServiceAvailabilityData(this NetDataReader reader)
    {
        var traderServicesClass = new TraderServiceAvailabilityData
        {
            TraderId = reader.GetMongoID(),
            ServiceType = reader.GetEnum<ETraderServiceType>(),
            CanAfford = reader.GetBool(),
            WasPurchasedInThisRaid = reader.GetBool()
        };
        var num = reader.GetInt();
        traderServicesClass.ItemsToPay = new Dictionary<MongoID, int>();
        for (var i = 0; i < num; i++)
        {
            traderServicesClass.ItemsToPay[reader.GetMongoID()] = reader.GetInt();
        }
        var num2 = reader.GetInt();
        traderServicesClass.UniqueItems = new MongoID[num2];
        for (var j = 0; j < num2; j++)
        {
            traderServicesClass.UniqueItems[j] = reader.GetMongoID();
        }
        var num3 = reader.GetInt();
        traderServicesClass.SubServices = new Dictionary<string, int>();
        for (var k = 0; k < num3; k++)
        {
            traderServicesClass.SubServices[reader.GetString()] = reader.GetInt();
        }
        return traderServicesClass;
    }

    /// <summary>
    /// Serializes an inventory item numeric element transfer operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTTransferOperationDescriptor(this NetDataWriter writer, TransferOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.ItemId);
        writer.Put(target.Item1Id);
        writer.Put(target.Count);
    }

    /// <summary>
    /// Deserializes and reconstructs a TransferOperationDescriptor object from the reader stream.
    /// </summary>
    public static TransferOperationDescriptor GetEFTTransferOperationDescriptor(this NetDataReader reader)
    {
        return new TransferOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            ItemId = reader.GetString(),
            Item1Id = reader.GetString(),
            Count = reader.GetInt()
        };
    }

    /// <summary>
    /// Serializes a fast-access hotkey structural item unbind operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTUnbindItemOperationDescriptor(this NetDataWriter writer, UnbindItemOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.ItemId);
        writer.PutEnum(target.Index);
    }

    /// <summary>
    /// Deserializes and reconstructs a UnbindItemOperationDescriptor object from the reader stream.
    /// </summary>
    public static UnbindItemOperationDescriptor GetEFTUnbindItemOperationDescriptor(this NetDataReader reader)
    {
        return new UnbindItemOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            ItemId = reader.GetString(),
            Index = reader.GetEnum<EBoundItem>()
        };
    }

    /// <summary>
    /// Serializes a weapon magazine emptying or unload operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTUnloadMagOperationDescriptor(this NetDataWriter writer, UnloadMagOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutPolymorph(target.InternalOperationDescriptor);
    }

    /// <summary>
    /// Deserializes and reconstructs a UnloadMagOperationDescriptor object from the reader stream.
    /// </summary>
    public static UnloadMagOperationDescriptor GetEFTUnloadMagOperationDescriptor(this NetDataReader reader)
    {
        return new UnloadMagOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            InternalOperationDescriptor = reader.GetPolymorph<InventoryOperationDescriptor>()
        };
    }

    /// <summary>
    /// Serializes kill tracking victim profile context attributes and range metrics logs into the writer stream.
    /// </summary>
    public static void PutEFTVictimStats(this NetDataWriter writer, VictimStats target)
    {
        writer.Put(target.AccountId);
        writer.Put(target.ProfileId);
        writer.Put(target.Name);
        writer.PutEnum(target.Side);
        writer.PutTimeSpan(target.Time);
        writer.Put(target.Level);
        writer.Put(target.PrestigeLevel);
        writer.PutEnum(target.BodyPart);
        writer.Put(target.Weapon);
        writer.Put(target.Distance);
        writer.PutEnum(target.Role);
        writer.Put(target.Location);
    }

    /// <summary>
    /// Deserializes and reconstructs a VictimStats object from the reader stream.
    /// </summary>
    public static VictimStats GetEFTVictimStats(this NetDataReader reader)
    {
        return new VictimStats
        {
            AccountId = reader.GetString(),
            ProfileId = reader.GetString(),
            Name = reader.GetString(),
            Side = reader.GetEnum<EPlayerSide>(),
            Time = reader.GetTimeSpan(),
            Level = reader.GetInt(),
            PrestigeLevel = reader.GetInt(),
            BodyPart = reader.GetEnum<EBodyPart>(),
            Weapon = reader.GetString(),
            Distance = reader.GetFloat(),
            Role = reader.GetEnum<WildSpawnType>(),
            Location = reader.GetString()
        };
    }

    /// <summary>
    /// Serializes a internal firearm bolt/rechamber interaction action operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTWeaponRechamberOperationDescriptor(this NetDataWriter writer, WeaponRechamberOperationDescriptor target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.WeaponId);
    }

    /// <summary>
    /// Deserializes and reconstructs a WeaponRechamberOperationDescriptor object from the reader stream.
    /// </summary>
    public static WeaponRechamberOperationDescriptor GetEFTWeaponRechamberOperationDescriptor(this NetDataReader reader)
    {
        return new WeaponRechamberOperationDescriptor
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            WeaponId = reader.GetString()
        };
    }

    /// <summary>
    /// Serializes item identity string associations for insurance tracking records into the writer stream.
    /// </summary>
    public static void PutJsonTypeInsuredProfileItems(this NetDataWriter writer, InsuredProfileItems target)
    {
        writer.Put(target.TraderId);
        writer.Put(target.ItemId);
    }

    /// <summary>
    /// Deserializes and reconstructs an InsuredProfileItems object from the reader stream.
    /// </summary>
    public static InsuredProfileItems GetJsonTypeInsuredProfileItems(this NetDataReader reader)
    {
        return new InsuredProfileItems
        {
            TraderId = reader.GetString(),
            ItemId = reader.GetString()
        };
    }

    /// <summary>
    /// Serializes visual identity parameters and comprehensive health state values for player profile models into the writer stream.
    /// </summary>
    public static void PutJsonTypePlayerInfo(this NetDataWriter writer, PlayerInfo target)
    {
        writer.Put(target.Nickname);
        writer.PutEnum(target.Side);
        writer.Put(target.Level);
        writer.Put(target.PrestigeLevel);
        writer.PutEnum(target.MemberCategory);
        writer.PutEnum(target.SelectedMemberCategory);
        writer.Put(target.SavageLockTime);
        writer.Put(target.SavageNickname);
        writer.Put(target.GameVersion);
        writer.Put(target.HasCoopExtension);
        writer.PutEFTProfileHealthInfo(target.Health);
    }

    /// <summary>
    /// Deserializes and reconstructs a PlayerInfo object from the reader stream.
    /// </summary>
    public static PlayerInfo GetJsonTypePlayerInfo(this NetDataReader reader)
    {
        return new PlayerInfo
        {
            Nickname = reader.GetString(),
            Side = reader.GetEnum<EPlayerSide>(),
            Level = reader.GetInt(),
            PrestigeLevel = reader.GetInt(),
            MemberCategory = reader.GetEnum<EMemberCategory>(),
            SelectedMemberCategory = reader.GetEnum<EMemberCategory>(),
            SavageLockTime = reader.GetDouble(),
            SavageNickname = reader.GetString(),
            GameVersion = reader.GetString(),
            HasCoopExtension = reader.GetBool(),
            Health = reader.GetEFTProfileHealthInfo()
        };
    }

    /// <summary>
    /// Serializes a spawn node configuration position and generation probability weights factor into the writer stream.
    /// </summary>
    public static void PutWeightedLootPointSpawnPosition(this NetDataWriter writer, WeightedLootPointSpawnPosition target)
    {
        writer.Put(target.Name);
        writer.Put(target.Weight);
        writer.PutClassVector3(target.Position);
        writer.PutClassVector3(target.Rotation);
    }

    /// <summary>
    /// Deserializes and reconstructs a WeightedLootPointSpawnPosition object from the reader stream.
    /// </summary>
    public static WeightedLootPointSpawnPosition GetWeightedLootPointSpawnPosition(this NetDataReader reader)
    {
        return new WeightedLootPointSpawnPosition
        {
            Name = reader.GetString(),
            Weight = reader.GetFloat(),
            Position = reader.GetClassVector3(),
            Rotation = reader.GetClassVector3()
        };
    }

    /// <summary>
    /// Serializes a four-component mathematical quaternion rotation structure into the writer stream.
    /// </summary>
    public static void PutClassQuaternion(this NetDataWriter writer, ClassQuaternion target)
    {
        writer.Put(target.x);
        writer.Put(target.y);
        writer.Put(target.z);
        writer.Put(target.w);
    }

    /// <summary>
    /// Deserializes and reconstructs a ClassQuaternion object from the reader stream.
    /// </summary>
    public static ClassQuaternion GetClassQuaternion(this NetDataReader reader)
    {
        return new ClassQuaternion
        {
            x = reader.GetFloat(),
            y = reader.GetFloat(),
            z = reader.GetFloat(),
            w = reader.GetFloat()
        };
    }

    /// <summary>
    /// Serializes synchronized physical object coordinates and raw rotational state bounds into the writer stream.
    /// </summary>
    public static void PutClassTransformSync(this NetDataWriter writer, ClassTransformSync target)
    {
        writer.PutClassVector3(target.Position);
        writer.PutClassQuaternion(target.Rotation);
    }

    /// <summary>
    /// Deserializes and reconstructs a ClassTransformSync object from the reader stream.
    /// </summary>
    public static ClassTransformSync GetClassTransformSync(this NetDataReader reader)
    {
        return new ClassTransformSync
        {
            Position = reader.GetClassVector3(),
            Rotation = reader.GetClassQuaternion()
        };
    }

    /// <summary>
    /// Serializes a standard dimensional three-element vector coordinate position mapping into the writer stream.
    /// </summary>
    public static void PutClassVector3(this NetDataWriter writer, ClassVector3 target)
    {
        writer.Put(target.x);
        writer.Put(target.y);
        writer.Put(target.z);
    }

    /// <summary>
    /// Deserializes and reconstructs a ClassVector3 object from the reader stream.
    /// </summary>
    public static ClassVector3 GetClassVector3(this NetDataReader reader)
    {
        return new ClassVector3
        {
            x = reader.GetFloat(),
            y = reader.GetFloat(),
            z = reader.GetFloat()
        };
    }
}
