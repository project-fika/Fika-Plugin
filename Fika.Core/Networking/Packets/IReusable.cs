using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets
{
    /// <summary>
    /// Packet that can be used with <see cref="IFikaNetworkManager.RegisterReusable{T}(System.Action{T})"/> <br/>
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

    /// <summary>
    /// Packet that can be used with <see cref="IFikaNetworkManager.RegisterReusable{T}(System.Action{T})"/> <br/>
    /// Requires manual serialization
    /// </summary>
    public interface INetReusable
    {
        /// <summary>
        /// Clears all the data in the packet after sending
        /// </summary>
        public void Clear();
        /// <summary>
        /// Clears all the data in the packet after receiving
        /// </summary>
        public void Flush();

        public void Serialize(NetDataWriter writer);
        public void Deserialize(NetDataReader reader);
    }
}