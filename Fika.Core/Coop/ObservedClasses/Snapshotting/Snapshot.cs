namespace Fika.Core.Coop.ObservedClasses.Snapshotting
{
	public interface ISnapshot
	{
		/// <summary>
		/// The remote timestamp (when it was sent by the remote)
		/// </summary>
		double RemoteTime { get; set; }

		/// <summary>
		/// The local timestamp (when it was received on our end)
		/// </summary>
		double LocalTime { get; set; }
	}
}
