/*using TMPro;
using UnityEngine.UI;

public class ListPlayer : MonoBehaviour
{
    [SerializeField]
    private Image Background;

    [SerializeField]
    private TMP_Text HPText;

    [SerializeField]
    private TMP_Text NameText;

    [SerializeField]
    private TMP_Text FactionText;

#pragma warning disable RCS1213, CS0649
    [SerializeField]
    private Image HPBackground;

    [SerializeField]

    private Image NameBackground;

    [SerializeField]
    private Image FactionBackground;
#pragma warning restore RCS1213, CS0649

    public void Init(string name, string faction, int hp, int maxHp)
    {
        NameText.text = name;
        FactionText.text = faction;
        HPText.text = $"{hp}/{maxHp}";
    }

    public void ToggleBackground(bool enabled)
    {
        Background.color = enabled ? new(0.5f, 0.5f, 0.5f, 0.3f) : Color.clear;
    }

    public void UpdateHealth(int hp, int maxHp)
    {
        HPText.text = $"{hp}/{maxHp}";
    }
}
*/