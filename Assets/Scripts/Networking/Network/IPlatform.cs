namespace Networking.Network {
    public interface IPlatformAPI {
        void Initialize();
        void CreateMatch(int maxPlayers);
        void Send<T>(Message<T> message, string destination, Channel channel) where T : ISerializable, new();
        Response<T> Receive<T>(Channel channel) where T: ISerializable, new();

    }
}