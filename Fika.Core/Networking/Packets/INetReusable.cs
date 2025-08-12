namespace Fika.Core.Networking.Packets;

/// <summary>
/// Packet that can be used with <see cref="IFikaNetworkManager.RegisterNetReusable{T}(System.Action{T}){T}(System.Action{T})"/> <br/>
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