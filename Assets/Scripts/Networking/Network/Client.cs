using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Networking.Network
{
    public class Client {
        bool synced = false;

        int inputOffset = 0;

        int inputFrame = 0;
        Dictionary<int, InputState> input = new Dictionary<int, InputState>();

        int worldFrame = 0;
        Dictionary<int, World.State> world = new Dictionary<int, World.State>();

        // client predicted world states used to interpolate rendered frame
        World.State previousClientWorld = new World.State();
        World.State currentClientWorld = new World.State();

        AsyncOperation sceneLoader = null;

        public void FixedUpdate() {
            // record the next input state
            if (input.ContainsKey(inputFrame - 300)) {
                input.Remove(inputFrame - 300);
            }
            inputFrame++;
            input[inputFrame] = InputState.GetCurrent();
            SendInput();

            // reset world
            ReceiveWorldState();
            if (!world.ContainsKey(worldFrame)) {
                if (!world.ContainsKey(worldFrame - 1)) {
                    world[worldFrame - 1] = new World.State();
                }
                world[worldFrame] = world[worldFrame - 1];
            }
            World.state = world[worldFrame];

            // load scene if it changed and we are not already loading a scene
            if (!NetworkManager.isHost) {
                if (world[worldFrame].scene != SceneManager.GetActiveScene().buildIndex && !NetworkManager.isLoading) {
                    if (sceneLoader == null || sceneLoader.isDone) {
                        sceneLoader = SceneManager.LoadSceneAsync(world[worldFrame].scene);
                    }
                }
            }

            // predict next frame by replaying all player input not incorporated into the current world state
            if (world.ContainsKey(worldFrame - 300)) {
                world.Remove(worldFrame - 300);
            }
            worldFrame++;
            for (int f = worldFrame - inputOffset; f <= inputFrame; f++)
            {
                // update objects that we own with our input
                foreach (NetworkObject networkObject in World.objects.Values)
                {
                    if (networkObject.owner == NetworkManager.localAddress)
                    {
                        if (!input.ContainsKey(f - 1)) {
                            input[f - 1] = new InputState();
                        }
                        if (!input.ContainsKey(f)) {
                            input[f] = input[f - 1];
                        }
                        InputState previousInputState = input[f - 1];
                        InputState currentInputState = input[f];
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
            message.worldFrame = worldFrame;
            message.inputFrame = inputFrame;
            message.content = input[inputFrame];
            Platform.API.Send(message, NetworkManager.hostAddress, Channel.Input);
        }

        void ReceiveWorldState()
        {
            Network.Response<World.State> response;
            while ((response = Platform.API.Receive<World.State>(Channel.World)) != null)
            {
                // start a few frames behind the server to allow time for packets to arrive and get sorted
                if (!synced)
                {
                    synced = true;
                    worldFrame = response.message.worldFrame - 5;
                    world.Clear();
                }

                // if we get way behind then jump back up
                if (response.message.worldFrame > worldFrame + 10) {
                    worldFrame = response.message.worldFrame - 5;
                }

                // calculate the offset from input frame to world frame
                inputOffset = response.message.worldFrame - response.message.inputFrame;

                // add the world frame unless it is too old to be of use
                if (response.message.worldFrame >= worldFrame) {
                    world[response.message.worldFrame] = response.message.content;
                }
            }
        }
    }
}