using System;

namespace Fika.Core.Coop.ObservedClasses.Snapshotting
{
	public struct ExponentialMovingAverage(int n)
	{
		readonly double alpha = 2.0 / (n + 1);
		bool initialized = false;

		public double Value = 0;
		public double Variance = 0;
		public double StandardDeviation = 0;

		public void Add(double newValue)
		{
			if (initialized)
			{
				double delta = newValue - Value;
				Value += alpha * delta;
				Variance = (1 - alpha) * (Variance + alpha * delta * delta);
				StandardDeviation = Math.Sqrt(Variance);
			}
			else
			{
				Value = newValue;
				initialized = true;
			}
		}

		public void Reset()
		{
			initialized = false;
			Value = 0;
			Variance = 0;
			StandardDeviation = 0;
		}
	}
}
