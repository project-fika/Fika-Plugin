using System;

namespace Fika.Core.Networking
{
    public static class EFTSerializationManager
    {
        private static readonly FikaReader reader = new([]);
        private static readonly FikaWriter writer = new();

        /// <summary>
        /// Gets a <see cref="FikaReader"/>
        /// </summary>
        /// <param name="data">The data to supply the reader with</param>
        /// <returns>A <see cref="FikaReader"/></returns>
        public static FikaReader GetReader(byte[] data)
        {
            reader.SetBuffer(new(data));
            return reader;
        }

        /// <summary>
        /// Gets a <see cref="FikaWriter"/> (<see cref="IDisposable"/>) <br/>
        /// Must call <see cref="FikaWriter.Dispose"/> manually or wrap in a using statement
        /// </summary>
        /// <returns>A <see cref="FikaWriter"/></returns>
        public static FikaWriter GetWriter()
        {
            return writer;
        }

        public class FikaReader(ArraySegment<byte> segment) : EFTReaderClass(segment)
        {

        }

        public class FikaWriter : EFTWriterClass, IDisposable
        {
            public void Dispose()
            {
                Reset();
            }
        }
    }
}
