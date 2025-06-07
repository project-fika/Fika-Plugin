using System;

namespace Fika.Core.Coop.ObservedClasses.Snapshotting
{
    public struct ExponentialMovingAverage(int n)
    {
        private readonly double _alpha = 2.0 / (n + 1);
        private bool _initialized = false;

        public double Value = 0;
        public double Variance = 0;
        public double StandardDeviation = 0;

        public void Add(double newValue)
        {
            if (_initialized)
            {
                double delta = newValue - Value;
                Value += _alpha * delta;
                Variance = (1 - _alpha) * (Variance + _alpha * delta * delta);
                StandardDeviation = Math.Sqrt(Variance);
                return;
            }

            Value = newValue;
            _initialized = true;
        }

        public void Reset()
        {
            _initialized = false;
            Value = 0;
            Variance = 0;
            StandardDeviation = 0;
        }
    }
}
