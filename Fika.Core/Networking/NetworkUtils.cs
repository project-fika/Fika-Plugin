/*#if DEBUG
using Fika.Core.Main.Utils;
using System.Diagnostics;
#endif*/
using System;
using System.Net;
using System.Net.Sockets;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.LZ4;

namespace Fika.Core.Networking;

public static class NetworkUtils
{
    public static IDataReader EventDataReader { get; private set; } = new GClass1364(new byte[16_384]);
    public static GInterface131 EventDataWriter { get; private set; } = new GClass1368(new byte[10_000]);

    internal static void ResetReaderAndWriter()
    {
        EventDataReader = new GClass1364(new byte[16_384]);
        EventDataWriter = new GClass1368(new byte[10_000]);
    }

    /// <summary>
    /// Compresses the given byte array using LZ4 compression
    /// </summary>
    /// <param name="input">The original uncompressed byte array</param>
    /// <returns>The compressed byte array</returns>
    public static ReadOnlySpan<byte> CompressBytes(byte[] input)
    {
/*#if DEBUG
        var sw = Stopwatch.StartNew();
#endif*/

        var buffer = new byte[LZ4Codec.MaximumOutputSize(input.Length)];
        var encoded = LZ4Codec.Encode(input, 0, input.Length, buffer, 0, buffer.Length, LZ4Level.L04_HC);

/*#if DEBUG
        sw.Stop();
        var compressionRate = 100.0 * (1.0 - (encoded / (double)input.Length));
        FikaGlobals.LogWarning($"Compression reduced size by {compressionRate:F2}%, took {sw.Elapsed.TotalMilliseconds:F2} ms");
#endif*/

        // Return exact compressed data slice without extra ToArray allocation
        if (encoded == buffer.Length)
        {
            // Compressed data fills buffer completely, just return it
            return buffer;
        }
        else
        {
            // Create trimmed array from buffer span
            return buffer.AsSpan(0, encoded).ToArray();
        }
    }

    /// <summary>
    /// Decompresses a LZ4-compressed byte array back to its original form
    /// </summary>
    /// <param name="compressedData">The compressed byte array to decompress</param>
    /// <param name="originalLength">The length of the original byte array</param>
    /// <returns>The decompressed byte array</returns>
    public static byte[] DecompressBytes(byte[] compressedData, int originalLength)
    {
/*#if DEBUG
        var sw = Stopwatch.StartNew();
#endif*/
        var result = new byte[originalLength];
        var decoded = LZ4Codec.Decode(compressedData, 0, compressedData.Length, result, 0, originalLength);
        if (decoded != originalLength)
        {
            throw new InvalidOperationException("LZ4 decompression failed: length mismatch.");
        }

/*#if DEBUG
        sw.Stop();
        var reverseRate = 100.0 * ((originalLength - compressedData.Length) / (double)compressedData.Length);
        FikaGlobals.LogWarning($"Original is {reverseRate:F2}% larger than compressed, took {sw.Elapsed.TotalMilliseconds:F2} ms");
#endif*/

        return result;
    }

    public static string FormatMongoId(uint timeStamp, ulong counter)
    {
        return string.Create(24, (timeStamp, counter), (span, state) =>
        {
            state.timeStamp.TryFormat(span[..8], out _, "x8");
            state.counter.TryFormat(span[8..], out _, "x16");
        });
    }

    /// <summary>
    /// Validates whether the given IP string represents a connectable, routable IP address.
    /// </summary>
    /// <param name="ip">The IP address string to validate (IPv4 or IPv6).</param>
    /// <returns>
    /// <see langword="true"/> if the IP is valid and suitable for advertising or binding; <see langword="false"/> otherwise.
    /// </returns>
    /// <remarks>
    /// Validation rules applied:
    /// <list type="bullet">
    ///   <item><description>Null, empty, or whitespace strings are rejected.</description></item>
    ///   <item><description>IPv6 scope identifiers (e.g., fe80::1%12) are removed before validation.</description></item>
    ///   <item><description>Unspecified addresses (<c>0.0.0.0</c> or <c>::</c>) are rejected.</description></item>
    ///   <item><description>Loopback addresses (<c>127.0.0.1</c> or <c>::1</c>) are rejected.</description></item>
    ///   <item><description>IPv4 APIPA addresses (169.254.x.x) are rejected.</description></item>
    ///   <item><description>IPv4 multicast and reserved addresses (224.0.0.0 and above) are rejected.</description></item>
    ///   <item><description>IPv6 link-local, site-local, and multicast addresses are rejected.</description></item>
    ///   <item><description>IPv6 addresses must be global unicast (2000::/3) to be accepted.</description></item>
    /// </list>
    /// </remarks>
    public static bool ValidateIP(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            return false;
        }

        // Remove IPv6 scope ID if present (fe80::1%12)
        var percentIndex = ip.IndexOf('%');
        if (percentIndex >= 0)
        {
            ip = ip[..percentIndex];
        }

        if (!IPAddress.TryParse(ip, out var address))
        {
            return false;
        }

        // Reject unspecified
        if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
        {
            return false;
        }

        // Reject loopback
        if (IPAddress.IsLoopback(address))
        {
            return false;
        }

        switch (address.AddressFamily)
        {
            case AddressFamily.InterNetwork:
                {
                    var b = address.GetAddressBytes();

                    // APIPA 169.254.0.0/16
                    if (b[0] == 169 && b[1] == 254)
                    {
                        return false;
                    }

                    // Multicast / reserved (224+)
                    if (b[0] >= 224)
                    {
                        return false;
                    }

                    return true;
                }

            case AddressFamily.InterNetworkV6:
                {
                    // Link-local, site-local, multicast
                    if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast)
                    {
                        return false;
                    }

                    // Global unicast must be in 2000::/3
                    var b = address.GetAddressBytes();
                    return (b[0] & 0b1110_0000) == 0b0010_0000;
                }
        }

        return false;
    }

    /// <summary>
    /// Resolves a remote address from a string IP or hostname and port.
    /// </summary>
    /// <param name="ip">The IP address or hostname.</param>
    /// <param name="port">The port number.</param>
    /// <returns>The resolved <see cref="IPEndPoint"/>.</returns>
    /// <exception cref="ParseException">Thrown if the address cannot be resolved.</exception>
    public static IPEndPoint ResolveRemoteAddress(string ip, int port)
    {
        var resolved = NetUtils.ResolveAddress(ip);
        return new IPEndPoint(resolved, port);
    }

    /// <summary>
    /// Used to determine what kind of packet was received on the <see cref="IFikaNetworkManager"/>
    /// </summary>
    public enum EPacketType : byte
    {
        /// <summary>
        /// A packet that implements <see cref="INetSerializable"/>
        /// </summary>
        Serializable,
        /// <summary>
        /// A raw <see cref="Packets.Player.PlayerStatePacket"/>
        /// </summary>
        PlayerState,
        /// <summary>
        /// A raw <see cref="BTRDataPacketStruct"/>
        /// </summary>
        BTR,
        /// <summary>
        /// A voip packet that contains a raw byte array
        /// </summary>
        VOIP
    }
}
