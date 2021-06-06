using UnityEngine;
using Networking;

public class Player : NetworkComponent
{
    public override void NetworkUpdate(InputHistory input)
    {
        Vector3 move = input.GetVector3("Move");
        transform.position += move * 6 * Time.fixedDeltaTime;
    }
}
