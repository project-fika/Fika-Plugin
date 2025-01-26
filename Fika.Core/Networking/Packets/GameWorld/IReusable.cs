namespace Fika.Core.Networking
{
    public interface IReusable
    {
        public bool HasData { get; }
        public void Flush();
    }
}