using System;
using System.Collections.Generic;
using Steamworks;

namespace Networking.Network.Platforms.Steam {
    public class PlatformAPI : IPlatformAPI {
        // must keep reference to prevent garabage collection from destroying
        private List<object> callbacks = new List<object>();
        private void RegisterCallback<T>(Callback<T>.DispatchDelegate func) {
            callbacks.Add(Callback<T>.Create(func));
        }

        public void Initialize() {
            // set the local address
            NetworkManager.localAddress = SteamUser.GetSteamID().ToString();

            // register callbacks
            RegisterCallback<LobbyCreated_t>(OnLobbyCreated);
            RegisterCallback<GameLobbyJoinRequested_t>(OnGameLobbyJoinRequested);
            RegisterCallback<LobbyEnter_t>(OnLobbyEnter);
            RegisterCallback<P2PSessionRequest_t>(OnSessionRequest);
        }

        public void CreateMatch(int maxPlayers) {
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, maxPlayers);
        }

        private void OnLobbyCreated(LobbyCreated_t callback) {
            if (callback.m_eResult != EResult.k_EResultOK) {
                NetworkManager.OnConnectionError("Failed to create lobby: " + callback.m_eResult);
                return;
            }
        }

        private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback) {
            NetworkManager.OnJoiningGame();
            SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
        }

        private void OnLobbyEnter(LobbyEnter_t callback) {
            CSteamID lobbyId = new CSteamID(callback.m_ulSteamIDLobby);

            // if we are the host then set the host address in the lobby to point to us so that clients know where to connect
            if (NetworkManager.isHost) {
                if (!SteamMatchmaking.SetLobbyData(lobbyId, "hostAddress", NetworkManager.localAddress)) {
                    NetworkManager.OnConnectionError("Failed to set host address");
                    return;
                }
            }

            // get the host address from the lobby so we know where to connect
            NetworkManager.hostAddress = SteamMatchmaking.GetLobbyData(lobbyId, "hostAddress");
            if (NetworkManager.hostAddress == "") {
                NetworkManager.OnConnectionError("Failed to get host address");
            }
            
            // let the manager know we have joined
            NetworkManager.OnGameJoined();
        }

        private void OnSessionRequest(P2PSessionRequest_t callback) {
            CSteamID remoteAddress = callback.m_steamIDRemote;
            SteamNetworking.AcceptP2PSessionWithUser(remoteAddress);
        }

        public void Send<T>(Message<T> message, string destination, Channel channel) where T : ISerializable, new() {
            CSteamID hostId = (CSteamID)Convert.ToUInt64(destination);
            byte[] data = message.Serialize();
            SteamNetworking.SendP2PPacket(hostId, data, (uint)data.Length, EP2PSend.k_EP2PSendReliable, (int)channel);
        }

        public Response<T> Receive<T>(Channel channel) where T : ISerializable, new() {
            // get size of next packet
            uint size;
            SteamNetworking.IsP2PPacketAvailable(out size, (int)channel);

            // if there is no next packet then return null
            if (size == 0) {
                return null;
            }

            // read the next packet
            byte[] data = new byte[size];
            uint bytesRead;
            CSteamID source;
            if (!SteamNetworking.ReadP2PPacket(data, size, out bytesRead, out source, (int)channel)) {
                return null;
            }

            // return a response
            return new Response<T>(source.ToString(), data);
        }
    }
}