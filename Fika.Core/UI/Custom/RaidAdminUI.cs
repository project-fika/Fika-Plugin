using TMPro;
using UnityEngine.UI;
using System;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.Main.Utils;
using Fika.Core.UI.Custom;

public class RaidAdminUI : MonoBehaviour
{
    public RaidAdminUI(IntPtr pointer) : base(pointer)
    {
    }

    public TMP_Dropdown ClientSelection;
    public Button CloseButton;

    public GameObject InfoPane;

    [PrefabPath("Canvas/Border/Frame/Content/InfoPane/Header")]
    public TextMeshProUGUI HeaderText;

    [PrefabPath("Canvas/Border/Frame/Content/InfoPane/Details/SentPackets")]
    public TextMeshProUGUI SentPacketsText;

    [PrefabPath("Canvas/Border/Frame/Content/InfoPane/Details/ReceivedPackets")]
    public TextMeshProUGUI ReceivedPacketsText;

    [PrefabPath("Canvas/Border/Frame/Content/InfoPane/Details/SentData")]
    public TextMeshProUGUI SentDataText;

    [PrefabPath("Canvas/Border/Frame/Content/InfoPane/Details/ReceivedData")]
    public TextMeshProUGUI ReceivedDataText;

    [PrefabPath("Canvas/Border/Frame/Content/InfoPane/Details/PacketLoss")]
    public TextMeshProUGUI PacketLossText;

    [PrefabPath("Canvas/Border/Frame/Content/InfoPane/Details/PacketLossPerc")]
    public TextMeshProUGUI PacketLossPercentText;

    public Button KickButton;
}