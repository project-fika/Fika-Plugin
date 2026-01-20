// © 2026 Lacyway All Rights Reserved

using TMPro;
using UnityEngine.UI;

/// <summary>
/// Controller for the Players Name Plate. <br/>
/// Created by: ssh_
/// </summary>
public class PlayerPlateUI : MonoBehaviour
{
    public GameObject ScreenSpaceNamePlate;
    public GameObject ScalarObjectScreen;
    public TextMeshProUGUI playerNameScreen;
    public Image healthBarBackgroundScreen;
    public Image healthBarScreen;
    public Image healthNumberBackgroundScreen;
    public TextMeshProUGUI healthNumberScreen;
    public Image usecPlateScreen;
    public Image bearPlateScreen;
    public GameObject EffectsBackground;
    public GameObject EffectImageTemplate;
    public CanvasGroup LabelsGroup;
    public CanvasGroup StatusGroup;

    public void SetNameText(string text)
    {
        playerNameScreen.SetText(text);
    }

    public void SetHealthNumberText(string text)
    {
        healthNumberScreen.SetText(text);
    }
}