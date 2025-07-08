using Fika.Core.Networking.Http;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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
            ApplySettings();
        });

        _adminSettingsUI.CloseButton.onClick.AddListener(() =>
        {
            GameObject.Destroy(this);
        });
    }

    private void ApplySettings()
    {
        SetSettingsRequest req = new(_adminSettingsUI.FriendlyFireCheck.isOn, _adminSettingsUI.FreecamCheck.isOn, _adminSettingsUI.SpectateFreecamCheck.isOn,
            _adminSettingsUI.SharedQuestProgressionCheck.isOn, _adminSettingsUI.AverageLevelCheck.isOn);
        SetSettingsResponse resp = FikaRequestHandler.SaveServerSettings(req);

        NotificationManagerClass.DisplayMessageNotification(resp.Success.ToString());
    }
}