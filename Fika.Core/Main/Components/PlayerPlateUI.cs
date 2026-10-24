// © 2026 Lacyway All Rights Reserved

using TMPro;
using UnityEngine.UI;
using System;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.Main.Utils;
using Fika.Core.UI.Custom;

/// <summary>
/// Controller for the Players Name Plate. <br/>
/// Created by: ssh_
/// </summary>
public class PlayerPlateUI : MonoBehaviour
{
    public PlayerPlateUI(IntPtr pointer) : base(pointer)
    {
    }

    [PrefabPath("Canvas/ScalarObject")]
    public GameObject ScalarObjectScreen;

    [PrefabPath("Canvas/ScalarObject/Labels/PlayerName")]
    public TextMeshProUGUI playerNameScreen;

    [PrefabPath("Canvas/ScalarObject/Status/HealthBarBorder")]
    public Image healthBarBackgroundScreen;

    [PrefabPath("Canvas/ScalarObject/Status/HealthBarBorder/HealthBar")]
    public Image healthBarScreen;

    [PrefabPath("Canvas/ScalarObject/Status/HealthNumber_Background")]
    public Image healthNumberBackgroundScreen;

    [PrefabPath("Canvas/ScalarObject/Status/HealthNumber_Background/HealthNumberText")]
    public TextMeshProUGUI healthNumberScreen;

    [PrefabPath("Canvas/ScalarObject/Status/DownedStatus_Background")]
    public Image downedStateBackgroundScreen;

    [PrefabPath("Canvas/ScalarObject/Status/DownedStatus_Background/DownedStatusText")]
    public TextMeshProUGUI downedStateScreen;

    [PrefabPath("Canvas/ScalarObject/Status/DownedStatus_Background/DownedStatusTimer")]
    public TextMeshProUGUI downedStateTimerScreen;

    [PrefabPath("Canvas/ScalarObject/Labels/Factions/Usec")]
    public Image usecPlateScreen;

    [PrefabPath("Canvas/ScalarObject/Labels/Factions/Bear")]
    public Image bearPlateScreen;

    [PrefabPath("Canvas/ScalarObject/Labels/Effects_Background")]
    public GameObject EffectsBackground;

    [PrefabPath("Canvas/ScalarObject/Labels/Effects_Background/EffectTemplate")]
    public GameObject EffectImageTemplate;
    
    [PrefabPath("Canvas/ScalarObject")]
    public CanvasGroup AlphaGroup;

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