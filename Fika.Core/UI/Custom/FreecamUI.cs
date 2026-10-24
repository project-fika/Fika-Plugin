using TMPro;
using System;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.Main.Utils;
using Fika.Core.UI.Custom;

public class FreecamUI : MonoBehaviour
{
    public FreecamUI(IntPtr pointer) : base(pointer)
    {
    }

    public GameObject OverlayGroup;

    [PrefabPath("Canvas/OverlayGroup/Jump")]
    public TextMeshProUGUI JumpText;

    public TextMeshProUGUI SwapMode;

    [PrefabPath("Canvas/OverlayGroup/Detach")]
    public TextMeshProUGUI DetachText;

    [PrefabPath("Canvas/OverlayGroup/Teleport")]
    public TextMeshProUGUI TeleportText;

    [PrefabPath("Canvas/OverlayGroup/Vision")]
    public TextMeshProUGUI VisionText;

    [PrefabPath("Canvas/OverlayGroup/Culling")]
    public TextMeshProUGUI CullingText;

    [PrefabPath("Canvas/OverlayGroup/Input")]
    public TextMeshProUGUI InputText;

    [PrefabPath("Canvas/OverlayGroup/Turbo")]
    public TextMeshProUGUI TurboText;

    public TextMeshProUGUI PlayerList;
    public Transform ListOfPlayers;
}
