using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Networking.Network;

namespace Networking
{
    public class NetworkComponent : MonoBehaviour
    {
        public class State : SortedDictionary<string, object>, ISerializable
        {
            public void Deserialize(Networking.Network.Buffer buffer)
            {
                List<string> keys = new List<string>(Keys);
                foreach (string key in keys)
                {
                    this[key] = buffer.Read(this[key].GetType());
                }
            }

            public byte[] Serialize()
            {
                Networking.Network.Buffer buffer = new Networking.Network.Buffer();
                foreach (object value in Values)
                {
                    buffer.Write(value);
                }
                return buffer.ToArray();
            }
        }

        public State state
        {
            get
            {
                State s = new State();
                foreach (PropertyInfo property in GetType().GetProperties())
                {
                    // skip properties that are not tagged with the Sync attribute
                    if (property.GetCustomAttribute<Sync>(true) == null)
                    {
                        continue;
                    }
                    s[property.Name] = property.GetValue(this);
                }
                return s;
            }

            set
            {
                foreach (string property in value.Keys)
                {
                    GetType().GetProperty(property).SetValue(this, value[property]);
                }
            }
        }

        public virtual void NetworkStart() {}
        
        public virtual void NetworkUpdate(InputHistory input) { }

        public virtual void Interpolate(State prevState, State nextState, float factor)
        {
            Type type = GetType();
            foreach (string propertyName in prevState.Keys)
            {
                PropertyInfo property = type.GetProperty(propertyName);
                Type propertyType = property.GetType();
                object prevValue = prevState[propertyName];
                object nextValue = nextState[propertyName];

                // use prev value for types that do not interpolate (e.g. strings, bool, etc.)
                object value = prevValue;

                // interpolate various types
                if (propertyType == typeof(float))
                {
                    value = Mathf.Lerp((float)prevValue, (float)nextValue, factor);
                }
                else if (propertyType == typeof(Vector2))
                {
                    value = Vector2.Lerp((Vector2)prevValue, (Vector2)nextValue, factor);
                }
                else if (propertyType == typeof(Vector3))
                {
                    value = Vector3.Lerp((Vector3)prevValue, (Vector3)nextValue, factor);
                }
                else if (propertyType == typeof(Quaternion))
                {
                    value = Quaternion.Lerp((Quaternion)prevValue, (Quaternion)nextValue, factor);
                }

                // set property value
                property.SetValue(this, value);
            }
        }
    
        public GameObject Spawn(string resource) {
            NetworkObject networkObject = GetComponentInParent<NetworkObject>();
            return World.Spawn(resource, networkObject.owner);
        }

        public void Despawn() {
            NetworkObject networkObject = GetComponentInParent<NetworkObject>();
            World.Despawn(networkObject);
        }
    }
}