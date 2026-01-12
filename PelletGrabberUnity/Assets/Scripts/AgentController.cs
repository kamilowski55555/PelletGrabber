// using UnityEngine;
// using Unity.MLAgents;
// using Unity.MLAgents.Actuators;
// using Unity.MLAgents.Sensors;
// using Random = UnityEngine.Random;
//
// [RequireComponent(typeof(Rigidbody))]
// public class AgentController : Agent
// {
//     [SerializeField] private Transform target;
//     [SerializeField] private Rigidbody rb;
//
//     [Header("Movement")]
//     [SerializeField] private float moveSpeed = 2f;      // units/sec
//     [SerializeField] private float turnSpeed = 120f;    // degrees/sec
//     
//     [SerializeField] private float stepPenalty = -0.001f;
//     [SerializeField] private float progressRewardScale = 0.05f; // tuning
//     [SerializeField] private float timeoutPenalty = -1f;
//     
//     [SerializeField] private float Y = 1000f;
//
//     private float prevDist;
//
//     
//
//     public override void Initialize()
//     {
//         if (!rb) rb = GetComponent<Rigidbody>();
//
//         // Stabilność: żeby nie koziołkował
//         rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
//     }
//
//     public override void OnEpisodeBegin()
//     {
//         // Reset fizyki
//         rb.linearVelocity = Vector3.zero;
//         rb.angularVelocity = Vector3.zero;
//
//         transform.localPosition = new Vector3(0f, Y, 0f);
//         transform.localRotation = Quaternion.identity;
//
//         // Prosty spawn targetu (potem rozszerzysz losowanie)
//         int rand = Random.Range(0, 2);
//         target.localPosition = rand == 0
//             ? new Vector3(-4f, Y, 0f)
//             : new Vector3( 4f, Y, 0f);
//     }
//
//     // CAMERA-ONLY: zostaw puste.
//     // Obraz dostarcza Camera Sensor component.
//     public override void CollectObservations(VectorSensor sensor)
//     {
//         // Intentionally empty for camera-only setup.
//     }
//
//     public override void OnActionReceived(ActionBuffers actions)
//     {
//         // 2 continuous actions: throttle, steer
//         float throttle = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
//         float steer    = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
//
//         float dt = Time.fixedDeltaTime;
//
//         // Ruch do przodu/tyłu po osi forward agenta
//         Vector3 deltaMove = transform.forward * (throttle * moveSpeed * dt);
//         rb.MovePosition(rb.position + deltaMove);
//
//         // Skręt wokół osi Y
//         float deltaYaw = steer * turnSpeed * dt;
//         rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, deltaYaw, 0f));
//
//         // Na razie bez rewardów – skupiamy się na ruchu
//     }
//
//     public override void Heuristic(in ActionBuffers actionsOut)
//     {
//         var a = actionsOut.ContinuousActions;
//
//         // WS / strzałki: throttle
//         a[0] = Input.GetAxisRaw("Vertical");
//
//         // AD / strzałki: steer
//         a[1] = Input.GetAxisRaw("Horizontal");
//     }
//
//     private void OnTriggerEnter(Collider other)
//     {
//         if (other.CompareTag("Pellet"))
//         {
//             AddReward(1f);
//             EndEpisode();
//         }
//     }
//
//     private void OnCollisionEnter(Collision collision)
//     {
//         if (collision.collider.CompareTag("Wall"))
//         {
//             // Na razie tylko kara (bez kończenia) – do testów
//             AddReward(-0.05f);
//
//             // Opcjonalnie: jeśli chcesz twardy fail:
//             // AddReward(-1f);
//             // EndEpisode();
//         }
//     }
// }

using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody))]
public class AgentController : Agent
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private Rigidbody rb;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;     // units/sec
    [SerializeField] private float turnSpeed = 120f;   // deg/sec
    [Tooltip("If true, throttle is clamped to [0,1] (no reverse).")]
    [SerializeField] private bool noReverse = true;

    [Header("Episode Setup")]
    [SerializeField] private float agentSpawnY = 0.25f;
    [SerializeField] private float targetSpawnY = 0.5f;
    [SerializeField] private float startYawRange = 30f; // degrees
    [SerializeField] private float spawnX = 0f;
    [SerializeField] private float spawnZ = 0f;

    [Header("Rewards")]
    [SerializeField] private float goalReward = 1.0f;
    [SerializeField] private float wallHitPenalty = -0.05f;
    [SerializeField] private float stepPenalty = -0.001f;
    [SerializeField] private float progressRewardScale = 0.05f;

    // internal state
    private float prevDist;

    public override void Initialize()
    {
        if (!rb) rb = GetComponent<Rigidbody>();

        // keep agent upright
        rb.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ |
            RigidbodyConstraints.FreezeRotationY;


    }

    public override void OnEpisodeBegin()
    {
        // Reset physics
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Reset pose
        transform.localPosition = new Vector3(spawnX, agentSpawnY, spawnZ);

        float yaw = Random.Range(-startYawRange, startYawRange);
        transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

        // Randomize target (left / right)
        int rand = Random.Range(0, 2);
        target.localPosition = rand == 0
            ? new Vector3(-4f, targetSpawnY, 0f)
            : new Vector3( 4f, targetSpawnY, 0f);

        // Initialize shaping distance
        prevDist = Vector3.Distance(transform.localPosition, target.localPosition);
    }

    // CAMERA-ONLY: no vector observations
    public override void CollectObservations(VectorSensor sensor)
    {
        // Intentionally empty: visuals are provided by Camera Sensor component.
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Continuous actions: [throttle, steer]
        float throttle = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float steer = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

        if (noReverse)
            throttle = Mathf.Clamp01(throttle);


        float dt = Time.fixedDeltaTime;

        // Move forward/back
        Vector3 deltaMove = transform.forward * (throttle * moveSpeed * dt);
        rb.MovePosition(rb.position + deltaMove);

        // Turn
        float deltaYaw = steer * turnSpeed * dt;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, deltaYaw, 0f));

        // Rewards: step penalty + progress shaping
        AddReward(stepPenalty);

        float dist = Vector3.Distance(transform.localPosition, target.localPosition);
        float progress = prevDist - dist; // positive if getting closer
        AddReward(progress * progressRewardScale);
        prevDist = dist;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var a = actionsOut.ContinuousActions;
        a[0] = Input.GetAxisRaw("Vertical");   // throttle
        a[1] = Input.GetAxisRaw("Horizontal"); // steer
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Pellet"))
        {
            AddReward(goalReward);
            EndEpisode();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Wall"))
        {
            AddReward(wallHitPenalty);
            // Not ending episode by default (better for wall-follow tasks)
            // If you want a hard fail, uncomment:
            // EndEpisode();
        }
    }
}

