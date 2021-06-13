using UnityEngine;

namespace Networking.Network
{
    public class Client {
        int inputOffset = 0;
        StateList<InputState> input = new StateList<InputState>();
        StateList<World.State> world = new StateList<World.State>();

        // client predicted world states used to interpolate rendered frame
        World.State previousClientWorld = new World.State();
        World.State currentClientWorld = new World.State();

        public void FixedUpdate() {
            // record the next input state
            input.Append(InputState.GetCurrent());
            SendInput();

            // reset world
            ReceiveWorldState();
            World.state = world.GetCurrent();

            // predict next frame by replaying all player input not incorporated into the current world state
            world.frame++;
            for (int f = world.frame - inputOffset; f <= input.topFrame; f++)
            {
                // update objects that we own with our input
                foreach (NetworkObject networkObject in World.objects.Values)
                {
                    if (networkObject.owner == NetworkManager.localAddress)
                    {
                        InputState previousInputState = input.Get(f - 1);
                        InputState currentInputState = input.Get(f);
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

        void SendInput()
        {
            Message<InputState> message = new Message<InputState>();
            message.worldFrame = world.frame;
            message.inputFrame = input.topFrame;
            message.content = input.GetTop();
            Platform.API.Send(message, NetworkManager.hostAddress, Channel.Input);
        }

        void ReceiveWorldState()
        {
            Response<World.State> response;
            while ((response = Platform.API.Receive<World.State>(Channel.World)) != null)
            {
                // calculate the offset from input frame to world frame
                inputOffset = response.message.worldFrame - response.message.inputFrame;
                world.Add(response.message.worldFrame, response.message.content);
            }
        }
    }
}