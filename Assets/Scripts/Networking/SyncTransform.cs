using UnityEngine;

namespace Networking
{
    public class SyncTransform : NetworkComponent
    {
        [Sync]
        public Vector3 Position
        {
            get
            {
                return transform.position;
            }

            set
            {
                transform.position = value;
            }
        }

        [Sync]
        public Quaternion Rotation
        {
            get
            {
                return transform.rotation;
            }

            set
            {
                transform.rotation = value;
            }
        }
    }
}