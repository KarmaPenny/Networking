using System.Collections.Generic;
using UnityEngine;

namespace Networking.Network
{
    public static class World
    {
        public class State : ISerializable
        {
            public SortedDictionary<int, NetworkObject.State> objectStates = new SortedDictionary<int, NetworkObject.State>();

            public void Deserialize(Buffer buffer)
            {
                while (!buffer.Empty)
                {
                    NetworkObject.State objectState = new NetworkObject.State();
                    objectState.Deserialize(buffer);
                    objectStates[objectState.id] = objectState;
                }
            }

            public byte[] Serialize()
            {
                Buffer buffer = new Buffer();
                foreach (NetworkObject.State objectState in objectStates.Values)
                {
                    buffer.WriteBytes(objectState.Serialize());
                }
                return buffer.ToArray();
            }
        }

        public static State state
        {
            get
            {
                State s = new State();
                foreach (NetworkObject networkGameObject in objects.Values)
                {
                    NetworkObject.State objectState = networkGameObject.state;
                    s.objectStates[objectState.id] = objectState;
                }
                return s;
            }

            set
            {
                List<int> ids = new List<int>(value.objectStates.Keys);
                foreach (int id in ids)
                {
                    NetworkObject.State objectState = value.objectStates[id];
                    if (!objects.ContainsKey(id))
                    {
                        Spawn(objectState.resourcePath, objectState.owner, objectState.id);
                    }

                    objects[id].state = value.objectStates[id];
                }

                // delete objects that are not in the state
                ids = new List<int>(objects.Keys);
                foreach (int id in ids) {
                    if (!value.objectStates.ContainsKey(id)) {
                        GameObject.Destroy(objects[id].gameObject, 0);
                        objects.Remove(id);
                    }
                }
            }
        }

        static int nextId;
        public static SortedDictionary<int, NetworkObject> objects = new SortedDictionary<int, NetworkObject>();

        // cache loaded resource for faster load times of frequently spawned objects
        static Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();
        public static GameObject Load(string path)
        {
            if (!cache.ContainsKey(path))
            {
                cache[path] = Resources.Load<GameObject>(path);
            }
            return cache[path];
        }

        public static GameObject Spawn(string path, string owner) {
            nextId++;
            return Spawn(path, owner, nextId);
        }

        public static GameObject Spawn(string path, string owner, int id)
        {
            // load the prefab from file or cache if it has already been loaded
            GameObject prefab = Load(path);

            // create a new instance from the prefab
            GameObject gameObject = Object.Instantiate(prefab);

            // add a network identity to the object and set its id and prefab path so that clients know what to spawn
            NetworkObject networkObject = gameObject.AddComponent<NetworkObject>();
            networkObject.id = id;
            networkObject.resourcePath = path;
            networkObject.owner = owner;

            // add the network identity to a list of tracked objects
            objects[networkObject.id] = networkObject;

            // reutrn the new object in case the caller wants to do something with it
            return gameObject;
        }

        public static void Interpolate(State prevState, State nextState, float factor)
        {
            foreach (int id in prevState.objectStates.Keys)
            {
                // skip if object does not exist
                if (!objects.ContainsKey(id))
                {
                    continue;
                }

                // use previous state if there is no next state
                NetworkObject.State prevObjectState = prevState.objectStates[id];
                if (!nextState.objectStates.ContainsKey(id))
                {
                    objects[id].state = prevObjectState;
                    continue;
                }

                NetworkObject.State nextObjectState = nextState.objectStates[id];
                objects[id].Interpolate(prevObjectState, nextObjectState, factor);
            }
        }
    }
}