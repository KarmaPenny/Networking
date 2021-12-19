using UnityEngine;

namespace Networking {
    public class InputHistory {
        public InputState previousInputState;
        public InputState currentInputState;

        public InputHistory() {
            this.previousInputState = new InputState();
            this.currentInputState = new InputState();
        }

        public InputHistory(InputState previousInputState, InputState currentInputState) {
            this.previousInputState = previousInputState;
            this.currentInputState = currentInputState;
        }

        public bool IsPressed(string action) {
            return currentInputState.Get<bool>(action);
        }

        public bool WasPressed(string action) {
            return !previousInputState.Get<bool>(action) && currentInputState.Get<bool>(action);
        }

        public bool WasReleased(string action) {
            return previousInputState.Get<bool>(action) && !currentInputState.Get<bool>(action);
        }

        public Vector2 GetVector2(string action) {
            return currentInputState.Get<Vector2>(action);
        }

        public Vector3 GetVector3(string action, Vector3 forward, Vector3 right) {
            Vector2 direction = currentInputState.Get<Vector2>(action);
            return direction.x * right + direction.y * forward;
        }

        public Vector3 GetVector3(string action) {
            return GetVector3(action, Vector3.forward, Vector3.right);
        }
    }
}