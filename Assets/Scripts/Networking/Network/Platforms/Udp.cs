using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Networking.Network.Platforms.Udp {
    public class PlatformAPI : IPlatformAPI {
        Dictionary<Channel, UdpClient> connections = new Dictionary<Channel, UdpClient>();

        public void Initialize() {
            // set the local address
            NetworkManager.localAddress = "127.0.0.1";
        }

        public void CreateMatch(int maxPlayers) {
            NetworkManager.hostAddress = "127.0.0.1";
            NetworkManager.OnGameJoined();
        }

        UdpClient GetConnection(Channel channel) {
            if (!connections.ContainsKey(channel)) {
                connections[channel] = new UdpClient((int)channel);
            }
            return connections[channel];
        }

        public void Send<T>(Message<T> message, string destination, Channel channel) where T : ISerializable, new() {
            byte[] data = message.Serialize();
            UdpClient connection = GetConnection(channel);
            connection.Send(data, data.Length, destination, (int)channel);
        }

        public Response<T> Receive<T>(Channel channel) where T : ISerializable, new() {
            UdpClient connection = GetConnection(channel);
            if (connection.Available == 0) {
                return null;
            }
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = connection.Receive(ref endPoint);

            // return a response
            return new Response<T>(endPoint.Address.ToString(), data);
        }
    }
}