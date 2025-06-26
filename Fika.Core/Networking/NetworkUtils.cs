using LiteNetLib.Utils;
using System.IO;
using System.IO.Compression;

namespace Fika.Core.Networking
{
    public static class NetworkUtils
    {
        /// <summary>
        /// Compresses the given byte array using GZip compression
        /// </summary>
        /// <param name="data">The original uncompressed byte array</param>
        /// <returns>The compressed byte array</returns>
        public static byte[] CompressBytes(byte[] data)
        {
            using (MemoryStream output = new())
            {
                using (GZipStream gzip = new(output, CompressionMode.Compress, true))
                {
                    gzip.Write(data, 0, data.Length);
                }
                return output.ToArray();
            }
        }

        /// <summary>
        /// Decompresses a GZip-compressed byte array back to its original form
        /// </summary>
        /// <param name="compressedData">The compressed byte array to decompress</param>
        /// <returns>The decompressed byte array</returns>
        public static byte[] DecompressBytes(byte[] compressedData)
        {
            using (MemoryStream input = new(compressedData))
            using (GZipStream gzip = new(input, CompressionMode.Decompress))
            using (MemoryStream output = new())
            {
                gzip.CopyTo(output);
                return output.ToArray();
            }
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
            /// A voip packet that contains a raw byte array
            /// </summary>
            VOIP
        }
    }
}
