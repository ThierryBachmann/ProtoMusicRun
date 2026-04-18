/*
Voici une synthèse fonctionnelle (non technique) de la créature.

**Objectif Gameplay**
- La créature doit maintenir une pression constante sur le joueur.
- Le joueur doit pouvoir l’influencer (vitesse, trajectoire), sans la contrôler directement.
- Le ressenti recherché est une “grosse bête” avec inertie et autonomie limitée.

**Modes De Comportement**
- `FOLLOW` : la créature accompagne le joueur.
- `OVERTAKE` : la créature repasse devant le joueur.
- `HUNT` : la créature se place devant le joueur et cherche des instruments.
- `EAT` : la créature saute pour percuter et détruire un instrument.
- Le mode `RETURN` n’existe plus.

**Règles De Transition**
- `FOLLOW -> OVERTAKE` quand la vitesse du joueur descend sous un seuil configurable.
- `OVERTAKE -> HUNT` quand la créature a repris sa position de chasse devant le joueur.
- `HUNT -> FOLLOW` quand la vitesse du joueur dépasse un seuil configurable (valeur de référence: 3).
- `HUNT -> EAT` seulement si une cible instrument valide est disponible et que le délai minimal entre deux `EAT` est écoulé.
- `EAT -> HUNT` après l’attaque et retour au sol.
- Les changements d’état ne doivent pas “ping-pong” sans délai.

**Déplacement Attendu**
- Déplacement principal au sol, en suivant le relief du terrain.
- En `EAT`, saut vers la cible instrument.
- À l’atterrissage après `EAT`, conservation de l’élan puis retour progressif à la trajectoire de chasse.
- Pas de rotation brusque ou demi-tour instantané non naturel.

**La créature évolue sur un terrain vallonné.
- Elle doit s’incliner selon la pente du sol pour améliorer le contact visuel avec le terrain.
- Le comportement doit rester stable (pas de jitter, pas de rotation brutale, pas de régression gameplay).

** La créature ne doit jamais tomber dans le vide à cause du streaming terrain quand elle s’éloigne trop du joueur.**
Elle doit rester menaçante, lisible, et revenir naturellement dans une zone sûre.

**Ciblage Et Priorités**
- En `HUNT`, priorité à rester devant le joueur dans son axe.
- Si l’angle entre direction joueur et direction créature dépasse un seuil configurable, la créature arrête temporairement la recherche de nouvelles cibles et se recentre.
- La recherche de nouvelles cibles reprend quand l’angle revient sous un seuil de sortie (hystérésis).
- Quand une cible instrument est verrouillée, la créature s’engage vers cette cible (pas d’oscillation rapide entre deux intentions).

**Interactions Monde**
- Collision réussie en `EAT` avec un instrument: instrument détruit et bonus/score déclenché.
- Collision avec végétation: la créature peut renverser des éléments pour dégager le terrain au bénéfice du joueur.

**Contraintes De Ressenti**
- La créature ne doit pas s’éloigner excessivement du joueur en `HUNT`.
- Elle doit rester menaçante mais lisible.
- Le joueur doit sentir qu’il influence la bête, sans avoir l’impression de la piloter totalement.

**Paramètres De Gameplay À Exposer**
- Seuil vitesse joueur pour `FOLLOW -> OVERTAKE`.
- Seuil vitesse joueur pour `HUNT -> FOLLOW`.
- Délai minimal entre deux `EAT`.
- Angle d’activation du recentrage.
- Distance/position cible devant le joueur en `HUNT`.
- Intensité de rattrapage pour rester sous pression.

Demandes techniques incontournables :

1. **Machine d’état unique et centralisée**  
Toutes les transitions passent par une seule méthode (`ChangeState`), avec garde-fous (cooldown, conditions d’entrée/sortie), sinon on recrée vite des effets de bord.

2. **Mouvement unifié**  
Utiliser une seule logique de déplacement (idéalement `CharacterController`) pour le sol, la gravité, le saut `EAT` et l’atterrissage. Éviter le mélange `CharacterController` + physique dynamique sur le même acteur.

3. **Détection de collisions fiable et idempotente**  
Collision instrument traitée une seule fois par cible (pas de double destruction/score), avec configuration claire des layers/tags et de la matrice de collision.

4. **Ciblage stable (anti-oscillation)**  
Quand une cible est lockée, priorité à cette cible jusqu’à perte/consommation. Le recentrage et la recherche ne doivent pas se battre entre eux.

5. **Cooldown `HUNT` strict**  
Délai minimum entre deux `EAT`, géré dans la logique `HUNT` (pas dispersé), appliqué à chaque entrée en `HUNT`.

6. **Inertie crédible à l’atterrissage**  
Après `EAT`, conservation de l’élan puis retour progressif vers la trajectoire de chasse, sans rotation brutale.

7. **Références et perfs**  
Aucun `Find`/allocation en boucle `Update`; cache des références et calculs stables pour éviter jitter et coûts inutiles.

8. **Paramétrage designer-safe**  
Paramètres exposés, nommés clairement, avec `Tooltip` + bornes (`Min/Range`) pour éviter des réglages incohérents.

9. **Aligner l’orientation globale de la créature sur la normale du sol sous elle.**
Échantillonner le sol via SphereCast (fallback Raycast) sous la créature.
Lisser la normale (up) pour éviter les tremblements.
Construire la rotation avec la direction de mouvement projetée sur le plan du sol.
Limiter l’inclinaison maximale et réduire l’effet en l’air.
Conserver la logique de déplacement/états existante (CharacterController, FSM).
Correction de l’effet de “survol” via réglage automatique de la forme du CharacterController (height, radius, center) avec ConfigureCharacterControllerShape().
Objectif: faire correspondre collision et visuel sans retoucher chaque scène manuellement.
Limites connues: Cette option n’est pas du foot IK: elle améliore fortement le rendu, mais ne garantit pas un contact parfait des 4 pieds dans tous les cas extrêmes.

9. **CreatureController` pilote la logique de gameplay (déplacement, ciblage, transitions).**

10. **La classe de base `Creature` porte la machine d’état commune (`FOLLOW`, `OVERTAKE`, `HUNT`, `EAT`).**

11. **`HippoAlien` hérite de `Creature` et applique le comportement visuel/animation selon l’état courant.**

12. **Source de vérité unique: l’état n’est pas modifié directement ailleurs que via l’interface prévue entre `CreatureController` et `Creature`.**

13. **Si distance créature-joueur > leashEnterDistance, activation du mode leash.**
En leash, forcer la logique de retour vers le joueur (FOLLOW), annuler la cible courante, augmenter la vitesse de rattrapage (leashCatchupSpeedBoost) avec plafond (leashMaxSpeed).
Sortie du leash seulement quand distance < leashExitDistance (hystérésis pour éviter les oscillations).
No-ground failsafe
Sonder le terrain sous la créature (SphereCast puis Raycast).
Si absence de sol pendant plus de noGroundMaxDuration, déclencher une récupération d’urgence.
Repositionner la créature derrière le joueur (noGroundRecoveryBehindPlayerDistance) avec un offset vertical (noGroundRecoveryHeightOffset), réinitialiser son état de mouvement, repasser en FOLLOW.
Appliquer un cooldown (noGroundRecoveryCooldown) pour éviter les téléportations répétées.

14. **Contrainte animation liee a l'avancement reel.**
En Chase, la phase d'animation des jambes est pilotee par la distance horizontale reellement parcourue (pas par Time.time).
Si la creature ne se deplace pas entre deux updates, l'angle des jambes ne doit pas avancer.
La distance d'un cycle d'animation est calibree depuis la geometrie des jambes et l'angle max (`legAngle`).
Les grands deltas de position (teleport/recovery) sont ignores pour eviter des sauts visuels dans la marche.

*/
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
        public float maxSpeedFollow = 16f;

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

        [Tooltip("Maximum forward lead allowed in HUNT before speed is reduced to keep pressure on player.")]
        public float huntMaxLeadDistance = 10f;

        [Tooltip("Enter recenter mode when angle between player and creature headings exceeds this value (degrees). Exit at half this angle.")]
        public float huntRecenterHeadingAngleThreshold = 30f;

        [Tooltip("Smoothing applied to recenter target updates (higher is snappier, lower is smoother).")]
        public float huntRecenterPointSmoothing = 10f;

        [Tooltip("Player speed threshold for HUNT -> FOLLOW transition.")]
        [FormerlySerializedAs("huntExitToFollowPlayerSpeed")]
        public float huntToFollowPlayerSpeedThreshold = 3.5f;

        [Tooltip("Minimum delay before EAT can start after entering HUNT (from OVERTAKE or EAT).")]
        public float huntMinDelayBetweenEat = 1.0f;

        [Tooltip("Duration of inertial recovery after EAT -> HUNT before fully restoring normal HUNT steering.")]
        public float huntPostEatRecoveryDuration = 0.45f;

        [Tooltip("Reaction time used to update perceived player heading in HUNT (higher = slower creature response).")]
        public float huntPlayerHeadingReactionTime = 0.55f;

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

        [Header("Ground Alignment")]
        [Tooltip("Rotate creature to follow the ground slope under it.")]
        public bool alignToGroundSlope = true;
        [Tooltip("Layers used to probe the ground slope. When set to Nothing, TerrainCurrent is used if available.")]
        public LayerMask groundAlignmentLayerMask = 0;
        [Tooltip("Vertical offset of ground probe start above creature position.")]
        public float groundProbeStartHeight = 2.5f;
        [Tooltip("Maximum distance used to probe ground below creature.")]
        public float groundProbeDistance = 6f;
        [Tooltip("Smoothing speed applied to ground alignment (higher is snappier).")]
        public float groundAlignSmoothing = 12f;
        [Tooltip("Maximum slope tilt angle applied to the creature (degrees).")]
        public float maxGroundTiltAngle = 35f;
        [Tooltip("How much slope alignment is kept while airborne (0 = upright, 1 = full).")]
        [Range(0f, 1f)]
        public float airborneGroundAlignWeight = 0.2f;

        [Header("Character Controller Grounding")]
        [Tooltip("When enabled, applies tuned CharacterController shape values to keep the creature visually grounded.")]
        public bool autoConfigureCharacterControllerShape = true;
        [Tooltip("CharacterController height used by the creature.")]
        public float characterControllerHeight = 2f;
        [Tooltip("CharacterController radius used by the creature.")]
        public float characterControllerRadius = 0.5f;
        [Tooltip("CharacterController center in local space. Increase Y if the creature appears to float.")]
        public Vector3 characterControllerCenter = new Vector3(0f, 0.82f, 0f);

        [Header("Distance Safety")]
        [Tooltip("Enable a leash to keep creature near player and avoid leaving streamed terrain.")]
        public bool enableDistanceLeash = true;
        [Tooltip("Creature enters leash mode when horizontal distance to player exceeds this value.")]
        public float leashEnterDistance = 42f;
        [Tooltip("Creature exits leash mode when distance comes back below this value.")]
        public float leashExitDistance = 30f;
        [Tooltip("Extra speed added over player speed while leash mode is active.")]
        public float leashCatchupSpeedBoost = 8f;
        [Tooltip("Maximum speed cap used while leash mode is active.")]
        public float leashMaxSpeed = 30f;

        [Header("No Ground Failsafe")]
        [Tooltip("Enable emergency recovery when no terrain is detected below creature for too long.")]
        public bool enableNoGroundFailsafe = true;
        [Tooltip("Delay without ground before triggering emergency reposition.")]
        public float noGroundMaxDuration = 0.45f;
        [Tooltip("Vertical offset of ground probe start for no-ground detection.")]
        public float noGroundProbeStartHeight = 3.0f;
        [Tooltip("Probe distance used to detect terrain below creature for no-ground detection.")]
        public float noGroundProbeDistance = 20f;
        [Tooltip("Reposition distance behind player when no-ground failsafe triggers.")]
        public float noGroundRecoveryBehindPlayerDistance = 8f;
        [Tooltip("Reposition height offset applied during no-ground recovery.")]
        public float noGroundRecoveryHeightOffset = 1.5f;
        [Tooltip("Cooldown after a no-ground recovery to avoid repeated immediate teleports.")]
        public float noGroundRecoveryCooldown = 1.0f;

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
        private bool huntRecenterActive;
        private bool huntRecenterPointInitialized;
        private bool huntPerceivedPlayerForwardInitialized;
        private bool huntPostEatRecoveryActive;
        private Vector3 eatPostConsumeCarryDirection;
        private Vector3 huntPerceivedPlayerForward;
        private Vector3 huntPostEatRecoveryDirection;
        private float huntPostEatRecoveryStartSpeed;
        private float huntPostEatRecoveryElapsed;
        private float eatPostConsumeCarrySpeed;
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
            ResetEatPostConsumeCarry();
            ResetHuntPostEatRecovery();
            huntRecenterActive = false;
            huntRecenterPointInitialized = false;
            ResetHuntPerceivedPlayerForward();
            verticalVelocity = groundedStickVelocity;
            nextEatAllowedTime = 0f;
            knockedVegetationCooldowns.Clear();
            leashActive = false;
            noGroundTimer = 0f;
            nextNoGroundRecoveryAllowedTime = 0f;
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
            ResetEatPostConsumeCarry();
            ResetHuntPostEatRecovery();
            huntRecenterActive = false;
            huntRecenterPointInitialized = false;
            ResetHuntPerceivedPlayerForward();
            verticalVelocity = groundedStickVelocity;
            nextEatAllowedTime = 0f;
            leashActive = false;
            noGroundTimer = 0f;
            nextNoGroundRecoveryAllowedTime = 0f;
            ResetGroundAlignment();
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
            ResetGroundAlignment();
            desiredMovePoint = transform.position;
            verticalVelocity = groundedStickVelocity;
            nextEatAllowedTime = 0f;
            leashActive = false;
            noGroundTimer = 0f;
            nextNoGroundRecoveryAllowedTime = 0f;
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
                    UpdateHuntPerceivedPlayerForward(Time.deltaTime);
                    TickHuntInstrument();
                    break;
                case CreatureState.EAT:
                    TickEat();
                    break;
                default:
                    TickFollow();
                    break;
            }
            ApplyDistanceLeashOverride();
        }
        private void ChangeState(CreatureState nextState, bool force = false)
        {
            if (!force && state == nextState)
                return;

            CreatureState previous = state;
            Vector3 previousEatCarryDirection = eatPostConsumeCarryDirection;
            float previousEatCarrySpeed = eatPostConsumeCarrySpeed;
            stateTime = 0f;
            previousGapError = 0f;
            if (nextState == CreatureState.HUNT_INSTRUMENT)// && (previous == CreatureState.OVERTAKE || previous == CreatureState.EAT))
            {
                ResetEatPostConsumeCarry();
                huntRecenterActive = false;
                huntRecenterPointInitialized = false;
                InitializeHuntPerceivedPlayerForward();
                nextEatAllowedTime = Time.time + Mathf.Max(0f, huntMinDelayBetweenEat);
                if (debugLogs)
                    Debug.Log($"Creature entered HUNT_INSTRUMENT state, next eat allowed at {nextEatAllowedTime:F2}");

                if (previous == CreatureState.EAT && previousEatCarrySpeed > 0.01f)
                    StartHuntPostEatRecovery(previousEatCarryDirection, previousEatCarrySpeed);
                else
                    ResetHuntPostEatRecovery();
            }

            if (nextState == CreatureState.OVERTAKE)
            {
                lastOvertakeTime = Time.time;
                overtakeSideSign = UnityEngine.Random.value < 0.5f ? -1 : 1;
                currentTarget = null;
                ResetEatPostConsumeCarry();
                ResetHuntPostEatRecovery();
                huntRecenterActive = false;
                huntRecenterPointInitialized = false;
                ResetHuntPerceivedPlayerForward();
            }
            else if (nextState == CreatureState.EAT)
            {
                eatTriggered = false;
                eatJumpStarted = false;
                ResetEatPostConsumeCarry();
                ResetHuntPostEatRecovery();
                huntRecenterActive = false;
                huntRecenterPointInitialized = false;
                ResetHuntPerceivedPlayerForward();
            }
            else if (nextState == CreatureState.FOLLOW)
            {
                currentTarget = null;
                ResetEatPostConsumeCarry();
                ResetHuntPostEatRecovery();
                huntRecenterActive = false;
                huntRecenterPointInitialized = false;
                ResetHuntPerceivedPlayerForward();
            }

            if (debugLogs)
                Debug.Log($"Creature state: {state} -> {nextState}");
            state = nextState;
            PushCreatureVisualState();
            PushCreatureVisualRuntimeContext();
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

            if (currentTarget != null && !currentTarget.gameObject.activeInHierarchy)
                currentTarget = null;

            bool hasLockedTarget = currentTarget != null;
            if (!hasLockedTarget && Time.time < nextEatAllowedTime)
            {
                // During HUNT cooldown and without a locked target, keep pressure by
                // staying in front of the player and skip instrument acquisition.
                huntRecenterActive = false;
                Vector3 huntForward = GetHuntReferenceForward();
                Vector3 rawCooldownPoint = GetPointRelativeToPlayer(huntMaxLeadDistance, 0f, huntForward);
                desiredMovePoint = GetSmoothedHuntRecenterPoint(rawCooldownPoint);
                float baselineCooldownSpeed = Mathf.Clamp(GetPlayerSpeed() + 2f, 0f, maxSpeedHunt);
                desiredSpeed = ComputeHuntSpeedWithLeadLimit(baselineCooldownSpeed);
                return;
            }

            if (currentTarget == null)
                TryAcquireInstrumentTarget();

            if (currentTarget == null)
            {
                bool wasRecenterActive = huntRecenterActive;
                Vector3 huntForward = GetHuntReferenceForward();
                float headingAngle = GetHeadingAngleToForward(huntForward);
                UpdateHuntRecenterMode(headingAngle);
                if (!wasRecenterActive && huntRecenterActive)
                {
                    huntRecenterPointInitialized = false;
                }
                else if (wasRecenterActive && !huntRecenterActive)
                {
                    huntRecenterPointInitialized = false;
                }

                if (huntRecenterActive)
                {
                    Vector3 rawRecenterPoint = GetPointRelativeToPlayer(huntMaxLeadDistance, 0f, huntForward);
                    desiredMovePoint = GetSmoothedHuntRecenterPoint(rawRecenterPoint);
                    float baselineRecenterSpeed = Mathf.Clamp(GetPlayerSpeed() + 3f, 0f, maxSpeedHunt);
                    desiredSpeed = ComputeHuntSpeedWithLeadLimit(baselineRecenterSpeed);
                    return;
                }

                // No target yet: stay in hunt mode while moving ahead and scanning.
                desiredMovePoint = GetPointRelativeToPlayer(overtakeLeadDistance, overtakeSideSign * overtakeLateralOffset, huntForward);
                float baselineHuntSpeed = Mathf.Clamp(GetPlayerSpeed() + 2f, 0f, maxSpeedHunt);
                desiredSpeed = ComputeHuntSpeedWithLeadLimit(baselineHuntSpeed);
                return;
            }

            // When a target is locked, prioritize target pursuit and disable recentering.
            if (huntRecenterActive)
            {
                huntRecenterActive = false;
                huntRecenterPointInitialized = false;
            }

            // Goto the target and eat if close enough.
            Vector3 targetPos = currentTarget.transform.position;
            desiredMovePoint = targetPos;
            float baselineTargetHuntSpeed = Mathf.Clamp(GetPlayerSpeed() + 4f, 0f, maxSpeedHunt);
            // With a locked target, do not clamp speed by player lead distance:
            // keep pressure and let the creature commit to the target pursuit.
            desiredSpeed = baselineTargetHuntSpeed;

            Vector3 toTarget = targetPos - transform.position;
            toTarget.y = 0f;

            // if the creature is close enough to the target, trigger eat state. 
            if (toTarget.magnitude <= huntReachDistance)
            {
                if (Time.time >= nextEatAllowedTime)
                    ChangeState(CreatureState.EAT);
                return;
            }
        }

        private void TickEat()
        {
            if (eatPostConsumeCarryActive)
            {
                if (HasEatPostConsumeCarryLanded())
                {
                    ResetEatPostConsumeCarry();
                    ChangeState(CreatureState.HUNT_INSTRUMENT);
                    return;
                }

                desiredMovePoint = transform.position + eatPostConsumeCarryDirection * 100f;
                desiredSpeed = eatPostConsumeCarrySpeed;
                return;
            }

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
                BeginEatPostConsumeCarry();
                return;
            }

            if (stateTime >= eatDuration)
            {
                if (TryConsumeCurrentTargetByDistanceFallback())
                {
                    BeginEatPostConsumeCarry();
                    return;
                }
                ChangeState(CreatureState.HUNT_INSTRUMENT);
            }
        }

        private void BeginEatPostConsumeCarry()
        {
            if (debugLogs)
                Debug.Log($"Target {currentTarget?.name} consumed by creature at time {Time.time:F2}, starting post-eat carry with speed {currentSpeed:F1}");
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
            eatPostConsumeCarrySpeed = Mathf.Max(0f, currentSpeed);
            if (eatPostConsumeCarrySpeed < 0.01f)
                eatPostConsumeCarrySpeed = Mathf.Max(0f, desiredSpeed);

            currentSpeed = eatPostConsumeCarrySpeed;
            eatPostConsumeCarryActive = true;
            eatPostConsumeCarryWasAirborne = characterController != null && !characterController.isGrounded;
        }

        private bool HasEatPostConsumeCarryLanded()
        {
            if (!eatPostConsumeCarryActive)
                return false;
            if (characterController == null || !characterController.enabled)
                return true;

            if (!eatPostConsumeCarryWasAirborne && !characterController.isGrounded)
                eatPostConsumeCarryWasAirborne = true;

            return eatPostConsumeCarryWasAirborne && characterController.isGrounded && verticalVelocity <= 0f;
        }

        private void ResetEatPostConsumeCarry()
        {
            eatPostConsumeCarryActive = false;
            eatPostConsumeCarryWasAirborne = false;
            eatPostConsumeCarryDirection = Vector3.zero;
            eatPostConsumeCarrySpeed = 0f;
        }

        private void StartHuntPostEatRecovery(Vector3 carryDirection, float carrySpeed)
        {
            float duration = Mathf.Max(0f, huntPostEatRecoveryDuration);
            if (duration <= 0f)
            {
                ResetHuntPostEatRecovery();
                return;
            }

            carryDirection.y = 0f;
            if (carryDirection.sqrMagnitude < 0.0001f)
            {
                carryDirection = transform.forward;
                carryDirection.y = 0f;
            }
            if (carryDirection.sqrMagnitude < 0.0001f)
                carryDirection = GetPlayerForward();

            huntPostEatRecoveryDirection = carryDirection.normalized;
            huntPostEatRecoveryStartSpeed = Mathf.Max(0f, carrySpeed);
            if (huntPostEatRecoveryStartSpeed < 0.01f)
                huntPostEatRecoveryStartSpeed = Mathf.Max(0f, currentSpeed);
            huntPostEatRecoveryElapsed = 0f;
            huntPostEatRecoveryActive = true;
        }

        private void ResetHuntPostEatRecovery()
        {
            huntPostEatRecoveryActive = false;
            huntPostEatRecoveryDirection = Vector3.zero;
            huntPostEatRecoveryStartSpeed = 0f;
            huntPostEatRecoveryElapsed = 0f;
        }

        private void ApplyDistanceLeashOverride()
        {
            if (!enableDistanceLeash || playerController == null)
            {
                leashActive = false;
                return;
            }

            float enterDistance = Mathf.Max(1f, leashEnterDistance);
            float exitDistance = Mathf.Clamp(leashExitDistance, 0f, enterDistance);
            float distanceToPlayer = GetDistanceToPlayer();

            if (!leashActive)
                leashActive = distanceToPlayer > enterDistance;
            else if (distanceToPlayer <= exitDistance)
                leashActive = false;

            if (!leashActive)
                return;

            if (state != CreatureState.FOLLOW)
                ChangeState(CreatureState.FOLLOW);

            currentTarget = null;

            Vector3 playerPos = playerController.transform.position;
            desiredMovePoint = playerPos;
            desiredMovePoint.y = transform.position.y;

            float leashSpeedLimit = Mathf.Max(maxSpeedFollow, leashMaxSpeed);
            float catchupTarget = GetPlayerSpeed() + Mathf.Max(0f, leashCatchupSpeedBoost);
            desiredSpeed = Mathf.Clamp(Mathf.Max(desiredSpeed, catchupTarget), 0f, leashSpeedLimit);
        }

        private void SetFollowMotion(float desiredOffset, float speedLimit, float additiveBoost = 0f)
        {
            desiredMovePoint = GetPointRelativeToPlayer(desiredOffset, 0f);
            desiredSpeed = ComputeGapSpeed(desiredOffset, speedLimit, additiveBoost);
        }

        private float ComputeHuntSpeedWithLeadLimit(float baselineSpeed)
        {
            float desiredLead = Mathf.Max(0f, huntMaxLeadDistance);
            float gapLimitedSpeed = ComputeGapSpeed(desiredLead, maxSpeedHunt);
            return Mathf.Min(baselineSpeed, gapLimitedSpeed);
        }

        private void UpdateHuntRecenterMode(float headingAngleDeg)
        {
            float enterThreshold = Mathf.Max(0f, huntRecenterHeadingAngleThreshold);
            float exitThreshold = enterThreshold * 0.5f;

            if (!huntRecenterActive)
            {
                huntRecenterActive = headingAngleDeg > enterThreshold;
            }
            else if (headingAngleDeg <= exitThreshold)
            {
                huntRecenterActive = false;
            }
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

            float smoothing = Mathf.Max(0f, huntRecenterPointSmoothing);
            if (smoothing <= 0f)
            {
                huntRecenterPoint = rawPoint;
                return huntRecenterPoint;
            }

            float t = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
            huntRecenterPoint = Vector3.Lerp(huntRecenterPoint, rawPoint, t);
            return huntRecenterPoint;
        }

        private void UpdateHuntPerceivedPlayerForward(float dt)
        {
            if (playerController == null)
                return;

            Vector3 actualForward = GetPlayerForward();
            if (!huntPerceivedPlayerForwardInitialized || huntPerceivedPlayerForward.sqrMagnitude < 0.0001f)
            {
                InitializeHuntPerceivedPlayerForward();
                return;
            }

            float reactionTime = Mathf.Max(0.01f, huntPlayerHeadingReactionTime);
            float blend = 1f - Mathf.Exp(-Mathf.Max(0f, dt) / reactionTime);
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
            Vector3 initialForward = transform.forward;
            initialForward.y = 0f;
            if (initialForward.sqrMagnitude < 0.0001f)
                initialForward = GetPlayerForward();
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
            point.y = transform.position.y;
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
            if (TryHandleNoGroundFailsafe(dt))
                return;

            Vector3 toTarget = desiredMovePoint - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            Vector3 moveDir = distance > 0.001f ? toTarget / distance : transform.forward;
            bool useHuntPostEatInertia = state == CreatureState.HUNT_INSTRUMENT && huntPostEatRecoveryActive;
            if (useHuntPostEatInertia)
            {
                float duration = Mathf.Max(0.01f, huntPostEatRecoveryDuration);
                huntPostEatRecoveryElapsed += Mathf.Max(0f, dt);
                float t = Mathf.Clamp01(huntPostEatRecoveryElapsed / duration);

                Vector3 inertiaDir = huntPostEatRecoveryDirection;
                inertiaDir.y = 0f;
                if (inertiaDir.sqrMagnitude < 0.0001f)
                    inertiaDir = transform.forward;
                if (inertiaDir.sqrMagnitude < 0.0001f)
                    inertiaDir = moveDir;
                inertiaDir.Normalize();

                float steerBlend = t * t;
                moveDir = Vector3.Slerp(inertiaDir, moveDir, steerBlend);
                if (moveDir.sqrMagnitude < 0.0001f)
                    moveDir = inertiaDir;
                else
                    moveDir.Normalize();

                float retainedSpeed = Mathf.Lerp(huntPostEatRecoveryStartSpeed, 0f, t);
                desiredSpeed = Mathf.Clamp(Mathf.Max(desiredSpeed, retainedSpeed), 0f, maxSpeedHunt);

                if (t >= 1f)
                {
                    ResetHuntPostEatRecovery();
                    useHuntPostEatInertia = false;
                }
            }

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
            if (!useHuntPostEatInertia && distance < maxHorizontalDistance && maxHorizontalDistance > 0.0001f)
                horizontalVelocity = moveDir * (distance / dt);

            Vector3 velocity = horizontalVelocity;
            velocity.y = verticalVelocity;
            characterController.Move(velocity * dt);

            Vector3 lookDir = horizontalVelocity;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude < 0.0001f)
            {
                lookDir = moveDir;
                lookDir.y = 0f;
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
                    ? Mathf.Clamp01(airborneGroundAlignWeight)
                    : 1f;
                targetUp = Vector3.Slerp(Vector3.up, sampledGroundNormal, alignWeight);
            }

            if (!groundUpInitialized)
            {
                smoothedGroundUp = targetUp;
                groundUpInitialized = true;
                return smoothedGroundUp;
            }

            float smoothing = Mathf.Max(0f, groundAlignSmoothing);
            if (smoothing <= 0f)
            {
                smoothedGroundUp = targetUp;
                return smoothedGroundUp;
            }

            float t = 1f - Mathf.Exp(-smoothing * Mathf.Max(0f, dt));
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

            float startHeight = Mathf.Max(0.1f, groundProbeStartHeight);
            float probeDistance = Mathf.Max(0.2f, groundProbeDistance);
            float sphereRadius = 0.2f;
            if (characterController != null)
                sphereRadius = Mathf.Max(0.1f, characterController.radius * 0.8f);

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

            float clampedMaxTilt = Mathf.Clamp(maxGroundTiltAngle, 0f, 89.9f);
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

            noGroundTimer += Mathf.Max(0f, dt);
            if (noGroundTimer < Mathf.Max(0.05f, noGroundMaxDuration))
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

            float startHeight = Mathf.Max(0.1f, noGroundProbeStartHeight);
            float probeDistance = Mathf.Max(0.5f, noGroundProbeDistance);
            float sphereRadius = 0.2f;
            if (characterController != null)
                sphereRadius = Mathf.Max(0.1f, characterController.radius * 0.8f);

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
            float behindDistance = Mathf.Max(0f, noGroundRecoveryBehindPlayerDistance);
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
            nextNoGroundRecoveryAllowedTime = Time.time + Mathf.Max(0f, noGroundRecoveryCooldown);

            ResetEatPostConsumeCarry();
            ResetHuntPostEatRecovery();
            huntRecenterActive = false;
            huntRecenterPointInitialized = false;
            ResetHuntPerceivedPlayerForward();
            ResetGroundAlignment();
            ChangeState(CreatureState.FOLLOW, force: true);

            if (debugLogs)
                Debug.LogWarning($"Creature recovered near player ({reason}) at {recoverPos}");
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
            Vector3 playerForward = state == CreatureState.HUNT_INSTRUMENT ? GetHuntReferenceForward() : GetPlayerForward();

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
            if (state != CreatureState.EAT || eatTriggered)
                return;
            if (!IsCurrentTargetCollider(other))
                return;

            ConsumeCurrentTarget();
            eatTriggered = true;
            BeginEatPostConsumeCarry();
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.collider == null)
                return;

            if (state == CreatureState.EAT && !eatTriggered && IsCurrentTargetCollider(hit.collider))
            {
                ConsumeCurrentTarget();
                eatTriggered = true;
                BeginEatPostConsumeCarry();
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

            float configuredRadius = Mathf.Max(0.05f, characterControllerRadius);
            float configuredHeight = Mathf.Max(configuredRadius * 2f + 0.01f, characterControllerHeight);
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
            Gizmos.DrawWireCube(desiredMovePoint, Vector3.one * 0.3f);

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
