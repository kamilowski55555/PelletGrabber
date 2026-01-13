using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody))]
public class AgentController : Agent
{
    // =========================================================================
    // 1) REFERENCES / WORLD SETUP (Unity-side wiring)
    // =========================================================================
    [Header("World References")]
    [Tooltip("Root transform of the arena (e.g., Env). Spawns are defined in LOCAL coords of this root.")]
    [SerializeField] private Transform arenaRoot;

    [Tooltip("Target object (e.g., Pellet transform). Must have Collider marked as Trigger + tag 'Pellet'.")]
    [SerializeField] private Transform target;

    [Tooltip("Agent rigidbody. If empty, will be auto-fetched from the same GameObject.")]
    [SerializeField] private Rigidbody rb;

    // =========================================================================
    // 2) MOVEMENT (action -> physics)
    // =========================================================================
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;       // units/sec
    [SerializeField] private float turnSpeed = 120f;     // deg/sec
    [SerializeField] private bool noReverse = true;      // clamp throttle to [0,1]

    // =========================================================================
    // 3) EPISODE RESET / SPAWN (defined in LOCAL arenaRoot space)
    // =========================================================================
    [Header("Episode Reset (LOCAL to arenaRoot)")]
    [SerializeField] private float agentSpawnY = 0.25f;
    [SerializeField] private float targetSpawnY = 0.5f;

    [Tooltip("Random yaw at episode start: [-range, +range] degrees.")]
    [SerializeField] private float startYawRange = 180f;

    [Tooltip("Agent spawn local coordinates (X,Z). Y is controlled by agentSpawnY.")]
    [SerializeField] private float spawnX = 0f;
    [SerializeField] private float spawnZ = 0f;

    [Tooltip("Target X positions (local) used for left/right randomization.")]
    [SerializeField] private float targetXLeft = -4f;
    [SerializeField] private float targetXRight = 4f;

    // =========================================================================
    // 4) STABILITY (physics tuning; prevents tipping without blocking yaw)
    // =========================================================================
    [Header("Stability")]
    [SerializeField] private bool keepUpright = true;

    [Tooltip("Lower center of mass to reduce tipping/rolling in corners.")]
    [SerializeField] private float centerOfMassY = -0.15f;

    // =========================================================================
    // 5) RL SETTINGS (rewards + observations)
    // =========================================================================
    [Header("RL - Rewards")]
    //PRZY MAX STEPS 2000
    //FAZA 1 - PROWADZIMY GO TROCHĘ ZA RĘKE KOMPASEM DO CELU, JAK TO OGARNIE DOBRZE TO FAZA2
    [SerializeField] private float goalReward = 1.0f;
    [SerializeField] private float wallHitPenalty = -0.05f;
    [SerializeField] private float stepPenalty = -0.001f;

    //FAZA 1 - PROWADZIMY GO TROCHĘ ZA RĘKE KOMPASEM DO CELU, JAK TO OGARNIE DOBRZE TO FAZA2
    [Tooltip("Reward for getting closer: reward += (prevDist - dist) * scale")]
    [SerializeField] private float progressRewardScale = 0.0f;
    //FAZA 2 - USTAWIAMY NA 0.0 LECI JUŻ NA SAMEJ WIZJI, BEZ KOMPASU
    
    // Internal RL state
    private float prevDist;
    
    private StatsRecorder stats;
    // =========================================================================
    // UNITY / ML-AGENTS LIFECYCLE
    // =========================================================================

    public override void Initialize()
    {
        stats = Academy.Instance.StatsRecorder;
        // --- Resolve references ---
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!arenaRoot) arenaRoot = transform.parent; // fallback: parent as arena root

        // --- Rigidbody constraints ---
        // Freeze X/Z to keep upright, but DO NOT freeze Y (yaw) because steering needs it.
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // --- Stability tweaks ---
        rb.centerOfMass = new Vector3(0f, centerOfMassY, 0f);

        // (Optional) tiny damping to reduce jitter
        rb.linearDamping = Mathf.Max(rb.linearDamping, 0.05f);
        rb.angularDamping = Mathf.Max(rb.angularDamping, 0.05f);
    }

    public override void OnEpisodeBegin()
    {
        ResetPhysics();
        ResetAgentPose();
        ResetTargetPose();

        // Initialize shaping distance
        prevDist = Vector3.Distance(rb.position, target.position);
    }

    // CAMERA-ONLY: leave empty (CameraSensor provides observations).
    public override void CollectObservations(VectorSensor sensor) { }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // 1) Decode actions
        float throttle = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float steer    = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        if (noReverse) throttle = Mathf.Clamp01(throttle);

        // 2) Apply movement (physics)
        ApplyMovement(throttle, steer);

        // 3) RL: rewards (time penalty + progress shaping)
        ApplyStepRewards();
    }

    private void FixedUpdate()
    {
        if (!keepUpright) return;
        KeepUprightYawOnly();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Manual control for testing
        var a = actionsOut.ContinuousActions;
        a[0] = Input.GetAxisRaw("Vertical");   // throttle
        a[1] = Input.GetAxisRaw("Horizontal"); // steer
    }

    // =========================================================================
    // WORLD / PHYSICS HELPERS (top-of-file concerns)
    // =========================================================================

    private void ResetPhysics()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    private void ResetAgentPose()
    {
        // Spawn defined in LOCAL arena coordinates -> converted to WORLD
        Vector3 localPos = new Vector3(spawnX, agentSpawnY, spawnZ);
        rb.position = ToWorldPoint(localPos);

        float yaw = Random.Range(-startYawRange, startYawRange);
        rb.rotation = ToWorldRotation(Quaternion.Euler(0f, yaw, 0f));
    }

    private void ResetTargetPose()
    {
        int rand = Random.Range(0, 2);
        float x = (rand == 0) ? targetXLeft : targetXRight;

        Vector3 localPos = new Vector3(x, targetSpawnY, 0f);
        target.position = ToWorldPoint(localPos);
    }

    private void ApplyMovement(float throttle, float steer)
    {
        float dt = Time.fixedDeltaTime;

        // Move along forward
        Vector3 deltaMove = transform.forward * (throttle * moveSpeed * dt);
        rb.MovePosition(rb.position + deltaMove);

        // Turn around Y (yaw)
        float deltaYaw = steer * turnSpeed * dt;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, deltaYaw, 0f));
    }

    private void KeepUprightYawOnly()
    {
        // Remove pitch/roll while keeping yaw
        var e = rb.rotation.eulerAngles;
        rb.MoveRotation(Quaternion.Euler(0f, e.y, 0f));
    }

    private Vector3 ToWorldPoint(Vector3 localPoint)
    {
        return arenaRoot ? arenaRoot.TransformPoint(localPoint) : localPoint;
    }

    private Quaternion ToWorldRotation(Quaternion localRotation)
    {
        return (arenaRoot ? arenaRoot.rotation : Quaternion.identity) * localRotation;
    }

    // =========================================================================
    // RL HELPERS (bottom-of-file: reward / termination logic)
    // =========================================================================

    private void ApplyStepRewards()
    {
        // Time penalty encourages faster solutions
        AddReward(stepPenalty);

        // Progress shaping: reward for moving closer
        float dist = Vector3.Distance(rb.position, target.position);
        float progress = prevDist - dist;
        AddReward(progress * progressRewardScale);

        prevDist = dist;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Pellet")) return;

        AddReward(goalReward);
        EndEpisode();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.collider.CompareTag("Wall")) return;

        AddReward(wallHitPenalty);
        // Optional hard fail:
        // EndEpisode();
    }
}
