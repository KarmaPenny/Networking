using System.Collections.Generic;
using UnityEngine;

namespace Networking.Network
{
    public class Server {
        Dictionary<string, StateList<ClientState>> clientStates = new Dictionary<string, StateList<ClientState>>();
        Dictionary<string, StateList<ServerState>> serverStates = new Dictionary<string, StateList<ServerState>>();
        StateList<World.State> world = new StateList<World.State>();

        public void FixedUpdate() {
            // reset world state
            World.state = world.GetTop();

            // receive client states
            ReceiveClientState();

            // update network objects
            foreach (NetworkObject networkObject in World.objects.Values) {
                InputHistory input = null;
                if (clientStates.ContainsKey(networkObject.owner)) {
                    InputState previousInputState = clientStates[networkObject.owner].GetPrevious().inputState;
                    InputState currentInputState = clientStates[networkObject.owner].GetCurrent().inputState;
                    input = new InputHistory(previousInputState, currentInputState);
                }
                networkObject.NetworkUpdate(input);
            }

            // run physics
            Physics.Simulate(Time.fixedDeltaTime);

            // record the new world state
            world.Append(World.state);

            // send server state to clients
            foreach (string clientAddress in clientStates.Keys) {
                SendServerState(clientAddress);
                clientStates[clientAddress].frame++;
            }
        }

        void ReceiveClientState() {
            Response<ClientState> response;
            while ((response = Platform.API.Receive<ClientState>(Channel.ClientState)) != null) {
                if (!clientStates.ContainsKey(response.address)) {
                    clientStates[response.address] = new StateList<ClientState>();
                    serverStates[response.address] = new StateList<ServerState>();
                    World.Spawn(NetworkManager.singleton.playerPrefabPath, response.address);
                }
                ClientState state = response.message.GetContent(clientStates[response.address]);
                clientStates[response.address].Add(state.clientFrame, state);
            }
        }

        void SendServerState(string address) {
            ServerState state = new ServerState() {
                serverFrame = world.topFrame,
                clientFrame = clientStates[address].frame,
                topClientFrame = clientStates[address].topFrame,
                worldState = world.GetTop(),
            };
            serverStates[address].Append(state);

            int referenceFrame = clientStates[address].GetTop().topServerFrame;

            Message<ServerState> message = new Message<ServerState>(serverStates[address], referenceFrame);
            Platform.API.Send(message, address, Channel.ServerState);
        }
    }
}