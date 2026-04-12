using UnityEngine;
using System.Text;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace MusicRun
{
    public enum CreatureState
    {
        FOLLOW = 0,
        OVERTAKE = 1,
        HUNT_INSTRUMENT = 2,
        EAT = 3,
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public class CreatureController : MonoBehaviour
    {
        [Header("Spawn")] // Spawning parameters
        [Tooltip("Enable delayed apparition of the creature after level start.")]
        public bool enableDelayedSpawn = true;
        [Tooltip("Delay in seconds before the creature appears after level start.")]
        public float spawnDelay = 2f;
        [Tooltip("Spawn creature only once per run.")]
        public bool spawnOncePerRun = true;
        [Tooltip("Optional explicit spawn point. When empty, terrainGenerator.currentStart is used.")]
        public Transform spawnStartOverride;
        [Tooltip("Vertical offset applied when spawning the creature.")]
        public float spawnHeightOffset = 1.5f;

        [Header("Follow")]
        [Tooltip("Desired distance behind the player.")]
        public float desiredFollowDistance = 12f;
        [Tooltip("Speed cap in FOLLOW.")]
        public float maxSpeedChase = 16f;

        [Tooltip("Player speed threshold for FOLLOW -> OVERTAKE transition.")]
        [FormerlySerializedAs("PlayerSpeedThresholdForOvertake")]
        public float followToOvertakePlayerSpeedThreshold = 3f;


        [Header("Overtake")] // OVERTAKE state parameters

        [Tooltip("Forward distance aimed during overtake maneuver.")]
        public float overtakeLeadDistance = 12f;

        [Tooltip("Lateral side offset used during overtake.")]
        public float overtakeLateralOffset = 2.5f;

        [Tooltip("Speed cap in OVERTAKE.")]
        public float maxSpeedOvertake = 20f;

        [Tooltip("Cooldown between overtake attempts.")]
        public float overtakeCooldown = 5f;

        [Tooltip("Maximum duration in overtake state before giving up.")]
        public float overtakeNoTargetTimeout = 10f;

        [Header("Hunt")] // HUNT_INSTRUMENT state parameters

        [Tooltip("Search radius to find nearby instruments.")]
        public float huntSearchRadius = 28f;

        [Tooltip("Distance threshold to switch from HUNT to EAT.")]
        public float huntReachDistance = 1.6f;

        [Tooltip("Speed cap in HUNT and EAT approach.")]
        public float maxSpeedHunt = 22f;

        [Tooltip("Player speed threshold for HUNT -> FOLLOW transition.")]
        [FormerlySerializedAs("huntExitToFollowPlayerSpeed")]
        public float huntToFollowPlayerSpeedThreshold = 3.5f;

        [Tooltip("Minimum delay before EAT can start after entering HUNT (from OVERTAKE or EAT).")]
        public float huntMinDelayBetweenEat = 1.0f;

        [Tooltip("Physics layers included when scanning for instrument colliders.")]
        public LayerMask instrumentScanLayerMask = ~0;

        [Header("Eat")] // EAT state parameters

        [Tooltip("Duration of the EAT state.")]
        public float eatDuration = 0.6f;

        [Tooltip("Vertical height of the creature jump while eating an instrument.")]
        public float eatJumpHeight = 1.8f;

        [Tooltip("Extra distance used as a fallback for collision detection at end of jump.")]
        public float eatContactDistance = 0.35f;

        [Header("Movement")] // General movement parameters

        [Tooltip("Proportional gain for follow speed controller.")]
        public float kp = 0.9f;
        [Tooltip("Derivative gain for follow speed controller.")]
        public float kd = 0.25f;
        [Tooltip("Maximum acceleration.")]
        public float accelMax = 20f;
        [Tooltip("Maximum deceleration.")]
        public float brakeMax = 24f;
        [Tooltip("Distance above which catch-up boost is added.")]
        public float catchupBoostDistance = 20f;
        [Tooltip("Extra speed boost when far from player.")]
        public float catchupBoost = 3f;
        [Tooltip("Maximum turning speed in degrees per second.")]
        public float turnRateDegPerSec = 360f;
        [Tooltip("Gravity applied to creature when airborne.")]
        public float gravity = 18f;
        [Tooltip("Small negative velocity to keep controller grounded.")]
        public float groundedStickVelocity = -1f;

        [Header("Vegetation Knockdown")]
        [Tooltip("Enable creature collisions that can knock down vegetation.")]
        public bool enableVegetationKnockdown = true;
        [Tooltip("Tags that can be knocked down by the creature.")]
        public string[] vegetationKnockTags = { "TreeScalable", "Grass" };
        [Tooltip("Impulse applied to vegetation in movement direction.")]
        public float vegetationKnockForce = 6f;
        [Tooltip("Vertical impulse applied when knocking vegetation.")]
        public float vegetationKnockUpwardForce = 2f;
        [Tooltip("Torque applied to topple vegetation.")]
        public float vegetationKnockTorque = 4f;
        [Tooltip("Mass assigned to generated rigidbody when needed.")]
        public float vegetationRigidbodyMass = 10f;
        [Tooltip("Cooldown to avoid repeated knockdown on same object.")]
        public float vegetationKnockCooldown = 0.5f;
        [Tooltip("Destroy knocked vegetation after delay. <= 0 keeps it in scene.")]
        public float vegetationDestroyDelay = 8f;

        [Header("Target Score")]
        [Tooltip("Weight of distance in instrument target scoring.")]
        public float targetWeightDistance = 1f;
        [Tooltip("Weight of angle to player forward in target scoring.")]
        public float targetWeightAngle = 0.35f;
        [Tooltip("Penalty weight when target is behind player.")]
        public float targetWeightBehindPlayer = 0.5f;

        [Header("Debug")]
        [Tooltip("Enable verbose creature state logs.")]
        public bool debugLogs;
        [Tooltip("Draw debug gizmos for creature state and targets.")]
        public bool drawDebugGizmos = true;
        [Tooltip("Enable detailed logs for hunt scan candidates.")]
        public bool debugTargetScanLogs = false;
        [Tooltip("Interval between detailed hunt scan logs.")]
        public float debugTargetScanLogInterval = 1f;

        /// <summary>
        /// Current state of the creature finite state machine.
        /// </summary>
        public CreatureState State => state;
        /// <summary>
        /// True when creature visuals/collisions are active in the scene.
        /// </summary>
        public bool IsSpawned => isSpawned;
        /// <summary>
        /// Current resolved instrument collider targeted by the creature.
        /// </summary>
        public Collider CurrentTarget => currentTarget;

        private GameManager gameManager;
        private PlayerController playerController;
        private TerrainGenerator terrainGenerator;
        private BonusManager bonusManager;
        private CharacterController characterController;

        private CreatureState state = CreatureState.FOLLOW;
        [Tooltip("Elapsed time in current creature state (debug display).")]
        public float stateTime;
        private float spawnTimer;
        [Tooltip("Current horizontal speed used by creature movement (debug display).")]
        public float currentSpeed;
        private float previousGapError;
        private float lastOvertakeTime = -999f;
        private int overtakeSideSign = 1;
        private bool levelActive;
        private bool spawnPending;
        private bool isSpawned;
        private bool hasSpawnedThisLevel;
        private bool eatTriggered;
        private bool eatJumpStarted;
        private float verticalVelocity;
        private float nextEatAllowedTime;

        private Collider currentTarget;
        private readonly Collider[] instrumentScanBuffer = new Collider[64];
        private Renderer[] cachedRenderers;
        private Collider[] cachedColliders;
        private readonly Dictionary<int, float> knockedVegetationCooldowns = new Dictionary<int, float>();

        private Vector3 desiredMovePoint;
        private float desiredSpeed;
        private float nextTargetScanLogTime;

        private void Awake()
        {
            ResolveReferences();
            CacheVisibilityComponents();
            PrepareForLevel();
        }

        private void Update()
        {
            if (!ResolveReferences())
                return;
            if (!levelActive || gameManager.levelPaused)
                return;

            if (!isSpawned)
            {
                UpdateSpawn(Time.deltaTime);
                return;
            }

            stateTime += Time.deltaTime;
            UpdateStateMachine();
            ApplyMovement(Time.deltaTime);
        }

        /// <summary>
        /// Called by GameManager when a level is created/recreated.
        /// </summary>
        public void PrepareForLevel()
        {
            levelActive = false;
            spawnPending = false;
            isSpawned = false;
            hasSpawnedThisLevel = false;
            spawnTimer = 0f;
            stateTime = 0f;
            currentSpeed = 0f;
            desiredSpeed = 0f;
            previousGapError = 0f;
            currentTarget = null;
            eatTriggered = false;
            eatJumpStarted = false;
            verticalVelocity = groundedStickVelocity;
            nextEatAllowedTime = 0f;
            knockedVegetationCooldowns.Clear();
            ChangeState(CreatureState.FOLLOW, force: true);
            SetCreatureVisible(false);
        }

        /// <summary>
        /// Called by GameManager when the level truly starts.
        /// </summary>
        public void NotifyLevelStarted()
        {
            if (!ResolveReferences())
                return;

            levelActive = true;
            spawnPending = true;
            spawnTimer = 0f;
            if (!enableDelayedSpawn)
                TrySpawnNow();
        }

        /// <summary>
        /// Called by GameManager when the level ends.
        /// </summary>
        public void NotifyLevelStopped()
        {
            levelActive = false;
            currentTarget = null;
            desiredSpeed = 0f;
            currentSpeed = 0f;
            verticalVelocity = groundedStickVelocity;
            nextEatAllowedTime = 0f;
        }

        private void UpdateSpawn(float dt)
        {
            if (!spawnPending)
                return;
            if (spawnOncePerRun && hasSpawnedThisLevel)
                return;

            spawnTimer += dt;
            if (!enableDelayedSpawn || spawnTimer >= Mathf.Max(0f, spawnDelay))
                TrySpawnNow();
        }

        private void TrySpawnNow()
        {
            if (spawnOncePerRun && hasSpawnedThisLevel)
                return;

            Transform spawnStart = GetSpawnStartTransform();
            Vector3 spawnPos;
            Quaternion spawnRot;

            if (spawnStart != null)
            {
                spawnPos = spawnStart.position;
                spawnRot = spawnStart.rotation;
            }
            else
            {
                Vector3 playerPos = playerController.transform.position;
                Vector3 playerForward = GetPlayerForward();
                spawnPos = playerPos - playerForward * desiredFollowDistance;
                spawnRot = Quaternion.LookRotation(playerForward, Vector3.up);
            }

            spawnPos.y += spawnHeightOffset;
            bool restoreController = characterController != null && characterController.enabled;
            if (restoreController)
                characterController.enabled = false;
            transform.SetPositionAndRotation(spawnPos, spawnRot);
            if (restoreController)
                characterController.enabled = true;

            isSpawned = true;
            spawnPending = false;
            hasSpawnedThisLevel = true;
            desiredMovePoint = transform.position;
            verticalVelocity = groundedStickVelocity;
            nextEatAllowedTime = 0f;
            ChangeState(CreatureState.FOLLOW, force: true);
            SetCreatureVisible(true);

            if (debugLogs)
                Debug.Log($"Creature spawned at {spawnPos} state:{state}");
        }

        private Transform GetSpawnStartTransform()
        {
            if (spawnStartOverride != null)
                return spawnStartOverride;
            if (terrainGenerator != null && terrainGenerator.currentStart != null)
                return terrainGenerator.currentStart.transform;
            return null;
        }

        private void UpdateStateMachine()
        {
            if (playerController == null)
                return;

            desiredMovePoint = transform.position;
            desiredSpeed = 0f;

            switch (state)
            {
                case CreatureState.FOLLOW:
                    TickFollow();
                    break;
                case CreatureState.OVERTAKE:
                    TickOvertake();
                    break;
                case CreatureState.HUNT_INSTRUMENT:
                    TickHuntInstrument();
                    break;
                case CreatureState.EAT:
                    TickEat();
                    break;
                default:
                    TickFollow();
                    break;
            }
        }

        private void TickFollow()
        {
            if (GetPlayerSpeed() < followToOvertakePlayerSpeedThreshold)
            {
                ChangeState(CreatureState.OVERTAKE);
                return;
            }

            SetFollowMotion(-desiredFollowDistance, maxSpeedChase);
        }

        private void TickOvertake()
        {
            // OVERTAKE is only the "pass player" phase.
            // As soon as the creature is in front, switch to hunt mode.
            if (GetPlayerOffsetAlongForward() > 0.5f)
            {
                ChangeState(CreatureState.HUNT_INSTRUMENT);
                return;
            }

            if (stateTime >= overtakeNoTargetTimeout)
            {
                ChangeState(CreatureState.FOLLOW);
                return;
            }

            desiredMovePoint = GetPointRelativeToPlayer(overtakeLeadDistance, overtakeSideSign * overtakeLateralOffset);
            desiredSpeed = ComputeGapSpeed(overtakeLeadDistance, maxSpeedOvertake, 1.25f);
        }

        private void TickHuntInstrument()
        {
            if (GetPlayerSpeed() >= huntToFollowPlayerSpeedThreshold)
            {
                ChangeState(CreatureState.FOLLOW);
                return;
            }

            if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
            {
                currentTarget = null;
                TryAcquireInstrumentTarget();
            }

            if (currentTarget == null)
            {
                // No target yet: stay in hunt mode while moving ahead and scanning.
                desiredMovePoint = GetPointRelativeToPlayer(overtakeLeadDistance, overtakeSideSign * overtakeLateralOffset);
                desiredSpeed = Mathf.Clamp(GetPlayerSpeed() + 2f, 0f, maxSpeedHunt);
                return;
            }

            Vector3 targetPos = currentTarget.transform.position;
            desiredMovePoint = targetPos;
            desiredSpeed = Mathf.Clamp(GetPlayerSpeed() + 4f, 0f, maxSpeedHunt);

            Vector3 toTarget = targetPos - transform.position;
            toTarget.y = 0f;
            if (toTarget.magnitude <= huntReachDistance)
            {
                if (Time.time >= nextEatAllowedTime)
                    ChangeState(CreatureState.EAT);
                return;
            }
        }

        private void TickEat()
        {
            if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
            {
                ChangeState(CreatureState.HUNT_INSTRUMENT);
                return;
            }

            Vector3 targetCenter = currentTarget.bounds.center;
            desiredMovePoint = targetCenter;
            desiredSpeed = Mathf.Clamp(GetPlayerSpeed() + 5f, 0f, maxSpeedHunt);

            if (TryConsumeCurrentTargetOnCollision())
            {
                ChangeState(CreatureState.HUNT_INSTRUMENT);
                return;
            }

            if (stateTime >= eatDuration)
            {
                TryConsumeCurrentTargetByDistanceFallback();
                ChangeState(CreatureState.HUNT_INSTRUMENT);
            }
        }

        private void SetFollowMotion(float desiredOffset, float speedLimit, float additiveBoost = 0f)
        {
            desiredMovePoint = GetPointRelativeToPlayer(desiredOffset, 0f);
            desiredSpeed = ComputeGapSpeed(desiredOffset, speedLimit, additiveBoost);
        }

        private Vector3 GetPointRelativeToPlayer(float forwardOffset, float lateralOffset)
        {
            Vector3 playerPos = playerController.transform.position;
            Vector3 playerForward = GetPlayerForward();
            Vector3 playerRight = Vector3.Cross(Vector3.up, playerForward);
            Vector3 point = playerPos + playerForward * forwardOffset + playerRight * lateralOffset;
            point.y = transform.position.y;
            return point;
        }

        // PD gap controller: tracks desired longitudinal offset relative to the player.
        private float ComputeGapSpeed(float desiredOffset, float speedLimit, float additiveBoost = 0f)
        {
            float dt = Mathf.Max(0.0001f, Time.deltaTime);
            float currentOffset = GetPlayerOffsetAlongForward();
            float error = desiredOffset - currentOffset;
            float errorRate = (error - previousGapError) / dt;
            previousGapError = error;

            float target = GetPlayerSpeed() + kp * error - kd * errorRate + additiveBoost;
            if (GetDistanceToPlayer() > catchupBoostDistance)
                target += catchupBoost;

            return Mathf.Clamp(target, 0f, speedLimit);
        }

        private void ApplyMovement(float dt)
        {
            if (characterController == null || !characterController.enabled)
                return;

            Vector3 toTarget = desiredMovePoint - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            Vector3 moveDir = distance > 0.001f ? toTarget / distance : transform.forward;

            float accel = desiredSpeed >= currentSpeed ? accelMax : brakeMax;
            currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, accel * dt);

            if (state == CreatureState.EAT && !eatJumpStarted)
            {
                verticalVelocity = ComputeEatJumpUpVelocity();
                eatJumpStarted = true;
            }
            else
            {
                if (characterController.isGrounded && verticalVelocity <= 0f)
                    verticalVelocity = groundedStickVelocity;
                verticalVelocity -= gravity * dt;
            }

            Vector3 horizontalVelocity = moveDir * currentSpeed;
            float maxHorizontalDistance = horizontalVelocity.magnitude * dt;
            if (distance < maxHorizontalDistance && maxHorizontalDistance > 0.0001f)
                horizontalVelocity = moveDir * (distance / dt);

            Vector3 velocity = horizontalVelocity;
            velocity.y = verticalVelocity;
            characterController.Move(velocity * dt);

            Vector3 lookDir = horizontalVelocity;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                Quaternion look = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnRateDegPerSec * dt);
            }
        }

        private float ComputeEatJumpUpVelocity()
        {
            float clampedHeight = Mathf.Max(0.01f, eatJumpHeight);
            float clampedGravity = Mathf.Max(0.01f, gravity);
            return Mathf.Sqrt(2f * clampedGravity * clampedHeight);
        }

        private bool TryAcquireInstrumentTarget()
        {
            Collider[] scanColliders = instrumentScanBuffer;
            int count = Physics.OverlapSphereNonAlloc(
                transform.position,
                huntSearchRadius,
                instrumentScanBuffer,
                instrumentScanLayerMask,
                QueryTriggerInteraction.Collide);

            if (count >= instrumentScanBuffer.Length)
            {
                // Non-alloc scan can drop colliders when the buffer is full. Fallback once to avoid missing instruments.
                Collider[] expandedScan = Physics.OverlapSphere(
                    transform.position,
                    huntSearchRadius,
                    instrumentScanLayerMask,
                    QueryTriggerInteraction.Collide);

                if (expandedScan != null && expandedScan.Length > count)
                {
                    scanColliders = expandedScan;
                    count = expandedScan.Length;
                }
            }

            if (debugTargetScanLogs && Time.time >= nextTargetScanLogTime)
            {
                //LogClosestScanColliders(count);
                for (int i = 0; i < count; i++)
                {
                    Collider candidate = scanColliders[i];
                    if (candidate == null)
                        continue;

                    StringBuilder sb = new StringBuilder(512);
                    sb.Append("#").Append(i)
                        .Append(" radius=").Append(huntSearchRadius.ToString("F1"))
                        .Append(" candidate: ").Append(candidate.name)
                        .Append(" ").Append(candidate.tag);
                    if (candidate.transform.parent != null)
                        sb.Append(" parent: ").Append(candidate.name).Append(candidate.transform.parent.tag);

                    Debug.Log(sb.ToString());
                }
                nextTargetScanLogTime = Time.time + Mathf.Max(0.1f, debugTargetScanLogInterval);
            }

            if (count <= 0)
                return false;

            if (count >= instrumentScanBuffer.Length && debugLogs)
            {
                Debug.LogWarning($"Creature scan buffer full ({instrumentScanBuffer.Length}). Consider narrowing instrumentScanLayerMask to instrument layers.");
            }

            Collider best = null;
            float bestScore = float.MaxValue;

            Vector3 playerPos = playerController.transform.position;
            Vector3 playerForward = GetPlayerForward();

            for (int i = 0; i < count; i++)
            {
                Collider candidate = scanColliders[i];
                if (!TryResolveInstrumentCollider(candidate, out Collider resolvedInstrumentCollider))
                    continue;

                Vector3 candidatePos = resolvedInstrumentCollider.bounds.center;
                Vector3 toCandidate = candidatePos - transform.position;
                toCandidate.y = 0f;
                float dist = toCandidate.magnitude;
                if (dist < 0.01f)
                    dist = 0.01f;

                float angle = Vector3.Angle(playerForward, toCandidate.normalized);
                float candidateOffset = Vector3.Dot(candidatePos - playerPos, playerForward);
                float behindPenalty = candidateOffset < -2f ? Mathf.Abs(candidateOffset) : 0f;
                float score = targetWeightDistance * dist + targetWeightAngle * angle + targetWeightBehindPlayer * behindPenalty;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = resolvedInstrumentCollider;
                }
            }

            currentTarget = best;
            return currentTarget != null;
        }

        private bool TryResolveInstrumentCollider(Collider candidate, out Collider resolvedInstrumentCollider)
        {
            resolvedInstrumentCollider = null;
            if (candidate == null || !candidate.gameObject.activeInHierarchy)
                return false;

            if (candidate.CompareTag("Instrument"))
            {
                resolvedInstrumentCollider = candidate;
                return true;
            }

            Rigidbody attachedBody = candidate.attachedRigidbody;
            if (attachedBody != null && attachedBody.gameObject.CompareTag("Instrument"))
            {
                Collider bodyCollider = attachedBody.GetComponent<Collider>();
                resolvedInstrumentCollider = bodyCollider != null ? bodyCollider : candidate;
                return true;
            }

            Transform parent = candidate.transform.parent;
            while (parent != null)
            {
                if (parent.CompareTag("Instrument"))
                {
                    Collider parentCollider = parent.GetComponent<Collider>();
                    if (parentCollider == null)
                        parentCollider = parent.GetComponentInChildren<Collider>();
                    resolvedInstrumentCollider = parentCollider != null ? parentCollider : candidate;
                    return true;
                }
                parent = parent.parent;
            }

            return false;
        }

        private void ConsumeCurrentTarget()
        {
            if (currentTarget == null)
                return;

            Collider target = currentTarget;
            currentTarget = null;

            if (bonusManager != null)
                bonusManager.TriggerInstrumentByCreature(target);
            else if (target != null)
                Destroy(target.gameObject);
        }

        private bool TryConsumeCurrentTargetOnCollision()
        {
            if (eatTriggered || currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
                return false;

            if (characterController == null || !characterController.enabled)
                return false;

            bool hasCollision = characterController.bounds.Intersects(currentTarget.bounds);

            if (!hasCollision)
                return false;

            ConsumeCurrentTarget();
            eatTriggered = true;
            return true;
        }

        private void TryConsumeCurrentTargetByDistanceFallback()
        {
            if (eatTriggered || currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
                return;

            Vector3 delta = currentTarget.bounds.center - transform.position;
            float maxDistance = eatContactDistance;
            if (characterController != null)
                maxDistance += characterController.radius;
            maxDistance += currentTarget.bounds.extents.magnitude * 0.5f;

            if (delta.sqrMagnitude > maxDistance * maxDistance)
                return;

            ConsumeCurrentTarget();
            eatTriggered = true;
        }

        private bool IsCurrentTargetCollider(Collider other)
        {
            if (other == null || currentTarget == null)
                return false;

            if (other == currentTarget)
                return true;
            if (other.transform == currentTarget.transform)
                return true;
            if (other.transform.IsChildOf(currentTarget.transform) || currentTarget.transform.IsChildOf(other.transform))
                return true;

            Rigidbody otherBody = other.attachedRigidbody;
            Rigidbody targetBody = currentTarget.attachedRigidbody;
            if (otherBody != null && targetBody != null && otherBody == targetBody)
                return true;

            if (TryResolveInstrumentCollider(other, out Collider resolved))
                return resolved == currentTarget;

            return false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (state != CreatureState.EAT || eatTriggered)
                return;
            if (!IsCurrentTargetCollider(other))
                return;

            ConsumeCurrentTarget();
            eatTriggered = true;
            ChangeState(CreatureState.HUNT_INSTRUMENT);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.collider == null)
                return;

            if (state == CreatureState.EAT && !eatTriggered && IsCurrentTargetCollider(hit.collider))
            {
                ConsumeCurrentTarget();
                eatTriggered = true;
                ChangeState(CreatureState.HUNT_INSTRUMENT);
                return;
            }

            if (!enableVegetationKnockdown)
                return;

            if (!TryResolveKnockableVegetationTransform(hit.collider, out Transform vegetationTransform))
                return;

            KnockdownVegetation(vegetationTransform, hit.moveDirection);
        }

        private bool TryResolveKnockableVegetationTransform(Collider candidate, out Transform vegetationTransform)
        {
            vegetationTransform = null;
            if (candidate == null)
                return false;

            Transform current = candidate.transform;
            while (current != null)
            {
                if (HasKnockableVegetationTag(current))
                {
                    vegetationTransform = current;
                    return true;
                }
                current = current.parent;
            }

            return false;
        }

        private bool HasKnockableVegetationTag(Transform target)
        {
            if (target == null || vegetationKnockTags == null)
                return false;

            for (int i = 0; i < vegetationKnockTags.Length; i++)
            {
                string tag = vegetationKnockTags[i];
                if (string.IsNullOrWhiteSpace(tag))
                    continue;
                if (target.tag == tag)
                    return true;
            }

            return false;
        }

        private void KnockdownVegetation(Transform vegetationTransform, Vector3 moveDirection)
        {
            if (vegetationTransform == null)
                return;
            if (!vegetationTransform.gameObject.activeInHierarchy)
                return;

            PruneKnockdownCooldowns();
            int key = vegetationTransform.GetInstanceID();
            if (knockedVegetationCooldowns.TryGetValue(key, out float nextAllowedTime) && Time.time < nextAllowedTime)
                return;

            knockedVegetationCooldowns[key] = Time.time + Mathf.Max(0.05f, vegetationKnockCooldown);

            Collider[] vegetationColliders = vegetationTransform.GetComponentsInChildren<Collider>();
            for (int i = 0; i < vegetationColliders.Length; i++)
            {
                if (vegetationColliders[i] == null)
                    continue;
                vegetationColliders[i].enabled = false;
            }

            Rigidbody rb = vegetationTransform.GetComponent<Rigidbody>();
            if (rb == null)
                rb = vegetationTransform.gameObject.AddComponent<Rigidbody>();

            rb.mass = Mathf.Max(0.1f, vegetationRigidbodyMass);
            rb.useGravity = true;
            rb.isKinematic = false;

            Vector3 pushDir = Vector3.ProjectOnPlane(moveDirection, Vector3.up);
            if (pushDir.sqrMagnitude < 0.001f)
            {
                pushDir = vegetationTransform.position - transform.position;
                pushDir.y = 0f;
            }
            if (pushDir.sqrMagnitude < 0.001f)
                pushDir = transform.forward;
            pushDir.Normalize();

            rb.AddForce(pushDir * vegetationKnockForce + Vector3.up * vegetationKnockUpwardForce, ForceMode.Impulse);
            Vector3 torqueAxis = Vector3.Cross(Vector3.up, pushDir);
            if (torqueAxis.sqrMagnitude < 0.001f)
                torqueAxis = Vector3.right;
            rb.AddTorque(torqueAxis.normalized * vegetationKnockTorque, ForceMode.Impulse);

            if (vegetationDestroyDelay > 0f)
                Destroy(vegetationTransform.gameObject, vegetationDestroyDelay);
        }

        private void PruneKnockdownCooldowns()
        {
            if (knockedVegetationCooldowns.Count < 64)
                return;

            List<int> toRemove = null;
            foreach (KeyValuePair<int, float> kv in knockedVegetationCooldowns)
            {
                if (Time.time <= kv.Value)
                    continue;
                if (toRemove == null)
                    toRemove = new List<int>(16);
                toRemove.Add(kv.Key);
            }

            if (toRemove == null)
                return;

            for (int i = 0; i < toRemove.Count; i++)
                knockedVegetationCooldowns.Remove(toRemove[i]);
        }

        private void ChangeState(CreatureState nextState, bool force = false)
        {
            if (!force && state == nextState)
                return;

            //CreatureState previous = state;
            stateTime = 0f;
            previousGapError = 0f;
            if (nextState == CreatureState.HUNT_INSTRUMENT)// && (previous == CreatureState.OVERTAKE || previous == CreatureState.EAT))
            {
                nextEatAllowedTime = Time.time + Mathf.Max(0f, huntMinDelayBetweenEat);
            }

            if (nextState == CreatureState.OVERTAKE)
            {
                lastOvertakeTime = Time.time;
                overtakeSideSign = UnityEngine.Random.value < 0.5f ? -1 : 1;
                currentTarget = null;
            }
            else if (nextState == CreatureState.EAT)
            {
                eatTriggered = false;
                eatJumpStarted = false;
            }
            else if (nextState == CreatureState.FOLLOW)
            {
                currentTarget = null;
            }

            if (debugLogs)
                Debug.Log($"Creature state: {state} -> {nextState}");
            state = nextState;
        }

        private float GetPlayerSpeed()
        {
            return playerController != null ? playerController.Speed : 0f;
        }

        private Vector3 GetPlayerForward()
        {
            if (playerController == null)
                return Vector3.forward;

            Vector3 forward = playerController.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                return Vector3.forward;
            return forward.normalized;
        }

        /// <summary>
        /// Returns current horizontal distance between creature and player.
        /// </summary>
        public float GetDistanceToPlayer()
        {
            if (playerController == null)
                return float.MaxValue;

            Vector3 delta = playerController.transform.position - transform.position;
            delta.y = 0f;
            return delta.magnitude;
        }

        /// <summary>
        /// Returns creature offset projected on player forward axis.
        /// Positive means creature is in front of player.
        /// </summary>
        public float GetPlayerOffsetAlongForward()
        {
            if (playerController == null)
                return 0f;

            Vector3 delta = transform.position - playerController.transform.position;
            delta.y = 0f;
            return Vector3.Dot(delta, GetPlayerForward());
        }

        private void CacheVisibilityComponents()
        {
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
            cachedColliders = GetComponentsInChildren<Collider>(true);
        }

        private void SetCreatureVisible(bool visible)
        {
            if (cachedRenderers == null || cachedColliders == null)
                CacheVisibilityComponents();

            for (int i = 0; i < cachedRenderers.Length; i++)
                cachedRenderers[i].enabled = visible;

            for (int i = 0; i < cachedColliders.Length; i++)
                cachedColliders[i].enabled = visible;
        }

        private bool ResolveReferences()
        {
            if (gameManager == null)
                gameManager = Utilities.FindGameManager();
            if (gameManager == null)
                return false;

            if (characterController == null)
                characterController = GetComponent<CharacterController>();
            if (characterController == null)
                characterController = gameObject.AddComponent<CharacterController>();

            if (playerController == null)
                playerController = gameManager.playerController;
            if (terrainGenerator == null)
                terrainGenerator = gameManager.terrainGenerator;
            if (bonusManager == null)
                bonusManager = gameManager.bonusManager;

            return playerController != null && characterController != null;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos)
                return;

            Color stateColor;
            switch (state)
            {
                case CreatureState.FOLLOW:
                    stateColor = Color.green;
                    break;
                case CreatureState.OVERTAKE:
                    stateColor = Color.red;
                    break;
                case CreatureState.HUNT_INSTRUMENT:
                    stateColor = new Color(1f, 0.5f, 0f);
                    break;
                case CreatureState.EAT:
                    stateColor = new Color(0.65f, 0.2f, 0.85f);
                    break;
                default:
                    stateColor = Color.white;
                    break;
            }

            Gizmos.color = stateColor;
            Gizmos.DrawWireSphere(transform.position, 0.6f);
            Gizmos.DrawLine(transform.position, desiredMovePoint);
            Gizmos.DrawWireSphere(desiredMovePoint, 0.25f);

            if (state == CreatureState.OVERTAKE || state == CreatureState.HUNT_INSTRUMENT)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, huntSearchRadius);
            }

            if (currentTarget != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, currentTarget.transform.position);
                Gizmos.DrawWireSphere(currentTarget.transform.position, 0.4f);
            }
        }
    }
}
