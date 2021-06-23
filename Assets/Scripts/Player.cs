using UnityEngine;
using Networking;

public class Player : NetworkComponent {
    public override void NetworkUpdate(InputHistory input) {
        Vector3 move = input.GetVector3("Move");
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.velocity = move * 6;

        if (input.WasPressed("Fire")) {
            GameObject bomb = Spawn("Bomb");
            float x = Mathf.Round(transform.position.x);
            float z = Mathf.Round(transform.position.z);
            bomb.transform.position = new Vector3(x, 0, z);
        }
    }
}
