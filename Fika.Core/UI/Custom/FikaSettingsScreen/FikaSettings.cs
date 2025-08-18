using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using EFT.InputSystem;
using EFT.UI;
using EFT.UI.Settings;
using Fika.Core.Main.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine.UI;

namespace Fika.Core.UI.Custom.FikaSettingsScreen;

public class FikaSettings : SettingsTab
{
    public static FikaSettings Instance { get; internal set; }

    private static readonly FieldInfo _floatSliderChildren = typeof(SettingFloatSlider)
                .GetField("_children", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo _numberSlider = typeof(SettingFloatSlider)
            .GetField("Slider", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo _updatableToggle = typeof(SettingToggle)
            .GetField("Toggle", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo _dropdownBox = typeof(SettingDropDown)
            .GetField("DropDown", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo _uiList = typeof(SettingControl)
            .GetField("UI", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo _localizedText = typeof(SettingControl)
            .GetField("Text", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly KeyCode[] _keysToCheck = [.. UnityInput.Current.SupportedKeyCodes.Except([KeyCode.Mouse0, KeyCode.Mouse1])];

    private SettingControl[] _advancedSettings = [];

    private SettingDropDown _dropDownPrefab;
    private SettingSelectSlider _intSliderPrefab;
    private SettingFloatSlider _floatSliderPrefab;
    private SettingToggle _togglePrefab;
    private GameObject _textInputPrefab;

    private ScrollRect _scrollRect;

    private KeybindHandler _currentKeybindHandler;

    private SettingToggle _advancedToggle;
    private bool _showAdvanced;

    private SettingToggle _autoUseHeadless;
    private SettingToggle _showNotifications;
    private SettingToggle _autoExtract;
    private SettingToggle _showExtractMessage;
    private GameObject _extractKey;
    private KeybindHandler _extractKeyHandler;
    private SettingToggle _enableChat;
    private GameObject _chatKey;
    private KeybindHandler _chatKeyHandler;
    private SettingToggle _onlinePlayers;
    private SettingFloatSlider _onlinePlayersScale;

    private SettingToggle _useNamePlates;
    private SettingToggle _hideHealthBar;
    private SettingToggle _useHealthNumber;
    private SettingToggle _showEffects;
    private SettingToggle _usePlateFactionSide;
    private SettingToggle _hideNamePlateInOptic;
    private SettingToggle _namePlateUseOpticZoom;
    private SettingToggle _decreaseOpacityNotLookingAt;
    private SettingFloatSlider _namePlateScale;
    private SettingFloatSlider _opacityInAds;
    private SettingFloatSlider _maxDistanceToShow;
    private SettingFloatSlider _minimumOpacity;
    private SettingFloatSlider _minimumNamePlateScale;
    private SettingToggle _useOcclusion;
    private GameObject _fullHealthColor;
    private GameObject _lowHealthColor;
    private GameObject _namePlateTextColor;

    private SettingToggle _questSharingKills;
    private SettingToggle _questSharingItem;
    private SettingToggle _questSharingLocation;
    private SettingToggle _questSharingPlaceBeacon;
    /*private SettingToggle _questSharingAll;*/
    private SettingToggle _questSharingNotifications;
    private SettingToggle _easyKillConditions;
    private SettingToggle _sharedKillExperience;
    private SettingToggle _sharedBossExperience;

    private SettingToggle _usePingSystem;
    private GameObject _pingButton;
    private KeybindHandler _pingButtonHandler;
    private GameObject _pingColor;
    private SettingFloatSlider _pingSize;
    private SettingFloatSlider _pingTime;
    private SettingToggle _playPingAnimation;
    private SettingToggle _showPingDuringOptics;
    private SettingToggle _pingUseOpticZoom;
    private SettingToggle _pingScaleWithDistance;
    private SettingFloatSlider _pingMinimumOpacity;
    private SettingToggle _showPingRange;
    private SettingDropDown _pingSound;
    private GameObject _freecamButton;
    private KeybindHandler _freecamButtonHandler;
    private SettingToggle _allowSpectateBots;
    private SettingToggle _azertyMode;
    private SettingToggle _droneMode;
    private SettingToggle _keybindOverlay;

    private GameObject _forceIp;
    private TextFieldHandler _forceIpHandler;
    private SettingDropDown _bindIp;
    private SettingFloatSlider _udpPort;
    private SettingToggle _useUpnp;
    private SettingToggle _useNatPunching;
    private SettingFloatSlider _connectionTimeout;
    private SettingDropDown _sendRate;
    private SettingToggle _allowVoip;

    private SettingToggle _disableBotMetabolism;

    public static FikaSettings Create(GameObject parent, SettingDropDown dropdownPrefab,
        SettingSelectSlider selectSlider, SettingFloatSlider sliderPrefab, SettingToggle togglePrefab,
        GameSettingsTab gameSettingsScreen)
    {
        FikaSettings fikaSettings = parent.AddComponent<FikaSettings>();
        fikaSettings._dropDownPrefab = dropdownPrefab;
        fikaSettings._intSliderPrefab = selectSlider;
        fikaSettings._floatSliderPrefab = sliderPrefab;
        fikaSettings._togglePrefab = togglePrefab;
        fikaSettings._textInputPrefab = gameSettingsScreen.gameObject.transform.GetChild(0)
            .GetChild(0).gameObject;
        return fikaSettings;
    }

    internal void Init(Transform content)
    {
        RectTransform parent = (RectTransform)content.parent;
        parent.sizeDelta = new(-100f, 0f);
        parent.offsetMin = new(-75f, 0f);
        RectTransform scrollBar = (RectTransform)parent.parent.GetChild(1).transform;
        scrollBar.anchoredPosition = Vector2.zero;

        List<SettingControl> advancedOptions = [];

        RectTransform section1 = CreateSubSection(content);
        _autoUseHeadless = CreateToggle(section1);
        _showNotifications = CreateToggle(section1);
        RectTransform section2 = CreateSubSection(content);
        _autoExtract = CreateToggle(section2);
        _showExtractMessage = CreateToggle(section2);
        _extractKey = CreateKeybind(content);
        RectTransform section3 = CreateSubSection(content);
        _enableChat = CreateToggle(section3);
        _chatKey = CreateKeybind(content);
        _onlinePlayers = CreateToggle(section3);
        _onlinePlayersScale = CreateFloatSlider(content);

        RectTransform section4 = CreateSubSection(content);
        _useNamePlates = CreateToggle(section4);
        _hideHealthBar = CreateToggle(section4);
        RectTransform section5 = CreateSubSection(content);
        _useHealthNumber = CreateToggle(section5);
        _showEffects = CreateToggle(section5);
        RectTransform section6 = CreateSubSection(content);
        _usePlateFactionSide = CreateToggle(section6);
        _hideNamePlateInOptic = CreateToggle(section6);
        RectTransform section7 = CreateSubSection(content);
        _namePlateUseOpticZoom = CreateToggle(section7);
        advancedOptions.Add(_namePlateUseOpticZoom);
        _decreaseOpacityNotLookingAt = CreateToggle(section7);
        _namePlateScale = CreateFloatSlider(content);
        _opacityInAds = CreateFloatSlider(content);
        _maxDistanceToShow = CreateFloatSlider(content);
        _minimumOpacity = CreateFloatSlider(content);
        _minimumNamePlateScale = CreateFloatSlider(content);
        _useOcclusion = CreateToggle(content);
        _fullHealthColor = CreateColor(content);
        _lowHealthColor = CreateColor(content);
        _namePlateTextColor = CreateColor(content);

        RectTransform section8 = CreateSubSection(content);
        _questSharingKills = CreateToggle(section8);
        _questSharingItem = CreateToggle(section8);
        RectTransform section9 = CreateSubSection(content);
        _questSharingLocation = CreateToggle(section9);
        _questSharingPlaceBeacon = CreateToggle(section9);
        RectTransform section10 = CreateSubSection(content);
        //_questSharingAll = CreateToggle(content);
        _questSharingNotifications = CreateToggle(section10);
        _easyKillConditions = CreateToggle(section10);
        RectTransform section11 = CreateSubSection(content);
        _sharedKillExperience = CreateToggle(section11);
        _sharedBossExperience = CreateToggle(section11);

        _usePingSystem = CreateToggle(content);
        _pingButton = CreateKeybind(content);
        _pingColor = CreateColor(content);
        _pingSize = CreateFloatSlider(content);
        _pingTime = CreateFloatSlider(content);
        RectTransform section12 = CreateSubSection(content);
        _playPingAnimation = CreateToggle(section12);
        _showPingDuringOptics = CreateToggle(section12);
        RectTransform section13 = CreateSubSection(content);
        _pingUseOpticZoom = CreateToggle(section13);
        advancedOptions.Add(_pingUseOpticZoom);
        _pingScaleWithDistance = CreateToggle(section13);
        advancedOptions.Add(_pingScaleWithDistance);
        _pingMinimumOpacity = CreateFloatSlider(content);
        advancedOptions.Add(_pingMinimumOpacity);
        _showPingRange = CreateToggle(content);
        _pingSound = CreateDropDown(content);

        _freecamButton = CreateKeybind(content);
        RectTransform section14 = CreateSubSection(content);
        _allowSpectateBots = CreateToggle(section14);
        _azertyMode = CreateToggle(section14);
        RectTransform section15 = CreateSubSection(content);
        _droneMode = CreateToggle(section15);
        _keybindOverlay = CreateToggle(section15);

        _forceIp = CreateTextField(content);
        _bindIp = CreateDropDown(content);
        _udpPort = CreateFloatSlider(content);
        _useUpnp = CreateToggle(content);
        advancedOptions.Add(_useUpnp);
        _useNatPunching = CreateToggle(content);
        advancedOptions.Add(_useNatPunching);
        _connectionTimeout = CreateFloatSlider(content);
        _sendRate = CreateDropDown(content);
        _allowVoip = CreateToggle(content);

        _disableBotMetabolism = CreateToggle(content);

        _advancedSettings = [.. advancedOptions];

        Transform root = transform.GetChild(0);
        _scrollRect = root.GetChild(0).GetComponent<ScrollRect>();

        Transform bottomSection = root.GetChild(2);
        Transform newBottomSection = GameObject.Instantiate(bottomSection, root);
        for (int i = newBottomSection.childCount - 1; i >= 0; i--)
        {
            Destroy(newBottomSection.GetChild(i).gameObject);
        }
        _advancedToggle = CreateToggle(newBottomSection);
        GameObject.Destroy(bottomSection.gameObject);
        GameObject.Destroy(root.GetChild(3).gameObject);
    }

    private GameObject CreateKeybind(Transform parent)
    {
        GameObject textField = GameObject.Instantiate(_textInputPrefab, parent);
        textField.AddComponent<LayoutElement>().minHeight = 50f;
        return textField;
    }

    private RectTransform CreateSubSection(Transform parent, bool createLayout = true)
    {
        GameObject newSection = new("SubSection", typeof(RectTransform));
        if (createLayout)
        {
            HorizontalLayoutGroup horizontalGroup = newSection.AddComponent<HorizontalLayoutGroup>();
            horizontalGroup.childAlignment = TextAnchor.MiddleCenter;
            horizontalGroup.spacing = 5f;
        }
        RectTransform rectTransform = (RectTransform)newSection.transform;
        rectTransform.SetParent(parent);
        rectTransform.localScale = Vector3.one;
        return rectTransform;
    }

    public void Show()
    {
        SetupAdvancedToggle();

        SetupToggle(_autoUseHeadless, FikaPlugin.UseHeadlessIfAvailable);
        SetupToggle(_showNotifications, FikaPlugin.ShowNotifications);
        SetupToggle(_autoExtract, FikaPlugin.AutoExtract);
        SetupToggle(_showExtractMessage, FikaPlugin.ShowExtractMessage);
        _extractKeyHandler = SetupKeybind(_extractKey, FikaPlugin.ExtractKey);
        SetupToggle(_enableChat, FikaPlugin.EnableChat);
        _chatKeyHandler = SetupKeybind(_chatKey, FikaPlugin.ChatKey);
        SetupToggle(_onlinePlayers, FikaPlugin.EnableOnlinePlayers);
        SetupFloatSlider(_onlinePlayersScale, FikaPlugin.OnlinePlayersScale, "F1");

        SetupToggle(_useNamePlates, FikaPlugin.UseNamePlates);
        SetupToggle(_hideHealthBar, FikaPlugin.HideHealthBar);
        SetupToggle(_useHealthNumber, FikaPlugin.UseHealthNumber);
        SetupToggle(_showEffects, FikaPlugin.ShowEffects);
        SetupToggle(_usePlateFactionSide, FikaPlugin.UsePlateFactionSide);
        SetupToggle(_hideNamePlateInOptic, FikaPlugin.HideNamePlateInOptic);
        SetupToggle(_namePlateUseOpticZoom, FikaPlugin.NamePlateUseOpticZoom);
        SetupToggle(_decreaseOpacityNotLookingAt, FikaPlugin.DecreaseOpacityNotLookingAt);
        SetupFloatSlider(_namePlateScale, FikaPlugin.NamePlateScale, "F2");
        SetupFloatSlider(_opacityInAds, FikaPlugin.OpacityInADS, "F2");
        SetupFloatSlider(_maxDistanceToShow, FikaPlugin.MaxDistanceToShow);
        SetupFloatSlider(_minimumOpacity, FikaPlugin.MinimumOpacity, "F2");
        SetupFloatSlider(_minimumNamePlateScale, FikaPlugin.MinimumNamePlateScale, "F2");
        SetupToggle(_useOcclusion, FikaPlugin.UseOcclusion);
        SetupRGBSlider(_fullHealthColor, FikaPlugin.FullHealthColor);
        SetupRGBSlider(_lowHealthColor, FikaPlugin.LowHealthColor);
        SetupRGBSlider(_namePlateTextColor, FikaPlugin.NamePlateTextColor);

        SetupToggle(_questSharingKills, FikaPlugin.QuestTypesToShareAndReceive, FikaPlugin.EQuestSharingTypes.Kills);
        SetupToggle(_questSharingItem, FikaPlugin.QuestTypesToShareAndReceive, FikaPlugin.EQuestSharingTypes.Item);
        SetupToggle(_questSharingLocation, FikaPlugin.QuestTypesToShareAndReceive, FikaPlugin.EQuestSharingTypes.Location);
        SetupToggle(_questSharingPlaceBeacon, FikaPlugin.QuestTypesToShareAndReceive, FikaPlugin.EQuestSharingTypes.PlaceBeacon);
        //SetupToggle(_questSharingAll, FikaPlugin.QuestTypesToShareAndReceive, FikaPlugin.EQuestSharingTypes.All);
        SetupToggle(_questSharingNotifications, FikaPlugin.QuestSharingNotifications);
        SetupToggle(_easyKillConditions, FikaPlugin.EasyKillConditions);
        SetupToggle(_sharedKillExperience, FikaPlugin.SharedKillExperience);
        SetupToggle(_sharedBossExperience, FikaPlugin.SharedBossExperience);

        _pingButtonHandler = SetupKeybind(_pingButton, FikaPlugin.PingButton);
        SetupToggle(_usePingSystem, FikaPlugin.UsePingSystem);
        SetupRGBSlider(_pingColor, FikaPlugin.PingColor);
        SetupFloatSlider(_pingSize, FikaPlugin.PingSize, "F2");
        SetupIntSlider(_pingTime, FikaPlugin.PingTime);
        SetupToggle(_playPingAnimation, FikaPlugin.PlayPingAnimation);
        SetupToggle(_showPingDuringOptics, FikaPlugin.ShowPingDuringOptics);
        SetupToggle(_pingUseOpticZoom, FikaPlugin.PingUseOpticZoom);
        SetupToggle(_pingScaleWithDistance, FikaPlugin.PingScaleWithDistance);
        SetupFloatSlider(_pingMinimumOpacity, FikaPlugin.PingMinimumOpacity, "F2");
        SetupToggle(_showPingRange, FikaPlugin.ShowPingRange);
        SetupDropDown(_pingSound, FikaPlugin.PingSound);

        _freecamButtonHandler = SetupKeybind(_freecamButton, FikaPlugin.FreeCamButton);
        SetupToggle(_allowSpectateBots, FikaPlugin.AllowSpectateBots);
        SetupToggle(_azertyMode, FikaPlugin.AZERTYMode);
        SetupToggle(_droneMode, FikaPlugin.DroneMode);
        SetupToggle(_keybindOverlay, FikaPlugin.KeybindOverlay);

        _forceIpHandler = SetupTextField(_forceIp, FikaPlugin.ForceIP);
        SetupDropDown(_bindIp, FikaPlugin.ForceBindIP);
        SetupUshortSlider(_udpPort, FikaPlugin.UDPPort);
        SetupToggle(_useUpnp, FikaPlugin.UseUPnP);
        SetupToggle(_useNatPunching, FikaPlugin.UseNatPunching);
        SetupIntSlider(_connectionTimeout, FikaPlugin.ConnectionTimeout);
        SetupDropDown(_sendRate, FikaPlugin.SendRate);
        SetupToggle(_allowVoip, FikaPlugin.AllowVOIP);

        SetupToggle(_disableBotMetabolism, FikaPlugin.DisableBotMetabolism);

        // reset scroll
        _scrollRect.verticalNormalizedPosition = 1f;
        RefreshAdvanced();
    }

    private void SetupAdvancedToggle()
    {
        InitSetting(_advancedToggle, "Show Advanced Settings");

        _advancedToggle.GetOrCreateTooltip()
            .SetMessageText("If advanced user settings should be shown.");

        UpdatableToggle updatableToggle = (UpdatableToggle)_updatableToggle.GetValue(_advancedToggle);

        updatableToggle.UpdateValue(_showAdvanced);
        updatableToggle.Bind(value =>
        {
            _showAdvanced = value;
            RefreshAdvanced();
        });
    }

    private void RefreshAdvanced()
    {
        for (int i = 0; i < _advancedSettings.Length; i++)
        {
            _advancedSettings[i].gameObject.SetActive(_showAdvanced);
        }
    }

    private KeybindHandler SetupKeybind(GameObject textField, ConfigEntry<KeyboardShortcut> configEntry)
    {
        GameObject.Destroy(textField.GetComponent<Image>());
        textField.transform.GetChild(0).gameObject.SetActive(false);
        RectTransform label = (RectTransform)textField.transform.GetChild(2);
        label.sizeDelta = new(-490f, 60f);
        label.GetComponent<LocalizedText>().LocalizationKey = configEntry.Definition.Key;
        CustomTextMeshProUGUI tmpText = label.gameObject.GetComponent<CustomTextMeshProUGUI>();
        tmpText.text = configEntry.Definition.Key;
        tmpText.raycastTarget = false;

        Transform inputField = textField.transform.GetChild(1);
        inputField.RectTransform().sizeDelta = new(-600f, 50f);
        ValidationInputField validation = inputField.gameObject.GetComponent<ValidationInputField>();
        DefaultUIButton button = inputField.GetChild(1)
            .GetComponent<DefaultUIButton>();
        KeybindHandler handler = new(validation, button, configEntry, this);
        HoverTooltipArea tooltip = inputField.GetChild(0).gameObject.GetComponent<HoverTooltipArea>()
            ?? inputField.GetChild(0).gameObject.AddComponent<HoverTooltipArea>();
        tooltip.SetMessageText(configEntry.Description.Description);

        UI.AddDisposable(handler.Dispose);
        return handler;
    }

    public override void OnTabSelected()
    {
        Show();
    }

    public override Task TakeSettingsFrom(SharedGameSettingsClass settingsManager)
    {
        return Task.CompletedTask;
    }

    private void InitSetting<T>(SettingControl control, ConfigEntry<T> configEntry)
    {
        LocalizedText value = (LocalizedText)_localizedText.GetValue(control);
        AddViewListClass ui = (AddViewListClass)_uiList.GetValue(control);

        ui.Dispose();
        value.LocalizationKey = configEntry.Definition.Key; // TODO: Use description.tags
        value.method_2(configEntry.Definition.Key);
    }

    private void InitSetting(SettingControl control, string text)
    {
        LocalizedText value = (LocalizedText)_localizedText.GetValue(control);
        AddViewListClass ui = (AddViewListClass)_uiList.GetValue(control);

        ui.Dispose();
        value.LocalizationKey = text;
        value.method_2(text);
    }

    private void BindDropDown(SettingDropDown dropDown, ConfigEntry<string> configEntry, Func<int, bool> validator = null)
    {
        InitSetting(dropDown, configEntry);

        DropDownBox dropDownBox = (DropDownBox)_dropdownBox.GetValue(dropDown);
        AddViewListClass ui = (AddViewListClass)_uiList.GetValue(dropDown);

        if (configEntry.Description.AcceptableValues is AcceptableValueList<string> list)
        {
            dropDownBox.Show(list.AcceptableValues, validator);
            int curIndex = GetIndex(configEntry, list);
            dropDownBox.UpdateValue(curIndex);
            ui.AddDisposable(() =>
            {
                int ind = GetIndex(configEntry, list);
                dropDownBox.UpdateValue(curIndex);
            });
            ui.SubscribeEvent(dropDownBox.OnValueChanged,
                value => configEntry.Value = list.AcceptableValues[value]);

            UI.AddDisposable(dropDown.Close);
        }
        else
        {
            FikaGlobals.LogError("AcceptableValues was not of type string");
        }
    }

    private void BindDropDown<TEnum>(SettingDropDown dropDown, ConfigEntry<TEnum> configEntry, Func<int, bool> validator = null)
        where TEnum : struct, Enum
    {
        InitSetting(dropDown, configEntry);

        DropDownBox dropDownBox = (DropDownBox)_dropdownBox.GetValue(dropDown);
        AddViewListClass ui = (AddViewListClass)_uiList.GetValue(dropDown);

        TEnum[] enumValues = (TEnum[])Enum.GetValues(typeof(TEnum));
        string[] enumNames = [.. enumValues.Select(e => e.ToString())];

        dropDownBox.Show(enumNames, validator);

        int curIndex = Array.IndexOf(enumValues, configEntry.Value);
        dropDownBox.UpdateValue(curIndex);

        ui.AddDisposable(() =>
        {
            int index = Array.IndexOf(enumValues, configEntry.Value);
            dropDownBox.UpdateValue(index);
        });

        ui.SubscribeEvent(dropDownBox.OnValueChanged, value =>
        {
            if (value >= 0 && value < enumValues.Length)
            {
                configEntry.Value = enumValues[value];
            }
        });

        UI.AddDisposable(dropDown.Close);
    }


    public SettingDropDown CreateDropDown(Transform parent)
    {
        return CreateControl(_dropDownPrefab, parent);
    }

    public SettingSelectSlider CreateSelectSlider(Transform parent)
    {
        return CreateControl(_intSliderPrefab, parent);
    }

    public SettingFloatSlider CreateFloatSlider(Transform parent)
    {
        return CreateControl(_floatSliderPrefab, parent);
    }

    public SettingToggle CreateToggle(Transform parent)
    {
        return CreateControl(_togglePrefab, parent);
    }

    public GameObject CreateTextField(Transform parent)
    {
        GameObject textField = GameObject.Instantiate(_textInputPrefab, parent);
        textField.AddComponent<LayoutElement>().minHeight = 50f;
        return textField;
    }

    private GameObject CreateColor(Transform parent)
    {
        RectTransform subSection = CreateSubSection(parent);
        HorizontalLayoutGroup horizontalGroup = subSection.GetComponent<HorizontalLayoutGroup>();
        horizontalGroup.childForceExpandHeight = false;
        horizontalGroup.childForceExpandWidth = false;
        //horizontalGroup.spacing = -100f; // move sliders closer to image

        RectTransform sliderSection = CreateSubSection(subSection, false);
        sliderSection.gameObject.name = "RGBSliders";
        VerticalLayoutGroup verticalLayout = sliderSection.gameObject.AddComponent<VerticalLayoutGroup>();
        verticalLayout.spacing = 5f;
        verticalLayout.childAlignment = TextAnchor.MiddleLeft;
        GameObject label = GameObject.Instantiate(_togglePrefab.transform.GetChild(1).gameObject, sliderSection);
        label.AddComponent<LayoutElement>();
        label.GetComponent<CustomTextMeshProUGUI>().alignment = TMPro.TextAlignmentOptions.Center;
        CreateFloatSlider(sliderSection);
        CreateFloatSlider(sliderSection);
        CreateFloatSlider(sliderSection);

        RectTransform imageSection = CreateSubSection(subSection);
        imageSection.gameObject.name = "Image";
        Image image = imageSection.gameObject.AddComponent<Image>();
        image.color = Color.white;
        LayoutElement layoutElement = imageSection.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 96f;
        layoutElement.preferredWidth = 96f;
        layoutElement.flexibleHeight = 0f;
        layoutElement.flexibleWidth = 0f;

        return subSection.gameObject;
    }

    private TextFieldHandler SetupTextField(GameObject textField, ConfigEntry<string> configEntry)
    {
        GameObject.Destroy(textField.GetComponent<Image>());
        textField.transform.GetChild(0).gameObject.SetActive(false);
        RectTransform label = (RectTransform)textField.transform.GetChild(2);
        label.sizeDelta = new(-490f, 60f);
        label.GetComponent<LocalizedText>().LocalizationKey = configEntry.Definition.Key;
        CustomTextMeshProUGUI tmpText = label.gameObject.GetComponent<CustomTextMeshProUGUI>();
        tmpText.text = configEntry.Definition.Key;
        tmpText.raycastTarget = false;

        Transform inputField = textField.transform.GetChild(1);
        inputField.RectTransform().sizeDelta = new(-600f, 50f);
        ValidationInputField validation = inputField.gameObject.GetComponent<ValidationInputField>();
        DefaultUIButton button = inputField.GetChild(1)
            .GetComponent<DefaultUIButton>();
        TextFieldHandler handler = new(validation, button, configEntry);
        HoverTooltipArea tooltip = inputField.GetChild(0).gameObject.GetComponent<HoverTooltipArea>()
            ?? inputField.GetChild(0).gameObject.AddComponent<HoverTooltipArea>();
        tooltip.SetMessageText(configEntry.Description.Description);

        UI.AddDisposable(handler.Dispose);
        return handler;
    }

    public void SetupDropDown(SettingDropDown dropDown, ConfigEntry<string> configEntry, Func<int, bool> validator = null)
    {
        dropDown.GetOrCreateTooltip()
            .SetMessageText(configEntry.Description.Description);

        BindDropDown(dropDown, configEntry, validator);
    }

    public void SetupDropDown<TEnum>(SettingDropDown dropDown, ConfigEntry<TEnum> configEntry, Func<int, bool> validator = null)
        where TEnum : struct, Enum
    {
        dropDown.GetOrCreateTooltip()
            .SetMessageText(configEntry.Description.Description);

        BindDropDown(dropDown, configEntry, validator);
    }

    public void SetupToggle(SettingToggle toggle, ConfigEntry<bool> configEntry)
    {
        InitSetting(toggle, configEntry);

        toggle.GetOrCreateTooltip()
            .SetMessageText(configEntry.Description.Description);

        UpdatableToggle updatableToggle = (UpdatableToggle)_updatableToggle.GetValue(toggle);

        updatableToggle.UpdateValue(configEntry.Value);
        updatableToggle.Bind(value => configEntry.Value = value);
    }

    public void SetupToggle<TEnum>(SettingToggle toggle, ConfigEntry<TEnum> configEntry, TEnum flag)
        where TEnum : struct, Enum
    {
        InitSetting(toggle, flag.ToString());
        toggle.GetOrCreateTooltip()
              .SetMessageText(configEntry.Description.Description);

        UpdatableToggle updatableToggle = (UpdatableToggle)_updatableToggle.GetValue(toggle);

        bool initialState = configEntry.Value.HasFlag(flag);
        updatableToggle.UpdateValue(initialState);

        updatableToggle.Bind(value =>
        {
            long currentValue = Convert.ToInt64(configEntry.Value);
            long flagValue = Convert.ToInt64(flag);

            if (value)
            {
                currentValue |= flagValue;
            }
            else
            {
                currentValue &= ~flagValue;
            }

            configEntry.Value = (TEnum)Enum.ToObject(typeof(TEnum), currentValue);
        });
    }

    private void SetupFloatSlider(SettingFloatSlider slider, ConfigEntry<float> configEntry, string format = "0")
    {
        InitSetting(slider, configEntry);

        slider.GetOrCreateTooltip()
            .SetMessageText(configEntry.Description.Description);

        NumberSlider numberSlider = (NumberSlider)_numberSlider.GetValue(slider);

        if (configEntry.Description.AcceptableValues is AcceptableValueRange<float> floatRange)
        {
            numberSlider.Show(floatRange.MinValue, floatRange.MaxValue, format);
            numberSlider.UpdateValue(configEntry.Value);
            numberSlider.Bind(value => configEntry.Value = value);

            List<InputNode> children = (List<InputNode>)_floatSliderChildren.GetValue(slider);

            children.Add(numberSlider);
        }
        else
        {
            FikaGlobals.LogError("AcceptableValues was not of type float!");
        }
    }

    private void SetupUshortSlider(SettingFloatSlider slider, ConfigEntry<ushort> configEntry)
    {
        InitSetting(slider, configEntry);

        slider.GetOrCreateTooltip()
            .SetMessageText(configEntry.Description.Description);

        NumberSlider numberSlider = (NumberSlider)_numberSlider.GetValue(slider);

        if (configEntry.Description.AcceptableValues is AcceptableValueRange<ushort> intRange)
        {
            numberSlider.Show(intRange.MinValue, intRange.MaxValue, "0");
            numberSlider.UpdateValue(configEntry.Value);
            numberSlider.Bind(value => configEntry.Value = (ushort)value);

            List<InputNode> children = (List<InputNode>)_floatSliderChildren.GetValue(slider);

            children.Add(numberSlider);
        }
        else
        {
            FikaGlobals.LogError("AcceptableValues was not of type ushort!");
        }
    }

    private void SetupIntSlider(SettingFloatSlider slider, ConfigEntry<int> configEntry)
    {
        InitSetting(slider, configEntry);

        slider.GetOrCreateTooltip()
            .SetMessageText(configEntry.Description.Description);

        NumberSlider numberSlider = (NumberSlider)_numberSlider.GetValue(slider);

        if (configEntry.Description.AcceptableValues is AcceptableValueRange<int> intRange)
        {
            numberSlider.Show(intRange.MinValue, intRange.MaxValue, "0");
            numberSlider.UpdateValue(configEntry.Value);
            numberSlider.Bind(value => configEntry.Value = (int)value);

            List<InputNode> children = (List<InputNode>)_floatSliderChildren.GetValue(slider);

            children.Add(numberSlider);
        }
        else
        {
            FikaGlobals.LogError("AcceptableValues was not of type int!");
        }
    }

    private void SetupRGBSlider(GameObject sliderObject, ConfigEntry<Color> configEntry)
    {
        Transform sliderContainer = sliderObject.transform.GetChild(0);
        sliderContainer.GetChild(0)
            .GetComponent<LocalizedText>()
            .method_2(configEntry.Definition.Key);

        sliderContainer.GetChild(0).gameObject
            .GetOrAddComponent<HoverTooltipArea>()
            .SetMessageText(configEntry.Description.Description);

        Image image = sliderObject.transform.GetChild(1)
            .GetComponent<Image>();

        SettingFloatSlider redSlider = sliderContainer.transform.GetChild(1)
            .GetComponent<SettingFloatSlider>();

        InitSetting(redSlider, "Red");
        redSlider.GetOrCreateTooltip()
            .SetMessageText("Red value");

        NumberSlider numberSliderRed = (NumberSlider)typeof(SettingFloatSlider)
            .GetField("Slider", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(redSlider);

        numberSliderRed.Show(0f, 1f, "F2");
        numberSliderRed.UpdateValue(configEntry.Value.r);
        numberSliderRed.Bind(value =>
        {
            configEntry.Value = new(value, configEntry.Value.g, configEntry.Value.b);
            image.color = configEntry.Value;
        });

        List<InputNode> childrenRed = (List<InputNode>)_floatSliderChildren.GetValue(redSlider);
        childrenRed.Add(numberSliderRed);

        SettingFloatSlider greenSlider = sliderContainer.transform.GetChild(2)
            .GetComponent<SettingFloatSlider>();

        InitSetting(greenSlider, "Green");
        greenSlider.GetOrCreateTooltip()
            .SetMessageText("Green value");

        NumberSlider numberSliderGreen = (NumberSlider)typeof(SettingFloatSlider)
            .GetField("Slider", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(greenSlider);

        numberSliderGreen.Show(0f, 1f, "F2");
        numberSliderGreen.UpdateValue(configEntry.Value.g);
        numberSliderGreen.Bind(value =>
        {
            configEntry.Value = new(configEntry.Value.r, value, configEntry.Value.b);
            image.color = configEntry.Value;
        });

        List<InputNode> childrenGreen = (List<InputNode>)_floatSliderChildren.GetValue(greenSlider);
        childrenGreen.Add(numberSliderGreen);

        SettingFloatSlider blueSlider = sliderContainer.transform.GetChild(3)
            .GetComponent<SettingFloatSlider>();

        InitSetting(blueSlider, "Blue");
        blueSlider.GetOrCreateTooltip()
            .SetMessageText("Blue value");

        NumberSlider numberSliderBlue = (NumberSlider)typeof(SettingFloatSlider)
            .GetField("Slider", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(blueSlider);

        numberSliderBlue.Show(0f, 1f, "F2");
        numberSliderBlue.UpdateValue(configEntry.Value.b);
        numberSliderBlue.Bind(value =>
        {
            configEntry.Value = new(configEntry.Value.r, configEntry.Value.g, value);
            image.color = configEntry.Value;
        });

        List<InputNode> childrenBlue = (List<InputNode>)_floatSliderChildren.GetValue(blueSlider);
        childrenBlue.Add(numberSliderBlue);

        image.color = configEntry.Value;
    }

    public override void Close()
    {
        CancelBind();
        base.Close();
    }

    public void CancelBind()
    {
        if (_currentKeybindHandler != null)
        {
            _currentKeybindHandler.StopRoutine();
            _currentKeybindHandler = null;
        }
    }

    private static int GetIndex(ConfigEntry<string> configEntry, AcceptableValueList<string> list)
    {
        string curValue = configEntry.Value;
        int curIndex = 0;
        for (int i = 0; i < list.AcceptableValues.Length; i++)
        {
            if (list.AcceptableValues[i] == curValue)
            {
                curIndex = i;
                break;
            }
        }

        return curIndex;
    }

    private class TextFieldHandler : IDisposable
    {
        private readonly ValidationInputField _text;
        private readonly DefaultUIButton _button;
        private readonly ConfigEntry<string> _configEntry;

        public TextFieldHandler(ValidationInputField text, DefaultUIButton button, ConfigEntry<string> configEntry)
        {
            _text = text;
            _button = button;
            _configEntry = configEntry;

            _text.onValueChanged.RemoveAllListeners();
            _button.OnClick.RemoveAllListeners();

            _text.richText = false;
            _text.isRichTextEditingAllowed = false;
            _text.text = _configEntry.Value;

            _text.onValueChanged.AddListener(OnValueChanged);
            _button.OnClick.AddListener(OnButtonClick);
        }

        private void OnButtonClick()
        {
            string ip = _text.text;
            if (string.IsNullOrEmpty(ip))
            {
                _configEntry.Value = null;
                _button.Interactable = false;
                return;
            }

            try
            {
                IPAddress[] dnsAddress = Dns.GetHostAddresses(ip);
                if (dnsAddress.Length > 0)
                {
                    ip = dnsAddress[0].ToString();
                }
            }
            catch
            {

            }

            if (IPAddress.TryParse(ip, out IPAddress _))
            {
                _configEntry.Value = _text.text;
                _button.Interactable = false;
            }
            else
            {
                string message = $"{_text.text} is not a valid IP address!";
                Singleton<PreloaderUI>.Instance.ShowCriticalErrorScreen("INVALID IP ADDRESS", message,
            ErrorScreen.EButtonType.OkButton, -1f);
            }
        }

        private void OnValueChanged(string value)
        {
            if (!string.IsNullOrEmpty(value) && value != _configEntry.Value)
            {
                _button.Interactable = true;
            }
        }

        public void Dispose()
        {
            _text.onValueChanged.RemoveAllListeners();
            _button.OnClick.RemoveAllListeners();
        }
    }

    private class KeybindHandler : IDisposable
    {
        private readonly ValidationInputField _text;
        private readonly DefaultUIButton _button;
        private readonly ConfigEntry<KeyboardShortcut> _configEntry;
        private readonly FikaSettings _fikaSettings;

        private Coroutine _bindRoutine;

        public KeybindHandler(ValidationInputField text, DefaultUIButton button, ConfigEntry<KeyboardShortcut> configEntry, FikaSettings fikaSettings)
        {
            _text = text;
            _button = button;
            _configEntry = configEntry;
            _fikaSettings = fikaSettings;

            _text.onValueChanged.RemoveAllListeners();
            _button.OnClick.RemoveAllListeners();

            _text.richText = false;
            _text.isRichTextEditingAllowed = false;
            _text.text = _configEntry.Value.ToString();
            _text.interactable = false;

            _button.Interactable = true;

            _button.OnClick.AddListener(OnButtonClick);
        }

        private void OnButtonClick()
        {
            _text.text = "Press a key...";
            _button.Interactable = false;
            _fikaSettings.CancelBind();
            _fikaSettings._currentKeybindHandler = this;
            _bindRoutine = _fikaSettings.StartCoroutine(HandleKeybind());
        }

        private IEnumerator HandleKeybind()
        {
            IInputSystem input = UnityInput.Current;
            bool done = false;

            while (!done)
            {
                foreach (KeyCode item in _keysToCheck)
                {
                    if (input.GetKeyUp(item))
                    {
                        _configEntry.Value = new(item,
                            [.. _keysToCheck.Where(input.GetKey)]);
                        done = true;
                        break;
                    }
                }

                yield return null;
            }

            _text.text = _configEntry.Value.ToString();
            _button.Interactable = true;
        }

        public void Dispose()
        {
            _text.onValueChanged.RemoveAllListeners();
            _button.OnClick.RemoveAllListeners();
        }

        internal void StopRoutine()
        {
            if (_bindRoutine != null)
            {
                _fikaSettings.StopCoroutine(_bindRoutine);
                _fikaSettings._currentKeybindHandler = null;
                _text.text = _configEntry.Value.ToString();
                _button.Interactable = true;
            }
        }
    }
}
