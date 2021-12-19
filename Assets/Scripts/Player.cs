using UnityEngine;
using Networking;

public class Player : NetworkComponent {
    public float max_speed = 5;
    public float max_ground_acceleration = 50;
    public float max_air_acceleration = 10;
    public float max_jump_duration = 0.4f;
    public float jump_start_velocity = 2.5f;
    public float max_slope = 45f;

    private float radius = 0.75f;
    [HideInInspector, Sync] public float jump_start_time = 0;

    public float GetGroundSlope() {
        RaycastHit hit = new RaycastHit();
        LayerMask ground = LayerMask.GetMask(new string[] { "Default" });
        Ray ray = new Ray(transform.position + transform.up *(radius + Physics.defaultContactOffset), -transform.up);
        if (!Physics.SphereCast(ray, radius, out hit, Physics.defaultContactOffset * 2, ground)) {
            return -1;
        }
        return Vector3.Angle(hit.normal, transform.up);
    }

    public override void NetworkUpdate(InputHistory input) {
        // check if player is on the ground
        float slope = GetGroundSlope();
        bool grounded = slope >= 0;
        bool sliding = slope > max_slope;
        
        // get our current velocity excluding vertical motion
        Rigidbody rb = GetComponent<Rigidbody>();
        Vector3 current_velocity = Vector3.ProjectOnPlane(rb.velocity, transform.up);

        // get desired velocity from input using camera and transform oreintation
        // TODO: use actual camera object
        Vector3 camera_forward = Vector3.forward;
        Vector3 forward = Vector3.ProjectOnPlane(camera_forward, transform.up).normalized;
        Vector3 right = Vector3.Cross(transform.up, forward);
        Vector3 desired_velocity = input.GetVector3("Move", forward, right) * max_speed;

        // accelerate towards desired velocity at a max acceleration rate
        Vector3 required_acceleration = (desired_velocity - current_velocity) / Time.fixedDeltaTime;
        float max_acceleration = (grounded) ? max_ground_acceleration : max_air_acceleration;
        Vector3 acceleration = Vector3.ClampMagnitude(required_acceleration, max_acceleration);
        rb.AddForce(acceleration, ForceMode.Acceleration);

        // start jump
        if (input.WasPressed("Fire") && grounded) {
            jump_start_time = NetworkManager.time;
            rb.AddForce(transform.up * jump_start_velocity, ForceMode.VelocityChange);
        }

        // stop jump
        if (!input.IsPressed("Fire")) {
            jump_start_time = -100;
        }

        // apply jump force while jump is held up to max jump duration
        if (input.IsPressed("Fire") && NetworkManager.time <= jump_start_time + max_jump_duration) {
            rb.AddForce(transform.up * Physics.gravity.magnitude, ForceMode.Acceleration);
        }
    }
}
