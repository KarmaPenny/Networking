using UnityEngine;
using Networking;

public class Player : NetworkComponent {
    // camera vars
    new public Transform camera = null;
    public float max_pitch = 80.0f;

    // movement vars
    public float max_speed = 5;
    public float max_ground_acceleration = 50;
    public float max_air_acceleration = 10;
    public float max_jump_duration = 0.4f;
    public float jump_start_velocity = 2.5f;
    public float max_slope = 45f;

    private float radius = 0.75f;
    [HideInInspector, Sync] public float jump_start_time = 0;

    class Ground {
        public bool contacted;
        public Vector3 normal;
        public float slope;

        public Ground(Transform transform, float radius) {
            RaycastHit hit = new RaycastHit();
            LayerMask groundLayer = LayerMask.GetMask("Default");
            Ray ray = new Ray(transform.position + (transform.up * (radius + (10 * Physics.defaultContactOffset))), -transform.up);
            contacted = Physics.SphereCast(ray, radius, out hit, 20 * Physics.defaultContactOffset, groundLayer);
            normal = (contacted) ? hit.normal : transform.up;
            slope = Vector3.Angle(normal, transform.up);
        }
    }

    public override void NetworkUpdate(InputHistory input) {
        // Apply look input to camera rotation and clamp pitch
        Vector3 look = input.GetVector3("Look", -Vector3.right, Vector3.up) * Time.fixedDeltaTime;
        Vector3 localRotation = camera.localRotation.eulerAngles + look;
        float pitch = localRotation.x % 360.0f;
        if (pitch > 180.0f) {
            pitch -= 360.0f;
        }
        localRotation.x = Mathf.Clamp(pitch, -max_pitch, max_pitch);
        camera.localRotation = Quaternion.Euler(localRotation);

        // Find ground beneath player
        Ground ground = new Ground(transform, radius);
        
        // get our current velocity excluding vertical motion
        Rigidbody rb = GetComponent<Rigidbody>();
        Vector3 current_velocity = Vector3.ProjectOnPlane(rb.velocity, ground.normal);

        // get desired velocity from input and camera orientation
        // project onto transform up plane before ground plane to prevent going backwards on large slopes when looking down
        Vector3 cameraForward = Vector3.ProjectOnPlane(camera.forward, transform.up);
        cameraForward = Vector3.ProjectOnPlane(cameraForward, ground.normal).normalized;
        Vector3 cameraRight = Vector3.ProjectOnPlane(camera.right, ground.normal).normalized;
        Vector3 desired_velocity = input.GetVector3("Move", cameraForward, cameraRight) * max_speed;

        // accelerate towards desired velocity at a max acceleration rate
        Vector3 required_acceleration = (desired_velocity - current_velocity) / Time.fixedDeltaTime;
        float max_acceleration = (ground.contacted) ? max_ground_acceleration : max_air_acceleration;
        Vector3 acceleration = Vector3.ClampMagnitude(required_acceleration, max_acceleration);
        rb.AddForce(acceleration, ForceMode.Acceleration);

        // start jump
        if (input.WasPressed("Jump") && ground.contacted) {
            jump_start_time = NetworkManager.time;
            rb.AddForce(transform.up * jump_start_velocity, ForceMode.VelocityChange);
        }

        // stop jump
        if (!input.IsPressed("Jump")) {
            jump_start_time = -100;
        }

        // apply jump force while jump is held up to max jump duration
        if (input.IsPressed("Jump") && NetworkManager.time <= jump_start_time + max_jump_duration) {
            rb.AddForce(transform.up * Physics.gravity.magnitude, ForceMode.Acceleration);
        }
    }
}
