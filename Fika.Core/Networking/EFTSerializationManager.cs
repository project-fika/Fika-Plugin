namespace Fika.Core.Networking
{
    public static class EFTSerializationManager
    {
        public static GClass1207 GetReader(byte[] data)
        {
            return GClass1210.Get(data);
        }

        public static GClass1212 GetWriter()
        {
            return GClass1215.Get();
        }
    }
}
