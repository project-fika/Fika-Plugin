using DG.Tweening;
using TMPro;
using UnityEngine.UI;

public class LoadingScreenPlayer : MonoBehaviour
{
    public TMP_Text Nickname;
    public TMP_Text Percentage;
    public Image ProgressBar;

    public float Progress
    {
        get
        {
            return _progress;
        }
    }

    private const float _division = 100f;
    private const float _tweenDuration = 0.25f;

    private float _progress;

    public void SetNickname(string nickname)
    {
        Nickname.text = nickname;
    }

    public void SetProgress(float progress)
    {
        _progress = progress;
        ProgressBar.DOFillAmount(progress / _division, _tweenDuration);
        Percentage.text = $"{Mathf.CeilToInt(progress)}%";
    }
}
