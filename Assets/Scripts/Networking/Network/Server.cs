using System.Collections.Generic;
using UnityEngine;

namespace Networking.Network
{
    public class Server {
        Dictionary<string, StateList<InputState>> clients = new Dictionary<string, StateList<InputState>>();
        StateList<World.State> world = new StateList<World.State>();

        public void FixedUpdate() {
            // reset world state
            World.state = world.GetTop();

            // receive input from players
            ReceiveInput();

            // update network objects
            foreach (NetworkObject networkObject in World.objects.Values) {
                InputHistory input = null;
                if (clients.ContainsKey(networkObject.owner)) {
                    InputState previousInputState = clients[networkObject.owner].GetPrevious();
                    InputState currentInputState = clients[networkObject.owner].GetCurrent();
                    input = new InputHistory(previousInputState, currentInputState);
                }
                networkObject.NetworkUpdate(input);
            }

            // run physics
            Physics.Simulate(Time.fixedDeltaTime);

            // record the new world state
            world.Append(World.state);
            foreach (string clientAddress in clients.Keys) {
                SendWorldState(clientAddress);
                clients[clientAddress].frame++;
            }
        }

        void ReceiveInput() {
            Response<InputState> response;
            while ((response = Platform.API.Receive<InputState>(Channel.Input)) != null) {
                if (!clients.ContainsKey(response.address)) {
                    clients[response.address] = new StateList<InputState>();
                    World.Spawn(NetworkManager.singleton.playerPrefabPath, response.address);
                }
                clients[response.address].Add(response.message.inputFrame, response.message.content);
            }
        }

        void SendWorldState(string clientAddress) {
            Message<World.State> message = new Message<World.State>();
            message.worldFrame = world.topFrame;
            message.inputFrame = clients[clientAddress].frame;
            message.content = world.GetTop();
            Platform.API.Send(message, clientAddress, Channel.World);
        }
    }
}