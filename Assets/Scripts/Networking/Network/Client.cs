using UnityEngine;

namespace Networking.Network
{
    public class Client {
        StateList<ClientState> clientState = new StateList<ClientState>();
        StateList<ServerState> serverState = new StateList<ServerState>();

        // client predicted world states used to interpolate rendered frame
        World.State previousClientWorld = new World.State();
        World.State currentClientWorld = new World.State();

        public void FixedUpdate() {
            // update client state
            clientState.Append(new ClientState() {
                clientFrame = clientState.topFrame + 1,
                topServerFrame = serverState.GetTop().serverFrame,
                inputState = InputState.GetCurrent(),
            });
            SendClientState();

            // reset world
            ReceiveServerState();
            World.state = serverState.GetCurrent().worldState;

            // predict next frame by replaying all player input not incorporated into the current world state
            serverState.frame++;
            for (int f = serverState.GetCurrent().clientFrame; f <= clientState.topFrame; f++)
            {
                // update objects that we own with our input
                foreach (NetworkObject networkObject in World.objects.Values)
                {
                    if (networkObject.owner == NetworkManager.localAddress)
                    {
                        InputState previousInputState = clientState.Get(f - 1).inputState;
                        InputState currentInputState = clientState.Get(f).inputState;
                        networkObject.NetworkUpdate(new InputHistory(previousInputState, currentInputState));
                    }
                }

                // simulate physics
                Physics.Simulate(Time.fixedDeltaTime);
            }

            // save world state for interpolation
            previousClientWorld = currentClientWorld;
            currentClientWorld = World.state;
        }

        public void Update()
        {
            // interpolate world between most recent fixed frames to smooth things out
            float factor = 1 - ((Time.fixedTime - Time.time) / Time.fixedDeltaTime);
            World.Interpolate(previousClientWorld, currentClientWorld, factor);
        }

        void SendClientState()
        {
            int referenceFrame = serverState.GetTop().topClientFrame;
            Message<ClientState> message = new Message<ClientState>(clientState, referenceFrame);
            Platform.API.Send(message, NetworkManager.hostAddress, Channel.ClientState);
        }

        void ReceiveServerState()
        {
            Response<ServerState> response;
            while ((response = Platform.API.Receive<ServerState>(Channel.ServerState)) != null)
            {
                ServerState state = response.message.GetContent(serverState);
                serverState.Add(state.serverFrame, state);
            }
        }
    }
}