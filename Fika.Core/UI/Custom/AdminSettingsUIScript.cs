using Comfort.Common;
using EFT.UI;
using Fika.Core.Bundles;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Http;
using System;

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

        CurrentSettingsResponse currentSettings = FikaRequestHandler.GetServerSettings();
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
        SetSettingsResponse resp = FikaRequestHandler.SaveServerSettings(req);

        NotificationManagerClass.DisplayMessageNotification(resp.Success.ToString());

        Destroy(this);
    }

    internal static void Create()
    {
        GameObject gameObject = InternalBundleLoader.Instance.GetFikaAsset(InternalBundleLoader.EFikaAsset.AdminUI);
        GameObject obj = Instantiate(gameObject);
        obj.AddComponent<AdminSettingsUIScript>();
        RectTransform rectTransform = obj.transform.GetChild(0).GetChild(0).RectTransform();
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