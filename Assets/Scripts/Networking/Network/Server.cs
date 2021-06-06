using System.Collections.Generic;
using UnityEngine;

namespace Networking.Network
{
    public class Server {
        Dictionary<string, ClientState> clients = new Dictionary<string, ClientState>();

        int worldFrame = 0;
        Dictionary<int, World.State> world = new Dictionary<int, World.State>();
        
        string playerPrefabPath;

        public Server(string playerPrefabPath) {
            this.playerPrefabPath = playerPrefabPath;
        }

        public void FixedUpdate() {
            // reset world state
            if (!world.ContainsKey(worldFrame)) {
                world[worldFrame] = new World.State();
            }
            World.state = world[worldFrame];

            // receive input from players
            ReceiveInput();

            // spawn new players
            foreach(string clientId in clients.Keys) {
                if (!clients[clientId].spawned) {
                    World.Spawn(playerPrefabPath, clientId);
                    clients[clientId].spawned = true;
                }
            }

            // update network objects
            foreach (NetworkObject networkObject in World.objects.Values) {
                InputHistory input = null;
                if (clients.ContainsKey(networkObject.owner)) {
                    InputState previousInputState = clients[networkObject.owner].GetPreviousInputState();
                    InputState currentInputState = clients[networkObject.owner].GetCurrentInputState();
                    input = new InputHistory(previousInputState, currentInputState);
                }
                networkObject.NetworkUpdate(input);
            }

            // run physics
            Physics.Simulate(Time.fixedDeltaTime);

            // record the new world state
            if (world.ContainsKey(worldFrame - 300)) {
                world.Remove(worldFrame - 300);
            }
            worldFrame++;
            world[worldFrame] = World.state;
            foreach (string clientAddress in clients.Keys) {
                SendWorldState(world[worldFrame], clientAddress);
                clients[clientAddress].RemoveOldInput();
                clients[clientAddress].inputFrame++;
            }

            // List<string> lines = new List<string>();
            // lines.Add("Server Status");
            // lines.Add("world frame = " + worldFrame);
            // foreach (string clientAddress in clients.Keys) {
            //     lines.Add(clientAddress + ": " + clients[clientAddress].inputFrame + " - " + clients[clientAddress].highestInputFrameReceived);
            // }
            // Debug.LogError(string.Join("\n", lines));
        }

        void ReceiveInput() {
            Network.Response<InputState> response;
            while ((response = Platform.API.Receive<InputState>(Channel.Input)) != null) {
                // add new clients to client list
                if (!clients.ContainsKey(response.address)) {
                    // start a few frames behind so new packets have time to come in and get sorted before they are needed
                    clients[response.address] = new ClientState(response.message.inputFrame - 5);
                }

                if (response.message.inputFrame > clients[response.address].highestInputFrameReceived) {
                    clients[response.address].highestInputFrameReceived = response.message.inputFrame;
                }

                // if we get way behind then jump back up
                if (response.message.inputFrame > clients[response.address].inputFrame + 10) {
                    clients[response.address].inputFrame = response.message.inputFrame - 5;
                }

                // add client input if it is not too old to be used
                if (response.message.inputFrame >= clients[response.address].inputFrame) {
                    clients[response.address].input[response.message.inputFrame] = response.message.content;
                }
            }
        }

        void SendWorldState(World.State state, string clientAddress) {
            Message<World.State> message = new Message<World.State>();
            message.worldFrame = worldFrame;
            message.inputFrame = clients[clientAddress].inputFrame;
            message.content = world[worldFrame];
            Platform.API.Send(message, clientAddress, Channel.World);
        }
    }
}