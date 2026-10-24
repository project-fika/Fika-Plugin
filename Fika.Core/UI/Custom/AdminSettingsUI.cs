using UnityEngine.UI;
using System;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.Main.Utils;
using Fika.Core.UI.Custom;

public class AdminSettingsUI : MonoBehaviour
{
    public AdminSettingsUI(IntPtr pointer) : base(pointer)
    {
    }
    
    [PrefabPath("Canvas/Border/Frame/Content/FFCheck/Toggle")]
    public Toggle FriendlyFireCheck;

    [PrefabPath("Canvas/Border/Frame/Content/FCCheck/Toggle")]
    public Toggle FreecamCheck;

    [PrefabPath("Canvas/Border/Frame/Content/SFCCheck/Toggle")]
    public Toggle SpectateFreecamCheck;

    [PrefabPath("Canvas/Border/Frame/Content/SCPCheck/Toggle")]
    public Toggle SharedQuestProgressionCheck;

    [PrefabPath("Canvas/Border/Frame/Content/SALCheck/Toggle")]
    public Toggle AverageLevelCheck;

    public Button ApplyButton;
    public Button CloseButton;
}