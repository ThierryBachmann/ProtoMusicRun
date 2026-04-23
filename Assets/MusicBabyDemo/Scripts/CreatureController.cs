/*
CreatureController - Requirements and technical constraints (FSM V2)

Gameplay goals
- Keep constant pressure on the player.
- Stay readable and threatening.
- Preserve a heavy/massive feeling with inertia.
- Let player speed/trajectory influence the creature without direct control.

FSM V2 states
- FOLLOW
- OVERTAKE
- HUNT
- RECENTER
- WAIT_PLAYER
- EAT_ATTACK
- EAT_RECOVERY
- LEASH_RETURN

State table (guards/actions/transitions)
- FOLLOW
  entry: clear target, clear recenter lock
  action: SetFollowMotion(-desiredFollowDistance, maxSpeedFollow)
  transitions: playerSpeed < followToOvertakePlayerSpeedThreshold -> OVERTAKE

- OVERTAKE
  entry: overtakeSideSign = random +/-1
  action: desiredMovePoint = GetPointRelativeToPlayer(overtakeLeadDistance, overtakeSideSign * overtakeLateralOffset)
  transitions: leadDistance > 0.5 -> HUNT ; stateTime >= overtakeNoTargetTimeout -> FOLLOW

- HUNT
  entry: init smoothed huntForward, clear recenter lock
  action:
    if hasTarget: chase target
    else: desiredMovePoint = GetPointRelativeToPlayer(huntMaxLeadDistance, 0f, huntForward)
  transitions:
    playerSpeed >= huntToFollowPlayerSpeedThreshold -> FOLLOW
    !hasTarget && headingAngle > huntRecenterHeadingAngleThreshold -> RECENTER
    !hasTarget && leadDistance > huntMaxLeadDistance && headingAngle aligned -> WAIT_PLAYER
    hasTarget && distanceToTarget <= huntReachDistance && eat cooldown elapsed -> EAT_ATTACK

- RECENTER
  entry: lock recenterForward from current player forward
  action: desiredMovePoint = GetPointRelativeToPlayer(huntMaxLeadDistance, 0f, recenterForward), relock recenterForward if it diverges from player heading
  transitions: alignedStable(recenterExitStableDuration) && leadDistance <= huntMaxLeadDistance -> HUNT
  exit: clear recenter lock

- WAIT_PLAYER
  entry: clear target/recenter lock
  action: wait point in front of player (ratio on huntMaxLeadDistance), no lateral offset
  transitions:
    playerSpeed >= huntToFollowPlayerSpeedThreshold -> FOLLOW
    headingAngle > huntRecenterHeadingAngleThreshold -> RECENTER
    leadDistance <= waitPlayerExitLeadRatio * huntMaxLeadDistance -> HUNT

- EAT_ATTACK
  entry: lock target snapshot + reset jump trigger
  action: attack trajectory toward snapshot target + consume on collision/fallback
  transitions: target consumed OR attack timeout -> EAT_RECOVERY

- EAT_RECOVERY
  entry: set carryDirection/carrySpeed (post-consume inertia)
  action: keep inertial run until landing and slowdown
  transitions: (grounded && horizontalSpeed <= eatRecoveryExitSpeed) OR timeout -> HUNT

- LEASH_RETURN
  entry: clear target + clear recenter
  action: hard return toward player with leash speed cap/boost
  transitions: distance <= leashExitDistance -> FOLLOW

Cross-cutting safety
- Any state can transition to LEASH_RETURN when distance > leashEnterDistance.
- No-ground failsafe: if no terrain detected under creature for too long, recover near player and go to FOLLOW.

Technical constraints
1) Single centralized FSM: all transitions must go through ChangeState().
2) Unified movement path: CharacterController for ground motion, jump, gravity, landing.
3) Idempotent instrument consumption: one consume event per target.
4) Stable targeting: locked target has priority; no oscillation between targeting/recenter/wait.
5) Strict hunt eat cooldown on HUNT entry after OVERTAKE or EAT phases.
6) Realistic inertia after attack: EAT split into ATTACK then RECOVERY.
7) Ground slope alignment: spherecast/raycast ground normal, smoothing, tilt clamp, airborne weight.
8) Distance safety: explicit leash mode + no-ground recovery.
9) Animation tied to real displacement (chase gait phase driven by horizontal travel).
10) Designer-safe tuning: exposed parameters with tooltips/ranges.
11) Performance discipline: cached refs, non-alloc scans first, no per-frame Find/GC churn.
12) Visual sync contract: controller pushes state + motion context + target context to CreatureVisualBase.
*/
using UnityEngine;
using System.Text;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace MusicRun
{
    // FSM V2:
    // FOLLOW -> OVERTAKE -> HUNT
    // HUNT <-> RECENTER, HUNT <-> WAIT_PLAYER
    // HUNT -> EAT_ATTACK -> EAT_RECOVERY -> HUNT
    // Any state can enter LEASH_RETURN when too far from player.
    public enum CreatureState
    {
        FOLLOW = 0,
        OVERTAKE = 1,
        HUNT = 2,
        RECENTER = 3,
        WAIT_PLAYER = 4,
        EAT_ATTACK = 5,
        EAT_RECOVERY = 6,
        LEASH_RETURN = 7,
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public class CreatureController : MonoBehaviour
    {
        [Header("Spawn")]
        [Tooltip("Enable delayed apparition of the creature after level start.")]
        public bool enableDelayedSpawn = true;
        [Tooltip("Delay in seconds before the creature appears after level start.")]
        [Range(0f, 60f)]
        public float spawnDelay = 10f;
        [Tooltip("Spawn creature only once per run.")]
        public bool spawnOncePerRun = true;
        [Tooltip("Optional explicit spawn point. When empty, terrainGenerator.currentStart is used.")]
        public Transform spawnStartOverride;
        [Tooltip("Vertical offset applied when spawning the creature.")]
        [Range(0f, 10f)]
        public float spawnHeightOffset = 0f;

        [Header("FOLLOW Mode")]
        [Tooltip("Desired distance behind the player.")]
        [Range(0f, 60f)]
        public float desiredFollowDistance = 8f;
        [Tooltip("Speed cap in FOLLOW.")]
        [Range(0f, 80f)]
        public float maxSpeedFollow = 10f;
        [Tooltip("Player speed threshold for FOLLOW -> OVERTAKE transition.")]
        [FormerlySerializedAs("PlayerSpeedThresholdForOvertake")]
        [Range(0f, 30f)]
        public float followToOvertakePlayerSpeedThreshold = 5.5f;

        [Header("OVERTAKE Mode")]
        [Tooltip("Forward distance aimed during overtake maneuver.")]
        [Range(0f, 60f)]
        public float overtakeLeadDistance = 6f;
        [Tooltip("Lateral side offset used during overtake.")]
        [Range(0f, 20f)]
        public float overtakeLateralOffset = 5f;
        [Tooltip("Speed cap in OVERTAKE.")]
        [Range(0f, 80f)]
        public float maxSpeedOvertake = 20f;
        [Tooltip("Cooldown between overtake attempts.")]
        [Range(0f, 30f)]
        public float overtakeCooldown = 5f;
        [Tooltip("Maximum duration in overtake state before giving up.")]
        [Range(0f, 30f)]
        public float overtakeNoTargetTimeout = 10f;

        [Header("HUNT Mode")]
        [Tooltip("Search radius to find nearby instruments.")]
        [Range(1f, 12f)]
        public float huntSearchRadius = 5.3f;
        [Tooltip("Distance threshold to switch from HUNT to EAT_ATTACK.")]
        [Range(1f, 10f)]
        public float huntReachDistance = 3.6f;
        [Tooltip("Speed cap in HUNT and EAT approach.")]
        [Range(0f, 15f)]
        public float maxSpeedHunt = 10f;
        [Tooltip("Maximum forward lead allowed in HUNT before speed is reduced to keep pressure on player.")]
        [Range(0.1f, 20f)]
        public float huntMaxLeadDistance = 7.9f;
        [Tooltip("Player speed threshold for HUNT/RECENTER/WAIT_PLAYER -> FOLLOW transition.")]
        [FormerlySerializedAs("huntExitToFollowPlayerSpeed")]
        [Range(0f, 30f)]
        public float huntToFollowPlayerSpeedThreshold = 6.5f;
        [Tooltip("Minimum delay before EAT can start after entering HUNT (from OVERTAKE or EAT).")]
        [Range(0f, 60f)]
        public float huntMinDelayBetweenEat = 2f;
        [Tooltip("Reaction time used to update perceived player heading in HUNT (higher = slower creature response).")]
        [Range(0.01f, 5f)]
        public float huntPlayerHeadingReactionTime = 1.9f;
        [Tooltip("Physics layers included when scanning for instrument colliders.")]
        public LayerMask instrumentScanLayerMask = ~0;

        [Header("RECENTER Mode")]
        [Tooltip("Enter recenter mode when angle between player and creature headings exceeds this value (degrees). Exit at half this angle.")]
        [Range(0f, 180f)]
        public float huntRecenterHeadingAngleThreshold = 30f;
        [Tooltip("Time during which heading must remain aligned before leaving RECENTER.")]
        [Range(0f, 5f)]
        public float recenterExitStableDuration = 0.2f;
        [Tooltip("Smoothing applied to recenter target updates (higher is snappier, lower is smoother).")]
        [Range(0f, 50f)]
        public float huntRecenterPointSmoothing = 13f;

        [Header("WAIT_PLAYER Mode")]
        [Tooltip("Lead ratio used to exit WAIT_PLAYER and return to HUNT (0.7 means leave wait when lead <= 70% of huntMaxLeadDistance).")]
        [Range(0.1f, 1f)]
        public float waitPlayerExitLeadRatio = 0.7f;
        [Tooltip("Desired lead ratio while in WAIT_PLAYER (lower values make the creature wait more aggressively).")]
        [Range(0.1f, 1f)]
        public float waitPlayerDesiredLeadRatio = 0.45f;

        [Header("EAT_ATTACK Mode")]
        [Tooltip("Duration of EAT_ATTACK state before fallback to recovery.")]
        [Range(0f, 5f)]
        public float eatDuration = 0.6f;
        [Tooltip("Vertical height of the creature jump while attacking an instrument.")]
        [Range(0.01f, 20f)]
        public float eatJumpHeight = 1.8f;
        [Tooltip("Extra distance used as a fallback for collision detection at end of jump.")]
        [Range(0f, 5f)]
        public float eatContactDistance = 0.35f;

        [Header("EAT_RECOVERY Mode")]
        [Tooltip("Deceleration applied during post-eat inertial carry.")]
        [Range(0f, 80f)]
        public float eatPostConsumeCarryDeceleration = 14f;
        [Tooltip("Horizontal speed threshold to leave EAT_RECOVERY after landing.")]
        [Range(0f, 30f)]
        public float eatRecoveryExitSpeed = 3f;
        [Tooltip("Safety timeout for post-eat carry (seconds).")]
        [Range(0.1f, 10f)]
        public float eatRecoveryMaxDuration = 1.5f;

        [Header("Movement")]
        [Tooltip("Proportional gain for follow speed controller.")]
        [Range(0f, 5f)]
        public float kp = 0.9f;
        [Tooltip("Derivative gain for follow speed controller.")]
        [Range(0f, 5f)]
        public float kd = 0.25f;
        [Tooltip("Maximum acceleration.")]
        [Range(0f, 100f)]
        public float accelMax = 20f;
        [Tooltip("Maximum deceleration.")]
        [Range(0f, 100f)]
        public float brakeMax = 16.8f;
        [Tooltip("Distance above which catch-up boost is added.")]
        [Range(0f, 200f)]
        public float catchupBoostDistance = 20f;
        [Tooltip("Extra speed boost when far from player.")]
        [Range(0f, 40f)]
        public float catchupBoost = 3f;
        [Tooltip("Maximum turning speed in degrees per second.")]
        [Range(0f, 1080f)]
        public float turnRateDegPerSec = 139f;
        [Tooltip("Gravity applied to creature when airborne.")]
        [Range(0.01f, 100f)]
        public float gravity = 7.2f;
        [Tooltip("Small negative velocity to keep controller grounded.")]
        [Range(-10f, 0f)]
        public float groundedStickVelocity = -1f;

        [Header("Ground Alignment")]
        [Tooltip("Rotate creature to follow the ground slope under it.")]
        public bool alignToGroundSlope = true;
        [Tooltip("Layers used to probe the ground slope. When set to Nothing, TerrainCurrent is used if available.")]
        public LayerMask groundAlignmentLayerMask = 0;
        [Tooltip("Vertical offset of ground probe start above creature position.")]
        [Range(0.1f, 20f)]
        public float groundProbeStartHeight = 2.5f;
        [Tooltip("Maximum distance used to probe ground below creature.")]
        [Range(0.2f, 50f)]
        public float groundProbeDistance = 6f;
        [Tooltip("Smoothing speed applied to ground alignment (higher is snappier).")]
        [Range(0f, 50f)]
        public float groundAlignSmoothing = 12f;
        [Tooltip("Maximum slope tilt angle applied to the creature (degrees).")]
        [Range(0f, 89.9f)]
        public float maxGroundTiltAngle = 35f;
        [Tooltip("How much slope alignment is kept while airborne (0 = upright, 1 = full).")]
        [Range(0f, 1f)]
        public float airborneGroundAlignWeight = 0.2f;

        [Header("Character Controller")]
        [Tooltip("When enabled, applies tuned CharacterController shape values to keep the creature visually grounded.")]
        public bool autoConfigureCharacterControllerShape = true;
        [Tooltip("CharacterController height used by the creature.")]
        [Range(0.2f, 10f)]
        public float characterControllerHeight = 2f;
        [Tooltip("CharacterController radius used by the creature.")]
        [Range(0.05f, 5f)]
        public float characterControllerRadius = 0.5f;
        [Tooltip("CharacterController center in local space. Increase Y if the creature appears to float.")]
        public Vector3 characterControllerCenter = new Vector3(0f, 0.82f, 0f);

        [Header("LEASH_RETURN Mode")]
        [Tooltip("Enable LEASH_RETURN to keep creature near player and avoid leaving streamed terrain.")]
        public bool enableDistanceLeash = true;
        [Tooltip("Creature enters LEASH_RETURN when horizontal distance to player exceeds this value.")]
        [Range(1f, 300f)]
        public float leashEnterDistance = 42f;
        [Tooltip("Creature exits LEASH_RETURN when distance comes back below this value.")]
        [Range(0f, 300f)]
        public float leashExitDistance = 30f;
        [Tooltip("Extra speed added over player speed while leash mode is active.")]
        [Range(0f, 60f)]
        public float leashCatchupSpeedBoost = 8f;
        [Tooltip("Maximum speed cap used while leash mode is active.")]
        [Range(0f, 120f)]
        public float leashMaxSpeed = 30f;

        [Header("No Ground Failsafe")]
        [Tooltip("Enable emergency recovery when no terrain is detected below creature for too long.")]
        public bool enableNoGroundFailsafe = true;
        [Tooltip("Delay without ground before triggering emergency reposition.")]
        [Range(0.05f, 5f)]
        public float noGroundMaxDuration = 0.45f;
        [Tooltip("Vertical offset of ground probe start for no-ground detection.")]
        [Range(0.1f, 20f)]
        public float noGroundProbeStartHeight = 3.0f;
        [Tooltip("Probe distance used to detect terrain below creature for no-ground detection.")]
        [Range(0.5f, 200f)]
        public float noGroundProbeDistance = 20f;
        [Tooltip("Reposition distance behind player when no-ground failsafe triggers.")]
        [Range(0f, 100f)]
        public float noGroundRecoveryBehindPlayerDistance = 8f;
        [Tooltip("Reposition height offset applied during no-ground recovery.")]
        [Range(0f, 20f)]
        public float noGroundRecoveryHeightOffset = 1.5f;
        [Tooltip("Cooldown after a no-ground recovery to avoid repeated immediate teleports.")]
        [Range(0f, 10f)]
        public float noGroundRecoveryCooldown = 1.0f;

        [Header("Vegetation Knockdown")]
        [Tooltip("Enable creature collisions that can knock down vegetation.")]
        public bool enableVegetationKnockdown = true;
        [Tooltip("Tags that can be knocked down by the creature.")]
        public string[] vegetationKnockTags = { "TreeScalable", "Grass" };
        [Tooltip("Impulse applied to vegetation in movement direction.")]
        [Range(0f, 100f)]
        public float vegetationKnockForce = 6f;
        [Tooltip("Vertical impulse applied when knocking vegetation.")]
        [Range(0f, 50f)]
        public float vegetationKnockUpwardForce = 2f;
        [Tooltip("Torque applied to topple vegetation.")]
        [Range(0f, 100f)]
        public float vegetationKnockTorque = 4f;
        [Tooltip("Mass assigned to generated rigidbody when needed.")]
        [Range(0.1f, 200f)]
        public float vegetationRigidbodyMass = 10f;
        [Tooltip("Cooldown to avoid repeated knockdown on same object.")]
        [Range(0.05f, 10f)]
        public float vegetationKnockCooldown = 0.5f;
        [Tooltip("Destroy knocked vegetation after delay. <= 0 keeps it in scene.")]
        [Range(0f, 60f)]
        public float vegetationDestroyDelay = 8f;

        [Header("Target Score")]
        [Tooltip("Weight of distance in instrument target scoring.")]
        [Range(0f, 5f)]
        public float targetWeightDistance = 1f;
        [Tooltip("Weight of angle to player forward in target scoring.")]
        [Range(0f, 5f)]
        public float targetWeightAngle = 0.35f;
        [Tooltip("Penalty weight when target is behind player.")]
        [Range(0f, 10f)]
        public float targetWeightBehindPlayer = 0.5f;

        [Header("Debug")]
        [Tooltip("Enable verbose creature state logs.")]
        public bool debugLogs = true;
        [Tooltip("Draw debug gizmos for creature state and targets.")]
        public bool drawDebugGizmos = true;
        [Tooltip("Enable detailed logs for hunt scan candidates.")]
        public bool debugTargetScanLogs = false;
        [Tooltip("Interval between detailed hunt scan logs.")]
        [Range(0.1f, 10f)]
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
        private CreatureVisualBase creatureVisual;

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
        private bool eatPostConsumeCarryActive;
        private bool eatPostConsumeCarryWasAirborne;
        private bool huntRecenterPointInitialized;
        private bool huntRecenterForwardLocked;
        private bool huntPerceivedPlayerForwardInitialized;
        private Vector3 eatPostConsumeCarryDirection;
        private Vector3 huntRecenterLockedForward;
        private Vector3 huntPerceivedPlayerForward;
        private Collider eatAttackTargetSnapshot;
        private Vector3 eatAttackTargetSnapshotPoint;
        private float recenterAlignedStableTimer;
        private float eatPostConsumeCarrySpeed;
        private float eatPostConsumeCarryElapsed;
        private float verticalVelocity;
        public float nextEatAllowedTime;

        private Collider currentTarget;
        private readonly Collider[] instrumentScanBuffer = new Collider[64];
        private Renderer[] cachedRenderers;
        private Collider[] cachedColliders;
        private readonly Dictionary<int, float> knockedVegetationCooldowns = new Dictionary<int, float>();

        private Vector3 desiredMovePoint;
        private Vector3 huntRecenterPoint;
        private Vector3 smoothedGroundUp = Vector3.up;
        private float desiredSpeed;
        private float nextTargetScanLogTime;
        private float nextDesiredPointDistanceLogTime;
        private bool groundUpInitialized;
        private bool characterControllerShapeConfigured;
        private bool leashActive;
        private float noGroundTimer;
        private float nextNoGroundRecoveryAllowedTime;

        private void Awake()
        {
            ResolveReferences();
            CacheVisibilityComponents();
            PrepareForLevel();
        }

        private void OnValidate()
        {
            characterControllerShapeConfigured = false;
            if (Application.isPlaying)
                return;

            if (leashExitDistance > leashEnterDistance)
                leashExitDistance = leashEnterDistance;

            float minControllerHeight = characterControllerRadius * 2f + 0.01f;
            if (characterControllerHeight < minControllerHeight)
                characterControllerHeight = minControllerHeight;

            if (characterController == null)
                characterController = GetComponent<CharacterController>();

            ConfigureCharacterControllerShape(force: true);
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
            LogDesiredPointDistancesPeriodic();
            ApplyMovement(Time.deltaTime);
            PushCreatureVisualRuntimeContext();
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
            eatAttackTargetSnapshot = null;
            eatAttackTargetSnapshotPoint = Vector3.zero;
            ResetEatPostConsumeCarry();
            huntRecenterPointInitialized = false;
            huntRecenterForwardLocked = false;
            huntRecenterLockedForward = Vector3.zero;
            recenterAlignedStableTimer = 0f;
            ResetHuntPerceivedPlayerForward();
            verticalVelocity = groundedStickVelocity;
            nextEatAllowedTime = 0f;
            knockedVegetationCooldowns.Clear();
            leashActive = false;
            noGroundTimer = 0f;
            nextNoGroundRecoveryAllowedTime = 0f;
            nextDesiredPointDistanceLogTime = 0f;
            ResetGroundAlignment();
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
            eatAttackTargetSnapshot = null;
            eatAttackTargetSnapshotPoint = Vector3.zero;
            ResetEatPostConsumeCarry();
            huntRecenterPointInitialized = false;
            huntRecenterForwardLocked = false;
            huntRecenterLockedForward = Vector3.zero;
            recenterAlignedStableTimer = 0f;
            ResetHuntPerceivedPlayerForward();
            verticalVelocity = groundedStickVelocity;
            nextEatAllowedTime = 0f;
            leashActive = false;
            noGroundTimer = 0f;
            nextNoGroundRecoveryAllowedTime = 0f;
            nextDesiredPointDistanceLogTime = 0f;
            ResetGroundAlignment();
        }

        private void UpdateSpawn(float dt)
        {
            if (!spawnPending)
                return;
            if (spawnOncePerRun && hasSpawnedThisLevel)
                return;

            spawnTimer += dt;
            if (!enableDelayedSpawn || spawnTimer >= spawnDelay)
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
            ResetGroundAlignment();
            desiredMovePoint = transform.position;
            verticalVelocity = groundedStickVelocity;
            nextEatAllowedTime = 0f;
            leashActive = false;
            noGroundTimer = 0f;
            nextNoGroundRecoveryAllowedTime = 0f;
            nextDesiredPointDistanceLogTime = 0f;
            ChangeState(CreatureState.FOLLOW, force: true);
            SetCreatureVisible(true);

            if (debugLogs)
                Debug.Log($"Creature spawned at {spawnPos} state:{state}");
        }

        private void LogDesiredPointDistancesPeriodic()
        {
            if (!debugLogs || playerController == null)
                return;
            if (Time.time < nextDesiredPointDistanceLogTime)
                return;

            Vector3 creaturePos = transform.position;
            Vector3 playerPos = playerController.transform.position;
            Vector3 playerForward = GetPlayerForward(); 

            float desiredToCreature = Vector3.Distance(desiredMovePoint, creaturePos);
            float desiredToPlayer = Vector3.Distance(desiredMovePoint, playerPos);
            Vector3 playerToDesired = desiredMovePoint - playerPos;
            playerToDesired.y = 0f;
            float aheadSigned = Vector3.Dot(playerToDesired, playerForward);

            Debug.Log(
                $"DesiredPointDist state:{state} desired:{desiredMovePoint} " +
                $"toCreature:{desiredToCreature:F2} " +
                $"toPlayer:{desiredToPlayer:F2} " +
                $"aheadSigned:{aheadSigned:F2}");

            nextDesiredPointDistanceLogTime = Time.time + 1f;
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

            // Keep previous desiredMovePoint by default. This avoids one-frame
            // "snap under creature" when a state transitions before setting
            // a new desired point in the same tick.
            desiredSpeed = 0f;
            ApplyDistanceLeashOverride();

            switch (state)
            {
                case CreatureState.FOLLOW:
                    TickFollow();
                    break;
                case CreatureState.OVERTAKE:
                    TickOvertake();
                    break;
                case CreatureState.HUNT:
                    UpdateHuntPerceivedPlayerForward(Time.deltaTime);
                    TickHunt();
                    break;
                case CreatureState.RECENTER:
                    UpdateHuntPerceivedPlayerForward(Time.deltaTime);
                    TickRecenter();
                    break;
                case CreatureState.WAIT_PLAYER:
                    UpdateHuntPerceivedPlayerForward(Time.deltaTime);
                    TickWaitPlayer();
                    break;
                case CreatureState.EAT_ATTACK:
                    TickEatAttack();
                    break;
                case CreatureState.EAT_RECOVERY:
                    TickEatRecovery();
                    break;
                case CreatureState.LEASH_RETURN:
                    TickLeashReturn();
                    break;
                default:
                    TickFollow();
                    break;
            }
        }

        private void ChangeState(CreatureState nextState, bool force = false)
        {
            if (!force && state == nextState)
                return;

            CreatureState previous = state;
            stateTime = 0f;
            previousGapError = 0f;

            switch (nextState)
            {
                case CreatureState.FOLLOW:
                    currentTarget = null;
                    eatAttackTargetSnapshot = null;
                    eatAttackTargetSnapshotPoint = Vector3.zero;
                    eatTriggered = false;
                    eatJumpStarted = false;
                    ResetEatPostConsumeCarry();
                    ClearRecenterLock();
                    ResetHuntPerceivedPlayerForward();
                    break;
                case CreatureState.OVERTAKE:
                    lastOvertakeTime = Time.time;
                    overtakeSideSign = UnityEngine.Random.value < 0.5f ? -1 : 1;
                    currentTarget = null;
                    eatAttackTargetSnapshot = null;
                    eatAttackTargetSnapshotPoint = Vector3.zero;
                    eatTriggered = false;
                    eatJumpStarted = false;
                    ResetEatPostConsumeCarry();
                    ClearRecenterLock();
                    ResetHuntPerceivedPlayerForward();
                    break;
                case CreatureState.HUNT:
                    currentTarget = null;
                    eatAttackTargetSnapshot = null;
                    eatAttackTargetSnapshotPoint = Vector3.zero;
                    eatTriggered = false;
                    eatJumpStarted = false;
                    ResetEatPostConsumeCarry();
                    ClearRecenterLock();
                    InitializeHuntPerceivedPlayerForward();
                    if (previous == CreatureState.OVERTAKE || previous == CreatureState.EAT_ATTACK || previous == CreatureState.EAT_RECOVERY)
                    {
                        nextEatAllowedTime = Time.time + huntMinDelayBetweenEat;
                        if (debugLogs)
                            Debug.Log($"Creature entered HUNT state, next eat allowed at {nextEatAllowedTime:F2}");
                    }
                    break;
                case CreatureState.RECENTER:
                    currentTarget = null;
                    eatAttackTargetSnapshot = null;
                    eatAttackTargetSnapshotPoint = Vector3.zero;
                    eatTriggered = false;
                    eatJumpStarted = false;
                    ResetEatPostConsumeCarry();
                    InitializeHuntPerceivedPlayerForward();
                    LockRecenterForward(GetPlayerForward());
                    desiredMovePoint = GetRecenterDesiredPoint();
                    desiredSpeed = ComputeHuntSpeedWithLeadLimit(GetPlayerSpeed() + 5f);
                    break;
                case CreatureState.WAIT_PLAYER:
                    currentTarget = null;
                    eatAttackTargetSnapshot = null;
                    eatAttackTargetSnapshotPoint = Vector3.zero;
                    eatTriggered = false;
                    eatJumpStarted = false;
                    ResetEatPostConsumeCarry();
                    ClearRecenterLock();
                    InitializeHuntPerceivedPlayerForward();
                    break;
                case CreatureState.EAT_ATTACK:
                    ResetEatPostConsumeCarry();
                    ClearRecenterLock();
                    ResetHuntPerceivedPlayerForward();
                    eatTriggered = false;
                    eatJumpStarted = false;
                    if (currentTarget != null && currentTarget.gameObject.activeInHierarchy)
                    {
                        eatAttackTargetSnapshot = currentTarget;
                        eatAttackTargetSnapshotPoint = currentTarget.bounds.center;
                    }
                    else
                    {
                        eatAttackTargetSnapshot = null;
                        Vector3 fallbackForward = transform.forward;
                        fallbackForward.y = 0f;
                        if (fallbackForward.sqrMagnitude < 0.0001f)
                            fallbackForward = GetPlayerForward();
                        if (fallbackForward.sqrMagnitude < 0.0001f)
                            fallbackForward = Vector3.forward;
                        fallbackForward.Normalize();
                        eatAttackTargetSnapshotPoint = transform.position + fallbackForward * huntReachDistance;
                    }
                    currentTarget = eatAttackTargetSnapshot;
                    break;
                case CreatureState.EAT_RECOVERY:
                    currentTarget = null;
                    eatAttackTargetSnapshot = null;
                    eatAttackTargetSnapshotPoint = Vector3.zero;
                    ClearRecenterLock();
                    ResetHuntPerceivedPlayerForward();
                    BeginEatPostConsumeCarry();
                    break;
                case CreatureState.LEASH_RETURN:
                    currentTarget = null;
                    eatAttackTargetSnapshot = null;
                    eatAttackTargetSnapshotPoint = Vector3.zero;
                    eatTriggered = false;
                    eatJumpStarted = false;
                    ResetEatPostConsumeCarry();
                    ClearRecenterLock();
                    ResetHuntPerceivedPlayerForward();
                    break;
            }

            if (debugLogs)
                Debug.Log($"Creature state: {state} -> {nextState}");
            state = nextState;
            PushCreatureVisualState();
            PushCreatureVisualRuntimeContext();
        }

        private void ClearRecenterLock()
        {
            huntRecenterPointInitialized = false;
            huntRecenterForwardLocked = false;
            huntRecenterLockedForward = Vector3.zero;
            recenterAlignedStableTimer = 0f;
        }

        private void LockRecenterForward(Vector3 recenterForward)
        {
            recenterForward.y = 0f;
            if (recenterForward.sqrMagnitude < 0.0001f)
                recenterForward = GetPlayerForward();
            if (recenterForward.sqrMagnitude < 0.0001f)
                recenterForward = Vector3.forward;

            huntRecenterLockedForward = recenterForward.normalized;
            huntRecenterForwardLocked = true;
            huntRecenterPointInitialized = false;
            recenterAlignedStableTimer = 0f;
        }

        private void TickFollow()
        {
            if (GetPlayerSpeed() < followToOvertakePlayerSpeedThreshold)
            {
                ChangeState(CreatureState.OVERTAKE);
                return;
            }

            SetFollowMotion(-desiredFollowDistance, maxSpeedFollow);
        }

        private void TickOvertake()
        {
            // As soon as the creature is in front, switch to hunt mode.
            if (GetPlayerOffsetAlongForward() > 0.5f)
            {
                ChangeState(CreatureState.HUNT);
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

        private void TickHunt()
        {
            if (GetPlayerSpeed() >= huntToFollowPlayerSpeedThreshold)
            {
                ChangeState(CreatureState.FOLLOW);
                return;
            }

            if (currentTarget != null && !currentTarget.gameObject.activeInHierarchy)
                currentTarget = null;

            bool hasLockedTarget = currentTarget != null;
            Vector3 huntForward = GetHuntReferenceForward();
            float headingAngle = GetHeadingAngleToForward(huntForward);
            float recenterEnterThreshold = huntRecenterHeadingAngleThreshold;
            float recenterExitThreshold = recenterEnterThreshold * 0.5f;
            float waitEnterLeadDistance = huntMaxLeadDistance;
            float leadDistance = GetPlayerOffsetAlongForward();

            if (!hasLockedTarget && headingAngle > recenterEnterThreshold)
            {
                ChangeState(CreatureState.RECENTER);
                return;
            }

            if (!hasLockedTarget &&
                leadDistance > waitEnterLeadDistance &&
                headingAngle <= recenterExitThreshold)
            {
                ChangeState(CreatureState.WAIT_PLAYER);
                return;
            }

            if (!hasLockedTarget && Time.time >= nextEatAllowedTime)
            {
                TryAcquireInstrumentTarget();
                hasLockedTarget = currentTarget != null;
            }

            if (!hasLockedTarget)
            {
                desiredMovePoint = GetPointRelativeToPlayer(huntMaxLeadDistance, 0f, huntForward);
                float baselineHuntSpeed = GetPlayerSpeed() + 2f;
                desiredSpeed = ComputeHuntSpeedWithLeadLimit(baselineHuntSpeed);
                return;
            }

            Vector3 targetPos = currentTarget.bounds.center;
            desiredMovePoint = targetPos;
            float baselineTargetHuntSpeed = GetPlayerSpeed() + 4f;
            desiredSpeed = baselineTargetHuntSpeed;

            Vector3 toTarget = targetPos - transform.position;
            toTarget.y = 0f;

            if (toTarget.magnitude <= huntReachDistance)
            {
                if (Time.time >= nextEatAllowedTime)
                    ChangeState(CreatureState.EAT_ATTACK);
                return;
            }
        }

        private void TickRecenter()
        {
            if (GetPlayerSpeed() >= huntToFollowPlayerSpeedThreshold)
            {
                ChangeState(CreatureState.FOLLOW);
                return;
            }

            currentTarget = null;

            RefreshRecenterForwardLock();

            Vector3 rawRecenterPoint = GetRecenterDesiredPoint();
            desiredMovePoint = GetSmoothedHuntRecenterPoint(rawRecenterPoint);
            float baselineRecenterSpeed = GetPlayerSpeed() + 5f;
            desiredSpeed = ComputeHuntSpeedWithLeadLimit(baselineRecenterSpeed);

            float recenterExitHeadingAngle = huntRecenterHeadingAngleThreshold * 0.5f;
            float headingAngle = GetHeadingAngleToForward(huntRecenterLockedForward);
            bool aligned = headingAngle <= recenterExitHeadingAngle;
            if (aligned)
                recenterAlignedStableTimer += Time.deltaTime;
            else
                recenterAlignedStableTimer = 0f;

            float stableDuration = recenterExitStableDuration;
            float leadDistance = GetPlayerOffsetAlongForward();
            if (recenterAlignedStableTimer >= stableDuration && leadDistance <= huntMaxLeadDistance)
            {
                ChangeState(CreatureState.HUNT);
                return;
            }
        }

        private void TickWaitPlayer()
        {
            if (GetPlayerSpeed() >= huntToFollowPlayerSpeedThreshold)
            {
                ChangeState(CreatureState.FOLLOW);
                return;
            }

            currentTarget = null;

            Vector3 huntForward = GetHuntReferenceForward();
            float headingAngle = GetHeadingAngleToForward(huntForward);
            float recenterEnterThreshold = huntRecenterHeadingAngleThreshold;
            if (headingAngle > recenterEnterThreshold)
            {
                ChangeState(CreatureState.RECENTER);
                return;
            }

            float enterLeadDistance = huntMaxLeadDistance;
            float exitLeadDistance = enterLeadDistance * waitPlayerExitLeadRatio;
            float leadDistance = GetPlayerOffsetAlongForward();
            if (leadDistance <= exitLeadDistance)
            {
                ChangeState(CreatureState.HUNT);
                return;
            }

            float desiredLeadDistance = enterLeadDistance * waitPlayerDesiredLeadRatio;
            desiredMovePoint = GetPointRelativeToPlayer(desiredLeadDistance, 0f, huntForward);
            desiredSpeed = ComputeGapSpeed(desiredLeadDistance, maxSpeedHunt);
        }

        private void TickEatAttack()
        {
            if (eatAttackTargetSnapshot != null && !eatAttackTargetSnapshot.gameObject.activeInHierarchy)
                eatAttackTargetSnapshot = null;
            currentTarget = eatAttackTargetSnapshot;

            Vector3 targetCenter = eatAttackTargetSnapshot != null ? eatAttackTargetSnapshot.bounds.center : eatAttackTargetSnapshotPoint;
            desiredMovePoint = targetCenter;
            desiredSpeed = GetPlayerSpeed() + 5f;

            if (TryConsumeCurrentTargetOnCollision())
            {
                ChangeState(CreatureState.EAT_RECOVERY);
                return;
            }

            if (stateTime >= eatDuration)
            {
                if (TryConsumeCurrentTargetByDistanceFallback())
                {
                    ChangeState(CreatureState.EAT_RECOVERY);
                    return;
                }
                ChangeState(CreatureState.EAT_RECOVERY);
            }
        }

        private void TickEatRecovery()
        {
            if (!eatPostConsumeCarryActive)
                BeginEatPostConsumeCarry();

            eatPostConsumeCarryElapsed += Time.deltaTime;
            if (IsEatPostConsumeCarryCompleted())
            {
                ResetEatPostConsumeCarry();
                ChangeState(CreatureState.HUNT);
                return;
            }

            desiredMovePoint = transform.position + eatPostConsumeCarryDirection * 100f;
            float carryDecel = eatPostConsumeCarryDeceleration;
            desiredSpeed = eatPostConsumeCarrySpeed - carryDecel * eatPostConsumeCarryElapsed;
            if (desiredSpeed < 0f)
                desiredSpeed = 0f;
        }

        private void TickLeashReturn()
        {
            currentTarget = null;
            ClearRecenterLock();

            if (!leashActive)
            {
                ChangeState(CreatureState.FOLLOW);
                return;
            }

            Vector3 playerPos = playerController.transform.position;
            desiredMovePoint = playerPos;
            desiredMovePoint.y = transform.position.y;

            float leashSpeedLimit = leashMaxSpeed;
            float catchupTarget = GetPlayerSpeed() + leashCatchupSpeedBoost;
            desiredSpeed = catchupTarget > leashSpeedLimit ? leashSpeedLimit : catchupTarget;
        }

        private void BeginEatPostConsumeCarry()
        {
            Vector3 carryDirection = transform.forward;
            carryDirection.y = 0f;
            if (carryDirection.sqrMagnitude < 0.0001f)
            {
                carryDirection = desiredMovePoint - transform.position;
                carryDirection.y = 0f;
            }
            if (carryDirection.sqrMagnitude < 0.0001f)
                carryDirection = GetPlayerForward();

            eatPostConsumeCarryDirection = carryDirection.normalized;
            eatPostConsumeCarrySpeed = currentSpeed;
            if (eatPostConsumeCarrySpeed < 0.01f)
                eatPostConsumeCarrySpeed = desiredSpeed;
            if (eatPostConsumeCarrySpeed < 0f)
                eatPostConsumeCarrySpeed = 0f;

            currentSpeed = eatPostConsumeCarrySpeed;
            eatPostConsumeCarryActive = true;
            eatPostConsumeCarryWasAirborne = characterController != null && !characterController.isGrounded;
            eatPostConsumeCarryElapsed = 0f;
        }

        private bool IsEatPostConsumeCarryCompleted()
        {
            if (!eatPostConsumeCarryActive)
                return false;

            float maxDuration = eatRecoveryMaxDuration;
            if (eatPostConsumeCarryElapsed >= maxDuration)
                return true;

            if (characterController == null || !characterController.enabled)
                return true;

            if (!eatPostConsumeCarryWasAirborne && !characterController.isGrounded)
                eatPostConsumeCarryWasAirborne = true;

            bool landed = eatPostConsumeCarryWasAirborne && characterController.isGrounded && verticalVelocity <= 0f;
            if (!landed)
                return false;

            float exitSpeed = eatRecoveryExitSpeed;
            return currentSpeed <= exitSpeed;
        }

        private void ResetEatPostConsumeCarry()
        {
            eatPostConsumeCarryActive = false;
            eatPostConsumeCarryWasAirborne = false;
            eatPostConsumeCarryDirection = Vector3.zero;
            eatPostConsumeCarrySpeed = 0f;
            eatPostConsumeCarryElapsed = 0f;
        }

        private void ApplyDistanceLeashOverride()
        {
            if (!enableDistanceLeash || playerController == null)
            {
                bool wasLeashing = leashActive;
                leashActive = false;
                if (wasLeashing && state == CreatureState.LEASH_RETURN)
                    ChangeState(CreatureState.FOLLOW);
                return;
            }

            float enterDistance = leashEnterDistance;
            float exitDistance = leashExitDistance;
            float distanceToPlayer = GetDistanceToPlayer();

            if (!leashActive)
                leashActive = distanceToPlayer > enterDistance;
            else if (distanceToPlayer <= exitDistance)
                leashActive = false;

            if (leashActive)
            {
                if (state != CreatureState.LEASH_RETURN)
                    ChangeState(CreatureState.LEASH_RETURN);
                return;
            }

            if (state == CreatureState.LEASH_RETURN)
                ChangeState(CreatureState.FOLLOW);
        }

        private void SetFollowMotion(float desiredOffset, float speedLimit, float additiveBoost = 0f)
        {
            desiredMovePoint = GetPointRelativeToPlayer(desiredOffset, 0f);
            desiredSpeed = ComputeGapSpeed(desiredOffset, speedLimit, additiveBoost);
        }

        private float ComputeHuntSpeedWithLeadLimit(float baselineSpeed)
        {
            float desiredLead = huntMaxLeadDistance;
            float gapLimitedSpeed = ComputeGapSpeed(desiredLead, maxSpeedHunt);
            return baselineSpeed < gapLimitedSpeed ? baselineSpeed : gapLimitedSpeed;
        }

        private float GetHeadingAngleToForward(Vector3 referenceForward)
        {
            Vector3 creatureForward = transform.forward;
            creatureForward.y = 0f;
            if (creatureForward.sqrMagnitude < 0.0001f)
                return 0f;

            referenceForward.y = 0f;
            if (referenceForward.sqrMagnitude < 0.0001f)
                referenceForward = GetPlayerForward();

            return Vector3.Angle(creatureForward.normalized, referenceForward.normalized);
        }

        private Vector3 GetSmoothedHuntRecenterPoint(Vector3 rawPoint)
        {
            if (!huntRecenterPointInitialized)
            {
                huntRecenterPoint = rawPoint;
                huntRecenterPointInitialized = true;
                return huntRecenterPoint;
            }

            float smoothing = huntRecenterPointSmoothing;
            if (smoothing <= 0f)
            {
                huntRecenterPoint = rawPoint;
                return huntRecenterPoint;
            }

            float t = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
            huntRecenterPoint = Vector3.Lerp(huntRecenterPoint, rawPoint, t);
            return huntRecenterPoint;
        }

        private void RefreshRecenterForwardLock()
        {
            Vector3 playerForward = GetPlayerForward();
            if (!huntRecenterForwardLocked || huntRecenterLockedForward.sqrMagnitude < 0.0001f)
            {
                LockRecenterForward(playerForward);
                return;
            }

            Vector3 recenterForward = huntRecenterLockedForward;
            recenterForward.y = 0f;
            if (recenterForward.sqrMagnitude < 0.0001f)
            {
                LockRecenterForward(playerForward);
                return;
            }
            recenterForward.Normalize();

            float relockDotThreshold = GetRecenterForwardRelockDotThreshold();
            float alignmentDot = Vector3.Dot(recenterForward, playerForward);
            if (alignmentDot < relockDotThreshold)
            {
                if (debugLogs)
                    Debug.Log($"RECENTER relock forward (dot:{alignmentDot:F2} threshold:{relockDotThreshold:F2})");
                LockRecenterForward(playerForward);
            }
        }

        private float GetRecenterForwardRelockDotThreshold()
        {
            // Keep recenter target truly ahead of the player:
            // allow only limited angular drift between locked recenter forward and current player forward.
            float maxAllowedDriftAngle = huntRecenterHeadingAngleThreshold * 0.5f;
            if (maxAllowedDriftAngle < 10f)
                maxAllowedDriftAngle = 10f;
            if (maxAllowedDriftAngle > 35f)
                maxAllowedDriftAngle = 35f;

            return Mathf.Cos(maxAllowedDriftAngle * Mathf.Deg2Rad);
        }

        private Vector3 GetRecenterDesiredPoint()
        {
            Vector3 recenterForward = huntRecenterLockedForward;
            recenterForward.y = 0f;
            if (recenterForward.sqrMagnitude < 0.0001f)
                recenterForward = GetPlayerForward();
            if (recenterForward.sqrMagnitude < 0.0001f)
                recenterForward = Vector3.forward;
            recenterForward.Normalize();

            return GetPointRelativeToPlayer(huntMaxLeadDistance, 0f, recenterForward);
        }

        private void UpdateHuntPerceivedPlayerForward(float dt)
        {
            if (playerController == null)
                return;

            Vector3 actualForward = GetPlayerForward();
            actualForward.y = 0f;
            if (actualForward.sqrMagnitude < 0.0001f)
                return;
            actualForward.Normalize();
            if (!huntPerceivedPlayerForwardInitialized || huntPerceivedPlayerForward.sqrMagnitude < 0.0001f)
            {
                InitializeHuntPerceivedPlayerForward();
                return;
            }

            // Keep heading continuity to avoid 180-degree flips that can make
            // recenter desired points jump left/right frame-to-frame.
            if (Vector3.Dot(huntPerceivedPlayerForward, actualForward) < 0f)
                actualForward = -actualForward;

            float reactionTime = huntPlayerHeadingReactionTime;
            float blend = 1f - Mathf.Exp(-dt / reactionTime);
            huntPerceivedPlayerForward = Vector3.Slerp(huntPerceivedPlayerForward, actualForward, blend);
            huntPerceivedPlayerForward.y = 0f;
            if (huntPerceivedPlayerForward.sqrMagnitude < 0.0001f)
                huntPerceivedPlayerForward = actualForward;
            else
                huntPerceivedPlayerForward.Normalize();
        }

        private Vector3 GetHuntReferenceForward()
        {
            if (huntPerceivedPlayerForwardInitialized && huntPerceivedPlayerForward.sqrMagnitude > 0.0001f)
                return huntPerceivedPlayerForward;
            return GetPlayerForward();
        }

        private void InitializeHuntPerceivedPlayerForward()
        {
            Vector3 initialForward = GetPlayerForward();
            initialForward.y = 0f;
            if (initialForward.sqrMagnitude < 0.0001f)
                initialForward = transform.forward;
            if (initialForward.sqrMagnitude < 0.0001f)
                initialForward = Vector3.forward;

            huntPerceivedPlayerForward = initialForward.normalized;
            huntPerceivedPlayerForwardInitialized = true;
        }

        private void ResetHuntPerceivedPlayerForward()
        {
            huntPerceivedPlayerForwardInitialized = false;
            huntPerceivedPlayerForward = Vector3.zero;
        }

        private Vector3 GetPointRelativeToPlayer(float forwardOffset, float lateralOffset)
        {
            return GetPointRelativeToPlayer(forwardOffset, lateralOffset, GetPlayerForward());
        }

        private Vector3 GetPointRelativeToPlayer(float forwardOffset, float lateralOffset, Vector3 referenceForward)
        {
            Vector3 playerPos = playerController.transform.position;
            Vector3 playerForward = referenceForward;
            playerForward.y = 0f;
            if (playerForward.sqrMagnitude < 0.0001f)
                playerForward = GetPlayerForward();
            if (playerForward.sqrMagnitude < 0.0001f)
                playerForward = Vector3.forward;
            playerForward.Normalize();
            Vector3 playerRight = Vector3.Cross(Vector3.up, playerForward);
            Vector3 point = playerPos + playerForward * forwardOffset + playerRight * lateralOffset;
            // Keep desired point at player height for clearer debug intent.
            // Movement stays horizontal anyway (y is ignored in ApplyMovement).
            point.y = playerPos.y;
            return point;
        }

        //
        /// Computes desired speed to maintain a target offset from the player using 
        /// a proportional-derivative controller.
        /// Calcule la vitesse cible de la créature pour tenir un écart voulu avec 
        /// le joueur (contrôle type PD), puis la borne à une limite max.
        /// </summary>
        /// <param name="desiredOffset"></param>
        /// <param name="speedLimit"></param>
        /// <param name="additiveBoost"></param>
        /// <returns></returns>
        private float ComputeGapSpeed(float desiredOffset, float speedLimit, float additiveBoost = 0f)

        {
            float dt = Time.deltaTime;
            if (dt < 0.0001f)
                dt = 0.0001f;
            float currentOffset = GetPlayerOffsetAlongForward();
            float error = desiredOffset - currentOffset;
            float errorRate = (error - previousGapError) / dt;
            previousGapError = error;

            float target = GetPlayerSpeed() + kp * error - kd * errorRate + additiveBoost;
            if (GetDistanceToPlayer() > catchupBoostDistance)
                target += catchupBoost;
            if (target < 0f)
                return 0f;
            if (target > speedLimit)
                return speedLimit;
            return target;
        }

        private void ApplyMovement(float dt)
        {
            if (characterController == null || !characterController.enabled)
                return;
            if (TryHandleNoGroundFailsafe(dt))
                return;

            Vector3 toTarget = desiredMovePoint - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            Vector3 desiredMoveDir = distance > 0.001f ? toTarget / distance : transform.forward;
            desiredMoveDir.y = 0f;
            if (desiredMoveDir.sqrMagnitude > 0.0001f)
                desiredMoveDir.Normalize();
            else
                desiredMoveDir = Vector3.forward;

            Vector3 currentForward = transform.forward;
            currentForward.y = 0f;
            if (currentForward.sqrMagnitude > 0.0001f)
                currentForward.Normalize();
            else
                currentForward = desiredMoveDir;

            float maxTurnRadians = turnRateDegPerSec * Mathf.Deg2Rad * dt;
            Vector3 moveDir = Vector3.RotateTowards(currentForward, desiredMoveDir, maxTurnRadians, 0f);
            moveDir.y = 0f;
            if (moveDir.sqrMagnitude > 0.0001f)
                moveDir.Normalize();
            else
                moveDir = desiredMoveDir;

            float accel = desiredSpeed >= currentSpeed ? accelMax : brakeMax;
            currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, accel * dt);

            if (state == CreatureState.EAT_ATTACK && !eatJumpStarted)
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
            if (lookDir.sqrMagnitude < 0.0001f)
            {
                lookDir = currentForward;
            }

            Vector3 desiredUp = GetDesiredUpVector(dt);
            Vector3 projectedLookDir = Vector3.ProjectOnPlane(lookDir, desiredUp);
            if (projectedLookDir.sqrMagnitude > 0.0001f)
            {
                Quaternion look = Quaternion.LookRotation(projectedLookDir.normalized, desiredUp);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnRateDegPerSec * dt);
            }
        }

        private void ResetGroundAlignment()
        {
            groundUpInitialized = false;
            smoothedGroundUp = Vector3.up;
        }

        private Vector3 GetDesiredUpVector(float dt)
        {
            Vector3 targetUp = Vector3.up;
            if (alignToGroundSlope && TrySampleGroundNormal(out Vector3 sampledGroundNormal))
            {
                float alignWeight = characterController != null && !characterController.isGrounded
                    ? airborneGroundAlignWeight
                    : 1f;
                targetUp = Vector3.Slerp(Vector3.up, sampledGroundNormal, alignWeight);
            }

            if (!groundUpInitialized)
            {
                smoothedGroundUp = targetUp;
                groundUpInitialized = true;
                return smoothedGroundUp;
            }

            float smoothing = groundAlignSmoothing;
            if (smoothing <= 0f)
            {
                smoothedGroundUp = targetUp;
                return smoothedGroundUp;
            }

            float t = 1f - Mathf.Exp(-smoothing * dt);
            smoothedGroundUp = Vector3.Slerp(smoothedGroundUp, targetUp, t);
            if (smoothedGroundUp.sqrMagnitude < 0.0001f)
                smoothedGroundUp = Vector3.up;
            else
                smoothedGroundUp.Normalize();

            return smoothedGroundUp;
        }

        private bool TrySampleGroundNormal(out Vector3 sampledNormal)
        {
            sampledNormal = Vector3.up;
            int probeLayerMask = GetGroundAlignmentLayerMask();
            if (probeLayerMask == 0)
                return false;

            float startHeight = groundProbeStartHeight;
            float probeDistance = groundProbeDistance;
            float sphereRadius = 0.2f;
            if (characterController != null)
            {
                sphereRadius = characterController.radius * 0.8f;
                if (sphereRadius < 0.1f)
                    sphereRadius = 0.1f;
            }

            Vector3 rayOrigin = transform.position + Vector3.up * startHeight;
            bool hasHit = Physics.SphereCast(
                rayOrigin,
                sphereRadius,
                Vector3.down,
                out RaycastHit hit,
                probeDistance,
                probeLayerMask,
                QueryTriggerInteraction.Ignore);

            if (!hasHit)
            {
                hasHit = Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out hit,
                    probeDistance,
                    probeLayerMask,
                    QueryTriggerInteraction.Ignore);
            }

            if (!hasHit)
                return false;

            Vector3 normal = hit.normal;
            if (normal.sqrMagnitude < 0.0001f)
                return false;
            normal.Normalize();

            float clampedMaxTilt = maxGroundTiltAngle;
            if (clampedMaxTilt < 89.9f)
            {
                float angleToUp = Vector3.Angle(Vector3.up, normal);
                if (angleToUp > clampedMaxTilt && angleToUp > 0.001f)
                {
                    float blend = clampedMaxTilt / angleToUp;
                    normal = Vector3.Slerp(Vector3.up, normal, blend);
                    normal.Normalize();
                }
            }

            sampledNormal = normal;
            return true;
        }

        private int GetGroundAlignmentLayerMask()
        {
            int configuredMask = groundAlignmentLayerMask.value;
            if (configuredMask != 0)
                return configuredMask;

            int terrainMask = global::TerrainLayer.TerrainCurrentBit;
            if (terrainMask != 0)
                return terrainMask;

            return Physics.DefaultRaycastLayers;
        }

        private bool TryHandleNoGroundFailsafe(float dt)
        {
            if (!enableNoGroundFailsafe || playerController == null)
            {
                noGroundTimer = 0f;
                return false;
            }

            if (HasGroundBelow(out _) || (characterController != null && characterController.isGrounded))
            {
                noGroundTimer = 0f;
                return false;
            }

            noGroundTimer += dt;
            if (noGroundTimer < noGroundMaxDuration)
                return false;

            if (Time.time < nextNoGroundRecoveryAllowedTime)
                return false;

            RecoverCreatureNearPlayer("No ground detected under creature");
            return true;
        }

        private bool HasGroundBelow(out RaycastHit hit)
        {
            hit = default(RaycastHit);
            int probeLayerMask = GetGroundAlignmentLayerMask();
            if (probeLayerMask == 0)
                return false;

            float startHeight = noGroundProbeStartHeight;
            float probeDistance = noGroundProbeDistance;
            float sphereRadius = 0.2f;
            if (characterController != null)
            {
                sphereRadius = characterController.radius * 0.8f;
                if (sphereRadius < 0.1f)
                    sphereRadius = 0.1f;
            }

            Vector3 rayOrigin = transform.position + Vector3.up * startHeight;
            bool hasHit = Physics.SphereCast(
                rayOrigin,
                sphereRadius,
                Vector3.down,
                out hit,
                probeDistance,
                probeLayerMask,
                QueryTriggerInteraction.Ignore);

            if (!hasHit)
            {
                hasHit = Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out hit,
                    probeDistance,
                    probeLayerMask,
                    QueryTriggerInteraction.Ignore);
            }

            return hasHit;
        }

        private void RecoverCreatureNearPlayer(string reason)
        {
            if (playerController == null)
                return;

            Vector3 playerForward = GetPlayerForward();
            float behindDistance = noGroundRecoveryBehindPlayerDistance;
            Vector3 recoverPos = playerController.transform.position - playerForward * behindDistance;
            recoverPos.y += noGroundRecoveryHeightOffset;
            Quaternion recoverRot = Quaternion.LookRotation(playerForward, Vector3.up);

            bool restoreController = characterController != null && characterController.enabled;
            if (restoreController)
                characterController.enabled = false;
            transform.SetPositionAndRotation(recoverPos, recoverRot);
            if (restoreController)
                characterController.enabled = true;

            currentTarget = null;
            desiredMovePoint = recoverPos;
            desiredSpeed = 0f;
            currentSpeed = 0f;
            verticalVelocity = groundedStickVelocity;
            leashActive = false;
            noGroundTimer = 0f;
            nextNoGroundRecoveryAllowedTime = Time.time + noGroundRecoveryCooldown;

            ResetEatPostConsumeCarry();
            eatAttackTargetSnapshot = null;
            eatAttackTargetSnapshotPoint = Vector3.zero;
            ClearRecenterLock();
            ResetHuntPerceivedPlayerForward();
            ResetGroundAlignment();
            ChangeState(CreatureState.FOLLOW, force: true);

            if (debugLogs)
                Debug.LogWarning($"Creature recovered near player ({reason}) at {recoverPos}");
        }

        private float ComputeEatJumpUpVelocity()
        {
            float clampedHeight = eatJumpHeight;
            float clampedGravity = gravity;
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
                nextTargetScanLogTime = Time.time + debugTargetScanLogInterval;
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
            Vector3 playerForward = state == CreatureState.HUNT ? GetHuntReferenceForward() : GetPlayerForward();

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

        private bool TryConsumeCurrentTargetByDistanceFallback()
        {
            if (eatTriggered || currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
                return false;

            Vector3 delta = currentTarget.bounds.center - transform.position;
            float maxDistance = eatContactDistance;
            if (characterController != null)
                maxDistance += characterController.radius;
            maxDistance += currentTarget.bounds.extents.magnitude * 0.5f;

            if (delta.sqrMagnitude > maxDistance * maxDistance)
                return false;

            ConsumeCurrentTarget();
            eatTriggered = true;
            return true;
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
            if (state != CreatureState.EAT_ATTACK || eatTriggered)
                return;
            if (!IsCurrentTargetCollider(other))
                return;

            ConsumeCurrentTarget();
            eatTriggered = true;
            ChangeState(CreatureState.EAT_RECOVERY);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.collider == null)
                return;

            if (state == CreatureState.EAT_ATTACK && !eatTriggered && IsCurrentTargetCollider(hit.collider))
            {
                ConsumeCurrentTarget();
                eatTriggered = true;
                ChangeState(CreatureState.EAT_RECOVERY);
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

            knockedVegetationCooldowns[key] = Time.time + vegetationKnockCooldown;

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

            rb.mass = vegetationRigidbodyMass;
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
            ConfigureCharacterControllerShape();

            if (playerController == null)
                playerController = gameManager.playerController;
            if (terrainGenerator == null)
                terrainGenerator = gameManager.terrainGenerator;
            if (bonusManager == null)
                bonusManager = gameManager.bonusManager;
            bool visualJustResolved = false;
            if (creatureVisual == null)
            {
                creatureVisual = GetComponentInChildren<CreatureVisualBase>(true);
                visualJustResolved = creatureVisual != null;
            }
            if (visualJustResolved)
            {
                PushCreatureVisualState();
                PushCreatureVisualRuntimeContext();
            }

            return playerController != null && characterController != null;
        }

        private void PushCreatureVisualState()
        {
            if (creatureVisual == null)
                return;

            creatureVisual.SetControllerState(state);
        }

        private void PushCreatureVisualRuntimeContext()
        {
            if (creatureVisual == null)
                return;

            bool grounded = characterController != null && characterController.isGrounded;
            Vector3 groundNormal = groundUpInitialized ? smoothedGroundUp : Vector3.up;
            creatureVisual.SetMotionContext(currentSpeed, grounded, groundNormal);
            creatureVisual.SetTargetContext(currentTarget != null);
        }

        private void ConfigureCharacterControllerShape(bool force = false)
        {
            if (characterController == null)
                return;
            if (!autoConfigureCharacterControllerShape)
                return;
            if (!force && characterControllerShapeConfigured)
                return;

            float configuredRadius = characterControllerRadius;
            float configuredHeight = characterControllerHeight;
            Vector3 configuredCenter = characterControllerCenter;

            bool restoreEnabled = characterController.enabled;
            if (restoreEnabled)
                characterController.enabled = false;

            characterController.radius = configuredRadius;
            characterController.height = configuredHeight;
            characterController.center = configuredCenter;
            if (characterController.stepOffset > configuredHeight)
                characterController.stepOffset = configuredHeight * 0.5f;

            if (restoreEnabled)
                characterController.enabled = true;

            characterControllerShapeConfigured = true;
        }

        private void OnDrawGizmos()
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
                case CreatureState.HUNT:
                    stateColor = new Color(1f, 0.5f, 0f);
                    break;
                case CreatureState.RECENTER:
                    stateColor = new Color(0.2f, 0.9f, 0.9f);
                    break;
                case CreatureState.WAIT_PLAYER:
                    stateColor = new Color(0.95f, 0.85f, 0.2f);
                    break;
                case CreatureState.EAT_ATTACK:
                    stateColor = new Color(0.7f, 0.2f, 0.95f);
                    break;
                case CreatureState.EAT_RECOVERY:
                    stateColor = new Color(0.65f, 0.2f, 0.85f);
                    break;
                case CreatureState.LEASH_RETURN:
                    stateColor = new Color(0.2f, 0.45f, 1f);
                    break;
                default:
                    stateColor = Color.white;
                    break;
            }

            Gizmos.color = stateColor;
            Gizmos.DrawWireSphere(transform.position, 0.6f);
            Gizmos.DrawLine(transform.position, desiredMovePoint);
            Gizmos.DrawWireCube(desiredMovePoint, Vector3.one * 0.5f);

            if (state == CreatureState.OVERTAKE || state == CreatureState.HUNT || state == CreatureState.RECENTER || state == CreatureState.WAIT_PLAYER)
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
