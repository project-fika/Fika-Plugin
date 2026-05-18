using EFT.UI;
using Fika.Core.Main.ClientClasses;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.Components;

internal sealed class Bleedout : MonoBehaviour
{
    private ClientHealthController _healthController;
    private float _bleedoutTime;
    private float _counter;
    private bool _shouldBleed;

    internal void Init(ClientHealthController healthController)
    {
        _healthController = healthController;
        _bleedoutTime = healthController.BleedoutTime + 0.25f; // bad safety against delays
        _shouldBleed = healthController.ShouldBleedOut;
    }

    private void Update()
    {
        if (!_shouldBleed)
        {
            return;
        }

        _counter += Time.unscaledDeltaTime;
        if (_counter >= _bleedoutTime)
        {
            if (_healthController != null)
            {
                _healthController.BleedOut();
            }
            Destroy(this);
        }
    }

    public void ShowRevive(string nickname)
    {
        _shouldBleed = false;

        var gameUi = MonoBehaviourSingleton<GameUI>.Instance;
        gameUi.BattleUiPanelExtraction.Show(string.Format(LocaleUtils.UI_REVIVING_BEING_REVIVED_BY.Localized(), nickname)); 
    }

    public void HideRevive()
    {
        _shouldBleed = true;
        HideUI();
    }

    public void ShowUI()
    {
        if (!_shouldBleed)
        {
            return;
        }

        var gameUi = MonoBehaviourSingleton<GameUI>.Instance;
        gameUi.BattleUiPanelExtraction.Show(LocaleUtils.UI_REVIVING_BLEEDING_OUT.Localized(),
            _healthController.BleedoutTime - _counter);
    }

    public void HideUI()
    {
        var gameUi = MonoBehaviourSingleton<GameUI>.Instance;
        gameUi.BattleUiPanelExtraction.Close();
    }
}
