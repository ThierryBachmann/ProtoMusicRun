using UnityEngine;

namespace MusicRun
{
    public enum CreatureState
    {
        CHASE = 0,
        OVERTAKE = 1,
        HUNT_INSTRUMENT = 2,
        EAT = 3,
        RETURN = 4,
    }

    [DisallowMultipleComponent]
    public class CreatureController : MonoBehaviour
    {
        [Header("Spawn")]
        public bool enableDelayedSpawn = true;
        [Tooltip("Delay in seconds before the creature appears after level start.")]
        public float spawnDelay = 2f;
        public bool spawnOncePerRun = true;
        [Tooltip("Optional explicit spawn point. When empty, terrainGenerator.currentStart is used.")]
        public Transform spawnStartOverride;
        public float spawnHeightOffset = 0f;

        [Header("Follow")]
        public float desiredFollowDistance = 12f;
        public float followDeadZone = 2f;
        public float returnDistance = 12f;
        public float maxLeashDistance = 35f;

        [Header("Overtake")]
        public float overtakeLeadDistance = 5f;
        public float overtakeLateralOffset = 2.5f;
        public float overtakeCooldown = 2f;
        public float overtakeNoTargetTimeout = 1.2f;
        public float overtakeRandomCheckInterval = 0.3f;
        [Range(0f, 1f)] public float overtakeChancePerCheck = 0.25f;

        [Header("Hunt")]
        public float huntSearchRadius = 28f;
        public float huntReachDistance = 1.6f;
        public float huntMaxDuration = 4f;

        [Header("Eat")]
        public float eatDuration = 0.6f;

        [Header("Movement")]
        public float kp = 0.9f;
        public float kd = 0.25f;
        public float accelMax = 20f;
        public float brakeMax = 24f;
        public float maxSpeedChase = 16f;
        public float maxSpeedOvertake = 20f;
        public float maxSpeedHunt = 22f;
        public float maxSpeedReturn = 18f;
        public float catchupBoostDistance = 20f;
        public float catchupBoost = 3f;
        public float turnRateDegPerSec = 360f;

        [Header("Target Score")]
        public float targetWeightDistance = 1f;
        public float targetWeightAngle = 0.35f;
        public float targetWeightBehindPlayer = 0.5f;

        [Header("Debug")]
        public bool debugLogs;
        public bool drawDebugGizmos = true;

        public CreatureState State => state;
        public bool IsSpawned => isSpawned;
        public Collider CurrentTarget => currentTarget;

        private GameManager gameManager;
        private PlayerController playerController;
        private TerrainGenerator terrainGenerator;
        private BonusManager bonusManager;

        private CreatureState state = CreatureState.RETURN;
        private float stateTime;
        private float spawnTimer;
        private float currentSpeed;
        private float previousGapError;
        private float lastOvertakeTime = -999f;
        private float nextOvertakeCheckTime;
        private int overtakeSideSign = 1;
        private bool levelActive;
        private bool spawnPending;
        private bool isSpawned;
        private bool hasSpawnedThisLevel;
        private bool eatTriggered;

        private Collider currentTarget;
        private readonly Collider[] instrumentScanBuffer = new Collider[64];
        private Renderer[] cachedRenderers;
        private Collider[] cachedColliders;

        private Vector3 desiredMovePoint;
        private float desiredSpeed;

        private void Awake()
        {
            CacheVisibilityComponents();
            ResolveReferences();
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
            ChangeState(CreatureState.RETURN, force: true);
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
                spawnPos = playerPos - playerForward * returnDistance;
                spawnRot = Quaternion.LookRotation(playerForward, Vector3.up);
            }

            spawnPos.y += spawnHeightOffset;
            transform.SetPositionAndRotation(spawnPos, spawnRot);

            isSpawned = true;
            spawnPending = false;
            hasSpawnedThisLevel = true;
            desiredMovePoint = transform.position;
            nextOvertakeCheckTime = Time.time + UnityEngine.Random.Range(0f, overtakeRandomCheckInterval);
            ChangeState(CreatureState.RETURN, force: true);
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

            if (state != CreatureState.RETURN && GetDistanceToPlayer() > maxLeashDistance)
            {
                ChangeState(CreatureState.RETURN);
            }

            desiredMovePoint = transform.position;
            desiredSpeed = 0f;

            switch (state)
            {
                case CreatureState.CHASE:
                    TickChase();
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
                case CreatureState.RETURN:
                    TickReturn();
                    break;
                default:
                    TickReturn();
                    break;
            }
        }

        private void TickChase()
        {
            if (CanStartOpportunisticOvertake())
            {
                ChangeState(CreatureState.OVERTAKE);
                return;
            }

            SetFollowMotion(-desiredFollowDistance, maxSpeedChase);
        }

        private void TickOvertake()
        {
            if (TryAcquireInstrumentTarget())
            {
                ChangeState(CreatureState.HUNT_INSTRUMENT);
                return;
            }

            if (stateTime >= overtakeNoTargetTimeout)
            {
                ChangeState(CreatureState.RETURN);
                return;
            }

            desiredMovePoint = GetPointRelativeToPlayer(overtakeLeadDistance, overtakeSideSign * overtakeLateralOffset);
            desiredSpeed = ComputeGapSpeed(overtakeLeadDistance, maxSpeedOvertake, 1.25f);
        }

        private void TickHuntInstrument()
        {
            if (currentTarget == null)
            {
                ChangeState(CreatureState.RETURN);
                return;
            }
            if (!currentTarget.gameObject.activeInHierarchy)
            {
                currentTarget = null;
                ChangeState(CreatureState.RETURN);
                return;
            }
            if (stateTime >= huntMaxDuration)
            {
                ChangeState(CreatureState.RETURN);
                return;
            }

            Vector3 targetPos = currentTarget.transform.position;
            desiredMovePoint = targetPos;
            desiredSpeed = Mathf.Clamp(GetPlayerSpeed() + 4f, 0f, maxSpeedHunt);

            Vector3 toTarget = targetPos - transform.position;
            toTarget.y = 0f;
            if (toTarget.magnitude <= huntReachDistance)
            {
                ChangeState(CreatureState.EAT);
                return;
            }
        }

        private void TickEat()
        {
            if (!eatTriggered)
            {
                ConsumeCurrentTarget();
                eatTriggered = true;
            }

            desiredMovePoint = transform.position;
            desiredSpeed = 0f;

            if (stateTime >= eatDuration)
                ChangeState(CreatureState.RETURN);
        }

        private void TickReturn()
        {
            float offset = GetPlayerOffsetAlongForward();
            float desiredOffset = -returnDistance;

            if (offset < 0f && Mathf.Abs(offset - desiredOffset) <= followDeadZone)
            {
                ChangeState(CreatureState.CHASE);
                return;
            }

            if (offset > 0.5f && CanStartOpportunisticOvertake())
            {
                ChangeState(CreatureState.OVERTAKE);
                return;
            }

            SetFollowMotion(desiredOffset, maxSpeedReturn, 0.75f);
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
            Vector3 toTarget = desiredMovePoint - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            Vector3 moveDir = distance > 0.001f ? toTarget / distance : transform.forward;

            float accel = desiredSpeed >= currentSpeed ? accelMax : brakeMax;
            currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, accel * dt);

            float moveDistance = currentSpeed * dt;
            if (moveDistance > distance)
                moveDistance = distance;
            transform.position += moveDir * moveDistance;
            //Debug.Log(transform.position);  
            if (moveDir.sqrMagnitude > 0.0001f)
            {
                Quaternion look = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnRateDegPerSec * dt);
            }
        }

        private bool CanStartOpportunisticOvertake()
        {
            if (Time.time < lastOvertakeTime + overtakeCooldown)
                return false;
            if (GetPlayerSpeed() <= 1.5f)
                return false;
            if (GetDistanceToPlayer() >= 18f)
                return false;
            if (Time.time < nextOvertakeCheckTime)
                return false;

            nextOvertakeCheckTime = Time.time + Mathf.Max(0.05f, overtakeRandomCheckInterval);
            return UnityEngine.Random.value <= overtakeChancePerCheck;
        }

        private bool TryAcquireInstrumentTarget()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, huntSearchRadius, instrumentScanBuffer);
            if (count <= 0)
                return false;

            Collider best = null;
            float bestScore = float.MaxValue;

            Vector3 playerPos = playerController.transform.position;
            Vector3 playerForward = GetPlayerForward();

            for (int i = 0; i < count; i++)
            {
                Collider candidate = instrumentScanBuffer[i];
                if (candidate == null || !candidate.gameObject.activeInHierarchy || !candidate.CompareTag("Instrument"))
                    continue;

                Vector3 candidatePos = candidate.transform.position;
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
                    best = candidate;
                }
            }

            currentTarget = best;
            return currentTarget != null;
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

        private void ChangeState(CreatureState nextState, bool force = false)
        {
            if (!force && state == nextState)
                return;

            CreatureState previous = state;
            state = nextState;
            stateTime = 0f;
            previousGapError = 0f;

            if (state == CreatureState.OVERTAKE)
            {
                lastOvertakeTime = Time.time;
                overtakeSideSign = UnityEngine.Random.value < 0.5f ? -1 : 1;
                currentTarget = null;
            }
            else if (state == CreatureState.EAT)
            {
                eatTriggered = false;
            }
            else if (state == CreatureState.RETURN)
            {
                currentTarget = null;
            }

            if (debugLogs)
                Debug.Log($"Creature state: {previous} -> {state}");
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

        private float GetDistanceToPlayer()
        {
            if (playerController == null)
                return float.MaxValue;

            Vector3 delta = playerController.transform.position - transform.position;
            delta.y = 0f;
            return delta.magnitude;
        }

        private float GetPlayerOffsetAlongForward()
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

            if (playerController == null)
                playerController = gameManager.playerController;
            if (terrainGenerator == null)
                terrainGenerator = gameManager.terrainGenerator;
            if (bonusManager == null)
                bonusManager = gameManager.bonusManager;

            return playerController != null;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos)
                return;

            Color stateColor;
            switch (state)
            {
                case CreatureState.CHASE:
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
                case CreatureState.RETURN:
                    stateColor = Color.cyan;
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
