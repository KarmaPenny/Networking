using UnityEngine;
using Networking;

public class Bomb : NetworkComponent {
    float detonationTime;

    public override void NetworkStart() {
        detonationTime = NetworkManager.time + 3.0f;
    }

    public override void NetworkUpdate(InputHistory input) {
        if (NetworkManager.time >= detonationTime) {
            Despawn();
        }
    }
}

