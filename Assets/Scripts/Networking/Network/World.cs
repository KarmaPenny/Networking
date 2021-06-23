using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Networking.Network {
    public static class World {
        public static SortedDictionary<string, NetworkObject> objects = new SortedDictionary<string, NetworkObject>();
        
        static Dictionary<string, GameObject> resourceCache = new Dictionary<string, GameObject>();
        static AsyncOperation sceneLoader = null;

        public class State : ISerializable {
            public int scene = NetworkManager.singleton.onlineScene;
            public SortedDictionary<string, NetworkObject.State> objectStates = new SortedDictionary<string, NetworkObject.State>();

            public byte[] Serialize() {
                Buffer buffer = new Buffer();
                buffer.WriteInt(scene);
                foreach (NetworkObject.State objectState in objectStates.Values) {
                    buffer.WriteBytes(objectState.Serialize());
                }
                return buffer.ToArray();
            }

            public void Deserialize(Buffer buffer) {
                scene = buffer.ReadInt();
                while (!buffer.Empty) {
                    NetworkObject.State objectState = new NetworkObject.State();
                    objectState.Deserialize(buffer);
                    objectStates[objectState.key] = objectState;
                }
            }
        }

        public static State state {
            get {
                State s = new State();
                s.scene = SceneManager.GetActiveScene().buildIndex;
                foreach (NetworkObject networkGameObject in objects.Values) {
                    if (networkGameObject.gameObject.activeSelf) {
                        NetworkObject.State objectState = networkGameObject.state;
                        s.objectStates[objectState.key] = objectState;
                    }
                }
                return s;
            }

            set {
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

                foreach (string key in value.objectStates.Keys) {
                    // spawn object if it does not exist yet
                    NetworkObject.State objectState = value.objectStates[key];
                    if (!objects.ContainsKey(key)) {
                        Spawn(objectState.resourcePath, objectState.owner, objectState.id);
                    }

                    // activate the object and set the state
                    objects[key].gameObject.SetActive(true);
                    objects[key].state = value.objectStates[key];
                }

                // disable objects that are not in the state
                foreach (string key in objects.Keys) {
                    if (!value.objectStates.ContainsKey(key)) {
                        objects[key].gameObject.SetActive(false);
                    }
                }
            }
        }

        public static GameObject Load(string path) {
            if (!resourceCache.ContainsKey(path)) {
                resourceCache[path] = Resources.Load<GameObject>(path);
            }
            return resourceCache[path];
        }

        public static GameObject Spawn(string path, string owner) {
            int id = 0;
            if (NetworkManager.isServer) {
                ClientState clientState = NetworkManager.server.clientStates[owner].GetCurrent();
                id = clientState.nextSpawnId + clientState.nextSpawnIdOffset;
                clientState.nextSpawnIdOffset++;
            } else {
                id = NetworkManager.client.spawnId;
                NetworkManager.client.spawnId++;
            }
            return Spawn(path, owner, id);
        }

        public static GameObject Spawn(string path, string owner, int id) {
            // if the object is already spawned then just make sure it is turned on
            string key = owner + ":" + id;
            if (objects.ContainsKey(key)) {
                objects[key].gameObject.SetActive(true);
                return objects[key].gameObject;
            }

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
            objects[networkObject.key] = networkObject;

            // run network start
            networkObject.NetworkStart();

            // reutrn the new object in case the caller wants to do something with it
            return gameObject;
        }

        public static void Despawn(NetworkObject networkObject) {
            networkObject.gameObject.SetActive(false);
        }

        public static void Interpolate(State prevState, State nextState, float factor) {
            foreach (string key in prevState.objectStates.Keys) {
                // skip if object does not exist
                if (!objects.ContainsKey(key)) {
                    continue;
                }

                // use previous state if there is no next state
                NetworkObject.State prevObjectState = prevState.objectStates[key];
                if (!nextState.objectStates.ContainsKey(key)) {
                    objects[key].state = prevObjectState;
                    continue;
                }

                NetworkObject.State nextObjectState = nextState.objectStates[key];
                objects[key].Interpolate(prevObjectState, nextObjectState, factor);
            }
        }

        public static void GarbageCollect(State firstState, State lastState) {
            foreach (string key in firstState.objectStates.Keys) {
                if (!lastState.objectStates.ContainsKey(key) && objects.ContainsKey(key)) {
                    GameObject.Destroy(objects[key].gameObject);
                    objects.Remove(key);
                }
            }
        }
    }
}