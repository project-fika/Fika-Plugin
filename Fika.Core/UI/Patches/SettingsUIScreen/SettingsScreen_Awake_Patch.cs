using EFT.UI;
using EFT.UI.Settings;
using SPT.Reflection.Patching;
using Fika.Core.UI.Custom.FikaSettingsScreen;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.UI;

namespace Fika.Core.UI.Patches.SettingsUIScreen;

internal class SettingsScreen_Awake_Patch : ModulePatch
{
    private static readonly FieldInfo _settingsTabField = typeof(SettingsScreen)
        .GetField("settingsTab_0", BindingFlags.Instance | BindingFlags.NonPublic);

    protected override MethodBase GetTargetMethod()
    {
        return typeof(SettingsScreen)
            .GetMethod(nameof(SettingsScreen.Awake));
    }

    [PatchPostfix]
    public static void Postfix(SettingsScreen __instance, GraphicsSettingsTab ____graphicsSettingsScreen,
        SoundSettingsTab ____soundSettingsScreen, GameSettingsTab ____gameSettingsScreen)
    {
        if (FikaSettings.Instance != null)
        {
            return;
        }

        __instance.StartCoroutine(CreateScreen(__instance, ____graphicsSettingsScreen, ____soundSettingsScreen, ____gameSettingsScreen));
    }

    public static IEnumerator CreateScreen(SettingsScreen instance, GraphicsSettingsTab graphicsSettingsTab,
        SoundSettingsTab soundSettingsScreen, GameSettingsTab gameSettingsScreen)
    {
        GameObject gameObject = new("FikaSettingsScreen")
        {
            layer = graphicsSettingsTab.gameObject.layer
        };
        GameObject.DontDestroyOnLoad(gameObject);

        yield return new WaitUntil(() => gameObject != null);
        Traverse traverse = Traverse.Create(soundSettingsScreen);

        SettingDropDown settingDropdown = traverse.Field<SettingDropDown>("_dropDownPrefab").Value;
        SettingSelectSlider intSlider = traverse.Field<SettingSelectSlider>("_selectSliderPrefab").Value;
        SettingFloatSlider floatSlider = traverse.Field<SettingFloatSlider>("_floatSliderPrefab").Value;
        SettingToggle toggle = traverse.Field<SettingToggle>("_togglePrefab").Value;

        FikaSettings fikaSettings = FikaSettings.Create(gameObject, settingDropdown,
            intSlider, floatSlider, toggle, gameSettingsScreen);
        fikaSettings.enabled = true;
        FikaSettings.Instance = fikaSettings;
        VerticalLayoutGroup verticalLayoutGroup = gameObject.AddComponent<VerticalLayoutGroup>();

        yield return new WaitUntil(() => verticalLayoutGroup != null);

        verticalLayoutGroup.childControlHeight = true;
        verticalLayoutGroup.childControlWidth = true;

        verticalLayoutGroup.childForceExpandHeight = false;
        verticalLayoutGroup.childForceExpandWidth = true;

        verticalLayoutGroup.childScaleHeight = false;
        verticalLayoutGroup.childScaleWidth = false;

        verticalLayoutGroup.spacing = 4;

        gameObject.transform.SetParent(instance.gameObject.transform);

        GameObject toCopy = graphicsSettingsTab.gameObject.transform.GetChild(2).gameObject;
        GameObject newLayout = GameObject.Instantiate(toCopy, gameObject.transform, gameSettingsScreen);
        newLayout.name = "FikaSettings";
        newLayout.layer = gameSettingsScreen.gameObject.layer;
        newLayout.RectTransform().localScale = Vector3.one;

        Transform content = newLayout.transform.GetChild(0)
            .GetChild(0)
            .GetChild(0);

        yield return new WaitUntil(() => content.childCount > 20);
        SetupFikaMenu(gameObject, content, graphicsSettingsTab.gameObject.RectTransform());

        yield return new WaitUntil(() => instance.gameObject.activeSelf);
        SetupToggle(instance);
    }

    private static void SetupToggle(SettingsScreen instance)
    {
        Transform controlsToggle = instance.gameObject.transform.GetChild(2)
            .GetChild(4);

        GameObject fikaToggle = GameObject.Instantiate(controlsToggle.gameObject, controlsToggle.transform.parent);
        fikaToggle.name = "FikaToggleSpawner";

        UIAnimatedToggleSpawner spawner = fikaToggle.GetComponent<UIAnimatedToggleSpawner>();
        spawner.vmethod_0("FIKA", 25);
        spawner.SpawnedObject.onValueChanged.AddListener(value =>
        {
            SettingsTab settingsTab = (SettingsTab)_settingsTabField.GetValue(instance);
            if (settingsTab != null)
            {
                settingsTab.IsSelected = false;
            }
            FikaSettings.Instance.IsSelected = value;
        });
    }

    private static void SetupFikaMenu(GameObject gameObject, Transform content, RectTransform rectTransform)
    {
        List<GameObject> children = [];
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Transform child = content.GetChild(i);
            if (child != null)
            {
                children.Add(child.gameObject);
            }
        }

        for (int i = 0; i < children.Count; i++)
        {
            GameObject.Destroy(children[i]);
        }

        RectTransform rect = (RectTransform)gameObject.transform;

        /*rect.anchorMax = new(0.5f, 1f);
        rect.anchorMin = new(0.5f, 1f);

        rect.offsetMax = new(500f, -135f);
        rect.offsetMin = new(-500f, -935f);

        rect.sizeDelta = new(1000f, 800f);
        rect.localScale = Vector3.one;

        rect.pivot = new(0.5f, 0f);

        rect.anchoredPosition = new(0f, 935f);*/

        // copy data from graphics settings recttransform
        rect.anchorMax = rectTransform.anchorMax;
        rect.anchorMin = rectTransform.anchorMin;

        rect.offsetMax = rectTransform.offsetMax;
        rect.offsetMin = rectTransform.offsetMin;

        rect.sizeDelta = rectTransform.sizeDelta;
        rect.localScale = rectTransform.localScale;

        rect.pivot = rectTransform.pivot;

        rect.anchoredPosition = rectTransform.anchoredPosition;
        rect.localPosition = rectTransform.localPosition;

        content.GetComponent<VerticalLayoutGroup>().spacing = 15f;
        FikaSettings.Instance.Init(content);
        FikaSettings.Instance.IsSelected = false;
    }
}
