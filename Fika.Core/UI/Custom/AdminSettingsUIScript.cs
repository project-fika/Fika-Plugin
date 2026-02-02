using System;
using Comfort.Common;
using EFT.UI;
using Fika.Core.Bundles;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Models.Admin;

public class AdminSettingsUIScript : MonoBehaviour
{
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

        _adminSettingsUI.ApplyButton.onClick.AddListener(() =>
        {
            Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ButtonClick);
            ApplySettings();
        });

        _adminSettingsUI.CloseButton.onClick.AddListener(() =>
        {
            Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ButtonClick);

            Destroy(this);
        });
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

        NotificationManagerClass.DisplayMessageNotification(resp.Success.ToString());

        Destroy(this);
    }

    internal static void Create()
    {
        var gameObject = InternalBundleLoader.Instance.GetFikaAsset(InternalBundleLoader.EFikaAsset.AdminUI);
        var obj = Instantiate(gameObject);
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