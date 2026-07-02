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
    private static readonly List<Type> _indexToType = GClass3695.List_0;
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

        RegisterSerializer<GClass788>((w, t) => w.PutAggressorStats(t));
        RegisterSerializer<ClassQuaternion>((w, t) => w.PutClassQuaternion(t));
        RegisterSerializer<ClassTransformSync>((w, t) => w.PutClassTransformSync(t));
        RegisterSerializer<ClassVector3>((w, t) => w.PutClassVector3(t));
        RegisterSerializer<AddNoteDescriptorClass>((w, t) => w.PutEFTAddNoteOperationDescriptor(t));
        RegisterSerializer<ApplyKeyDescriptorClass>((w, t) => w.PutEFTApplyKeyOperationDescriptor(t));
        RegisterSerializer<GClass1959>((w, t) => w.PutEFTBindItemOperationDescriptor(t));
        RegisterSerializer<GClass2215>((w, t) => w.PutEFTBodyPartDamageHistoryDescriptor(t));
        RegisterSerializer<ProfileBonusesClass>((w, t) => w.PutEFTBonusDescriptor(t));
        RegisterSerializer<CheckMagazineDescriptorClass>((w, t) => w.PutEFTCheckMagazineOperationDescriptor(t));
        RegisterSerializer<GClass1949>((w, t) => w.PutEFTContainerDescriptor(t));
        RegisterSerializer<GClass2219>((w, t) => w.PutEFTCounterCollectionDescriptor(t));
        RegisterSerializer<GClass2218>((w, t) => w.PutEFTCounterCollectionItemDescriptor(t));
        RegisterSerializer<CreateMapMarkerDescriptorClass>((w, t) => w.PutEFTCreateMapMarkerOperationDescriptor(t));
        RegisterSerializer<GClass1944>((w, t) => w.PutEFTCultistAmuletComponentDescriptor(t));
        RegisterSerializer<GClass2216>((w, t) => w.PutEFTDamageHistoryDescriptor(t));
        RegisterSerializer<GClass2217>((w, t) => w.PutEFTDamageStatsDescriptor(t));
        RegisterSerializer<GClass2199>((w, t) => w.PutEFTDeathCause(t));
        RegisterSerializer<GClass1962>((w, t) => w.PutEFTDeleteMapMarkerOperationDescriptor(t));
        RegisterSerializer<GClass1963>((w, t) => w.PutEFTDeleteNoteOperationDescriptor(t));
        RegisterSerializer<GClass1955>((w, t) => w.PutEFTDestroyedItem(t));
        RegisterSerializer<GClass1938>((w, t) => w.PutEFTDogTagComponentDescriptor(t));
        RegisterSerializer<GClass2185>((w, t) => w.PutEFTDroppedItem(t));
        RegisterSerializer<GClass1964>((w, t) => w.PutEFTEditMapMarkerOperationDescriptor(t));
        RegisterSerializer<EditNoteDescriptorClass>((w, t) => w.PutEFTEditNoteOperationDescriptor(t));
        RegisterSerializer<GClass1966>((w, t) => w.PutEFTExamineMalfTypeOperationDescriptor(t));
        RegisterSerializer<GClass1967>((w, t) => w.PutEFTExamineMalfunctionOperationDescriptor(t));
        RegisterSerializer<GClass1968>((w, t) => w.PutEFTExamineOperationDescriptor(t));
        RegisterSerializer<GClass1935>((w, t) => w.PutEFTFaceShieldComponentDescriptor(t));
        RegisterSerializer<GClass1969>((w, t) => w.PutEFTFaceshieldMarkOperationDescriptor(t));
        RegisterSerializer<GClass1937>((w, t) => w.PutEFTFireModeComponentDescriptor(t));
        RegisterSerializer<GClass1936>((w, t) => w.PutEFTFoldableComponentDescriptor(t));
        RegisterSerializer<GClass1970>((w, t) => w.PutEFTFoldOperationDescriptor(t));
        RegisterSerializer<GClass1925>((w, t) => w.PutEFTFoodDrinkComponentDescriptor(t));
        RegisterSerializer<GClass2186>((w, t) => w.PutEFTFoundInRaidItem(t));
        RegisterSerializer<GClass1919>((w, t) => w.PutEFTGridDescriptor(t));
        RegisterSerializer<GClass1954>((w, t) => w.PutEFTGridItemAddressDescriptor(t));
        RegisterSerializer<EFTInventoryClass>((w, t) => w.PutEFTInventoryDescriptor(t));
        RegisterSerializer<GClass2211>((w, t) => w.PutEFTInventoryEquipmentDescriptor(t));
        RegisterSerializer<MapMarker>((w, t) => w.PutEFTInventoryLogicMapMarker(t));
        RegisterSerializer<GClass1994>((w, t) => w.PutEFTInventoryLogicOperationsAddToWishlistOperationDescriptor(t));
        RegisterSerializer<GClass1997>((w, t) => w.PutEFTInventoryLogicOperationsChangeItemsOperationDescriptor(t));
        RegisterSerializer<GClass1998>((w, t) => w.PutEFTInventoryLogicOperationsChangeWishlistItemCategoryOperationDescriptor(t));
        RegisterSerializer<GClass1999>((w, t) => w.PutEFTInventoryLogicOperationsPurchaseTraderServiceOperationDescriptor(t));
        RegisterSerializer<GClass2000>((w, t) => w.PutEFTInventoryLogicOperationsRemoveFromWishlistOperationDescriptor(t));
        RegisterSerializer<GClass2002>((w, t) => w.PutEFTInventoryLogicOperationsSearchContentOperationDescriptor(t));
        RegisterSerializer<GClass2001>((w, t) => w.PutEFTInventoryLogicOperationsSearchSuboperationDescriptor(t));
        RegisterSerializer<GClass1995>((w, t) => w.PutEFTInventoryLogicOperationsSplitToNowhereDescriptor(t));
        RegisterSerializer<GClass1996>((w, t) => w.PutEFTInventoryLogicOperationsTransferFromNowhereDescriptor(t));
        RegisterSerializer<InventoryDescriptorClass>((w, t) => w.PutEFTItemDescriptor(t));
        RegisterSerializer<GClass1924>((w, t) => w.PutEFTItemInfoDescriptor(t));
        RegisterSerializer<GClass1918>((w, t) => w.PutEFTItemInGridDescriptor(t));
        RegisterSerializer<GClass1946>((w, t) => w.PutEFTJsonCorpseDescriptor(t));
        RegisterSerializer<GClass1945>((w, t) => w.PutEFTJsonLootItemDescriptor(t));
        RegisterSerializer<GClass1940>((w, t) => w.PutEFTKeyComponentDescriptor(t));
        RegisterSerializer<GClass1928>((w, t) => w.PutEFTLightComponentDescriptor(t));
        RegisterSerializer<GClass1972>((w, t) => w.PutEFTLoadMagOperationDescriptor(t));
        RegisterSerializer<GClass1929>((w, t) => w.PutEFTLockableComponentDescriptor(t));
        RegisterSerializer<GClass1947>((w, t) => w.PutEFTLootDataDescriptor(t));
        RegisterSerializer<GClass1917>((w, t) => w.PutEFTMalfunctionDescriptor(t));
        RegisterSerializer<GClass1930>((w, t) => w.PutEFTMapComponentDescriptor(t));
        RegisterSerializer<GClass1931>((w, t) => w.PutEFTMedKitComponentDescriptor(t));
        RegisterSerializer<MergeDescriptorClass>((w, t) => w.PutEFTMergeOperationDescriptor(t));
        RegisterSerializer<MoveDescriptorClass>((w, t) => w.PutEFTMoveOperationDescriptor(t));
        RegisterSerializer<GClass1921>((w, t) => w.PutEFTNestedItemDescriptor(t));
        RegisterSerializer<GClass3107>((w, t) => w.PutEFTNotesNote(t));
        RegisterSerializer<NotesManagerClass.GClass3109>((w, t) => w.PutEFTNotesNotesManagerNotesDescriptor(t));
        RegisterSerializer<GClass1982>((w, t) => w.PutEFTOperateStationaryWeaponOperationDescriptor(t));
        RegisterSerializer<GClass1951>((w, t) => w.PutEFTOwnerItselfDescriptor(t));
        RegisterSerializer<GClass1976>((w, t) => w.PutEFTPlantTripwireOperationDescriptor(t));
        RegisterSerializer<GClass2214>((w, t) => w.PutEFTPlayerVisualRepresentationDescriptor(t));
        RegisterSerializer<GClass1926>((w, t) => w.PutEFTPoisonComponentDescriptor(t));
        RegisterSerializer<GClass2660>((w, t) => w.PutEFTPrestigePrestigeStatusData(t));
        RegisterSerializer<Profile.ProfileHealthClass>((w, t) => w.PutEFTProfileHealthInfo(t));
        RegisterSerializer<Profile.ProfileHealthClass.ProfileBodyPartHealthClass>((w, t) => w.PutEFTProfileHealthInfoBodyPartInfo(t));
        RegisterSerializer<Profile.ProfileHealthClass.GClass2206>((w, t) => w.PutEFTProfileHealthInfoEffectInfo(t));
        RegisterSerializer<Profile.ProfileHealthClass.ValueInfo>((w, t) => w.PutEFTProfileHealthInfoValueInfo(t));
        RegisterSerializer<Profile.GClass2209>((w, t) => w.PutEFTProfileMoneyTransferLimitData(t));
        RegisterSerializer<Profile.GClass2208>((w, t) => w.PutEFTProfileUnlockedInfo(t));
        RegisterSerializer<GClass2222>((w, t) => w.PutEFTProfileBanDescriptor(t));
        RegisterSerializer<CompleteProfileDescriptorClass>((w, t) => w.PutEFTProfileDescriptor(t));
        RegisterSerializer<ProfileInfoClass>((w, t) => w.PutEFTProfileInfoDescriptor(t));
        RegisterSerializer<ProfileInfoSettingsClass>((w, t) => w.PutEFTProfileSettings(t));
        RegisterSerializer<ProfileEftStatsClass>((w, t) => w.PutEFTProfileStatsDescriptor(t));
        RegisterSerializer<ProfileStatsClass>((w, t) => w.PutEFTProfileStatsSeparatorDescriptor(t));
        RegisterSerializer<GClass1991>((w, t) => w.PutEFTQuestAcceptDescriptor(t));
        RegisterSerializer<GClass1992>((w, t) => w.PutEFTQuestFinishDescriptor(t));
        RegisterSerializer<GClass1993>((w, t) => w.PutEFTQuestHandoverDescriptor(t));
        RegisterSerializer<QuestDataClass>((w, t) => w.PutEFTQuestsQuestStatusData(t));
        RegisterSerializer<GClass1943>((w, t) => w.PutEFTRecodableComponentDescriptor(t));
        RegisterSerializer<GClass1977>((w, t) => w.PutEFTRemoveOperationDescriptor(t));
        RegisterSerializer<GClass1932>((w, t) => w.PutEFTRepairableComponentDescriptor(t));
        RegisterSerializer<GClass1942>((w, t) => w.PutEFTRepairEnhancementComponentDescriptor(t));
        RegisterSerializer<GClass1941>((w, t) => w.PutEFTRepairKitComponentDescriptor(t));
        RegisterSerializer<GClass1927>((w, t) => w.PutEFTResourceItemComponentDescriptor(t));
        RegisterSerializer<SceneResourceKey>((w, t) => w.PutEFTSceneResourceKey(t));
        RegisterSerializer<ResourceKey>((w, t) => w.PutEFTResourceKey(t));
        RegisterSerializer<GClass1978>((w, t) => w.PutEFTSetDialogProgressOperationDescriptor(t));
        RegisterSerializer<GClass1979>((w, t) => w.PutEFTSetupItemOperationDescriptor(t));
        RegisterSerializer<GClass1980>((w, t) => w.PutEFTSetVariableOperationDescriptor(t));
        RegisterSerializer<GClass1916>((w, t) => w.PutEFTShellTemplateDescriptor(t));
        RegisterSerializer<GClass1933>((w, t) => w.PutEFTSightComponentDescriptor(t));
        RegisterSerializer<SkillsDescriptorClass>((w, t) => w.PutEFTSkillsDescriptor(t));
        RegisterSerializer<SkillsDescriptorClass.GClass2226>((w, t) => w.PutEFTSkillsDescriptorMasteringInfoDescriptor(t));
        RegisterSerializer<SkillsDescriptorClass.GClass2225>((w, t) => w.PutEFTSkillsDescriptorSkillInfoDescriptor(t));
        RegisterSerializer<GClass1915>((w, t) => w.PutEFTSlotDescriptor(t));
        RegisterSerializer<GClass1952>((w, t) => w.PutEFTSlotItemAddressDescriptor(t));
        RegisterSerializer<SplitDescriptorClass>((w, t) => w.PutEFTSplitOperationDescriptor(t));
        RegisterSerializer<GClass1920>((w, t) => w.PutEFTStackSlotDescriptor(t));
        RegisterSerializer<GClass1953>((w, t) => w.PutEFTStackSlotItemAddressDescriptor(t));
        RegisterSerializer<GClass1983>((w, t) => w.PutEFTSwapOperationDescriptor(t));
        RegisterSerializer<GClass1939>((w, t) => w.PutEFTTagComponentDescriptor(t));
        RegisterSerializer<TagDescriptorClass>((w, t) => w.PutEFTTagOperationDescriptor(t));
        RegisterSerializer<GClass2227>((w, t) => w.PutEFTTaskConditionCounterDescriptor(t));
        RegisterSerializer<ThrowDescriptorClass>((w, t) => w.PutEFTThrowOperationDescriptor(t));
        RegisterSerializer<GClass1934>((w, t) => w.PutEFTTogglableComponentDescriptor(t));
        RegisterSerializer<GClass1986>((w, t) => w.PutEFTToggleOperationDescriptor(t));
        RegisterSerializer<TraderInfoClass>((w, t) => w.PutEFTTraderInfoDescriptor(t));
        RegisterSerializer<TraderServicesClass>((w, t) => w.PutEFTTraderServiceAvailabilityData(t));
        RegisterSerializer<GClass1987>((w, t) => w.PutEFTTransferOperationDescriptor(t));
        RegisterSerializer<GClass1988>((w, t) => w.PutEFTUnbindItemOperationDescriptor(t));
        RegisterSerializer<GClass1973>((w, t) => w.PutEFTUnloadMagOperationDescriptor(t));
        RegisterSerializer<GClass2201>((w, t) => w.PutEFTVictimStats(t));
        RegisterSerializer<GClass1989>((w, t) => w.PutEFTWeaponRechamberOperationDescriptor(t));
        RegisterSerializer<InsuredItemClass>((w, t) => w.PutJsonTypeInsuredProfileItems(t));
        RegisterSerializer<GClass1410>((w, t) => w.PutJsonTypePlayerInfo(t));
        RegisterSerializer<WeightedLootPointSpawnPosition>((w, t) => w.PutWeightedLootPointSpawnPosition(t));

        RegisterDeserializer<GClass788>(r => r.GetAggressorStats());
        RegisterDeserializer<ClassQuaternion>(r => r.GetClassQuaternion());
        RegisterDeserializer<ClassTransformSync>(r => r.GetClassTransformSync());
        RegisterDeserializer<ClassVector3>(r => r.GetClassVector3());
        RegisterDeserializer<AddNoteDescriptorClass>(r => r.GetEFTAddNoteOperationDescriptor());
        RegisterDeserializer<ApplyKeyDescriptorClass>(r => r.GetEFTApplyKeyOperationDescriptor());
        RegisterDeserializer<GClass1959>(r => r.GetEFTBindItemOperationDescriptor());
        RegisterDeserializer<GClass2215>(r => r.GetEFTBodyPartDamageHistoryDescriptor());
        RegisterDeserializer<ProfileBonusesClass>(r => r.GetEFTBonusDescriptor());
        RegisterDeserializer<CheckMagazineDescriptorClass>(r => r.GetEFTCheckMagazineOperationDescriptor());
        RegisterDeserializer<GClass1949>(r => r.GetEFTContainerDescriptor());
        RegisterDeserializer<GClass2219>(r => r.GetEFTCounterCollectionDescriptor());
        RegisterDeserializer<GClass2218>(r => r.GetEFTCounterCollectionItemDescriptor());
        RegisterDeserializer<CreateMapMarkerDescriptorClass>(r => r.GetEFTCreateMapMarkerOperationDescriptor());
        RegisterDeserializer<GClass1944>(r => r.GetEFTCultistAmuletComponentDescriptor());
        RegisterDeserializer<GClass2216>(r => r.GetEFTDamageHistoryDescriptor());
        RegisterDeserializer<GClass2217>(r => r.GetEFTDamageStatsDescriptor());
        RegisterDeserializer<GClass2199>(r => r.GetEFTDeathCause());
        RegisterDeserializer<GClass1962>(r => r.GetEFTDeleteMapMarkerOperationDescriptor());
        RegisterDeserializer<GClass1963>(r => r.GetEFTDeleteNoteOperationDescriptor());
        RegisterDeserializer<GClass1955>(r => r.GetEFTDestroyedItem());
        RegisterDeserializer<GClass1938>(r => r.GetEFTDogTagComponentDescriptor());
        RegisterDeserializer<GClass2185>(r => r.GetEFTDroppedItem());
        RegisterDeserializer<GClass1964>(r => r.GetEFTEditMapMarkerOperationDescriptor());
        RegisterDeserializer<EditNoteDescriptorClass>(r => r.GetEFTEditNoteOperationDescriptor());
        RegisterDeserializer<GClass1966>(r => r.GetEFTExamineMalfTypeOperationDescriptor());
        RegisterDeserializer<GClass1967>(r => r.GetEFTExamineMalfunctionOperationDescriptor());
        RegisterDeserializer<GClass1968>(r => r.GetEFTExamineOperationDescriptor());
        RegisterDeserializer<GClass1935>(r => r.GetEFTFaceShieldComponentDescriptor());
        RegisterDeserializer<GClass1969>(r => r.GetEFTFaceshieldMarkOperationDescriptor());
        RegisterDeserializer<GClass1937>(r => r.GetEFTFireModeComponentDescriptor());
        RegisterDeserializer<GClass1936>(r => r.GetEFTFoldableComponentDescriptor());
        RegisterDeserializer<GClass1970>(r => r.GetEFTFoldOperationDescriptor());
        RegisterDeserializer<GClass1925>(r => r.GetEFTFoodDrinkComponentDescriptor());
        RegisterDeserializer<GClass2186>(r => r.GetEFTFoundInRaidItem());
        RegisterDeserializer<GClass1919>(r => r.GetEFTGridDescriptor());
        RegisterDeserializer<GClass1954>(r => r.GetEFTGridItemAddressDescriptor());
        RegisterDeserializer<EFTInventoryClass>(r => r.GetEFTInventoryDescriptor());
        RegisterDeserializer<GClass2211>(r => r.GetEFTInventoryEquipmentDescriptor());
        RegisterDeserializer<MapMarker>(r => r.GetEFTInventoryLogicMapMarker());
        RegisterDeserializer<GClass1994>(r => r.GetEFTInventoryLogicOperationsAddToWishlistOperationDescriptor());
        RegisterDeserializer<GClass1997>(r => r.GetEFTInventoryLogicOperationsChangeItemsOperationDescriptor());
        RegisterDeserializer<GClass1998>(r => r.GetEFTInventoryLogicOperationsChangeWishlistItemCategoryOperationDescriptor());
        RegisterDeserializer<GClass1999>(r => r.GetEFTInventoryLogicOperationsPurchaseTraderServiceOperationDescriptor());
        RegisterDeserializer<GClass2000>(r => r.GetEFTInventoryLogicOperationsRemoveFromWishlistOperationDescriptor());
        RegisterDeserializer<GClass2002>(r => r.GetEFTInventoryLogicOperationsSearchContentOperationDescriptor());
        RegisterDeserializer<GClass2001>(r => r.GetEFTInventoryLogicOperationsSearchSuboperationDescriptor());
        RegisterDeserializer<GClass1995>(r => r.GetEFTInventoryLogicOperationsSplitToNowhereDescriptor());
        RegisterDeserializer<GClass1996>(r => r.GetEFTInventoryLogicOperationsTransferFromNowhereDescriptor());
        RegisterDeserializer<InventoryDescriptorClass>(r => r.GetEFTItemDescriptor());
        RegisterDeserializer<GClass1924>(r => r.GetEFTItemInfoDescriptor());
        RegisterDeserializer<GClass1918>(r => r.GetEFTItemInGridDescriptor());
        RegisterDeserializer<GClass1946>(r => r.GetEFTJsonCorpseDescriptor());
        RegisterDeserializer<GClass1945>(r => r.GetEFTJsonLootItemDescriptor());
        RegisterDeserializer<GClass1940>(r => r.GetEFTKeyComponentDescriptor());
        RegisterDeserializer<GClass1928>(r => r.GetEFTLightComponentDescriptor());
        RegisterDeserializer<GClass1972>(r => r.GetEFTLoadMagOperationDescriptor());
        RegisterDeserializer<GClass1929>(r => r.GetEFTLockableComponentDescriptor());
        RegisterDeserializer<GClass1947>(r => r.GetEFTLootDataDescriptor());
        RegisterDeserializer<GClass1917>(r => r.GetEFTMalfunctionDescriptor());
        RegisterDeserializer<GClass1930>(r => r.GetEFTMapComponentDescriptor());
        RegisterDeserializer<GClass1931>(r => r.GetEFTMedKitComponentDescriptor());
        RegisterDeserializer<MergeDescriptorClass>(r => r.GetEFTMergeOperationDescriptor());
        RegisterDeserializer<MoveDescriptorClass>(r => r.GetEFTMoveOperationDescriptor());
        RegisterDeserializer<GClass1921>(r => r.GetEFTNestedItemDescriptor());
        RegisterDeserializer<GClass3107>(r => r.GetEFTNotesNote());
        RegisterDeserializer<NotesManagerClass.GClass3109>(r => r.GetEFTNotesNotesManagerNotesDescriptor());
        RegisterDeserializer<GClass1982>(r => r.GetEFTOperateStationaryWeaponOperationDescriptor());
        RegisterDeserializer<GClass1951>(r => r.GetEFTOwnerItselfDescriptor());
        RegisterDeserializer<GClass1976>(r => r.GetEFTPlantTripwireOperationDescriptor());
        RegisterDeserializer<GClass2214>(r => r.GetEFTPlayerVisualRepresentationDescriptor());
        RegisterDeserializer<GClass1926>(r => r.GetEFTPoisonComponentDescriptor());
        RegisterDeserializer<GClass2660>(r => r.GetEFTPrestigePrestigeStatusData());
        RegisterDeserializer<Profile.ProfileHealthClass>(r => r.GetEFTProfileHealthInfo());
        RegisterDeserializer<Profile.ProfileHealthClass.ProfileBodyPartHealthClass>(r => r.GetEFTProfileHealthInfoBodyPartInfo());
        RegisterDeserializer<Profile.ProfileHealthClass.GClass2206>(r => r.GetEFTProfileHealthInfoEffectInfo());
        RegisterDeserializer<Profile.ProfileHealthClass.ValueInfo>(r => r.GetEFTProfileHealthInfoValueInfo());
        RegisterDeserializer<Profile.GClass2209>(r => r.GetEFTProfileMoneyTransferLimitData());
        RegisterDeserializer<Profile.GClass2208>(r => r.GetEFTProfileUnlockedInfo());
        RegisterDeserializer<GClass2222>(r => r.GetEFTProfileBanDescriptor());
        RegisterDeserializer<CompleteProfileDescriptorClass>(r => r.GetEFTProfileDescriptor());
        RegisterDeserializer<ProfileInfoClass>(r => r.GetEFTProfileInfoDescriptor());
        RegisterDeserializer<ProfileInfoSettingsClass>(r => r.GetEFTProfileSettings());
        RegisterDeserializer<ProfileEftStatsClass>(r => r.GetEFTProfileStatsDescriptor());
        RegisterDeserializer<ProfileStatsClass>(r => r.GetEFTProfileStatsSeparatorDescriptor());
        RegisterDeserializer<GClass1991>(r => r.GetEFTQuestAcceptDescriptor());
        RegisterDeserializer<GClass1992>(r => r.GetEFTQuestFinishDescriptor());
        RegisterDeserializer<GClass1993>(r => r.GetEFTQuestHandoverDescriptor());
        RegisterDeserializer<QuestDataClass>(r => r.GetEFTQuestsQuestStatusData());
        RegisterDeserializer<GClass1943>(r => r.GetEFTRecodableComponentDescriptor());
        RegisterDeserializer<GClass1977>(r => r.GetEFTRemoveOperationDescriptor());
        RegisterDeserializer<GClass1932>(r => r.GetEFTRepairableComponentDescriptor());
        RegisterDeserializer<GClass1942>(r => r.GetEFTRepairEnhancementComponentDescriptor());
        RegisterDeserializer<GClass1941>(r => r.GetEFTRepairKitComponentDescriptor());
        RegisterDeserializer<GClass1927>(r => r.GetEFTResourceItemComponentDescriptor());
        RegisterDeserializer<SceneResourceKey>(r => r.GetEFTSceneResourceKey());
        RegisterDeserializer<ResourceKey>(r => r.GetEFTResourceKey());
        RegisterDeserializer<GClass1978>(r => r.GetEFTSetDialogProgressOperationDescriptor());
        RegisterDeserializer<GClass1979>(r => r.GetEFTSetupItemOperationDescriptor());
        RegisterDeserializer<GClass1980>(r => r.GetEFTSetVariableOperationDescriptor());
        RegisterDeserializer<GClass1916>(r => r.GetEFTShellTemplateDescriptor());
        RegisterDeserializer<GClass1933>(r => r.GetEFTSightComponentDescriptor());
        RegisterDeserializer<SkillsDescriptorClass>(r => r.GetEFTSkillsDescriptor());
        RegisterDeserializer<SkillsDescriptorClass.GClass2226>(r => r.GetEFTSkillsDescriptorMasteringInfoDescriptor());
        RegisterDeserializer<SkillsDescriptorClass.GClass2225>(r => r.GetEFTSkillsDescriptorSkillInfoDescriptor());
        RegisterDeserializer<GClass1915>(r => r.GetEFTSlotDescriptor());
        RegisterDeserializer<GClass1952>(r => r.GetEFTSlotItemAddressDescriptor());
        RegisterDeserializer<SplitDescriptorClass>(r => r.GetEFTSplitOperationDescriptor());
        RegisterDeserializer<GClass1920>(r => r.GetEFTStackSlotDescriptor());
        RegisterDeserializer<GClass1953>(r => r.GetEFTStackSlotItemAddressDescriptor());
        RegisterDeserializer<GClass1983>(r => r.GetEFTSwapOperationDescriptor());
        RegisterDeserializer<GClass1939>(r => r.GetEFTTagComponentDescriptor());
        RegisterDeserializer<TagDescriptorClass>(r => r.GetEFTTagOperationDescriptor());
        RegisterDeserializer<GClass2227>(r => r.GetEFTTaskConditionCounterDescriptor());
        RegisterDeserializer<ThrowDescriptorClass>(r => r.GetEFTThrowOperationDescriptor());
        RegisterDeserializer<GClass1934>(r => r.GetEFTTogglableComponentDescriptor());
        RegisterDeserializer<GClass1986>(r => r.GetEFTToggleOperationDescriptor());
        RegisterDeserializer<TraderInfoClass>(r => r.GetEFTTraderInfoDescriptor());
        RegisterDeserializer<TraderServicesClass>(r => r.GetEFTTraderServiceAvailabilityData());
        RegisterDeserializer<GClass1987>(r => r.GetEFTTransferOperationDescriptor());
        RegisterDeserializer<GClass1988>(r => r.GetEFTUnbindItemOperationDescriptor());
        RegisterDeserializer<GClass1973>(r => r.GetEFTUnloadMagOperationDescriptor());
        RegisterDeserializer<GClass2201>(r => r.GetEFTVictimStats());
        RegisterDeserializer<GClass1989>(r => r.GetEFTWeaponRechamberOperationDescriptor());
        RegisterDeserializer<InsuredItemClass>(r => r.GetJsonTypeInsuredProfileItems());
        RegisterDeserializer<GClass1410>(r => r.GetJsonTypePlayerInfo());
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
    /// Serializes the aggressor statistics from a GClass788 object into the writer stream.
    /// </summary>
    /// <param name="target">The GClass788 instance containing the statistics to write.</param>
    public static void PutAggressorStats(this NetDataWriter writer, GClass788 target)
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
    /// Deserializes and reconstructs a GClass788 object from the reader stream.
    /// </summary>
    /// <returns>A new instance of GClass788 populated with the stream data.</returns>
    public static GClass788 GetAggressorStats(this NetDataReader reader)
    {
        return new GClass788
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
    /// <param name="target">The AddNoteDescriptorClass instance containing the descriptor data to write.</param>
    public static void PutEFTAddNoteOperationDescriptor(this NetDataWriter writer, AddNoteDescriptorClass target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutEFTNotesNote(target.Note);
    }

    /// <summary>
    /// Deserializes and reconstructs an AddNoteDescriptorClass object from the reader stream.
    /// </summary>
    /// <returns>A new instance of AddNoteDescriptorClass populated with the stream data.</returns>
    public static AddNoteDescriptorClass GetEFTAddNoteOperationDescriptor(this NetDataReader reader)
    {
        return new AddNoteDescriptorClass
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            Note = reader.GetEFTNotesNote()
        };
    }

    /// <summary>
    /// Serializes a GClass3107 note object into the writer stream.
    /// </summary>
    /// <param name="target">The GClass3107 instance containing the note data to write.</param>
    public static void PutEFTNotesNote(this NetDataWriter writer, GClass3107 target)
    {
        writer.Put(target.Time);
        writer.Put(target.Text);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass3107 note object from the reader stream.
    /// </summary>
    /// <param name="reader">The reader instance.</param>
    /// <returns>A new instance of GClass3107 populated with the stream data.</returns>
    public static GClass3107 GetEFTNotesNote(this NetDataReader reader)
    {
        return new GClass3107
        {
            Time = reader.GetFloat(),
            Text = reader.GetString()
        };
    }

    /// <summary>
    /// Serializes the key application operation descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The ApplyKeyDescriptorClass instance containing the descriptor data to write.</param>
    public static void PutEFTApplyKeyOperationDescriptor(this NetDataWriter writer, ApplyKeyDescriptorClass target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.ItemId);
        writer.Put(target.TargetItemId);
    }

    /// <summary>
    /// Deserializes and reconstructs an ApplyKeyDescriptorClass object from the reader stream.
    /// </summary>
    /// <returns>A new instance of ApplyKeyDescriptorClass populated with the stream data.</returns>
    public static ApplyKeyDescriptorClass GetEFTApplyKeyOperationDescriptor(this NetDataReader reader)
    {
        return new ApplyKeyDescriptorClass
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
    /// <param name="target">The GClass1959 instance containing the descriptor data to write.</param>
    public static void PutEFTBindItemOperationDescriptor(this NetDataWriter writer, GClass1959 target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.ItemId);
        writer.PutEnum(target.Index);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1959 object from the reader stream.
    /// </summary>
    /// <returns>A new instance of GClass1959 populated with the stream data.</returns>
    public static GClass1959 GetEFTBindItemOperationDescriptor(this NetDataReader reader)
    {
        return new GClass1959
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
    /// <param name="target">The GClass2215 instance containing the damage history data to write.</param>
    public static void PutEFTBodyPartDamageHistoryDescriptor(this NetDataWriter writer, GClass2215 target)
    {
        writer.Put(target.DamageList.Count);
        for (var i = 0; i < target.DamageList.Count; i++)
        {
            writer.PutEFTDamageStatsDescriptor(target.DamageList[i]);
        }
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass2215 object from the reader stream.
    /// </summary>
    /// <returns>A new instance of GClass2215 populated with the stream data.</returns>
    public static GClass2215 GetEFTBodyPartDamageHistoryDescriptor(this NetDataReader reader)
    {
        var gclass = new GClass2215();
        var num = reader.GetInt();
        gclass.DamageList = new List<GClass2217>(num);
        for (var i = 0; i < num; i++)
        {
            gclass.DamageList.Add(reader.GetEFTDamageStatsDescriptor());
        }
        return gclass;
    }

    /// <summary>
    /// Serializes the damage statistics descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The GClass2217 instance containing the damage stats data to write.</param>
    public static void PutEFTDamageStatsDescriptor(this NetDataWriter writer, GClass2217 target)
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
    /// Deserializes and reconstructs a GClass2217 object from the reader stream.
    /// </summary>
    /// <returns>A new instance of GClass2217 populated with the stream data.</returns>
    public static GClass2217 GetEFTDamageStatsDescriptor(this NetDataReader reader)
    {
        var gclass = new GClass2217
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
    /// <param name="target">The ProfileBonusesClass instance containing the bonus data to write.</param>
    public static void PutEFTBonusDescriptor(this NetDataWriter writer, ProfileBonusesClass target)
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
    /// Deserializes and reconstructs a ProfileBonusesClass object from the reader stream.
    /// </summary>
    /// <returns>A new instance of ProfileBonusesClass populated with the stream data.</returns>
    public static ProfileBonusesClass GetEFTBonusDescriptor(this NetDataReader reader)
    {
        var profileBonusesClass = new ProfileBonusesClass
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
    /// <param name="target">The CheckMagazineDescriptorClass instance containing the descriptor data to write.</param>
    public static void PutEFTCheckMagazineOperationDescriptor(this NetDataWriter writer, CheckMagazineDescriptorClass target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutMongoID(target.ItemId);
        writer.Put(target.CheckStatus);
        writer.Put(target.SkillLevel);
    }

    /// <summary>
    /// Deserializes and reconstructs a CheckMagazineDescriptorClass object from the reader stream.
    /// </summary>
    /// <returns>A new instance of CheckMagazineDescriptorClass populated with the stream data.</returns>
    public static CheckMagazineDescriptorClass GetEFTCheckMagazineOperationDescriptor(this NetDataReader reader)
    {
        return new CheckMagazineDescriptorClass
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
    /// <param name="target">The GClass1949 instance containing the container data to write.</param>
    public static void PutEFTContainerDescriptor(this NetDataWriter writer, GClass1949 target)
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
    /// Deserializes and reconstructs a GClass1949 object from the reader stream.
    /// </summary>
    /// <returns>A new instance of GClass1949 populated with the stream data.</returns>
    public static GClass1949 GetEFTContainerDescriptor(this NetDataReader reader)
    {
        var gclass = new GClass1949();
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
    /// <param name="target">The GClass2219 instance containing the collection items to write.</param>
    public static void PutEFTCounterCollectionDescriptor(this NetDataWriter writer, GClass2219 target)
    {
        writer.Put(target.Items.Count);
        for (var i = 0; i < target.Items.Count; i++)
        {
            writer.PutEFTCounterCollectionItemDescriptor(target.Items[i]);
        }
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass2219 object from the reader stream.
    /// </summary>
    /// <returns>A new instance of GClass2219 populated with the stream data.</returns>
    public static GClass2219 GetEFTCounterCollectionDescriptor(this NetDataReader reader)
    {
        var gclass = new GClass2219();
        var num = reader.GetInt();
        gclass.Items = new List<GClass2218>(num);
        for (var i = 0; i < num; i++)
        {
            gclass.Items.Add(reader.GetEFTCounterCollectionItemDescriptor());
        }
        return gclass;
    }

    /// <summary>
    /// Serializes a counter collection item descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The GClass2218 instance containing the item data to write.</param>
    public static void PutEFTCounterCollectionItemDescriptor(this NetDataWriter writer, GClass2218 target)
    {
        writer.Put(target.Key.Count);
        for (var i = 0; i < target.Key.Count; i++)
        {
            writer.Put(target.Key[i]);
        }
        writer.Put(target.Value);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass2218 object from the reader stream.
    /// </summary>
    /// <returns>A new instance of GClass2218 populated with the stream data.</returns>
    public static GClass2218 GetEFTCounterCollectionItemDescriptor(this NetDataReader reader)
    {
        var gclass = new GClass2218();
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
    /// <param name="target">The CreateMapMarkerDescriptorClass instance containing the descriptor data to write.</param>
    public static void PutEFTCreateMapMarkerOperationDescriptor(this NetDataWriter writer, CreateMapMarkerDescriptorClass target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.MapItemId);
        writer.PutEFTInventoryLogicMapMarker(target.MapMarker);
    }

    /// <summary>
    /// Deserializes and reconstructs a CreateMapMarkerDescriptorClass object from the reader stream.
    /// </summary>
    /// <returns>A new instance of CreateMapMarkerDescriptorClass populated with the stream data.</returns>
    public static CreateMapMarkerDescriptorClass GetEFTCreateMapMarkerOperationDescriptor(this NetDataReader reader)
    {
        return new CreateMapMarkerDescriptorClass
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
    /// <param name="target">The GClass1944 instance containing the component data to write.</param>
    public static void PutEFTCultistAmuletComponentDescriptor(this NetDataWriter writer, GClass1944 target)
    {
        writer.Put(target.NumberOfUsages);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1944 object from the reader stream.
    /// </summary>
    /// <returns>A new instance of GClass1944 populated with the stream data.</returns>
    public static GClass1944 GetEFTCultistAmuletComponentDescriptor(this NetDataReader reader)
    {
        return new GClass1944
        {
            NumberOfUsages = reader.GetInt()
        };
    }

    /// <summary>
    /// Serializes the total damage history descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The GClass2216 instance containing the entire damage history data to write.</param>
    public static void PutEFTDamageHistoryDescriptor(this NetDataWriter writer, GClass2216 target)
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
    /// Deserializes and reconstructs a GClass2216 object from the reader stream.
    /// </summary>
    /// <returns>A new instance of GClass2216 populated with the stream data.</returns>
    public static GClass2216 GetEFTDamageHistoryDescriptor(this NetDataReader reader)
    {
        var gclass = new GClass2216
        {
            LethalDamagePart = reader.GetEnum<EBodyPart>()
        };
        if (reader.GetBool())
        {
            gclass.LethalDamage = reader.GetEFTDamageStatsDescriptor();
        }
        var num = reader.GetInt();
        gclass.BodyParts = new Dictionary<EBodyPart, GClass2215>();
        for (var i = 0; i < num; i++)
        {
            gclass.BodyParts[reader.GetEnum<EBodyPart>()] = reader.GetEFTBodyPartDamageHistoryDescriptor();
        }
        return gclass;
    }

    /// <summary>
    /// Serializes combat health depletion logs and causal weapon attributes into the writer stream.
    /// </summary>
    public static void PutEFTDeathCause(this NetDataWriter writer, GClass2199 target)
    {
        writer.PutEnum(target.DamageType);
        writer.PutEnum(target.Side);
        writer.PutEnum(target.Role);
        writer.Put(target.WeaponId);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass2199 object from the reader stream.
    /// </summary>
    public static GClass2199 GetEFTDeathCause(this NetDataReader reader)
    {
        return new GClass2199
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
    /// <param name="target">The GClass1962 instance containing the descriptor data to write.</param>
    public static void PutEFTDeleteMapMarkerOperationDescriptor(this NetDataWriter writer, GClass1962 target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.MapItemId);
        writer.Put(target.X);
        writer.Put(target.Y);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1962 object from the reader stream.
    /// </summary>
    /// <returns>A new instance of GClass1962 populated with the stream data.</returns>
    public static GClass1962 GetEFTDeleteMapMarkerOperationDescriptor(this NetDataReader reader)
    {
        return new GClass1962
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
    /// <param name="target">The GClass1963 instance containing the descriptor data to write.</param>
    public static void PutEFTDeleteNoteOperationDescriptor(this NetDataWriter writer, GClass1963 target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.Index);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1963 object from the reader stream.
    /// </summary>
    /// <returns>A new instance of GClass1963 populated with the stream data.</returns>
    public static GClass1963 GetEFTDeleteNoteOperationDescriptor(this NetDataReader reader)
    {
        return new GClass1963
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            Index = reader.GetInt()
        };
    }

    /// <summary>
    /// Serializes the destroyed item details into the writer stream.
    /// </summary>
    /// <param name="target">The GClass1955 instance containing the item destruction data to write.</param>
    public static void PutEFTDestroyedItem(this NetDataWriter writer, GClass1955 target)
    {
        writer.PutMongoID(target.ItemId);
        writer.Put(target.NumberToDestroy);
        writer.Put(target.NumberToPreserve);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1955 object from the reader stream.
    /// </summary>
    /// <returns>A new instance of GClass1955 populated with the stream data.</returns>
    public static GClass1955 GetEFTDestroyedItem(this NetDataReader reader)
    {
        return new GClass1955
        {
            ItemId = reader.GetMongoID(),
            NumberToDestroy = reader.GetInt(),
            NumberToPreserve = reader.GetInt()
        };
    }

    /// <summary>
    /// Serializes the dog tag component descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The GClass1938 instance containing the dog tag component data to write.</param>
    public static void PutEFTDogTagComponentDescriptor(this NetDataWriter writer, GClass1938 target)
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
    /// Deserializes and reconstructs a GClass1938 object from the reader stream.
    /// </summary>
    /// <returns>A new instance of GClass1938 populated with the stream data.</returns>
    public static GClass1938 GetEFTDogTagComponentDescriptor(this NetDataReader reader)
    {
        return new GClass1938
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
    /// <param name="target">The GClass2185 instance containing the dropped item data to write.</param>
    public static void PutEFTDroppedItem(this NetDataWriter writer, GClass2185 target)
    {
        writer.PutMongoID(target.QuestId);
        writer.PutMongoID(target.ItemId);
        writer.Put(target.ZoneId);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass2185 object from the reader stream.
    /// </summary>
    /// <returns>A new instance of GClass2185 populated with the stream data.</returns>
    public static GClass2185 GetEFTDroppedItem(this NetDataReader reader)
    {
        return new GClass2185
        {
            QuestId = reader.GetMongoID(),
            ItemId = reader.GetMongoID(),
            ZoneId = reader.GetString()
        };
    }

    /// <summary>
    /// Serializes the map marker edit operation descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The GClass1964 instance containing the descriptor data to write.</param>
    public static void PutEFTEditMapMarkerOperationDescriptor(this NetDataWriter writer, GClass1964 target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.MapItemId);
        writer.Put(target.X);
        writer.Put(target.Y);
        writer.PutEFTInventoryLogicMapMarker(target.MapMarker);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1964 object from the reader stream.
    /// </summary>
    /// <returns>A new instance of GClass1964 populated with the stream data.</returns>
    public static GClass1964 GetEFTEditMapMarkerOperationDescriptor(this NetDataReader reader)
    {
        return new GClass1964
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
    /// <param name="target">The EditNoteDescriptorClass instance containing the descriptor data to write.</param>
    public static void PutEFTEditNoteOperationDescriptor(this NetDataWriter writer, EditNoteDescriptorClass target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.Index);
        writer.PutEFTNotesNote(target.Note);
    }

    /// <summary>
    /// Deserializes and reconstructs an EditNoteDescriptorClass object from the reader stream.
    /// </summary>
    /// <returns>A new instance of EditNoteDescriptorClass populated with the stream data.</returns>
    public static EditNoteDescriptorClass GetEFTEditNoteOperationDescriptor(this NetDataReader reader)
    {
        return new EditNoteDescriptorClass
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
    /// <param name="target">The GClass1966 instance containing the descriptor data to write.</param>
    public static void PutEFTExamineMalfTypeOperationDescriptor(this NetDataWriter writer, GClass1966 target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutMongoID(target.ItemId);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1966 object from the reader stream.
    /// </summary>
    /// <returns>A new instance of GClass1966 populated with the stream data.</returns>
    public static GClass1966 GetEFTExamineMalfTypeOperationDescriptor(this NetDataReader reader)
    {
        return new GClass1966
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            ItemId = reader.GetMongoID()
        };
    }

    /// <summary>
    /// Serializes the malfunction examination operation descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The GClass1967 instance containing the descriptor data to write.</param>
    public static void PutEFTExamineMalfunctionOperationDescriptor(this NetDataWriter writer, GClass1967 target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutMongoID(target.ItemId);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1967 object from the reader stream.
    /// </summary>
    /// <returns>A new instance of GClass1967 populated with the stream data.</returns>
    public static GClass1967 GetEFTExamineMalfunctionOperationDescriptor(this NetDataReader reader)
    {
        return new GClass1967
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            ItemId = reader.GetMongoID()
        };
    }

    /// <summary>
    /// Serializes the item examination operation descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The GClass1968 instance containing the descriptor data to write.</param>
    public static void PutEFTExamineOperationDescriptor(this NetDataWriter writer, GClass1968 target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.ItemId);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1968 object from the reader stream.
    /// </summary>
    /// <returns>A new instance of GClass1968 populated with the stream data.</returns>
    public static GClass1968 GetEFTExamineOperationDescriptor(this NetDataReader reader)
    {
        return new GClass1968
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            ItemId = reader.GetString()
        };
    }

    /// <summary>
    /// Serializes the face shield component descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The GClass1935 instance containing the face shield status data to write.</param>
    public static void PutEFTFaceShieldComponentDescriptor(this NetDataWriter writer, GClass1935 target)
    {
        writer.Put(target.Hits);
        writer.Put(target.HitSeed);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1935 object from the reader stream.
    /// </summary>
    /// <returns>A new instance of GClass1935 populated with the stream data.</returns>
    public static GClass1935 GetEFTFaceShieldComponentDescriptor(this NetDataReader reader)
    {
        return new GClass1935
        {
            Hits = reader.GetByte(),
            HitSeed = reader.GetByte()
        };
    }

    /// <summary>
    /// Serializes the face shield marking operation descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The GClass1969 instance containing the descriptor data to write.</param>
    public static void PutEFTFaceshieldMarkOperationDescriptor(this NetDataWriter writer, GClass1969 target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutMongoID(target.ItemId);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1969 object from the reader stream.
    /// </summary>
    /// <returns>A new instance of GClass1969 populated with the stream data.</returns>
    public static GClass1969 GetEFTFaceshieldMarkOperationDescriptor(this NetDataReader reader)
    {
        return new GClass1969
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            ItemId = reader.GetMongoID()
        };
    }

    /// <summary>
    /// Serializes the fire mode component descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The GClass1937 instance containing the fire mode status data to write.</param>
    public static void PutEFTFireModeComponentDescriptor(this NetDataWriter writer, GClass1937 target)
    {
        writer.PutEnum(target.FireMode);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1937 object from the reader stream.
    /// </summary>
    /// <returns>A new instance of GClass1937 populated with the stream data.</returns>
    public static GClass1937 GetEFTFireModeComponentDescriptor(this NetDataReader reader)
    {
        return new GClass1937
        {
            FireMode = reader.GetEnum<Weapon.EFireMode>()
        };
    }

    /// <summary>
    /// Serializes the foldable component descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The GClass1936 instance containing the folded status data to write.</param>
    public static void PutEFTFoldableComponentDescriptor(this NetDataWriter writer, GClass1936 target)
    {
        writer.Put(target.Folded);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1936 object from the reader stream.
    /// </summary>
    /// <returns>A new instance of GClass1936 populated with the stream data.</returns>
    public static GClass1936 GetEFTFoldableComponentDescriptor(this NetDataReader reader)
    {
        return new GClass1936
        {
            Folded = reader.GetBool()
        };
    }

    /// <summary>
    /// Serializes the item folding operation descriptor into the writer stream.
    /// </summary>
    /// <param name="target">The GClass1970 instance containing the descriptor data to write.</param>
    public static void PutEFTFoldOperationDescriptor(this NetDataWriter writer, GClass1970 target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.ItemId);
        writer.Put(target.Value);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1970 object from the reader stream.
    /// </summary>
    /// <returns>A new instance of GClass1970 populated with the stream data.</returns>
    public static GClass1970 GetEFTFoldOperationDescriptor(this NetDataReader reader)
    {
        return new GClass1970
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
    /// <param name="target">The GClass1925 instance containing the food/drink status data to write.</param>
    public static void PutEFTFoodDrinkComponentDescriptor(this NetDataWriter writer, GClass1925 target)
    {
        writer.Put(target.HpPercent);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1925 object from the reader stream.
    /// </summary>
    /// <returns>A new instance of GClass1925 populated with the stream data.</returns>
    public static GClass1925 GetEFTFoodDrinkComponentDescriptor(this NetDataReader reader)
    {
        return new GClass1925
        {
            HpPercent = reader.GetFloat()
        };
    }

    /// <summary>
    /// Serializes the found in raid item details into the writer stream.
    /// </summary>
    /// <param name="target">The GClass2186 instance containing the item raid data to write.</param>
    public static void PutEFTFoundInRaidItem(this NetDataWriter writer, GClass2186 target)
    {
        writer.PutMongoID(target.QuestId);
        writer.PutMongoID(target.ItemId);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass2186 object from the reader stream.
    /// </summary>
    /// <returns>A new instance of GClass2186 populated with the stream data.</returns>
    public static GClass2186 GetEFTFoundInRaidItem(this NetDataReader reader)
    {
        return new GClass2186
        {
            QuestId = reader.GetMongoID(),
            ItemId = reader.GetMongoID()
        };
    }

    /// <summary>
    /// Serializes a grid descriptor object into the writer stream.
    /// </summary>
    public static void PutEFTGridDescriptor(this NetDataWriter writer, GClass1919 target)
    {
        writer.Put(target.GridNumber);
        writer.Put(target.ContainedItems.Count);
        for (var i = 0; i < target.ContainedItems.Count; i++)
        {
            writer.PutEFTItemInGridDescriptor(target.ContainedItems[i]);
        }
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1919 object from the reader stream.
    /// </summary>
    public static GClass1919 GetEFTGridDescriptor(this NetDataReader reader)
    {
        var gclass = new GClass1919
        {
            GridNumber = reader.GetByte()
        };
        var num = reader.GetInt();
        gclass.ContainedItems = new List<GClass1918>(num);
        for (var i = 0; i < num; i++)
        {
            gclass.ContainedItems.Add(reader.GetEFTItemInGridDescriptor());
        }
        return gclass;
    }

    /// <summary>
    /// Serializes a grid item address descriptor object into the writer stream.
    /// </summary>
    public static void PutEFTGridItemAddressDescriptor(this NetDataWriter writer, GClass1954 target)
    {
        writer.Put(target.X);
        writer.Put(target.Y);
        writer.Put(target.Horizontal);
        writer.PutEFTContainerDescriptor(target.Container);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1954 object from the reader stream.
    /// </summary>
    public static GClass1954 GetEFTGridItemAddressDescriptor(this NetDataReader reader)
    {
        return new GClass1954
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
    public static void PutEFTInventoryDescriptor(this NetDataWriter writer, EFTInventoryClass target)
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
    /// Deserializes and reconstructs an EFTInventoryClass object from the reader stream.
    /// </summary>
    public static EFTInventoryClass GetEFTInventoryDescriptor(this NetDataReader reader)
    {
        var eftinventoryClass = new EFTInventoryClass
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
            eftinventoryClass.HideoutAreaStashes = new Dictionary<EAreaType, InventoryDescriptorClass>();
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
    public static void PutEFTInventoryEquipmentDescriptor(this NetDataWriter writer, GClass2211 target)
    {
        writer.PutEFTItemDescriptor(target.Items);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass2211 object from the reader stream.
    /// </summary>
    public static GClass2211 GetEFTInventoryEquipmentDescriptor(this NetDataReader reader)
    {
        return new GClass2211
        {
            Items = reader.GetEFTItemDescriptor()
        };
    }

    /// <summary>
    /// Serializes the wishlist addition operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTInventoryLogicOperationsAddToWishlistOperationDescriptor(this NetDataWriter writer, GClass1994 target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutMongoID(target.TemplateId);
        writer.PutEnum(target.GroupType);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1994 object from the reader stream.
    /// </summary>
    public static GClass1994 GetEFTInventoryLogicOperationsAddToWishlistOperationDescriptor(this NetDataReader reader)
    {
        return new GClass1994
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
    public static void PutEFTInventoryLogicOperationsChangeItemsOperationDescriptor(this NetDataWriter writer, GClass1997 target)
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
    /// Deserializes and reconstructs a GClass1997 object from the reader stream.
    /// </summary>
    public static GClass1997 GetEFTInventoryLogicOperationsChangeItemsOperationDescriptor(this NetDataReader reader)
    {
        var gclass = new GClass1997
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID()
        };
        if (reader.GetBool())
        {
            var num = reader.GetInt();
            gclass.ChangedItems = new List<GClass1924>(num);
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
            gclass.MovedItems = new Dictionary<MongoID, GClass1950>();
            for (var k = 0; k < num3; k++)
            {
                gclass.MovedItems[reader.GetMongoID()] = reader.GetPolymorph<GClass1950>();
            }
        }
        if (reader.GetBool())
        {
            var num4 = reader.GetInt();
            gclass.NewItems = new List<GClass1921>(num4);
            for (var l = 0; l < num4; l++)
            {
                gclass.NewItems.Add(reader.GetEFTNestedItemDescriptor());
            }
        }
        if (reader.GetBool())
        {
            gclass.InternalOperationDescriptor = reader.GetPolymorph<BaseDescriptorClass>();
        }
        return gclass;
    }

    /// <summary>
    /// Serializes the wishlist item category change operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTInventoryLogicOperationsChangeWishlistItemCategoryOperationDescriptor(this NetDataWriter writer, GClass1998 target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutMongoID(target.TemplateId);
        writer.PutEnum(target.GroupType);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1998 object from the reader stream.
    /// </summary>
    public static GClass1998 GetEFTInventoryLogicOperationsChangeWishlistItemCategoryOperationDescriptor(this NetDataReader reader)
    {
        return new GClass1998
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
    public static void PutEFTInventoryLogicOperationsPurchaseTraderServiceOperationDescriptor(this NetDataWriter writer, GClass1999 target)
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
    /// Deserializes and reconstructs a GClass1999 object from the reader stream.
    /// </summary>
    public static GClass1999 GetEFTInventoryLogicOperationsPurchaseTraderServiceOperationDescriptor(this NetDataReader reader)
    {
        var gclass = new GClass1999
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
    public static void PutEFTInventoryLogicOperationsRemoveFromWishlistOperationDescriptor(this NetDataWriter writer, GClass2000 target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutMongoID(target.TemplateId);
        writer.PutEnum(target.GroupType);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass2000 object from the reader stream.
    /// </summary>
    public static GClass2000 GetEFTInventoryLogicOperationsRemoveFromWishlistOperationDescriptor(this NetDataReader reader)
    {
        return new GClass2000
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
    public static void PutEFTInventoryLogicOperationsSearchContentOperationDescriptor(this NetDataWriter writer, GClass2002 target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutMongoID(target.ItemId);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass2002 object from the reader stream.
    /// </summary>
    public static GClass2002 GetEFTInventoryLogicOperationsSearchContentOperationDescriptor(this NetDataReader reader)
    {
        return new GClass2002
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            ItemId = reader.GetMongoID()
        };
    }

    /// <summary>
    /// Serializes the container content search suboperation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTInventoryLogicOperationsSearchSuboperationDescriptor(this NetDataWriter writer, GClass2001 target)
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
    /// Deserializes and reconstructs a GClass2001 object from the reader stream.
    /// </summary>
    public static GClass2001 GetEFTInventoryLogicOperationsSearchSuboperationDescriptor(this NetDataReader reader)
    {
        var gclass = new GClass2001
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            SearchedItem = reader.GetMongoID()
        };
        if (reader.GetBool())
        {
            var num = reader.GetInt();
            gclass.Content = new List<GClass1921>(num);
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
    public static void PutEFTInventoryLogicOperationsSplitToNowhereDescriptor(this NetDataWriter writer, GClass1995 target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutMongoID(target.ItemId);
        writer.Put(target.Count);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1995 object from the reader stream.
    /// </summary>
    public static GClass1995 GetEFTInventoryLogicOperationsSplitToNowhereDescriptor(this NetDataReader reader)
    {
        return new GClass1995
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
    public static void PutEFTInventoryLogicOperationsTransferFromNowhereDescriptor(this NetDataWriter writer, GClass1996 target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutMongoID(target.ItemId);
        writer.Put(target.Count);
        writer.Put(target.SpawnedInSession);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1996 object from the reader stream.
    /// </summary>
    public static GClass1996 GetEFTInventoryLogicOperationsTransferFromNowhereDescriptor(this NetDataReader reader)
    {
        return new GClass1996
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
    public static void PutEFTItemDescriptor(this NetDataWriter writer, InventoryDescriptorClass target)
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
    /// Deserializes and reconstructs an InventoryDescriptorClass object from the reader stream.
    /// </summary>
    public static InventoryDescriptorClass GetEFTItemDescriptor(this NetDataReader reader)
    {
        var inventoryDescriptorClass = new InventoryDescriptorClass
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
            inventoryDescriptorClass.Components = new List<GClass1923>(num);
            for (var i = 0; i < num; i++)
            {
                inventoryDescriptorClass.Components.Add(reader.GetPolymorph<GClass1923>());
            }
        }
        if (reader.GetBool())
        {
            var num2 = reader.GetInt();
            inventoryDescriptorClass.Slots = new List<GClass1915>(num2);
            for (var j = 0; j < num2; j++)
            {
                inventoryDescriptorClass.Slots.Add(reader.GetEFTSlotDescriptor());
            }
        }
        if (reader.GetBool())
        {
            var num3 = reader.GetInt();
            inventoryDescriptorClass.ShellsInWeapon = new List<GClass1916>(num3);
            for (var k = 0; k < num3; k++)
            {
                inventoryDescriptorClass.ShellsInWeapon.Add(reader.GetEFTShellTemplateDescriptor());
            }
        }
        if (reader.GetBool())
        {
            var num4 = reader.GetInt();
            inventoryDescriptorClass.Grids = new List<GClass1919>(num4);
            for (var l = 0; l < num4; l++)
            {
                inventoryDescriptorClass.Grids.Add(reader.GetEFTGridDescriptor());
            }
        }
        if (reader.GetBool())
        {
            var num5 = reader.GetInt();
            inventoryDescriptorClass.StackSlots = new List<GClass1920>(num5);
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
    public static void PutEFTItemInfoDescriptor(this NetDataWriter writer, GClass1924 target)
    {
        writer.PutMongoID(target.Id);
        writer.Put(target.Hash);
        writer.Put(target.Width);
        writer.Put(target.Height);
        writer.Put(target.TotalWeight);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1924 object from the reader stream.
    /// </summary>
    public static GClass1924 GetEFTItemInfoDescriptor(this NetDataReader reader)
    {
        return new GClass1924
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
    public static void PutEFTItemInGridDescriptor(this NetDataWriter writer, GClass1918 target)
    {
        writer.PutEFTItemDescriptor(target.Item);
        writer.Put(target.X);
        writer.Put(target.Y);
        writer.Put(target.Horizontal);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1918 object from the reader stream.
    /// </summary>
    public static GClass1918 GetEFTItemInGridDescriptor(this NetDataReader reader)
    {
        return new GClass1918
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
    public static void PutEFTJsonCorpseDescriptor(this NetDataWriter writer, GClass1946 target)
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
    /// Deserializes and reconstructs a GClass1946 object from the reader stream.
    /// </summary>
    public static GClass1946 GetEFTJsonCorpseDescriptor(this NetDataReader reader)
    {
        var gclass = new GClass1946();
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
    public static void PutEFTJsonLootItemDescriptor(this NetDataWriter writer, GClass1945 target)
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
    /// Deserializes and reconstructs a GClass1945 object from the reader stream.
    /// </summary>
    public static GClass1945 GetEFTJsonLootItemDescriptor(this NetDataReader reader)
    {
        var gclass = new GClass1945
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
    public static void PutEFTKeyComponentDescriptor(this NetDataWriter writer, GClass1940 target)
    {
        writer.Put(target.NumberOfUsages);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1940 object from the reader stream.
    /// </summary>
    public static GClass1940 GetEFTKeyComponentDescriptor(this NetDataReader reader)
    {
        return new GClass1940
        {
            NumberOfUsages = reader.GetInt()
        };
    }

    /// <summary>
    /// Serializes the light/tactical device component descriptor into the writer stream.
    /// </summary>
    public static void PutEFTLightComponentDescriptor(this NetDataWriter writer, GClass1928 target)
    {
        writer.Put(target.IsActive);
        writer.Put(target.SelectedMode);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1928 object from the reader stream.
    /// </summary>
    public static GClass1928 GetEFTLightComponentDescriptor(this NetDataReader reader)
    {
        return new GClass1928
        {
            IsActive = reader.GetBool(),
            SelectedMode = reader.GetInt()
        };
    }

    /// <summary>
    /// Serializes the magazine loading operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTLoadMagOperationDescriptor(this NetDataWriter writer, GClass1972 target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutPolymorph(target.InternalOperationDescriptor);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1972 object from the reader stream.
    /// </summary>
    public static GClass1972 GetEFTLoadMagOperationDescriptor(this NetDataReader reader)
    {
        return new GClass1972
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            InternalOperationDescriptor = reader.GetPolymorph<BaseDescriptorClass>()
        };
    }

    /// <summary>
    /// Serializes the lockable component descriptor into the writer stream.
    /// </summary>
    public static void PutEFTLockableComponentDescriptor(this NetDataWriter writer, GClass1929 target)
    {
        writer.Put(target.Locked);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1929 object from the reader stream.
    /// </summary>
    public static GClass1929 GetEFTLockableComponentDescriptor(this NetDataReader reader)
    {
        return new GClass1929
        {
            Locked = reader.GetBool()
        };
    }

    /// <summary>
    /// Serializes global loot data descriptors into the writer stream.
    /// </summary>
    public static void PutEFTLootDataDescriptor(this NetDataWriter writer, GClass1947 target)
    {
        writer.Put(target.Items.Count);
        for (var i = 0; i < target.Items.Count; i++)
        {
            writer.PutPolymorph(target.Items[i]);
        }
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1947 object from the reader stream.
    /// </summary>
    public static GClass1947 GetEFTLootDataDescriptor(this NetDataReader reader)
    {
        var gclass = new GClass1947();
        var num = reader.GetInt();
        gclass.Items = new List<GClass1945>(num);
        for (var i = 0; i < num; i++)
        {
            gclass.Items.Add(reader.GetPolymorph<GClass1945>());
        }
        return gclass;
    }

    /// <summary>
    /// Serializes detailed weapon malfunction descriptor structures into the writer stream.
    /// </summary>
    public static void PutEFTMalfunctionDescriptor(this NetDataWriter writer, GClass1917 target)
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
    /// Deserializes and reconstructs a GClass1917 object from the reader stream.
    /// </summary>
    public static GClass1917 GetEFTMalfunctionDescriptor(this NetDataReader reader)
    {
        var gclass = new GClass1917
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
    public static void PutEFTMapComponentDescriptor(this NetDataWriter writer, GClass1930 target)
    {
        writer.Put(target.Markers.Count);
        for (var i = 0; i < target.Markers.Count; i++)
        {
            writer.PutEFTInventoryLogicMapMarker(target.Markers[i]);
        }
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1930 object from the reader stream.
    /// </summary>
    public static GClass1930 GetEFTMapComponentDescriptor(this NetDataReader reader)
    {
        var gclass = new GClass1930();
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
    public static void PutEFTMedKitComponentDescriptor(this NetDataWriter writer, GClass1931 target)
    {
        writer.Put(target.HpResource);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1931 object from the reader stream.
    /// </summary>
    public static GClass1931 GetEFTMedKitComponentDescriptor(this NetDataReader reader)
    {
        return new GClass1931
        {
            HpResource = reader.GetFloat()
        };
    }

    /// <summary>
    /// Serializes the item stack merge operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTMergeOperationDescriptor(this NetDataWriter writer, MergeDescriptorClass target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.ItemId);
        writer.Put(target.Item1Id);
    }

    /// <summary>
    /// Deserializes and reconstructs a MergeDescriptorClass object from the reader stream.
    /// </summary>
    public static MergeDescriptorClass GetEFTMergeOperationDescriptor(this NetDataReader reader)
    {
        return new MergeDescriptorClass
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
    public static void PutEFTMoveOperationDescriptor(this NetDataWriter writer, MoveDescriptorClass target)
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
    /// Deserializes and reconstructs a MoveDescriptorClass object from the reader stream.
    /// </summary>
    public static MoveDescriptorClass GetEFTMoveOperationDescriptor(this NetDataReader reader)
    {
        var moveDescriptorClass = new MoveDescriptorClass
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            ItemId = reader.GetString(),
            From = reader.GetPolymorph<GClass1950>(),
            To = reader.GetPolymorph<GClass1950>()
        };
        if (reader.GetBool())
        {
            var num = reader.GetInt();
            moveDescriptorClass.DestroyedItems = new List<GClass1955>(num);
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
    public static void PutEFTNestedItemDescriptor(this NetDataWriter writer, GClass1921 target)
    {
        writer.PutPolymorph(target.Address);
        writer.PutEFTItemDescriptor(target.Item);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1921 object from the reader stream.
    /// </summary>
    public static GClass1921 GetEFTNestedItemDescriptor(this NetDataReader reader)
    {
        return new GClass1921
        {
            Address = reader.GetPolymorph<GClass1950>(),
            Item = reader.GetEFTItemDescriptor()
        };
    }

    /// <summary>
    /// Serializes the player profile context notes manager data arrays into the writer stream.
    /// </summary>
    public static void PutEFTNotesNotesManagerNotesDescriptor(this NetDataWriter writer, NotesManagerClass.GClass3109 target)
    {
        writer.Put(target.Notes.Length);
        for (var i = 0; i < target.Notes.Length; i++)
        {
            writer.PutEFTNotesNote(target.Notes[i]);
        }
    }

    /// <summary>
    /// Deserializes and reconstructs a NotesManagerClass.GClass3109 object from the reader stream.
    /// </summary>
    public static NotesManagerClass.GClass3109 GetEFTNotesNotesManagerNotesDescriptor(this NetDataReader reader)
    {
        var gclass = new NotesManagerClass.GClass3109();
        var num = reader.GetInt();
        gclass.Notes = new GClass3107[num];
        for (var i = 0; i < num; i++)
        {
            gclass.Notes[i] = reader.GetEFTNotesNote();
        }
        return gclass;
    }

    /// <summary>
    /// Serializes the stationary weapon interaction/operation context descriptor into the writer stream.
    /// </summary>
    public static void PutEFTOperateStationaryWeaponOperationDescriptor(this NetDataWriter writer, GClass1982 target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.WeaponId);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1982 object from the reader stream.
    /// </summary>
    public static GClass1982 GetEFTOperateStationaryWeaponOperationDescriptor(this NetDataReader reader)
    {
        return new GClass1982
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            WeaponId = reader.GetString()
        };
    }

    /// <summary>
    /// Serializes an inventory base root container self-owner tracking descriptor reference into the writer stream.
    /// </summary>
    public static void PutEFTOwnerItselfDescriptor(this NetDataWriter writer, GClass1951 target)
    {
        writer.PutEFTContainerDescriptor(target.Container);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1951 object from the reader stream.
    /// </summary>
    public static GClass1951 GetEFTOwnerItselfDescriptor(this NetDataReader reader)
    {
        return new GClass1951
        {
            Container = reader.GetEFTContainerDescriptor()
        };
    }

    /// <summary>
    /// Serializes a localized interactive structural tripwire deployment operation into the writer stream.
    /// </summary>
    public static void PutEFTPlantTripwireOperationDescriptor(this NetDataWriter writer, GClass1976 target)
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
    /// Deserializes and reconstructs a GClass1976 object from the reader stream.
    /// </summary>
    public static GClass1976 GetEFTPlantTripwireOperationDescriptor(this NetDataReader reader)
    {
        var gclass = new GClass1976
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
    public static void PutEFTPlayerVisualRepresentationDescriptor(this NetDataWriter writer, GClass2214 target)
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
    /// Deserializes and reconstructs a GClass2214 object from the reader stream.
    /// </summary>
    public static GClass2214 GetEFTPlayerVisualRepresentationDescriptor(this NetDataReader reader)
    {
        var gclass = new GClass2214
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
    public static void PutEFTPoisonComponentDescriptor(this NetDataWriter writer, GClass1926 target)
    {
        writer.Put(target.Resource);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1926 object from the reader stream.
    /// </summary>
    public static GClass1926 GetEFTPoisonComponentDescriptor(this NetDataReader reader)
    {
        return new GClass1926
        {
            Resource = reader.GetFloat()
        };
    }

    /// <summary>
    /// Serializes prestige status validation data points into the writer stream.
    /// </summary>
    public static void PutEFTPrestigePrestigeStatusData(this NetDataWriter writer, GClass2660 target)
    {
        writer.PutMongoID(target.TemplateId);
        writer.Put(target.Timestamp);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass2660 object from the reader stream.
    /// </summary>
    public static GClass2660 GetEFTPrestigePrestigeStatusData(this NetDataReader reader)
    {
        return new GClass2660
        {
            TemplateId = reader.GetMongoID(),
            Timestamp = reader.GetInt()
        };
    }

    /// <summary>
    /// Serializes comprehensive multi-zone structural profile health data matrices into the writer stream.
    /// </summary>
    public static void PutEFTProfileHealthInfo(this NetDataWriter writer, Profile.ProfileHealthClass target)
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
    /// Deserializes and reconstructs a Profile.ProfileHealthClass object from the reader stream.
    /// </summary>
    public static Profile.ProfileHealthClass GetEFTProfileHealthInfo(this NetDataReader reader)
    {
        var profileHealthClass = new Profile.ProfileHealthClass();
        var num = reader.GetInt();
        profileHealthClass.BodyParts = new Dictionary<EBodyPart, Profile.ProfileHealthClass.ProfileBodyPartHealthClass>();
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
    public static void PutEFTProfileHealthInfoBodyPartInfo(this NetDataWriter writer, Profile.ProfileHealthClass.ProfileBodyPartHealthClass target)
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
    /// Deserializes and reconstructs a Profile.ProfileHealthClass.ProfileBodyPartHealthClass object from the reader stream.
    /// </summary>
    public static Profile.ProfileHealthClass.ProfileBodyPartHealthClass GetEFTProfileHealthInfoBodyPartInfo(this NetDataReader reader)
    {
        var profileBodyPartHealthClass = new Profile.ProfileHealthClass.ProfileBodyPartHealthClass
        {
            Health = reader.GetEFTProfileHealthInfoValueInfo()
        };
        var num = reader.GetInt();
        profileBodyPartHealthClass.Effects = new Dictionary<string, Profile.ProfileHealthClass.GClass2206>();
        for (var i = 0; i < num; i++)
        {
            profileBodyPartHealthClass.Effects[reader.GetString()] = reader.GetEFTProfileHealthInfoEffectInfo();
        }
        return profileBodyPartHealthClass;
    }

    /// <summary>
    /// Serializes a targeted health anomaly or buff context duration element into the writer stream.
    /// </summary>
    public static void PutEFTProfileHealthInfoEffectInfo(this NetDataWriter writer, Profile.ProfileHealthClass.GClass2206 target)
    {
        writer.Put(target.Time);
    }

    /// <summary>
    /// Deserializes and reconstructs a Profile.ProfileHealthClass.GClass2206 object from the reader stream.
    /// </summary>
    public static Profile.ProfileHealthClass.GClass2206 GetEFTProfileHealthInfoEffectInfo(this NetDataReader reader)
    {
        return new Profile.ProfileHealthClass.GClass2206
        {
            Time = reader.GetFloat()
        };
    }

    /// <summary>
    /// Serializes precise structural attribute parameters including maximum thresholds and external damage scale coefficients into the writer stream.
    /// </summary>
    public static void PutEFTProfileHealthInfoValueInfo(this NetDataWriter writer, Profile.ProfileHealthClass.ValueInfo target)
    {
        writer.Put(target.Current);
        writer.Put(target.Minimum);
        writer.Put(target.Maximum);
        writer.Put(target.OverDamageReceivedMultiplier);
        writer.Put(target.EnvironmentDamageMultiplier);
    }

    /// <summary>
    /// Deserializes and reconstructs a Profile.ProfileHealthClass.ValueInfo object from the reader stream.
    /// </summary>
    public static Profile.ProfileHealthClass.ValueInfo GetEFTProfileHealthInfoValueInfo(this NetDataReader reader)
    {
        return new Profile.ProfileHealthClass.ValueInfo
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
    public static void PutEFTProfileMoneyTransferLimitData(this NetDataWriter writer, Profile.GClass2209 target)
    {
        writer.Put(target.nextResetTime);
        writer.Put(target.remainingLimit);
        writer.Put(target.totalLimit);
        writer.Put(target.resetInterval);
    }

    /// <summary>
    /// Deserializes and reconstructs a Profile.GClass2209 object from the reader stream.
    /// </summary>
    public static Profile.GClass2209 GetEFTProfileMoneyTransferLimitData(this NetDataReader reader)
    {
        return new Profile.GClass2209
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
    public static void PutEFTProfileUnlockedInfo(this NetDataWriter writer, Profile.GClass2208 target)
    {
        writer.Put(target.unlockedSchemeList.Count);
        for (var i = 0; i < target.unlockedSchemeList.Count; i++)
        {
            writer.PutMongoID(target.unlockedSchemeList[i]);
        }
    }

    /// <summary>
    /// Deserializes and reconstructs a Profile.GClass2208 object from the reader stream.
    /// </summary>
    public static Profile.GClass2208 GetEFTProfileUnlockedInfo(this NetDataReader reader)
    {
        var gclass = new Profile.GClass2208();
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
    public static void PutEFTProfileBanDescriptor(this NetDataWriter writer, GClass2222 target)
    {
        writer.PutEnum(target.Type);
        writer.Put(target.ExpirationTime);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass2222 object from the reader stream.
    /// </summary>
    public static GClass2222 GetEFTProfileBanDescriptor(this NetDataReader reader)
    {
        return new GClass2222
        {
            Type = reader.GetEnum<EBanType>(),
            ExpirationTime = reader.GetLong()
        };
    }

    /// <summary>
    /// Serializes an entire complete server profile context descriptor containing all sub-systems into the writer stream.
    /// </summary>
    public static void PutEFTProfileDescriptor(this NetDataWriter writer, CompleteProfileDescriptorClass target)
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
    /// Deserializes and reconstructs a CompleteProfileDescriptorClass object from the reader stream.
    /// </summary>
    public static CompleteProfileDescriptorClass GetEFTProfileDescriptor(this NetDataReader reader)
    {
        var completeProfileDescriptorClass = new CompleteProfileDescriptorClass
        {
            Id = reader.GetMongoID(),
            AccountId = reader.GetString()
        };
        if (reader.GetBool())
        {
            completeProfileDescriptorClass.PetId = new MongoID?(reader.GetMongoID());
        }
        completeProfileDescriptorClass.KarmaValue = reader.GetFloat();
        completeProfileDescriptorClass.Info = reader.GetPolymorph<ProfileInfoClass>();
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
        completeProfileDescriptorClass.InsuredItems = new InsuredItemClass[num3];
        for (var k = 0; k < num3; k++)
        {
            completeProfileDescriptorClass.InsuredItems[k] = reader.GetJsonTypeInsuredProfileItems();
        }
        completeProfileDescriptorClass.Skills = reader.GetEFTSkillsDescriptor();
        completeProfileDescriptorClass.Notes = reader.GetEFTNotesNotesManagerNotesDescriptor();
        var num4 = reader.GetInt();
        completeProfileDescriptorClass.TaskConditionCounters = new Dictionary<MongoID, GClass2227>();
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
        completeProfileDescriptorClass.Bonuses = new ProfileBonusesClass[num11];
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
        completeProfileDescriptorClass.TradersInfo = new Dictionary<MongoID, TraderInfoClass>();
        for (var num20 = 0; num20 < num19; num20++)
        {
            completeProfileDescriptorClass.TradersInfo[reader.GetMongoID()] = reader.GetEFTTraderInfoDescriptor();
        }
        return completeProfileDescriptorClass;
    }

    /// <summary>
    /// Serializes fundamental metadata and setting parameters for a profile character entry into the writer stream.
    /// </summary>
    public static void PutEFTProfileInfoDescriptor(this NetDataWriter writer, ProfileInfoClass target)
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
    /// Deserializes and reconstructs a ProfileInfoClass object from the reader stream.
    /// </summary>
    public static ProfileInfoClass GetEFTProfileInfoDescriptor(this NetDataReader reader)
    {
        var profileInfoClass = new ProfileInfoClass
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
        profileInfoClass.Bans = new List<GClass2222>(num);
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
    public static void PutEFTProfileSettings(this NetDataWriter writer, ProfileInfoSettingsClass target)
    {
        writer.PutEnum(target.Role);
        writer.PutEnum(target.BotDifficulty);
        writer.Put(target.Experience);
        writer.Put(target.StandingForKill);
        writer.Put(target.AggressorBonus);
        writer.Put(target.UseSimpleAnimator);
    }

    /// <summary>
    /// Deserializes and reconstructs a ProfileInfoSettingsClass object from the reader stream.
    /// </summary>
    public static ProfileInfoSettingsClass GetEFTProfileSettings(this NetDataReader reader)
    {
        return new ProfileInfoSettingsClass
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
    public static void PutEFTProfileStatsDescriptor(this NetDataWriter writer, ProfileEftStatsClass target)
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
    /// Deserializes and reconstructs a ProfileEftStatsClass object from the reader stream.
    /// </summary>
    public static ProfileEftStatsClass GetEFTProfileStatsDescriptor(this NetDataReader reader)
    {
        var profileEftStatsClass = new ProfileEftStatsClass
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
        profileEftStatsClass.DroppedItems = new List<GClass2185>(num);
        for (var i = 0; i < num; i++)
        {
            profileEftStatsClass.DroppedItems.Add(reader.GetEFTDroppedItem());
        }
        var num2 = reader.GetInt();
        profileEftStatsClass.FoundInRaidItems = new List<GClass2186>(num2);
        for (var j = 0; j < num2; j++)
        {
            profileEftStatsClass.FoundInRaidItems.Add(reader.GetEFTFoundInRaidItem());
        }
        var num3 = reader.GetInt();
        profileEftStatsClass.Victims = new List<GClass2201>(num3);
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
    public static void PutEFTProfileStatsSeparatorDescriptor(this NetDataWriter writer, ProfileStatsClass target)
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
    /// Deserializes and reconstructs a ProfileStatsClass object from the reader stream.
    /// </summary>
    public static ProfileStatsClass GetEFTProfileStatsSeparatorDescriptor(this NetDataReader reader)
    {
        var profileStatsClass = new ProfileStatsClass();
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
    public static void PutEFTQuestAcceptDescriptor(this NetDataWriter writer, GClass1991 target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutMongoID(target.QuestId);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1991 object from the reader stream.
    /// </summary>
    public static GClass1991 GetEFTQuestAcceptDescriptor(this NetDataReader reader)
    {
        return new GClass1991
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            QuestId = reader.GetMongoID()
        };
    }

    /// <summary>
    /// Serializes a quest finalization/finish operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTQuestFinishDescriptor(this NetDataWriter writer, GClass1992 target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutMongoID(target.QuestId);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1992 object from the reader stream.
    /// </summary>
    public static GClass1992 GetEFTQuestFinishDescriptor(this NetDataReader reader)
    {
        return new GClass1992
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            QuestId = reader.GetMongoID()
        };
    }

    /// <summary>
    /// Serializes a quest condition items handover operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTQuestHandoverDescriptor(this NetDataWriter writer, GClass1993 target)
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
    /// Deserializes and reconstructs a GClass1993 object from the reader stream.
    /// </summary>
    public static GClass1993 GetEFTQuestHandoverDescriptor(this NetDataReader reader)
    {
        var gclass = new GClass1993
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
    public static void PutEFTRecodableComponentDescriptor(this NetDataWriter writer, GClass1943 target)
    {
        writer.Put(target.IsEncoded);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1943 object from the reader stream.
    /// </summary>
    public static GClass1943 GetEFTRecodableComponentDescriptor(this NetDataReader reader)
    {
        return new GClass1943
        {
            IsEncoded = reader.GetBool()
        };
    }

    /// <summary>
    /// Serializes an inventory item removal operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTRemoveOperationDescriptor(this NetDataWriter writer, GClass1977 target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutMongoID(target.ItemId);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1977 object from the reader stream.
    /// </summary>
    public static GClass1977 GetEFTRemoveOperationDescriptor(this NetDataReader reader)
    {
        return new GClass1977
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            ItemId = reader.GetMongoID()
        };
    }

    /// <summary>
    /// Serializes the repairable item status parameters into the writer stream.
    /// </summary>
    public static void PutEFTRepairableComponentDescriptor(this NetDataWriter writer, GClass1932 target)
    {
        writer.Put(target.Durability);
        writer.Put(target.MaxDurability);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1932 object from the reader stream.
    /// </summary>
    public static GClass1932 GetEFTRepairableComponentDescriptor(this NetDataReader reader)
    {
        return new GClass1932
        {
            Durability = reader.GetFloat(),
            MaxDurability = reader.GetFloat()
        };
    }

    /// <summary>
    /// Serializes the repair enhancement state attributes and modifiers into the writer stream.
    /// </summary>
    public static void PutEFTRepairEnhancementComponentDescriptor(this NetDataWriter writer, GClass1942 target)
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
    /// Deserializes and reconstructs a GClass1942 object from the reader stream.
    /// </summary>
    public static GClass1942 GetEFTRepairEnhancementComponentDescriptor(this NetDataReader reader)
    {
        var gclass = new GClass1942();
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
    public static void PutEFTRepairKitComponentDescriptor(this NetDataWriter writer, GClass1941 target)
    {
        writer.Put(target.Resource);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1941 object from the reader stream.
    /// </summary>
    public static GClass1941 GetEFTRepairKitComponentDescriptor(this NetDataReader reader)
    {
        return new GClass1941
        {
            Resource = reader.GetFloat()
        };
    }

    /// <summary>
    /// Serializes general item resource usage levels into the writer stream.
    /// </summary>
    public static void PutEFTResourceItemComponentDescriptor(this NetDataWriter writer, GClass1927 target)
    {
        writer.Put(target.Resource);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1927 object from the reader stream.
    /// </summary>
    public static GClass1927 GetEFTResourceItemComponentDescriptor(this NetDataReader reader)
    {
        return new GClass1927
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
    public static void PutEFTSetDialogProgressOperationDescriptor(this NetDataWriter writer, GClass1978 target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutMongoID(target.TraderId);
        writer.PutMongoID(target.DialogId);
        writer.PutMongoID(target.LineId);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1978 object from the reader stream.
    /// </summary>
    public static GClass1978 GetEFTSetDialogProgressOperationDescriptor(this NetDataReader reader)
    {
        return new GClass1978
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
    public static void PutEFTSetupItemOperationDescriptor(this NetDataWriter writer, GClass1979 target)
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
    /// Deserializes and reconstructs a GClass1979 object from the reader stream.
    /// </summary>
    public static GClass1979 GetEFTSetupItemOperationDescriptor(this NetDataReader reader)
    {
        return new GClass1979
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
    public static void PutEFTSetVariableOperationDescriptor(this NetDataWriter writer, GClass1980 target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutMongoID(target.VariableId);
        writer.Put(target.Value);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1980 object from the reader stream.
    /// </summary>
    public static GClass1980 GetEFTSetVariableOperationDescriptor(this NetDataReader reader)
    {
        return new GClass1980
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
    public static void PutEFTShellTemplateDescriptor(this NetDataWriter writer, GClass1916 target)
    {
        writer.PutMongoID(target.AmmoTemplateId);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1916 object from the reader stream.
    /// </summary>
    public static GClass1916 GetEFTShellTemplateDescriptor(this NetDataReader reader)
    {
        return new GClass1916
        {
            AmmoTemplateId = reader.GetMongoID()
        };
    }

    /// <summary>
    /// Serializes complex structural firearm sight calibration and scope multi-mode arrays into the writer stream.
    /// </summary>
    public static void PutEFTSightComponentDescriptor(this NetDataWriter writer, GClass1933 target)
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
    /// Deserializes and reconstructs a GClass1933 object from the reader stream.
    /// </summary>
    public static GClass1933 GetEFTSightComponentDescriptor(this NetDataReader reader)
    {
        var gclass = new GClass1933
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
    public static void PutEFTSkillsDescriptor(this NetDataWriter writer, SkillsDescriptorClass target)
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
    /// Deserializes and reconstructs a SkillsDescriptorClass object from the reader stream.
    /// </summary>
    public static SkillsDescriptorClass GetEFTSkillsDescriptor(this NetDataReader reader)
    {
        var skillsDescriptorClass = new SkillsDescriptorClass();
        var num = reader.GetInt();
        skillsDescriptorClass.Common = new SkillsDescriptorClass.GClass2225[num];
        for (var i = 0; i < num; i++)
        {
            skillsDescriptorClass.Common[i] = reader.GetEFTSkillsDescriptorSkillInfoDescriptor();
        }
        var num2 = reader.GetInt();
        skillsDescriptorClass.Mastering = new SkillsDescriptorClass.GClass2226[num2];
        for (var j = 0; j < num2; j++)
        {
            skillsDescriptorClass.Mastering[j] = reader.GetEFTSkillsDescriptorMasteringInfoDescriptor();
        }
        return skillsDescriptorClass;
    }

    /// <summary>
    /// Serializes a specified weapon mastery identifier alongside its progression metric into the writer stream.
    /// </summary>
    public static void PutEFTSkillsDescriptorMasteringInfoDescriptor(this NetDataWriter writer, SkillsDescriptorClass.GClass2226 target)
    {
        writer.Put(target.Id);
        writer.Put(target.Progress);
    }

    /// <summary>
    /// Deserializes and reconstructs a SkillsDescriptorClass.GClass2226 object from the reader stream.
    /// </summary>
    public static SkillsDescriptorClass.GClass2226 GetEFTSkillsDescriptorMasteringInfoDescriptor(this NetDataReader reader)
    {
        return new SkillsDescriptorClass.GClass2226
        {
            Id = reader.GetString(),
            Progress = reader.GetFloat()
        };
    }

    /// <summary>
    /// Serializes an isolated character skill item encompassing session gain records and temporal markers into the writer stream.
    /// </summary>
    public static void PutEFTSkillsDescriptorSkillInfoDescriptor(this NetDataWriter writer, SkillsDescriptorClass.GClass2225 target)
    {
        writer.PutEnum(target.Id);
        writer.Put(target.Progress);
        writer.Put(target.PointsEarnedDuringSession);
        writer.Put(target.LastAccess);
    }

    /// <summary>
    /// Deserializes and reconstructs a SkillsDescriptorClass.GClass2225 object from the reader stream.
    /// </summary>
    public static SkillsDescriptorClass.GClass2225 GetEFTSkillsDescriptorSkillInfoDescriptor(this NetDataReader reader)
    {
        return new SkillsDescriptorClass.GClass2225
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
    public static void PutEFTSlotDescriptor(this NetDataWriter writer, GClass1915 target)
    {
        writer.Put(target.SlotNumber);
        writer.PutEFTItemDescriptor(target.ContainedItem);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1915 object from the reader stream.
    /// </summary>
    public static GClass1915 GetEFTSlotDescriptor(this NetDataReader reader)
    {
        return new GClass1915
        {
            SlotNumber = reader.GetByte(),
            ContainedItem = reader.GetEFTItemDescriptor()
        };
    }

    /// <summary>
    /// Serializes a slot item address container reference mapping into the writer stream.
    /// </summary>
    public static void PutEFTSlotItemAddressDescriptor(this NetDataWriter writer, GClass1952 target)
    {
        writer.PutEFTContainerDescriptor(target.Container);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1952 object from the reader stream.
    /// </summary>
    public static GClass1952 GetEFTSlotItemAddressDescriptor(this NetDataReader reader)
    {
        return new GClass1952
        {
            Container = reader.GetEFTContainerDescriptor()
        };
    }

    /// <summary>
    /// Serializes an item stack partition split operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTSplitOperationDescriptor(this NetDataWriter writer, SplitDescriptorClass target)
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
    /// Deserializes and reconstructs a SplitDescriptorClass object from the reader stream.
    /// </summary>
    public static SplitDescriptorClass GetEFTSplitOperationDescriptor(this NetDataReader reader)
    {
        return new SplitDescriptorClass
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            ItemId = reader.GetString(),
            CloneId = reader.GetString(),
            From = reader.GetPolymorph<GClass1950>(),
            To = reader.GetPolymorph<GClass1950>(),
            Count = reader.GetInt()
        };
    }

    /// <summary>
    /// Serializes an inventory multi-item stack slot layout block into the writer stream.
    /// </summary>
    public static void PutEFTStackSlotDescriptor(this NetDataWriter writer, GClass1920 target)
    {
        writer.Put(target.SlotNumber);
        writer.Put(target.ContainedItems.Count);
        for (var i = 0; i < target.ContainedItems.Count; i++)
        {
            writer.PutEFTItemDescriptor(target.ContainedItems[i]);
        }
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1920 object from the reader stream.
    /// </summary>
    public static GClass1920 GetEFTStackSlotDescriptor(this NetDataReader reader)
    {
        var gclass = new GClass1920
        {
            SlotNumber = reader.GetByte()
        };
        var num = reader.GetInt();
        gclass.ContainedItems = new List<InventoryDescriptorClass>(num);
        for (var i = 0; i < num; i++)
        {
            gclass.ContainedItems.Add(reader.GetEFTItemDescriptor());
        }
        return gclass;
    }

    /// <summary>
    /// Serializes a stack slot structural layout item address mapping descriptor into the writer stream.
    /// </summary>
    public static void PutEFTStackSlotItemAddressDescriptor(this NetDataWriter writer, GClass1953 target)
    {
        writer.PutEFTContainerDescriptor(target.Container);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1953 object from the reader stream.
    /// </summary>
    public static GClass1953 GetEFTStackSlotItemAddressDescriptor(this NetDataReader reader)
    {
        return new GClass1953
        {
            Container = reader.GetEFTContainerDescriptor()
        };
    }

    /// <summary>
    /// Serializes a complex item layout positional swap exchange operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTSwapOperationDescriptor(this NetDataWriter writer, GClass1983 target)
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
    /// Deserializes and reconstructs a GClass1983 object from the reader stream.
    /// </summary>
    public static GClass1983 GetEFTSwapOperationDescriptor(this NetDataReader reader)
    {
        var gclass = new GClass1983
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            ItemId = reader.GetString(),
            To = reader.GetPolymorph<GClass1950>(),
            Item1Id = reader.GetString(),
            To1 = reader.GetPolymorph<GClass1950>()
        };
        if (reader.GetBool())
        {
            var num = reader.GetInt();
            gclass.DestroyedItems = new List<GClass1955>(num);
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
    public static void PutEFTTagComponentDescriptor(this NetDataWriter writer, GClass1939 target)
    {
        writer.Put(target.Name);
        writer.Put(target.Color);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1939 object from the reader stream.
    /// </summary>
    public static GClass1939 GetEFTTagComponentDescriptor(this NetDataReader reader)
    {
        return new GClass1939
        {
            Name = reader.GetString(),
            Color = reader.GetInt()
        };
    }

    /// <summary>
    /// Serializes an individual item label tag alteration operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTTagOperationDescriptor(this NetDataWriter writer, TagDescriptorClass target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.ItemId);
        writer.Put(target.TagName);
        writer.Put(target.TagColor);
    }

    /// <summary>
    /// Deserializes and reconstructs a TagDescriptorClass object from the reader stream.
    /// </summary>
    public static TagDescriptorClass GetEFTTagOperationDescriptor(this NetDataReader reader)
    {
        return new TagDescriptorClass
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
    public static void PutEFTTaskConditionCounterDescriptor(this NetDataWriter writer, GClass2227 target)
    {
        writer.PutMongoID(target.Id);
        writer.Put(target.Value);
        writer.Put(target.SourceId);
        writer.Put(target.Type);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass2227 object from the reader stream.
    /// </summary>
    public static GClass2227 GetEFTTaskConditionCounterDescriptor(this NetDataReader reader)
    {
        return new GClass2227
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
    public static void PutEFTThrowOperationDescriptor(this NetDataWriter writer, ThrowDescriptorClass target)
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
    /// Deserializes and reconstructs a ThrowDescriptorClass object from the reader stream.
    /// </summary>
    public static ThrowDescriptorClass GetEFTThrowOperationDescriptor(this NetDataReader reader)
    {
        var throwDescriptorClass = new ThrowDescriptorClass
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            ItemId = reader.GetString(),
            DownDirection = reader.GetBool()
        };
        if (reader.GetBool())
        {
            var num = reader.GetInt();
            throwDescriptorClass.DestroyedItems = new List<GClass1955>(num);
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
    public static void PutEFTTogglableComponentDescriptor(this NetDataWriter writer, GClass1934 target)
    {
        writer.Put(target.IsOn);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1934 object from the reader stream.
    /// </summary>
    public static GClass1934 GetEFTTogglableComponentDescriptor(this NetDataReader reader)
    {
        return new GClass1934
        {
            IsOn = reader.GetBool()
        };
    }

    /// <summary>
    /// Serializes an inventory item toggle action operation descriptor into the writer stream.
    /// </summary>
    public static void PutEFTToggleOperationDescriptor(this NetDataWriter writer, GClass1986 target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.ItemId);
        writer.Put(target.Value);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1986 object from the reader stream.
    /// </summary>
    public static GClass1986 GetEFTToggleOperationDescriptor(this NetDataReader reader)
    {
        return new GClass1986
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
    public static void PutEFTTraderInfoDescriptor(this NetDataWriter writer, TraderInfoClass target)
    {
        writer.Put(target.Unlocked);
        writer.Put(target.LoyaltyLevel);
        writer.Put(target.SalesSum);
        writer.Put(target.Standing);
        writer.Put(target.NextResupply);
        writer.Put(target.Disabled);
    }

    /// <summary>
    /// Deserializes and reconstructs a TraderInfoClass object from the reader stream.
    /// </summary>
    public static TraderInfoClass GetEFTTraderInfoDescriptor(this NetDataReader reader)
    {
        return new TraderInfoClass
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
    public static void PutEFTTraderServiceAvailabilityData(this NetDataWriter writer, TraderServicesClass target)
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
    /// Deserializes and reconstructs a TraderServicesClass object from the reader stream.
    /// </summary>
    public static TraderServicesClass GetEFTTraderServiceAvailabilityData(this NetDataReader reader)
    {
        var traderServicesClass = new TraderServicesClass
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
    public static void PutEFTTransferOperationDescriptor(this NetDataWriter writer, GClass1987 target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.ItemId);
        writer.Put(target.Item1Id);
        writer.Put(target.Count);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1987 object from the reader stream.
    /// </summary>
    public static GClass1987 GetEFTTransferOperationDescriptor(this NetDataReader reader)
    {
        return new GClass1987
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
    public static void PutEFTUnbindItemOperationDescriptor(this NetDataWriter writer, GClass1988 target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.ItemId);
        writer.PutEnum(target.Index);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1988 object from the reader stream.
    /// </summary>
    public static GClass1988 GetEFTUnbindItemOperationDescriptor(this NetDataReader reader)
    {
        return new GClass1988
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
    public static void PutEFTUnloadMagOperationDescriptor(this NetDataWriter writer, GClass1973 target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.PutPolymorph(target.InternalOperationDescriptor);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1973 object from the reader stream.
    /// </summary>
    public static GClass1973 GetEFTUnloadMagOperationDescriptor(this NetDataReader reader)
    {
        return new GClass1973
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            InternalOperationDescriptor = reader.GetPolymorph<BaseDescriptorClass>()
        };
    }

    /// <summary>
    /// Serializes kill tracking victim profile context attributes and range metrics logs into the writer stream.
    /// </summary>
    public static void PutEFTVictimStats(this NetDataWriter writer, GClass2201 target)
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
    /// Deserializes and reconstructs a GClass2201 object from the reader stream.
    /// </summary>
    public static GClass2201 GetEFTVictimStats(this NetDataReader reader)
    {
        return new GClass2201
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
    public static void PutEFTWeaponRechamberOperationDescriptor(this NetDataWriter writer, GClass1989 target)
    {
        writer.Put(target.OperationId);
        writer.PutMongoID(target.OwnerId);
        writer.Put(target.WeaponId);
    }

    /// <summary>
    /// Deserializes and reconstructs a GClass1989 object from the reader stream.
    /// </summary>
    public static GClass1989 GetEFTWeaponRechamberOperationDescriptor(this NetDataReader reader)
    {
        return new GClass1989
        {
            OperationId = reader.GetUShort(),
            OwnerId = reader.GetMongoID(),
            WeaponId = reader.GetString()
        };
    }

    /// <summary>
    /// Serializes item identity string associations for insurance tracking records into the writer stream.
    /// </summary>
    public static void PutJsonTypeInsuredProfileItems(this NetDataWriter writer, InsuredItemClass target)
    {
        writer.Put(target.TraderId);
        writer.Put(target.ItemId);
    }

    /// <summary>
    /// Deserializes and reconstructs an InsuredItemClass object from the reader stream.
    /// </summary>
    public static InsuredItemClass GetJsonTypeInsuredProfileItems(this NetDataReader reader)
    {
        return new InsuredItemClass
        {
            TraderId = reader.GetString(),
            ItemId = reader.GetString()
        };
    }

    /// <summary>
    /// Serializes visual identity parameters and comprehensive health state values for player profile models into the writer stream.
    /// </summary>
    public static void PutJsonTypePlayerInfo(this NetDataWriter writer, GClass1410 target)
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
    /// Deserializes and reconstructs a GClass1410 object from the reader stream.
    /// </summary>
    public static GClass1410 GetJsonTypePlayerInfo(this NetDataReader reader)
    {
        return new GClass1410
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
