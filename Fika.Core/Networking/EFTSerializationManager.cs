namespace Fika.Core.Networking
{
    public static class EFTSerializationManager
    {
        /// <summary>
        /// Gets a <see cref="EFTReaderClass"/> from the reader pool (<see cref="System.IDisposable"/>) <br/>
        /// Must call <see cref="GClass1207.Dispose"/> manually or wrap in a using statement
        /// </summary>
        /// <param name="data">The data to supply the reader with</param>
        /// <returns>A free <see cref="GClass1207"/> from the reader pool</returns>
        public static GClass1207 GetReader(byte[] data)
        {
            return GClass1210.Get(data);
        }

        /// <summary>
        /// Gets a <see cref="EFTWriterClass"/> from the writer pool (<see cref="System.IDisposable"/>) <br/>
        /// Must call <see cref="GClass1212.Dispose"/> manually or wrap in a using statement
        /// </summary>
        /// <returns>A free <see cref="GClass1212"/> from the writer pool</returns>
        public static GClass1212 GetWriter()
        {
            return GClass1215.Get();
        }
    }
}
