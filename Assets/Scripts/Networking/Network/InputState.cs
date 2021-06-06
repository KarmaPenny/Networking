using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Networking.Network;

namespace Networking
{
    public class InputState : ISerializable
    {
        public SortedDictionary<string, object> actions = new SortedDictionary<string, object>();

        public InputState()
        {
            foreach (InputAction action in NetworkManager.controlBindings)
            {
                if (action.expectedControlType == "Vector2")
                {
                    actions[action.name] = Vector2.zero;
                }
                else if (action.expectedControlType == "Button")
                {
                    actions[action.name] = false;
                }
            }
        }

        public T Get<T>(string name)
        {
            return (T)actions[name];
        }

        public void Deserialize(Buffer buffer)
        {
            List<string> keys = new List<string>(actions.Keys);
            foreach (string key in keys)
            {
                if (actions[key] is Vector2)
                {
                    actions[key] = buffer.ReadVector2();
                }
                else if (actions[key] is bool)
                {
                    actions[key] = buffer.ReadBool();
                }
            }
        }

        public byte[] Serialize()
        {
            Buffer buffer = new Buffer();
            foreach (object value in actions.Values)
            {
                if (value is Vector2)
                {
                    buffer.WriteVector2((Vector2)value);
                }
                else if (value is bool)
                {
                    buffer.WriteBool((bool)value);
                }
            }
            return buffer.ToArray();
        }

        public static InputState GetCurrent()
        {
            InputState state = new InputState();
            foreach (InputAction action in NetworkManager.controlBindings)
            {
                if (action.expectedControlType == "Vector2")
                {
                    state.actions[action.name] = action.ReadValue<Vector2>();

                }
                else if (action.expectedControlType == "Button")
                {
                    float threshold = InputSystem.settings.defaultButtonPressPoint;
                    state.actions[action.name] = action.ReadValue<float>() >= threshold;
                }
            }
            return state;
        }
    }
}