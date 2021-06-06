// TODO: us #if statments to import appropriate platform
using Networking.Network.Platforms.Steam;

namespace Networking.Network {
    public static class Platform {
        public static IPlatformAPI API = new PlatformAPI();
    }
}