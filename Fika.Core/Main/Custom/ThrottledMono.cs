namespace Fika.Core.Main.Custom;

public abstract class ThrottledMono : MonoBehaviour
{
    /// <summary>
    /// Number of ticks per second (e.g., 10 means <see cref="Tick"/> runs 10 times per second).
    /// </summary>
    public abstract float UpdateRate { get; }

    private float _secondsPerTick;
    private float _tickTimer;

    private void Awake()
    {
        _secondsPerTick = 1f / UpdateRate;
    }

    private void Update()
    {
        _tickTimer += Time.unscaledDeltaTime;
        if (_tickTimer >= _secondsPerTick)
        {
            Tick();
            _tickTimer -= _secondsPerTick; // monos in Tarkov don't last long enough for accumulation drift to occur
        }
    }

    public abstract void Tick();
}