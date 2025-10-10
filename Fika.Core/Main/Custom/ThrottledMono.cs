namespace Fika.Core.Main.Custom;

/// <summary>
/// An abstract MonoBehaviour that throttles the frequency of <see cref="Tick"/> calls
/// based on a specified <see cref="UpdateRate"/>.
/// </summary>
public abstract class ThrottledMono : MonoBehaviour
{
    /// <summary>
    /// Gets the number of ticks per second. <br/>
    /// E.g., a value of 10 means <see cref="Tick"/> is called 10 times per second.
    /// </summary>
    public abstract float UpdateRate { get; }

    private float _secondsPerTick;
    private float _tickTimer;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the tick interval based on <see cref="UpdateRate"/>. <br/>
    /// If you override this, ensure you call the base implementation or <see cref="ThrottledMono"/> will not function correctly.
    /// </summary>
    /// <remarks>
    /// <see cref="_secondsPerTick"/> is assigned during <see cref="Awake"/>.
    /// </remarks>
    protected virtual void Awake()
    {
        _secondsPerTick = 1f / UpdateRate;
    }

    /// <summary>
    /// Unity callback called every frame.
    /// Accumulates unscaled delta time and calls <see cref="Tick"/> when the interval elapses.
    /// </summary>
    private void Update()
    {
        _tickTimer += Time.unscaledDeltaTime;
        if (_tickTimer >= _secondsPerTick)
        {
            Tick();
            _tickTimer -= _secondsPerTick; // monos in Tarkov don't last long enough for accumulation drift to occur
        }
    }

    /// <summary>
    /// Called at the throttled interval defined by <see cref="UpdateRate"/>.
    /// Implement this method to define custom tick behavior.
    /// </summary>
    public abstract void Tick();
}