using System;

namespace Fika.Core.Coop.ObservedClasses.Snapshotter
{
	public struct ExponentialMovingAverage
	{
		readonly double alpha;
		bool initialized;

		public double Value;
		public double Variance;
		public double StandardDeviation;

		public ExponentialMovingAverage(int n)
		{
			alpha = 2.0 / (n + 1);
			initialized = false;
			Value = 0;
			Variance = 0;
			StandardDeviation = 0;
		}

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
