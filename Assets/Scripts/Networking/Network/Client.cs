using System.Collections.Generic;
using UnityEngine;

namespace Networking.Network
{
    public class Client {
        StateList<ClientState> clientState = new StateList<ClientState>();
        StateList<ServerState> serverState = new StateList<ServerState>();

        int prevSpawnId;
        public int spawnId;

        // client predicted world states used to interpolate rendered frame
        World.State previousClientWorld = new World.State();
        World.State currentClientWorld = new World.State();

        public void FixedUpdate() {
            // reset world
            // TODO: interpolate between predicted state and actual state to smooth descrepancies out
            ReceiveServerState();
            World.state = serverState.GetCurrent().worldState;

            // update client state
            clientState.Append(new ClientState() {
                clientFrame = clientState.topFrame + 1,
                topServerFrame = serverState.GetTop().serverFrame,
                nextSpawnId = prevSpawnId,
                inputState = InputState.GetCurrent(),
            });
            SendClientState();

            // predict next frame by replaying all player input not incorporated into the current world state
            serverState.frame++;
            spawnId = clientState.Get(serverState.GetCurrent().clientFrame).nextSpawnId;
            for (int f = serverState.GetCurrent().clientFrame; f <= clientState.topFrame; f++) {
                // set network time
                NetworkManager.time = (serverState.frame - 1 + f - serverState.GetCurrent().clientFrame) * Time.fixedDeltaTime;

                // get our input history for this frame
                InputState previousInputState = clientState.Get(f - 1).inputState;
                InputState currentInputState = clientState.Get(f).inputState;
                InputHistory input = new InputHistory(previousInputState, currentInputState);

                // update all network objects
                List<NetworkObject> networkObjects = new List<NetworkObject>(World.objects.Values);
                foreach (NetworkObject networkObject in networkObjects) {
                    // use our input for objects we own
                    if (networkObject.gameObject.activeSelf && networkObject.owner == NetworkManager.localAddress) {
                        networkObject.NetworkUpdate(input);
                    } 
                    // use default input for objects we do not own
                    // TODO: use previous input for players?
                    else {
                        networkObject.NetworkUpdate(new InputHistory());
                    }
                }

                // simulate physics
                Physics.Simulate(Time.fixedDeltaTime);
            }
            prevSpawnId = spawnId;

            // save world state for interpolation
            previousClientWorld = currentClientWorld;
            currentClientWorld = World.state;

            // remove despawned objects
            World.GarbageCollect(serverState.GetBottom().worldState, serverState.GetTop().worldState);
        }

        public void Update() {
            // interpolate world between most recent fixed frames to smooth things out
            float factor = 1 - ((Time.fixedTime - Time.time) / Time.fixedDeltaTime);
            World.Interpolate(previousClientWorld, currentClientWorld, factor);
        }

        void SendClientState() {
            int referenceFrame = serverState.GetTop().topClientFrame;
            Message<ClientState> message = new Message<ClientState>(clientState, referenceFrame);
            Platform.API.Send(message, NetworkManager.hostAddress, Channel.ClientState);
        }

        void ReceiveServerState() {
            Response<ServerState> response;
            while ((response = Platform.API.Receive<ServerState>(Channel.ServerState)) != null) {
                // ignore messages from previous connections
                if (response.message.referenceFrame > serverState.topFrame) {
                    continue;
                }
                ServerState state = response.message.GetContent(serverState);
                serverState.Add(state.serverFrame, state);
            }
        }
    }
}