using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Networking.Network
{
    public static class World
    {
        static int nextId;
        public static SortedDictionary<int, NetworkObject> objects = new SortedDictionary<int, NetworkObject>();
        
        static Dictionary<string, GameObject> resourceCache = new Dictionary<string, GameObject>();
        static AsyncOperation sceneLoader = null;

        public class State : ISerializable
        {
            public int scene = NetworkManager.singleton.onlineScene;
            public SortedDictionary<int, NetworkObject.State> objectStates = new SortedDictionary<int, NetworkObject.State>();

            public void Deserialize(Buffer buffer)
            {
                scene = buffer.ReadInt();
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
                buffer.WriteInt(scene);
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
                s.scene = SceneManager.GetActiveScene().buildIndex;
                foreach (NetworkObject networkGameObject in objects.Values)
                {
                    NetworkObject.State objectState = networkGameObject.state;
                    s.objectStates[objectState.id] = objectState;
                }
                return s;
            }

            set
            {
                // dont load scene if we are the host because the server already loaded it
                if (!NetworkManager.isHost) {
                    // if scene has changed
                    if (value.scene != SceneManager.GetActiveScene().buildIndex) {
                        // if we are not currently loading a scene
                        if (sceneLoader == null || sceneLoader.isDone) {
                            sceneLoader = SceneManager.LoadSceneAsync(value.scene);
                        }
                    }
                }
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

        public static GameObject Load(string path)
        {
            if (!resourceCache.ContainsKey(path))
            {
                resourceCache[path] = Resources.Load<GameObject>(path);
            }
            return resourceCache[path];
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