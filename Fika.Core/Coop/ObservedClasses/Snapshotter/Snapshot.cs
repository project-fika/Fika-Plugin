namespace Fika.Core.Coop.ObservedClasses.Snapshotter
{
	public interface ISnapshot
	{
		// the remote timestamp (when it was sent by the remote)
		double RemoteTime { get; set; }

		// the local timestamp (when it was received on our end)
		// technically not needed for basic snapshot interpolation.
		// only for dynamic buffer time adjustment.
		double LocalTime { get; set; }
	}
}
