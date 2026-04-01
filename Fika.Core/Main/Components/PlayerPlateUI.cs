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

    public GameObject Skeleton;
    public GameObject Head;
    public GameObject LeftArm;
    public GameObject RightArm;
    public GameObject LeftLeg;
    public GameObject RightLeg;
    public GameObject Chest;
    public GameObject Stomach;

    public void SetNameText(string text)
    {
        playerNameScreen.SetText(text);
    }

    public void SetHealthNumberText(int amount)
    {
        healthNumberScreen.SetText("{0}%", amount);
    }
}