using System.Collections.Generic;
using UnityEngine;
using Networking.Network;

namespace Networking {
    public class NetworkObject : MonoBehaviour {
        public class State : ISerializable {
            public int id;
            public string resourcePath;
            public string owner;
            public List<NetworkComponent.State> componentStates = new List<NetworkComponent.State>();

            public string key {
                get {
                    return owner + ":" + id;
                }
            }

            public void Deserialize(Buffer buffer) {
                // read meta data
                id = buffer.ReadInt();
                resourcePath = buffer.ReadString();
                owner = buffer.ReadString();

                // load the prefab
                GameObject prefab = World.Load(resourcePath);

                // use components in prefab to deserilzie component states
                foreach (NetworkComponent component in prefab.GetComponentsInChildren<NetworkComponent>()) {
                    NetworkComponent.State componentState = component.state;
                    componentState.Deserialize(buffer);
                    componentStates.Add(componentState);
                }
            }

            public byte[] Serialize() {
                Buffer buffer = new Buffer();
                buffer.WriteInt(id);
                buffer.WriteString(resourcePath);
                buffer.WriteString(owner);
                foreach (NetworkComponent.State componentState in componentStates) {
                    buffer.WriteBytes(componentState.Serialize());
                }
                return buffer.ToArray();
            }
        }

        public State state {
            get {
                State s = new State();
                s.id = id;
                s.resourcePath = resourcePath;
                s.owner = owner;
                foreach (NetworkComponent component in components) {
                    s.componentStates.Add(component.state);
                }
                return s;
            }

            set {
                id = value.id;
                resourcePath = value.resourcePath;
                owner = value.owner;
                for (int i = 0; i < components.Length; i++) {
                    components[i].state = value.componentStates[i];
                }
            }
        }

        public int id;
        public string resourcePath;
        public string owner;

        public string key {
            get {
                return owner + ":" + id;
            }
        }

        NetworkComponent[] _components = null;
        NetworkComponent[] components
        {
            get
            {
                if (_components == null)
                {
                    _components = GetComponentsInChildren<NetworkComponent>();
                }
                return _components;
            }
        }

        void Start() {
            DontDestroyOnLoad(gameObject);
        }

        public void NetworkStart() {
            foreach (NetworkComponent component in components) {
                component.NetworkStart();
            }
        }

        public void NetworkUpdate(InputHistory input) {
            foreach (NetworkComponent component in components) {
                component.NetworkUpdate(input);
            }
        }

        public void Interpolate(State prevState, State nextState, float factor) {
            for (int i = 0; i < components.Length; i++) {
                components[i].Interpolate(prevState.componentStates[i], nextState.componentStates[i], factor);
            }
        }
    }
}