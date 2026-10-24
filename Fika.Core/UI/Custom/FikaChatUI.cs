using TMPro;
using UnityEngine.UI;
using System;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.Main.Utils;
using Fika.Core.UI.Custom;

public class FikaChatUI : MonoBehaviour
{
    public FikaChatUI(IntPtr pointer) : base(pointer)
    {
    }

    [PrefabPath("Canvas/Border/ChatFrame/Content/Scroll View/Viewport/Content/Text (TMP)")]
    public TextMeshProUGUI ChatText;

    [PrefabPath("Canvas/Border/ChatFrame/Content/Bottom/InputField/Text Area/Text")]
    public TextMeshProUGUI MessageText;

    public TMP_InputField InputField;
    public ScrollRect ScrollView;
    public Button SendButton;
    public Button CloseButton;

    /// <summary>
    /// Attaches and wires the component on an instantiated prefab
    /// </summary>
    /// <param name="root">The instantiated prefab</param>
    public static FikaChatUI Create(GameObject root)
    {
        var ui = PrefabWiring.Attach<FikaChatUI>(root);
        ui?.Init();
        return ui;
    }

    private void Init()
    {
        SendButton.gameObject.AddComponent<ButtonHandler>();
    }
}
