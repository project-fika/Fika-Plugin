#if DEBUG
using Fika.Core.Main.Utils;
# endif
using K4os.Compression.LZ4;
using LiteNetLib.Utils;
using System;
using System.Diagnostics;

namespace Fika.Core.Networking
{
    public static class NetworkUtils
    {
        /// <summary>
        /// Compresses the given byte array using LZ4 compression
        /// </summary>
        /// <param name="data">The original uncompressed byte array</param>
        /// <returns>The compressed byte array</returns>
        public static ReadOnlySpan<byte> CompressBytes(byte[] input)
        {
#if DEBUG
            Stopwatch sw = Stopwatch.StartNew();
#endif

            byte[] buffer = new byte[LZ4Codec.MaximumOutputSize(input.Length)];
            int encoded = LZ4Codec.Encode(input, 0, input.Length, buffer, 0, buffer.Length, LZ4Level.L04_HC);

#if DEBUG
            sw.Stop();
            double compressionRate = 100.0 * (1.0 - (encoded / (double)input.Length));
            FikaGlobals.LogWarning($"Compression reduced size by {compressionRate:F2}%, took {sw.Elapsed.TotalMilliseconds:F2} ms");
#endif

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
#if DEBUG
            Stopwatch sw = Stopwatch.StartNew();
#endif
            byte[] result = new byte[originalLength];
            int decoded = LZ4Codec.Decode(compressedData, 0, compressedData.Length, result, 0, originalLength);
            if (decoded != originalLength)
            {
                throw new InvalidOperationException("LZ4 decompression failed: length mismatch.");
            }

#if DEBUG
            sw.Stop();
            double reverseRate = 100.0 * ((originalLength - compressedData.Length) / (double)compressedData.Length);
            FikaGlobals.LogWarning($"Original is {reverseRate:F2}% larger than compressed, took {sw.Elapsed.TotalMilliseconds:F2} ms");
#endif

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
        /// Used to determine what kind of packet was received on the <see cref="IFikaNetworkManager"/>
        /// </summary>
        public enum EPacketType : byte
        {
            /// <summary>
            /// A packet that implements <see cref="INetSerializable"/>
            /// </summary>
            Serializable,
            PlayerState,
            /// <summary>
            /// A voip packet that contains a raw byte array
            /// </summary>
            VOIP
        }
    }
}
