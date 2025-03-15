namespace Fika.Core.Networking.Packets
{
    /// <summary>
    /// Packet that can be used with <see cref="IFikaNetworkManager.RegisterReusable{T, TUserData}(System.Action{T})"/> <br/>
    /// All data has to be Properties
    /// </summary>
    public interface IReusable
    {
        /// <summary>
        /// Verifies if there is data to send
        /// </summary>
        public bool HasData { get; }
        /// <summary>
        /// Clears all the data in the packet after sending/receiving
        /// </summary>
        public void Flush();
    }
}