using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Networking.Network;

namespace Networking {
    public class NetworkComponent : MonoBehaviour {
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

        public State state {
            get {
                State s = new State();
                foreach (string name in SyncVars.Keys) {
                    s[name] = SyncVars[name].GetValue(this);
                }
                return s;
            }

            set {
                foreach (string name in value.Keys) {
                    SyncVars[name].SetValue(this, value[name]);
                }
            }
        }

        private SortedDictionary<string, dynamic> syncVars = null;
        private SortedDictionary<string, dynamic> SyncVars {
            get {
                if (syncVars == null) {
                    syncVars = new SortedDictionary<string, dynamic>();
                    Type t = GetType();
                    foreach (PropertyInfo property in t.GetProperties()) {
                        if (property.GetCustomAttribute<Sync>(true) != null) {
                            syncVars[property.Name] = property;
                        }
                    }
                    foreach (FieldInfo field in t.GetFields()) {
                        if (field.GetCustomAttribute<Sync>(true) != null) {
                            syncVars[field.Name] = field;
                        }
                    }
                }
                return syncVars;
            }
        }

        public virtual void NetworkStart() {}
        
        public virtual void NetworkUpdate(InputHistory input) {}

        public virtual void Interpolate(State prevState, State nextState, float factor) {
            foreach (string name in SyncVars.Keys) {
                object prevValue = prevState[name];
                object nextValue = nextState[name];
                
                // use prev value for types that do not interpolate (e.g. strings, bool, etc.)
                object value = prevValue;

                // interpolate various types
                Type type = SyncVars[name].GetType();
                if (type == typeof(float))
                {
                    value = Mathf.Lerp((float)prevValue, (float)nextValue, factor);
                }
                else if (type == typeof(Vector2))
                {
                    value = Vector2.Lerp((Vector2)prevValue, (Vector2)nextValue, factor);
                }
                else if (type == typeof(Vector3))
                {
                    value = Vector3.Lerp((Vector3)prevValue, (Vector3)nextValue, factor);
                }
                else if (type == typeof(Quaternion))
                {
                    value = Quaternion.Lerp((Quaternion)prevValue, (Quaternion)nextValue, factor);
                }

                SyncVars[name].SetValue(this, value);
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