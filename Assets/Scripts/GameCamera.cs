using UnityEngine;
using Networking;

public class GameCamera : MonoBehaviour {
    Transform target = null;
    public float distance = 4.0f;
    public float clearance = 0.1f;

    void LateUpdate() {
        // find track target if we do not have one already
        if (target == null) {
            foreach (GameObject mount in GameObject.FindGameObjectsWithTag("MainCamera")) {
                if (mount.GetComponentInParent<NetworkObject>().owner == NetworkManager.localAddress) {
                    target = mount.transform;
                }
            }
        }

        // track target
        if (target != null) {
            RaycastHit hit = new RaycastHit();
            LayerMask mask =~ LayerMask.GetMask("Player");
            if (Physics.Raycast(target.position, -target.forward, out hit, distance + clearance, mask)) {
                transform.position = target.position - target.forward * (hit.distance - clearance);
            } else {
                transform.position = target.position - target.forward * distance;
            }
            transform.rotation = target.rotation;
        }
    }
}
