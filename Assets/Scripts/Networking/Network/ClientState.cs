using System.Collections.Generic;

namespace Networking.Network
{
    public class ClientState
    {
        public int inputFrame;
        public Dictionary<int, InputState> input = new Dictionary<int, InputState>();

        public int highestInputFrameReceived;

        public ClientState(int inputFrame) {
            this.inputFrame = inputFrame;
        }

        public InputState GetCurrentInputState() {
            return GetInputState(inputFrame);
        }

        public InputState GetPreviousInputState() {
            return GetInputState(inputFrame - 1);
        }

        public InputState GetInputState(int f) {
            if (!input.ContainsKey(f)) {
                if (!input.ContainsKey(f - 1)) {
                    input[f - 1] = new InputState();
                }
                input[f] = input[f - 1];
            }
            return input[f];
        }

        public void RemoveOldInput() {
            if (input.ContainsKey(inputFrame - 300)) {
                input.Remove(inputFrame - 300);
            }
        }
    }
}