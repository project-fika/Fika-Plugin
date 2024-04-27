// © 2024 Lacyway All Rights Reserved

using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controller for the Players Name Plate. <br/>
/// Created by: ssh_
/// </summary>
public class PlayerPlateUI : MonoBehaviour
{
    [SerializeField]
    public GameObject ScreenSpaceNamePlate;
    [SerializeField]
    public GameObject ScalarObjectScreen;
    [SerializeField]
    public TextMeshProUGUI playerNameScreen;
    [SerializeField]
    public Image healthBarBackgroundScreen;
    [SerializeField]
    public Image healthBarScreen;
    [SerializeField]
    public Image healthNumberBackgroundScreen;
    [SerializeField]
    public TextMeshProUGUI healthNumberScreen;
    [SerializeField]
    public Image usecPlateScreen;
    [SerializeField]
    public Image bearPlateScreen;

    public void SetNameText(string text)
    {
        playerNameScreen.SetText(text);
    }

    public void SetHealthNumberText(string text)
    {
        healthNumberScreen.SetText(text);
    }
}