using UnityEngine;

namespace Networking {
    public class SyncRotation : NetworkComponent {
        [Sync] public Quaternion Rotation {
            get {
                return transform.rotation;
            }

            set {
                transform.rotation = value;
            }
        }
    }
}