namespace Fika.Core.Models
{
    public class ServerGroup
    {
        public int ConnectedClients = 0;
        public int ReadyClients = 0;

        public ServerGroup(int connectedClients = 0, int readyClients = 0)
        {
            ConnectedClients = connectedClients;
            ReadyClients = readyClients;
        }
    }
}