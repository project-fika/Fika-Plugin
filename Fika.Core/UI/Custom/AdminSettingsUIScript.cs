using EFT.Communications;
using System;
using Comfort.Common;
using EFT.UI;
using Fika.Core.Bundles;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Models.Admin;
using Fika.Core.UI.Custom;
using Il2CppInterop.Runtime.Injection;

public class AdminSettingsUIScript : MonoBehaviour
{
    public AdminSettingsUIScript(IntPtr pointer) : base(pointer)
    {
    }

    private AdminSettingsUI _adminSettingsUI;

    private void Start()
    {
        _adminSettingsUI = gameObject.GetComponent<AdminSettingsUI>();
        if (_adminSettingsUI == null)
        {
            throw new NullReferenceException("Could not find AdminSettingsUI");
        }

        var currentSettings = FikaRequestHandler.GetServerSettings();
        _adminSettingsUI.FriendlyFireCheck.isOn = currentSettings.FriendlyFire;
        _adminSettingsUI.FreecamCheck.isOn = currentSettings.FreeCam;
        _adminSettingsUI.SpectateFreecamCheck.isOn = currentSettings.SpectateFreeCam;
        _adminSettingsUI.SharedQuestProgressionCheck.isOn = currentSettings.SharedQuestProgression;
        _adminSettingsUI.AverageLevelCheck.isOn = currentSettings.AverageLevel;

        _adminSettingsUI.ApplyButton.onClick.AddListener(new System.Action(() =>
        {
            Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ButtonClick);
            ApplySettings();
        }));

        _adminSettingsUI.CloseButton.onClick.AddListener(new System.Action(() =>
        {
            Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ButtonClick);

            Destroy(this);
        }));
    }

    private void OnDestroy()
    {
        Destroy(_adminSettingsUI.gameObject);
    }

    private void ApplySettings()
    {
        SetSettingsRequest req = new(_adminSettingsUI.FriendlyFireCheck.isOn, _adminSettingsUI.FreecamCheck.isOn, _adminSettingsUI.SpectateFreecamCheck.isOn,
            _adminSettingsUI.SharedQuestProgressionCheck.isOn, _adminSettingsUI.AverageLevelCheck.isOn);
        var resp = FikaRequestHandler.SaveServerSettings(req);

        FikaGlobals.DisplayMessage(resp.Success.ToString());

        Destroy(this);
    }

    internal static void Create()
    {
        var gameObject = InternalBundleLoader.Instance.GetFikaAsset(InternalBundleLoader.EFikaAsset.AdminUI);
        var obj = Instantiate(gameObject);
        var ui = PrefabWiring.Attach<AdminSettingsUI>(obj);
        if (ui == null)
        {
            Destroy(obj);
            return;
        }
        ui.ApplyButton.gameObject.AddComponent<ButtonHandler>();

        obj.AddComponent<AdminSettingsUIScript>();
        var rectTransform = obj.transform.GetChild(0).GetChild(0).RectTransform();
        if (rectTransform == null)
        {
            FikaGlobals.LogError("Could not get the RectTransform!");
            Destroy(obj);
            return;
        }
        rectTransform.gameObject.AddComponent<UIDragComponent>().Init(rectTransform, true);
        obj.SetActive(true);
    }
}