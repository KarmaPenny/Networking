using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Networking.Network;

namespace Networking
{
    public class NetworkManager : MonoBehaviour {
        public static NetworkManager singleton;

        public static string hostAddress;
        public static string localAddress;

        public static bool isHost;
        public static bool isRunning;
        public static bool isServer;
        public static float time;

        public static Server server;
        public static Client client;

        public static InputActionMap controlBindings;

        public int maxPlayers = 4;
        public int offlineScene = 0;
        public int onlineScene = 1;
        public string playerPrefabPath = "Player";
        public GameObject matchmakingMenu;

        void Start() {
            // setup singleton
            if (singleton != null) {
                Destroy(gameObject, 0);
                return;
            }
            singleton = this;
            DontDestroyOnLoad(gameObject);

            // find control bindings
            controlBindings = GetComponent<PlayerInput>().actions.FindActionMap("Player");

            // initialize the platform api
            Platform.API.Initialize();
        }

        void FixedUpdate() {
            if (isRunning) {
                isServer = false;
                client.FixedUpdate();
                if (isHost) {
                    isServer = true;
                    server.FixedUpdate();
                }
            }
        }

        void Update() {
            if (isRunning) {
                client.Update();
            }
        }

        public void CreateGame() {
            isHost = true;
            matchmakingMenu.SetActive(false);
            Platform.API.CreateMatch(maxPlayers);
        }

        public static void OnJoiningGame() {
            singleton.matchmakingMenu.SetActive(false);
        }

        public static void OnGameJoined() {
            singleton.matchmakingMenu.SetActive(false);
            if (isHost) {
                server = new Server();
                SceneManager.LoadSceneAsync(singleton.onlineScene);
            }
            client = new Client();
            isRunning = true;
        }

        static void Stop() {
            isHost = false;
            isRunning = false;
            server = null;
            client = null;
            singleton.matchmakingMenu.SetActive(true);
        }

        public static void OnConnectionError(string message) {
            Debug.LogError(message);
            Stop();
        }

        // returns the local player object
        public static GameObject GetLocalPlayer() {
            foreach (GameObject p in GameObject.FindGameObjectsWithTag("Player")) {
                NetworkObject netObj = p.GetComponent<NetworkObject>();
                if (netObj.owner == localAddress) {
                    return p;
                }
            }
            return null;
        }
    }
}