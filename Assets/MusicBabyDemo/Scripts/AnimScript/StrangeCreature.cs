using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class StrangeCreature : ProceduralCreatureVisualBase
{
    [Header("STRUCTURE / Global")]
    [Range(0.25f, 5f)] public float overallScale = 1f;

    [Header("STRUCTURE / Body")]
    [Range(0.1f, 3f)] public float bodyWidth = 1f;
    [Range(0.1f, 3f)] public float bodyHeight = 1.3f;
    [Range(0.1f, 3f)] public float bodyLength = 1f;
    [Tooltip("Rear part width (left/right rear body lobes).")]
    [Range(0.1f, 3f)] public float rearBodyWidth = 1f;
    [Tooltip("Rear part height (left/right rear body lobes).")]
    [Range(0.1f, 3f)] public float rearBodyHeight = 1f;
    [Tooltip("Rear part length (left/right rear body lobes).")]
    [Range(0.1f, 3f)] public float rearBodyLength = 1f;
    [Tooltip("Rear anchors side separation ratio from body half-width.")]
    [Range(0f, 1.5f)] public float rearBodySeparationRatio = 0.34f;
    [Tooltip("Rear anchors height ratio relative to body half-height. 0 = body center, 1 = top, -1 = bottom.")]
    [Range(-1.5f, 1.5f)] public float rearBodyAnchorHeightRatio = -0.05f;
    [Tooltip("Rear anchors length ratio from body center to back boundary. 0 = center, 1 = back surface.")]
    [Range(0f, 1.2f)] public float rearBodyAnchorLengthRatio = 0.72f;

    [Header("STRUCTURE / Head")]
    [Range(0.1f, 3f)] public float headWidth = 1.2f;
    [Range(0.1f, 3f)] public float headHeight = 1.2f;
    [Range(0.1f, 3f)] public float headLength = 1.2f;
    [Tooltip("Head anchor height relative to body half-height above body center. 0 = centered on body Y, 1 = top of body.")]
    [Range(-0.5f, 1.5f)] public float headAnchorHeightRatio = 0.072f;
    [Tooltip("Extra forward offset from the body front boundary.")]
    [Range(-1f, 2f)] public float headAnchorForwardOffset = 0.30f;

    [Header("STRUCTURE / Ears")]
    [Range(0.1f, 1f)] public float earWidth = 1.3f;
    [Range(0.1f, 1f)] public float earHeight = 1f;
    [Range(0.1f, 1f)] public float earLength = 1f;
    [Tooltip("Ear anchor height relative to head half-height. 0 = centered on head Y, 1 = top of head.")]
    [Range(-0.5f, 1.5f)] public float earAnchorHeightRatio = 0.52f;
    [Tooltip("Extra forward offset for both ear anchors in head local space.")]
    [Range(-1f, 1f)] public float earAnchorForwardOffset = -0.08f;
    [Tooltip("Ear spacing ratio from head half-width. Higher value increases distance between ears.")]
    [Range(0f, 2f)] public float earSeparationRatio = 0.47f;


    [Header("STRUCTURE / Eyes")]
    [Range(0.1f, 1f)] public float eyeWidth = 1f;
    [Range(0.1f, 1f)] public float eyeHeight = 1f;
    [Range(0.1f, 1f)] public float eyeLength = 1f;
    [Tooltip("Eye spacing ratio from head half-width.")]
    [Range(0f, 2f)] public float eyeSeparationRatio = 0.483f;
    [Tooltip("Eye anchor height ratio relative to head half-height. 0 = head center, 1 = top of head.")]
    [Range(-1f, 1.5f)] public float eyeAnchorHeightRatio = 0.348f;
    [Tooltip("Extra forward offset from the head front boundary.")]
    [Range(-1f, 1f)] public float eyeAnchorForwardOffset = -0.10f;
    [Tooltip("Pupil size ratio relative to eye size (0.5 = half-eye diameter).")]
    [Range(0.05f, 1.0f)] public float pupilScale = 0.48f;
    [Tooltip("Pupil center forward offset in eye local space (0=center, 0.5=eye surface).")]
    [Range(0f, 0.9f)] public float pupilForwardOffset = 0.27f;


    [Header("STRUCTURE / Mouth")]
    [Tooltip("Lower jaw width (1 = isotropic scale on X).")]
    [Range(0.1f, 1f)] public float jawWidth = 1.65f;
    [Tooltip("Lower jaw height (1 = isotropic scale on Y).")]
    [Range(0.1f, 1f)] public float jawHeight = 0.32f;
    [Tooltip("Lower jaw length (1 = isotropic scale on Z).")]
    [Range(0.1f, 3f)] public float jawLength = 0.92f;
    [Range(0.2f, 5f)] public float jawWidthUpperToLowerRatio = 1.03f;
    [Range(0.2f, 5f)] public float jawHeightUpperToLowerRatio = 1.5f;
    [Range(0.2f, 5f)] public float jawLengthUpperToLowerRatio = 1.087f;
    [Tooltip("Vertical separation between upper and lower jaws at rest.")]
    [Range(-2f, 2f)] public float jawVerticalSeparation = 0f;
    [Tooltip("Mouth anchor height ratio relative to head half-height.")]
    [Range(-1.5f, 1.5f)] public float jawAnchorHeightRatio = -0.45f;
    [Tooltip("Extra forward offset from the head front boundary.")]
    [Range(-1f, 1f)] public float jawAnchorForwardOffset = -0.20f;
    [Range(-45f, 45f)] public float jawBasePitch = 0f;

    [Header("STRUCTURE / Legs")]
    [Range(0, 6)] public int legPairCount = 2;
    [Tooltip("Leg anchors side offset from body center. Left uses -X, right uses +X.")]
    [Range(0f, 3f)] public float legAnchorSideOffset = 0.76f;
    [Tooltip("Leg anchor height relative to body half-height. 0 = body center, -1 = under body.")]
    [Range(-2f, 2f)] public float legAnchorHeightRatio = -0.17f;
    [Tooltip("Useful body-length ratio used to spread leg pairs (1 = full usable length, 0 = centered/grouped).")]
    [Range(0f, 1f)] public float legAnchorLengthRatio = 0.85f;
    [Range(0.1f, 2f)] public float upperLegThickness = 0.60f;
    [Range(0.2f, 3f)] public float upperLegLength = 1.30f;
    [Range(0.1f, 2f)] public float ankleDiameter = 0.50f;
    [Range(0.1f, 2f)] public float footHeight = 0.38f;
    [Range(0.1f, 3f)] public float footWidth = 0.84f;

    [Header("STRUCTURE / Tail")]
    [Range(0.01f, 0.5f)] public float tailWidth = 0.12f;
    [Range(0.01f, 0.5f)] public float tailHeight = 0.12f;
    [Range(0.01f, 3f)] public float tailLength = 0.44f;
    [Tooltip("Tail anchor side offset in body local space.")]
    [Range(-2f, 2f)] public float tailAnchorSideOffset = 0f;
    [Tooltip("Tail anchor height relative to body half-height. 0 = centered on body Y.")]
    [Range(-1.5f, 1.5f)] public float tailAnchorHeightRatio = 0.051f;
    [Tooltip("Extra forward/backward offset from body back boundary (negative moves backward).")]
    [Range(-2f, 2f)] public float tailAnchorForwardOffset = -0.15f;
    [Range(0f, 180f)] public float tailPitchDegrees = 90f;


    [Header("STRUCTURE / Collision")]
    [Tooltip("Remove physical colliders from generated visual parts. CharacterController remains the unique movement collider.")]
    public bool removeGeneratedPartColliders = true;
    [Tooltip("Ensure one trigger SphereCollider on Hippo root, auto-sized from current structure.")]
    public bool autoStructureTriggerCollider = true;
    [Tooltip("Optional local offset applied to the auto trigger center.")]
    public Vector3 triggerCenterOffset = Vector3.zero;
    [Tooltip("Multiplier applied to computed trigger radius.")]
    [Range(0.2f, 3f)] public float triggerRadiusScale = 1f;
    [Tooltip("Extra radius padding added after scaling.")]
    [Range(0f, 2f)] public float triggerRadiusPadding = 0.05f;

    [Header("STRUCTURE / Attachment")]
    [Tooltip("Keep visual anchored on parent local X/Z. Prevents apparent floating/sinking when controller tilts on slopes.")]
    public bool lockLocalXZToParent = true;

    [Header("Materials")]
    public Material bodyMaterial;
    public Material earMaterial;
    public Material scleraMaterial;
    public Material pupilMaterial;
    public Material mouthMaterial;
    public Material tailMaterial;

    [Header("Fallback Colors")]
    public Color fallbackBodyColor = new Color(0.56f, 0.58f, 0.63f);
    public Color fallbackEarColor = new Color(0.24f, 0.26f, 0.30f);
    public Color fallbackTailColor = new Color(0.24f, 0.26f, 0.30f);
    public Color fallbackScleraColor = new Color(0.92f, 0.94f, 0.97f);
    public Color fallbackPupilColor = new Color(0.08f, 0.08f, 0.08f);
    public Color fallbackMouthColor = new Color(0.55f, 0.22f, 0.24f);

    [Header("COUPLING / Movement-Driven Gait")]
    [Tooltip("When enabled, gait phase advances from real horizontal displacement instead of Time.time.")]
    public bool driveChasePhaseFromDisplacement = true;
    [Tooltip("Ignore tiny displacements to avoid micro-jitter in gait progression.")]
    [Range(0f, 1f)] public float chaseMinDisplacement = 0.0005f;
    [Tooltip("Ignore a single-frame displacement above this threshold (teleport/recovery safety).")]
    [Range(0.001f, 5f)] public float chaseMaxDisplacementPerFrame = 1.25f;

    [Header("ANIMATION / Idle")]
    public float idleBreathSpeed = 1.6f;
    public float idleBreathAmount = 0.03f;
    public float idleHeadNodAngle = 1.5f;
    public float idleHeadNodSpeed = 1.3f;
    public float idleTailSwingFactor = 0.25f;

    [Header("ANIMATION / Chase")]
    public bool animateWalk = true;
    [Range(0f, 20f)] public float walkCycleSpeed = 4.2f;
    [Range(0.01f, 85f)] public float legAngle = 16f;
    [Range(0f, 1f)] public float bodyBobAmount = 0.05f;
    [Range(0f, 45f)] public float headSwingAngle = 3f;
    [Range(0f, 60f)] public float tailSwingAngle = 14f;
    [Range(0f, 90f)] public float chaseMouthOpenAngle = 35f;

    [Header("ANIMATION / Wait Player")]
    [Range(0f, 1f)] public float waitBodyBobAmount = 0.035f;
    [Range(0f, 20f)] public float waitBodyBobSpeed = 2.4f;
    [Range(0f, 45f)] public float waitHeadYawAngle = 7f;
    [Range(0f, 20f)] public float waitHeadYawSpeed = 1.8f;
    [Range(0f, 60f)] public float waitLegStompAngle = 6f;
    [Range(0f, 20f)] public float waitLegStompSpeed = 5f;
    [Range(0f, 1f)] public float waitLegStompJitter = 0.35f;
    [Range(0f, 60f)] public float waitTailSwingAngle = 11f;
    [Range(0f, 60f)] public float waitMouthOpenBase = 8f;
    [Range(0f, 90f)] public float waitMouthTalkAngle = 30f;
    [Range(0f, 40f)] public float waitMouthTalkSpeed = 11f;
    [Range(0f, 75f)] public float waitEyeLookSideAngle = 35f;
    [Range(0f, 20f)] public float waitEyeLookSpeed = 2.2f;
    [Tooltip("Horizontal pupil travel in eye local space when looking left/right.")]
    [Range(0f, 0.23f)] public float waitPupilLookSideOffset = 0.08f;
    [Range(0f, 40f)] public float waitEyeDartSpeed = 14f;
    [Range(0f, 1f)] public float waitEyeDartSnap = 0.75f;
    [Range(0.5f, 2f)] public float waitEyeDartSideMultiplier = 1.15f;

    [Header("ANIMATION / Eat Attack")]
    [Range(0.01f, 5f)] public float eatAnimDuration = 0.75f;
    [Range(0f, 5f)] public float eatJumpHeight = 0.45f;
    [Range(0f, 2f)] public float eatForwardStretch = 0.18f;
    [Range(0f, 90f)] public float eatLegFoldAngle = 50f;
    [Range(0f, 90f)] public float eatAttackMouthOpenAngle = 35f;
    [Tooltip("Global hippo pitch during EAT_ATTACK (positive tilts up).")]
    [Range(-90f, 90f)] public float eatAttackGlobalPitch = 20f;

    [Header("ANIMATION / Eat Recovery")]
    [Range(0f, 90f)] public float eatRecoveryMouthOpenAngle = 35f;
    [Tooltip("Global hippo pitch during EAT_RECOVERY (negative tilts down).")]
    [Range(-90f, 90f)] public float eatRecoveryGlobalPitch = -20f;
    [Tooltip("Blend duration to transition global pitch from EAT_ATTACK to EAT_RECOVERY.")]
    [Range(0f, 2f)] public float eatRecoveryPitchBlendDuration = 0.25f;
    [Tooltip("Leg pitch used in landing/recovery pose (0 means vertical legs).")]
    [Range(0f, 90f)] public float eatRecoveryLegForwardAngle = 45f;

    [Header("ANIMATION / Stunned")]
    [Range(0f, 90f)] public float stunnedShakeAngle = 10f;
    [Range(0f, 60f)] public float stunnedShakeSpeed = 20f;
    [Range(0f, 90f)] public float stunnedMouthOpenAngle = 35f;

    [Header("Debug")]
    public bool drawDebug = false;

    private const string RootName = "__HippoVisualRoot";
    private const float UpperLegCenterYRatio = -0.24f / 1.30f;
    private const float AnkleCenterYRatio = -0.88f / 1.30f;
    private const float FootCenterYRatio = -1.16f / 1.30f;
    private const float AnkleForwardFromFootWidthRatio = 0.08f / 0.84f;
    private const float FootForwardFromFootWidthRatio = 0.18f / 0.84f;
    private const float FootDepthFromFootWidthRatio = 0.98f / 0.84f;
    private const float FusedBodyFieldSharpness = 2.75f;
    private const string FusedBodyMeshNamePrefix = "StrangeCreatureBodyCluster";
    private static readonly Vector3 DefaultBodyLocalPos = new Vector3(0f, 0.95f, 0f);

    private struct LegRig
    {
        public Transform anchor;
        public Transform root;
        public int pairIndex;
        public int sideSign;
    }

    private Transform root;
    private Transform visualRoot;
    private Transform bodyPivot;
    private Transform body;
    private Transform rearBodyAnchorL;
    private Transform rearBodyAnchorR;
    private Transform rearBodyL;
    private Transform rearBodyR;
    private Transform headAnchor;
    private Transform headPivot;
    private Transform head;
    private Transform mouthAnchor;
    private Transform upperJaw;
    private Transform jawPivot;
    private Transform lowerJaw;
    private Transform earAnchorL;
    private Transform earAnchorR;
    private Transform earL;
    private Transform earR;
    private Transform tailAnchor;
    private Transform tail;
    private Transform eyeAnchorL;
    private Transform eyeAnchorR;
    private readonly List<LegRig> legRigs = new List<LegRig>();
    private Transform eyeL;
    private Transform eyeR;
    private Transform pupilL;
    private Transform pupilR;

    private Vector3 bodyBaseLocalPos;
    private Vector3 bodyBaseLocalScale;
    private Vector3 rearBodyAnchorLBaseLocalPos;
    private Vector3 rearBodyAnchorRBaseLocalPos;
    private Quaternion headBaseLocalRot;
    private Quaternion tailBaseLocalRot;
    private Quaternion jawBaseLocalRot;
    private Quaternion visualBaseLocalRot;
    private Vector3 eyeBaseScale;
    private Vector3 eyeLBaseLocalPos;
    private Vector3 eyeRBaseLocalPos;
    private Vector3 pupilBaseScale;
    private Vector3 pupilBaseLocalPos;
    private Vector3 fusedBodyCachedBodyScale;
    private Vector3 fusedBodyCachedRearScale;
    private Vector3 fusedBodyCachedRearCenterL;
    private Vector3 fusedBodyCachedRearCenterR;
    private CreatureSphereDetailLevel fusedBodyCachedDetailLevel;
    private bool fusedBodyMeshCacheValid;

    private Material fallbackBodyMat;
    private Material fallbackEarMat;
    private Material fallbackTailMat;
    private Material fallbackScleraMat;
    private Material fallbackPupilMat;
    private Material fallbackMouthMat;
    private SphereCollider structureTriggerCollider;
    private bool generatedPartCollidersPurged;
    private bool rootColliderCleanupDone;
    private float chaseGaitPhase;
    private Vector3 chaseLastWorldPosition;
    private bool chaseHasLastWorldPosition;
    private float chaseLegLengthEstimate = 0.8f;
    private float chaseCycleDistance = 1f;
    private float chaseCachedLegAngle = float.NaN;

    [ContextMenu("Rebuild Visual")]
    public void Rebuild()
    {
        RebuildVisual();
    }

    protected override void OnStateChanged(CreatureVisualState oldState, CreatureVisualState newState)
    {
        base.OnStateChanged(oldState, newState);

        if (IsChaseLikeState(newState) || IsChaseLikeState(oldState))
            ResetChaseDisplacementSampling();
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        if (legPairCount < 0)
            legPairCount = 0;
        else if (legPairCount > 6)
            legPairCount = 6;

        if (chaseMaxDisplacementPerFrame <= chaseMinDisplacement)
            chaseMaxDisplacementPerFrame = chaseMinDisplacement + 0.001f;
    }

    // Central lifecycle method:
    // - fast path: reuse existing rig and apply live structure/eye updates
    // - forced path: rebuild the full procedural rig
    protected override void BuildIfNeeded(bool force)
    {
        ApplyAttachmentConstraint();
        Transform existing = transform.Find(RootName);

        if (!force)
        {
            if (existing != null)
            {
                if (root == null)
                {
                    root = existing;
                    RecoverReferences();
                    CacheBases();
                }

                if (eyeL == null || eyeR == null || pupilL == null || pupilR == null)
                {
                    BuildIfNeeded(true);
                    return;
                }

                if (!HasHybridBodyHeadAnchors())
                {
                    BuildIfNeeded(true);
                    return;
                }

                if (!HasUpdatedLegGeometry())
                {
                    BuildIfNeeded(true);
                    return;
                }

                if (materialsDirty)
                {
                    ApplyCurrentMaterialsToRig();
                    materialsDirty = false;
                }

                ApplyStructurePlacement();
                ApplyEyePlacement();
                ConfigureCollisionSetup();
                return;
            }

            if (root != null)
            {
                if (materialsDirty)
                {
                    ApplyCurrentMaterialsToRig();
                    materialsDirty = false;
                }

                ApplyStructurePlacement();
                ApplyEyePlacement();
                ConfigureCollisionSetup();
                return;
            }
        }

        ClearExisting();
        BuildVisual();
        RecoverReferences();
        ApplyStructurePlacement();
        ApplyEyePlacement();
        ConfigureCollisionSetup();
        CacheBases();
        ApplyCurrentMaterialsToRig();
        materialsDirty = false;
    }

    private void ApplyAttachmentConstraint()
    {
        if (!lockLocalXZToParent || transform.parent == null)
            return;

        Vector3 localPos = transform.localPosition;
        if (Mathf.Abs(localPos.x) <= 0.0001f && Mathf.Abs(localPos.z) <= 0.0001f)
            return;

        transform.localPosition = new Vector3(0f, localPos.y, 0f);
    }

    private bool HasHybridBodyHeadAnchors()
    {
        return bodyPivot != null &&
               body != null &&
               HasFusedBodyMesh(body) &&
               rearBodyAnchorL != null &&
               rearBodyAnchorR != null &&
               headAnchor != null &&
               tailAnchor != null &&
               eyeAnchorL != null &&
               eyeAnchorR != null &&
               headPivot != null &&
               headPivot.parent == headAnchor &&
               mouthAnchor != null &&
               tail != null &&
               tail.parent == tailAnchor &&
               upperJaw != null &&
               upperJaw.parent == mouthAnchor &&
               jawPivot != null &&
               jawPivot.parent == mouthAnchor &&
               lowerJaw != null &&
               lowerJaw.parent == jawPivot &&
               eyeL != null &&
               eyeR != null &&
               eyeL.parent == eyeAnchorL &&
               eyeR.parent == eyeAnchorR;
    }

    private static bool HasFusedBodyMesh(Transform candidate)
    {
        if (candidate == null)
            return false;

        MeshFilter meshFilter = candidate.GetComponent<MeshFilter>();
        Mesh mesh = meshFilter != null ? meshFilter.sharedMesh : null;
        return mesh != null && mesh.name.StartsWith(FusedBodyMeshNamePrefix);
    }

    private static Vector3 ResolveBodyLocalPosition()
    {
        return DefaultBodyLocalPos;
    }

    private Vector3 ResolveBodyLocalScale()
    {
        // Dimensions are absolute per-axis scale values.
        // With Width/Height/Length = 1,1,1 this part is isotropic (sphere/cube depending on detail mode).
        return new Vector3(
            bodyWidth,
            bodyHeight,
            bodyLength);
    }

    private Vector3 ResolveRearBodyLocalScale(Vector3 bodyLocalScale)
    {
        // Rear body dimensions are absolute too (same isotropic behavior as other Width/Height/Length groups).
        return new Vector3(
            rearBodyWidth,
            rearBodyHeight,
            rearBodyLength);
    }

    private void ResolveRearBodyAnchorLocalPositions(Vector3 bodyLocalPos, Vector3 bodyLocalScale, out Vector3 leftLocalPos, out Vector3 rightLocalPos)
    {
        float bodyHalfX = bodyLocalScale.x * 0.5f;
        float bodyHalfY = bodyLocalScale.y * 0.5f;
        float bodyHalfZ = bodyLocalScale.z * 0.5f;

        float side = bodyHalfX * rearBodySeparationRatio;
        float y = bodyLocalPos.y + (bodyHalfY * rearBodyAnchorHeightRatio);
        float z = bodyLocalPos.z - (bodyHalfZ * rearBodyAnchorLengthRatio);

        leftLocalPos = new Vector3(-side, y, z);
        rightLocalPos = new Vector3(side, y, z);
    }

    private Vector3 ResolveHeadLocalScale()
    {
        return new Vector3(
            headWidth,
            headHeight,
            headLength);
    }

    private Vector3 ResolveEarLocalScale()
    {
        return new Vector3(earWidth, earHeight, earLength);
    }

    private void ResolveEarAnchorLocalPositions(Vector3 headLocalScale, out Vector3 leftLocalPos, out Vector3 rightLocalPos)
    {
        float headHalfX = headLocalScale.x * 0.5f;
        float headHalfY = headLocalScale.y * 0.5f;
        float side = headHalfX * earSeparationRatio;
        float y = headHalfY * earAnchorHeightRatio;
        float z = earAnchorForwardOffset;

        leftLocalPos = new Vector3(-side, y, z);
        rightLocalPos = new Vector3(side, y, z);
    }

    private Vector3 ResolveEyeLocalScale()
    {
        return new Vector3(eyeWidth, eyeHeight, eyeLength);
    }

    private void ResolveEyeAnchorLocalPositions(Vector3 headLocalScale, out Vector3 leftLocalPos, out Vector3 rightLocalPos)
    {
        float headHalfX = headLocalScale.x * 0.5f;
        float headHalfY = headLocalScale.y * 0.5f;
        float headHalfZ = headLocalScale.z * 0.5f;
        float side = headHalfX * eyeSeparationRatio;
        float y = headHalfY * eyeAnchorHeightRatio;
        float z = headHalfZ + eyeAnchorForwardOffset;

        leftLocalPos = new Vector3(-side, y, z);
        rightLocalPos = new Vector3(side, y, z);
    }

    private Vector3 ResolveHeadAnchorLocalPosition(Vector3 bodyLocalPos, Vector3 bodyLocalScale)
    {
        float bodyHalfY = bodyLocalScale.y * 0.5f;
        float bodyHalfZ = bodyLocalScale.z * 0.5f;

        return new Vector3(
            0f,
            bodyLocalPos.y + (bodyHalfY * headAnchorHeightRatio),
            bodyLocalPos.z + bodyHalfZ + headAnchorForwardOffset);
    }

    private Vector3 ResolveMouthAnchorLocalPosition(Vector3 headLocalScale)
    {
        float headHalfY = headLocalScale.y * 0.5f;
        float headHalfZ = headLocalScale.z * 0.5f;

        return new Vector3(
            0f,
            headHalfY * jawAnchorHeightRatio,
            headHalfZ + jawAnchorForwardOffset);
    }

    private Vector3 ResolveTailAnchorLocalPosition(Vector3 bodyLocalPos, Vector3 bodyLocalScale)
    {
        float bodyHalfY = bodyLocalScale.y * 0.5f;
        float bodyHalfZ = bodyLocalScale.z * 0.5f;

        return new Vector3(
            bodyLocalPos.x + tailAnchorSideOffset,
            bodyLocalPos.y + (bodyHalfY * tailAnchorHeightRatio),
            bodyLocalPos.z - bodyHalfZ + tailAnchorForwardOffset);
    }

    private bool HasUpdatedLegGeometry()
    {
        int expectedLegCount = ResolveLegPairCount() * 2;
        if (legRigs.Count != expectedLegCount)
            return false;

        for (int i = 0; i < legRigs.Count; i++)
        {
            LegRig leg = legRigs[i];
            if (leg.anchor == null || leg.root == null)
                return false;
            if (leg.root.parent != leg.anchor)
                return false;
            if (!HasLegParts(leg.root))
                return false;
        }

        return true;
    }

    private int ResolveLegPairCount()
    {
        return Mathf.Clamp(legPairCount, 0, 6);
    }

    private float ResolveClampedLegAnchorSideAbs(Vector3 bodyLocalScale)
    {
        float bodyHalfX = bodyLocalScale.x * 0.5f;
        if (bodyHalfX <= 0.0001f)
            return 0f;

        // Keep anchors inside the body footprint on X so the Z distribution remains meaningful.
        float maxInsideSide = bodyHalfX * 0.95f;
        return Mathf.Min(Mathf.Abs(legAnchorSideOffset), maxInsideSide);
    }

    private float ResolveLegPairForward(int pairIndex, Vector3 bodyLocalPos, Vector3 bodyLocalScale)
    {
        int pairCount = ResolveLegPairCount();
        if (pairCount <= 1)
            return bodyLocalPos.z;

        float bodyHalfZ = bodyLocalScale.z * 0.5f;
        float usableHalfZ = bodyHalfZ * legAnchorLengthRatio;

        float t = pairIndex / (float)(pairCount - 1);
        float front = bodyLocalPos.z + usableHalfZ;
        float back = bodyLocalPos.z - usableHalfZ;
        return Mathf.Lerp(front, back, t);
    }

    private Vector3 ResolveLegAnchorLocalPosition(int pairIndex, int sideSign, Vector3 bodyLocalPos, Vector3 bodyLocalScale)
    {
        float bodyHalfY = bodyLocalScale.y * 0.5f;
        float y = bodyLocalPos.y + (bodyHalfY * legAnchorHeightRatio);
        float sideAbs = ResolveClampedLegAnchorSideAbs(bodyLocalScale);
        float x = sideSign < 0 ? -sideAbs : sideAbs;
        float z = ResolveLegPairForward(pairIndex, bodyLocalPos, bodyLocalScale);
        return new Vector3(x, y, z);
    }

    private static bool HasLegParts(Transform legRoot)
    {
        return legRoot != null &&
               legRoot.Find("Upper") != null &&
               legRoot.Find("Ankle") != null &&
               legRoot.Find("Foot") != null;
    }

    private void ApplyCurrentMaterialsToRig()
    {
        if (body != null)
            ApplyMaterial(body.gameObject, CreatureMaterialSlot.Body);

        if (head != null)
            ApplyMaterial(head.gameObject, CreatureMaterialSlot.Body);

        if (tail != null)
            ApplyMaterial(tail.gameObject, CreatureMaterialSlot.Tail);

        if (upperJaw != null)
            ApplyMaterial(upperJaw.gameObject, CreatureMaterialSlot.Mouth);

        if (lowerJaw != null)
            ApplyMaterial(lowerJaw.gameObject, CreatureMaterialSlot.Mouth);

        if (eyeL != null)
            ApplyMaterial(eyeL.gameObject, CreatureMaterialSlot.EyeSclera);

        if (eyeR != null)
            ApplyMaterial(eyeR.gameObject, CreatureMaterialSlot.EyeSclera);

        if (pupilL != null)
            ApplyMaterial(pupilL.gameObject, CreatureMaterialSlot.EyePupil);

        if (pupilR != null)
            ApplyMaterial(pupilR.gameObject, CreatureMaterialSlot.EyePupil);

        if (earL != null)
            ApplyMaterial(earL.gameObject, CreatureMaterialSlot.Ears);

        if (earR != null)
            ApplyMaterial(earR.gameObject, CreatureMaterialSlot.Ears);

        for (int i = 0; i < legRigs.Count; i++)
            ApplyLegMaterials(legRigs[i].root);
    }

    private void ApplyLegMaterials(Transform legRoot)
    {
        if (legRoot == null)
            return;

        Transform upper = legRoot.Find("Upper");
        if (upper != null)
            ApplyMaterial(upper.gameObject, CreatureMaterialSlot.Body);

        Transform ankle = legRoot.Find("Ankle");
        if (ankle != null)
            ApplyMaterial(ankle.gameObject, CreatureMaterialSlot.Body);

        Transform foot = legRoot.Find("Foot");
        if (foot != null)
            ApplyMaterial(foot.gameObject, CreatureMaterialSlot.Body);
    }

    protected override Material ResolveMaterialForSlot(CreatureMaterialSlot slot)
    {
        switch (slot)
        {
            case CreatureMaterialSlot.Body:
                return bodyMaterial != null ? bodyMaterial : GetOrCreateBodyFallbackMaterial();

            case CreatureMaterialSlot.Tail:
                return tailMaterial != null ? tailMaterial : GetOrCreateTailFallbackMaterial();

            case CreatureMaterialSlot.Ears:
                return earMaterial != null ? earMaterial : GetOrCreateEarFallbackMaterial();

            case CreatureMaterialSlot.EyeSclera:
                return scleraMaterial != null ? scleraMaterial : GetOrCreateScleraFallbackMaterial();

            case CreatureMaterialSlot.EyePupil:
                return pupilMaterial != null ? pupilMaterial : GetOrCreatePupilFallbackMaterial();

            case CreatureMaterialSlot.Mouth:
                return mouthMaterial != null ? mouthMaterial : GetOrCreateMouthFallbackMaterial();

            default:
                return bodyMaterial != null ? bodyMaterial : GetOrCreateBodyFallbackMaterial();
        }
    }

    protected override string FallbackMaterialContextName => nameof(StrangeCreature);

    private Material GetOrCreateBodyFallbackMaterial()
    {
        if (fallbackBodyMat == null)
            fallbackBodyMat = CreateFallbackMaterial("CreatureBody_Fallback", fallbackBodyColor);
        return fallbackBodyMat;
    }

    private Material GetOrCreateScleraFallbackMaterial()
    {
        if (fallbackScleraMat == null)
            fallbackScleraMat = CreateFallbackMaterial("CreatureSclera_Fallback", fallbackScleraColor);
        return fallbackScleraMat;
    }

    private Material GetOrCreateTailFallbackMaterial()
    {
        if (fallbackTailMat == null)
            fallbackTailMat = CreateFallbackMaterial("CreatureTail_Fallback", fallbackTailColor);
        return fallbackTailMat;
    }

    private Material GetOrCreateEarFallbackMaterial()
    {
        if (fallbackEarMat == null)
            fallbackEarMat = CreateFallbackMaterial("CreatureEars_Fallback", fallbackEarColor);
        return fallbackEarMat;
    }

    private Material GetOrCreatePupilFallbackMaterial()
    {
        if (fallbackPupilMat == null)
            fallbackPupilMat = CreateFallbackMaterial("CreaturePupil_Fallback", fallbackPupilColor);
        return fallbackPupilMat;
    }

    private Material GetOrCreateMouthFallbackMaterial()
    {
        if (fallbackMouthMat == null)
            fallbackMouthMat = CreateFallbackMaterial("CreatureMouth_Fallback", fallbackMouthColor);
        return fallbackMouthMat;
    }

    // Rebind cached Transform references from an already-built visual hierarchy.
    private void RecoverReferences()
    {
        root = transform.Find(RootName);
        if (root == null)
            return;

        visualRoot = root.Find("VisualRoot");
        if (visualRoot == null)
            return;

        bodyPivot = visualRoot.Find("BodyPivot");
        if (bodyPivot != null)
        {
            body = bodyPivot.Find("Body");
            rearBodyAnchorL = bodyPivot.Find("RearBodyAnchor_L");
            rearBodyAnchorR = bodyPivot.Find("RearBodyAnchor_R");
            headAnchor = bodyPivot.Find("HeadAnchor");
            tailAnchor = bodyPivot.Find("TailAnchor");
            if (rearBodyAnchorL != null)
                rearBodyL = rearBodyAnchorL.Find("RearBody_L");
            if (rearBodyAnchorR != null)
                rearBodyR = rearBodyAnchorR.Find("RearBody_R");
            if (headAnchor != null)
                headPivot = headAnchor.Find("HeadPivot");
            if (tailAnchor != null)
                tail = tailAnchor.Find("Tail");
        }
        else
        {
            // Backward-compat fallback if old hierarchy is still present before forced rebuild.
            body = visualRoot.Find("Body");
            rearBodyAnchorL = null;
            rearBodyAnchorR = null;
            rearBodyL = null;
            rearBodyR = null;
            headAnchor = null;
            tailAnchor = null;
            headPivot = null;
        }

        if (headPivot == null)
            headPivot = visualRoot.Find("HeadPivot");

        if (tail == null)
            tail = visualRoot.Find("Tail");
        RecoverLegReferences();

        if (headPivot != null)
        {
            head = headPivot.Find("Head");
            mouthAnchor = headPivot.Find("MouthAnchor");
            if (mouthAnchor != null)
            {
                upperJaw = mouthAnchor.Find("UpperJaw");
                jawPivot = mouthAnchor.Find("JawPivot");
            }
            else
            {
                upperJaw = headPivot.Find("UpperJaw");
                jawPivot = headPivot.Find("JawPivot");
            }
            earAnchorL = headPivot.Find("EarAnchor_L");
            earAnchorR = headPivot.Find("EarAnchor_R");
            if (earAnchorL != null)
                earL = earAnchorL.Find("Ear_L");
            if (earAnchorR != null)
                earR = earAnchorR.Find("Ear_R");
            if (earL == null)
                earL = headPivot.Find("Ear_L");
            if (earR == null)
                earR = headPivot.Find("Ear_R");
            eyeAnchorL = headPivot.Find("EyeAnchor_L");
            eyeAnchorR = headPivot.Find("EyeAnchor_R");
            if (eyeAnchorL != null)
                eyeL = eyeAnchorL.Find("Eye_L");
            if (eyeAnchorR != null)
                eyeR = eyeAnchorR.Find("Eye_R");
            if (eyeL == null)
                eyeL = headPivot.Find("Eye_L");
            if (eyeR == null)
                eyeR = headPivot.Find("Eye_R");

            if (jawPivot != null)
                lowerJaw = jawPivot.Find("LowerJaw");

            if (eyeL != null)
                pupilL = eyeL.Find("Pupil_L");

            if (eyeR != null)
                pupilR = eyeR.Find("Pupil_R");
        }
    }

    // Cache neutral poses/scales used by animation resets and additive animation.
    private void CacheBases()
    {
        if (body != null)
        {
            bodyBaseLocalPos = body.localPosition;
            bodyBaseLocalScale = body.localScale;
        }

        if (rearBodyAnchorL != null)
            rearBodyAnchorLBaseLocalPos = rearBodyAnchorL.localPosition;

        if (rearBodyAnchorR != null)
            rearBodyAnchorRBaseLocalPos = rearBodyAnchorR.localPosition;

        if (headPivot != null)
            headBaseLocalRot = headPivot.localRotation;

        if (tail != null)
            tailBaseLocalRot = tail.localRotation;

        if (jawPivot != null)
            jawBaseLocalRot = jawPivot.localRotation;

        if (visualRoot != null)
            visualBaseLocalRot = visualRoot.localRotation;

        if (eyeL != null)
        {
            eyeBaseScale = eyeL.localScale;
            eyeLBaseLocalPos = eyeL.localPosition;
        }

        if (eyeR != null)
        {
            if (eyeL == null)
                eyeBaseScale = eyeR.localScale;
            eyeRBaseLocalPos = eyeR.localPosition;
        }

        if (pupilL != null)
        {
            pupilBaseScale = pupilL.localScale;
            pupilBaseLocalPos = pupilL.localPosition;
        }
        else if (pupilR != null)
        {
            pupilBaseScale = pupilR.localScale;
            pupilBaseLocalPos = pupilR.localPosition;
        }

        SetEyeScaleImmediate(eyeBaseScale);
        SetEyeLocalPositionsImmediate(eyeLBaseLocalPos, eyeRBaseLocalPos);
        SetPupilScaleImmediate(pupilBaseScale);
        SetPupilLocalPositionImmediate(pupilBaseLocalPos);
        RecomputeChaseCalibration();
        ResetChaseDisplacementSampling();
    }

    private void ClearExisting()
    {
        Transform old = transform.Find(RootName);
        if (old == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(old.gameObject);
        else
            Destroy(old.gameObject);
#else
        Destroy(old.gameObject);
#endif

        root = null;
        visualRoot = null;
        bodyPivot = null;
        body = null;
        rearBodyAnchorL = null;
        rearBodyAnchorR = null;
        rearBodyL = null;
        rearBodyR = null;
        headAnchor = null;
        headPivot = null;
        head = null;
        mouthAnchor = null;
        upperJaw = null;
        jawPivot = null;
        lowerJaw = null;
        earAnchorL = null;
        earAnchorR = null;
        earL = null;
        earR = null;
        tailAnchor = null;
        tail = null;
        eyeAnchorL = null;
        eyeAnchorR = null;
        legRigs.Clear();
        eyeL = null;
        eyeR = null;
        pupilL = null;
        pupilR = null;
        structureTriggerCollider = null;
        generatedPartCollidersPurged = false;
        rootColliderCleanupDone = false;
        chaseHasLastWorldPosition = false;
        chaseGaitPhase = 0f;
        chaseLegLengthEstimate = 0.8f;
        chaseCycleDistance = 1f;
        chaseCachedLegAngle = float.NaN;
        fusedBodyMeshCacheValid = false;
    }

    // One-shot procedural construction of the full hippo hierarchy.
    // Geometry creation happens here; per-frame/live parameter application is separated.
    private void BuildVisual()
    {
        root = CreateNode(RootName, transform);
        root.localPosition = Vector3.zero;
        root.localRotation = Quaternion.identity;
        root.localScale = Vector3.one * overallScale;

        visualRoot = CreateNode("VisualRoot", root);
        visualRoot.localPosition = Vector3.zero;
        visualRoot.localRotation = Quaternion.identity;
        visualRoot.localScale = Vector3.one;

        Vector3 bodyLocalPos = ResolveBodyLocalPosition();
        Vector3 bodyLocalScale = ResolveBodyLocalScale();
        Vector3 headLocalScale = ResolveHeadLocalScale();

        bodyPivot = CreateNode("BodyPivot", visualRoot);
        bodyPivot.localPosition = Vector3.zero;
        bodyPivot.localRotation = Quaternion.identity;
        bodyPivot.localScale = Vector3.one;

        Vector3 rearBodyScale = ResolveRearBodyLocalScale(bodyLocalScale);
        ResolveRearBodyAnchorLocalPositions(bodyLocalPos, bodyLocalScale, out Vector3 rearBodyAnchorPosL, out Vector3 rearBodyAnchorPosR);

        body = CreateFusedBodyPart(
            "Body",
            bodyPivot,
            bodyLocalPos,
            bodyLocalScale,
            rearBodyScale,
            rearBodyAnchorPosL - bodyLocalPos,
            rearBodyAnchorPosR - bodyLocalPos);

        rearBodyAnchorL = CreateNode("RearBodyAnchor_L", bodyPivot);
        rearBodyAnchorL.localPosition = rearBodyAnchorPosL;
        rearBodyAnchorL.localRotation = Quaternion.identity;
        rearBodyAnchorL.localScale = Vector3.one;

        rearBodyAnchorR = CreateNode("RearBodyAnchor_R", bodyPivot);
        rearBodyAnchorR.localPosition = rearBodyAnchorPosR;
        rearBodyAnchorR.localRotation = Quaternion.identity;
        rearBodyAnchorR.localScale = Vector3.one;

        headAnchor = CreateNode("HeadAnchor", bodyPivot);
        headAnchor.localPosition = ResolveHeadAnchorLocalPosition(bodyLocalPos, bodyLocalScale);
        headAnchor.localRotation = Quaternion.identity;
        headAnchor.localScale = Vector3.one;

        headPivot = CreateNode("HeadPivot", headAnchor);
        headPivot.localPosition = Vector3.zero;
        headPivot.localRotation = Quaternion.identity;

        head = CreatePart(
            "Head",
            PrimitiveType.Sphere,
            headPivot,
            Vector3.zero,
            Vector3.zero,
            headLocalScale,
            CreatureMaterialSlot.Body);

        ResolveEarAnchorLocalPositions(headLocalScale, out Vector3 earAnchorPosL, out Vector3 earAnchorPosR);
        Vector3 earScale = ResolveEarLocalScale();

        ResolveJawDimensions(
            out float upperJawResolvedWidth,
            out float lowerJawResolvedWidth,
            out float upperJawResolvedHeight,
            out float lowerJawResolvedHeight,
            out float upperJawResolvedLength,
            out float lowerJawResolvedLength);
        ResolveJawVerticalOffsets(out float upperJawYOffset, out float lowerJawYOffset);

        mouthAnchor = CreateNode("MouthAnchor", headPivot);
        mouthAnchor.localPosition = ResolveMouthAnchorLocalPosition(headLocalScale);
        mouthAnchor.localRotation = Quaternion.identity;
        mouthAnchor.localScale = Vector3.one;

        upperJaw = CreatePart(
            "UpperJaw",
            PrimitiveType.Sphere,
            mouthAnchor,
            new Vector3(0f, upperJawYOffset, 0f),
            Vector3.zero,
            new Vector3(upperJawResolvedWidth, upperJawResolvedHeight, upperJawResolvedLength),
            CreatureMaterialSlot.Mouth);

        jawPivot = CreateNode("JawPivot", mouthAnchor);
        jawPivot.localPosition = Vector3.zero;
        jawPivot.localRotation = Quaternion.Euler(jawBasePitch, 0f, 0f);

        lowerJaw = CreatePart(
            "LowerJaw",
            PrimitiveType.Sphere,
            jawPivot,
            new Vector3(0f, lowerJawYOffset, 0f),
            Vector3.zero,
            new Vector3(lowerJawResolvedWidth, lowerJawResolvedHeight, lowerJawResolvedLength),
            CreatureMaterialSlot.Mouth);

        Vector3 eyeLocalScale = ResolveEyeLocalScale();
        ResolveEyeAnchorLocalPositions(headLocalScale, out Vector3 eyeAnchorPosL, out Vector3 eyeAnchorPosR);

        eyeAnchorL = CreateNode("EyeAnchor_L", headPivot);
        eyeAnchorL.localPosition = eyeAnchorPosL;
        eyeAnchorL.localRotation = Quaternion.identity;
        eyeAnchorL.localScale = Vector3.one;

        eyeAnchorR = CreateNode("EyeAnchor_R", headPivot);
        eyeAnchorR.localPosition = eyeAnchorPosR;
        eyeAnchorR.localRotation = Quaternion.identity;
        eyeAnchorR.localScale = Vector3.one;

        eyeL = CreatePart(
            "Eye_L",
            PrimitiveType.Sphere,
            eyeAnchorL,
            Vector3.zero,
            Vector3.zero,
            eyeLocalScale,
            CreatureMaterialSlot.EyeSclera);

        eyeR = CreatePart(
            "Eye_R",
            PrimitiveType.Sphere,
            eyeAnchorR,
            Vector3.zero,
            Vector3.zero,
            eyeLocalScale,
            CreatureMaterialSlot.EyeSclera);

        pupilL = CreatePart(
            "Pupil_L",
            PrimitiveType.Sphere,
            eyeL,
            new Vector3(0f, 0f, pupilForwardOffset),
            Vector3.zero,
            new Vector3(pupilScale, pupilScale, pupilScale),
            CreatureMaterialSlot.EyePupil);

        pupilR = CreatePart(
            "Pupil_R",
            PrimitiveType.Sphere,
            eyeR,
            new Vector3(0f, 0f, pupilForwardOffset),
            Vector3.zero,
            new Vector3(pupilScale, pupilScale, pupilScale),
            CreatureMaterialSlot.EyePupil);

        RemoveCollider(pupilL.gameObject);
        RemoveCollider(pupilR.gameObject);

        earAnchorL = CreateNode("EarAnchor_L", headPivot);
        earAnchorL.localPosition = earAnchorPosL;
        earAnchorL.localRotation = Quaternion.identity;
        earAnchorL.localScale = Vector3.one;

        earAnchorR = CreateNode("EarAnchor_R", headPivot);
        earAnchorR.localPosition = earAnchorPosR;
        earAnchorR.localRotation = Quaternion.identity;
        earAnchorR.localScale = Vector3.one;

        earL = CreatePart(
            "Ear_L",
            PrimitiveType.Sphere,
            earAnchorL,
            Vector3.zero,
            new Vector3(0f, 0f, 18f),
            earScale,
            CreatureMaterialSlot.Ears);

        earR = CreatePart(
            "Ear_R",
            PrimitiveType.Sphere,
            earAnchorR,
            Vector3.zero,
            new Vector3(0f, 0f, -18f),
            earScale,
            CreatureMaterialSlot.Ears);

        tail = CreatePart(
            "Tail",
            PrimitiveType.Sphere,
            visualRoot,
            Vector3.zero,
            new Vector3(tailPitchDegrees, 0f, 0f),
            GetTailEllipsoidScale(),
            CreatureMaterialSlot.Tail);

        tailAnchor = CreateNode("TailAnchor", bodyPivot);
        tailAnchor.localPosition = ResolveTailAnchorLocalPosition(bodyLocalPos, bodyLocalScale);
        tailAnchor.localRotation = Quaternion.identity;
        tailAnchor.localScale = Vector3.one;
        tail.SetParent(tailAnchor, false);

        BuildLegRig(bodyLocalPos, bodyLocalScale);
    }

    private Transform CreateFusedBodyPart(
        string name,
        Transform parent,
        Vector3 localPosition,
        Vector3 bodyLocalScale,
        Vector3 rearBodyScale,
        Vector3 rearBodyCenterL,
        Vector3 rearBodyCenterR)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        MeshFilter meshFilter = go.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = BuildFusedBodyMesh(bodyLocalScale, rearBodyScale, rearBodyCenterL, rearBodyCenterR);
        CacheFusedBodyMeshInputs(bodyLocalScale, rearBodyScale, rearBodyCenterL, rearBodyCenterR);
        go.AddComponent<MeshRenderer>();
        ApplyMaterial(go, CreatureMaterialSlot.Body);
        return go.transform;
    }

    private void UpdateFusedBodyMesh(Vector3 bodyLocalScale, Vector3 rearBodyScale, Vector3 rearBodyCenterL, Vector3 rearBodyCenterR)
    {
        if (body == null)
            return;

        MeshFilter meshFilter = body.GetComponent<MeshFilter>();
        if (meshFilter == null)
            return;

        if (fusedBodyMeshCacheValid &&
            fusedBodyCachedDetailLevel == sphereDetailLevel &&
            Approximately(fusedBodyCachedBodyScale, bodyLocalScale) &&
            Approximately(fusedBodyCachedRearScale, rearBodyScale) &&
            Approximately(fusedBodyCachedRearCenterL, rearBodyCenterL) &&
            Approximately(fusedBodyCachedRearCenterR, rearBodyCenterR) &&
            HasFusedBodyMesh(body))
        {
            return;
        }

        meshFilter.sharedMesh = BuildFusedBodyMesh(bodyLocalScale, rearBodyScale, rearBodyCenterL, rearBodyCenterR);
        CacheFusedBodyMeshInputs(bodyLocalScale, rearBodyScale, rearBodyCenterL, rearBodyCenterR);
    }

    private void CacheFusedBodyMeshInputs(Vector3 bodyLocalScale, Vector3 rearBodyScale, Vector3 rearBodyCenterL, Vector3 rearBodyCenterR)
    {
        fusedBodyCachedBodyScale = bodyLocalScale;
        fusedBodyCachedRearScale = rearBodyScale;
        fusedBodyCachedRearCenterL = rearBodyCenterL;
        fusedBodyCachedRearCenterR = rearBodyCenterR;
        fusedBodyCachedDetailLevel = sphereDetailLevel;
        fusedBodyMeshCacheValid = true;
    }

    private static bool Approximately(Vector3 a, Vector3 b)
    {
        return (a - b).sqrMagnitude <= 0.0000001f;
    }

    private Mesh BuildFusedBodyMesh(Vector3 bodyLocalScale, Vector3 rearBodyScale, Vector3 rearBodyCenterL, Vector3 rearBodyCenterR)
    {
        int subdivisionLevel = ResolveFusedBodyIcoSubdivisionLevel();
        BuildIcosphereDirectionMesh(subdivisionLevel, out List<Vector3> directions, out List<int> triangles);

        int vertexCount = directions.Count;
        List<Vector3> vertices = new List<Vector3>(vertexCount);
        List<Vector3> normals = new List<Vector3>(vertexCount);
        List<Vector2> uv = new List<Vector2>(vertexCount);

        Vector3 bodyRadii = EnsurePositiveRadii(bodyLocalScale * 0.5f);
        Vector3 rearRadii = EnsurePositiveRadii(rearBodyScale * 0.5f);
        float fieldThreshold = Mathf.Exp(-FusedBodyFieldSharpness);

        for (int i = 0; i < directions.Count; i++)
        {
            Vector3 direction = directions[i];
            float distance = ResolveFusedBodySurfaceDistance(direction, bodyRadii, rearRadii, rearBodyCenterL, rearBodyCenterR, fieldThreshold);
            Vector3 vertex = direction * distance;
            vertices.Add(vertex);
            normals.Add(ResolveFusedBodyNormal(vertex, bodyRadii, rearRadii, rearBodyCenterL, rearBodyCenterR));
            uv.Add(DirectionToUv(direction));
        }

        Mesh mesh = new Mesh();
        mesh.name = $"{FusedBodyMeshNamePrefix}_{sphereDetailLevel}_Ico{subdivisionLevel}";
        mesh.hideFlags = HideFlags.HideAndDontSave;

        if (ShouldUseFlatFusedBodyNormals())
            ApplyFlatShadedMeshData(mesh, vertices, uv, triangles);
        else
        {
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
        }

        mesh.RecalculateBounds();
        return mesh;
    }

    private static Vector2 DirectionToUv(Vector3 direction)
    {
        float u = Mathf.Atan2(direction.z, direction.x) / (Mathf.PI * 2f) + 0.5f;
        float v = Mathf.Acos(Mathf.Clamp(direction.y, -1f, 1f)) / Mathf.PI;
        return new Vector2(u, 1f - v);
    }

    private bool ShouldUseFlatFusedBodyNormals()
    {
        return sphereDetailLevel == CreatureSphereDetailLevel.Blocky ||
               sphereDetailLevel == CreatureSphereDetailLevel.VeryLow;
    }

    private static void ApplyFlatShadedMeshData(Mesh mesh, List<Vector3> sourceVertices, List<Vector2> sourceUv, List<int> sourceTriangles)
    {
        List<Vector3> flatVertices = new List<Vector3>(sourceTriangles.Count);
        List<Vector3> flatNormals = new List<Vector3>(sourceTriangles.Count);
        List<Vector2> flatUv = new List<Vector2>(sourceTriangles.Count);
        List<int> flatTriangles = new List<int>(sourceTriangles.Count);

        for (int i = 0; i < sourceTriangles.Count; i += 3)
        {
            Vector3 v0 = sourceVertices[sourceTriangles[i]];
            Vector3 v1 = sourceVertices[sourceTriangles[i + 1]];
            Vector3 v2 = sourceVertices[sourceTriangles[i + 2]];
            Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0);
            if (normal.sqrMagnitude <= 0.000001f)
                normal = Vector3.up;
            else
                normal.Normalize();

            Vector3 center = (v0 + v1 + v2) / 3f;
            if (Vector3.Dot(normal, center) < 0f)
                normal = -normal;

            int baseIndex = flatVertices.Count;
            flatVertices.Add(v0);
            flatVertices.Add(v1);
            flatVertices.Add(v2);
            flatNormals.Add(normal);
            flatNormals.Add(normal);
            flatNormals.Add(normal);
            flatUv.Add(sourceUv[sourceTriangles[i]]);
            flatUv.Add(sourceUv[sourceTriangles[i + 1]]);
            flatUv.Add(sourceUv[sourceTriangles[i + 2]]);
            flatTriangles.Add(baseIndex);
            flatTriangles.Add(baseIndex + 1);
            flatTriangles.Add(baseIndex + 2);
        }

        mesh.SetVertices(flatVertices);
        mesh.SetNormals(flatNormals);
        mesh.SetUVs(0, flatUv);
        mesh.SetTriangles(flatTriangles, 0);
    }

    private int ResolveFusedBodyIcoSubdivisionLevel()
    {
        switch (sphereDetailLevel)
        {
            case CreatureSphereDetailLevel.Blocky:
                return 0;
            case CreatureSphereDetailLevel.VeryLow:
            case CreatureSphereDetailLevel.Low:
                return 1;
            case CreatureSphereDetailLevel.Medium:
                return 2;
            case CreatureSphereDetailLevel.High:
                return 3;
            default:
                return 4;
        }
    }

    private static void BuildIcosphereDirectionMesh(int subdivisionLevel, out List<Vector3> directions, out List<int> triangles)
    {
        directions = new List<Vector3>(12);
        triangles = new List<int>(60);

        float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
        AddDirection(directions, new Vector3(-1f, t, 0f));
        AddDirection(directions, new Vector3(1f, t, 0f));
        AddDirection(directions, new Vector3(-1f, -t, 0f));
        AddDirection(directions, new Vector3(1f, -t, 0f));
        AddDirection(directions, new Vector3(0f, -1f, t));
        AddDirection(directions, new Vector3(0f, 1f, t));
        AddDirection(directions, new Vector3(0f, -1f, -t));
        AddDirection(directions, new Vector3(0f, 1f, -t));
        AddDirection(directions, new Vector3(t, 0f, -1f));
        AddDirection(directions, new Vector3(t, 0f, 1f));
        AddDirection(directions, new Vector3(-t, 0f, -1f));
        AddDirection(directions, new Vector3(-t, 0f, 1f));

        int[] baseTriangles =
        {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
            1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
            4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
        };
        triangles.AddRange(baseTriangles);

        for (int i = 0; i < subdivisionLevel; i++)
            SubdivideIcosphere(directions, triangles);
    }

    private static void SubdivideIcosphere(List<Vector3> directions, List<int> triangles)
    {
        Dictionary<long, int> midpointCache = new Dictionary<long, int>();
        List<int> subdivided = new List<int>(triangles.Count * 4);

        for (int i = 0; i < triangles.Count; i += 3)
        {
            int a = triangles[i];
            int b = triangles[i + 1];
            int c = triangles[i + 2];

            int ab = GetIcosphereMidpointIndex(directions, midpointCache, a, b);
            int bc = GetIcosphereMidpointIndex(directions, midpointCache, b, c);
            int ca = GetIcosphereMidpointIndex(directions, midpointCache, c, a);

            AddTriangle(subdivided, a, ab, ca);
            AddTriangle(subdivided, b, bc, ab);
            AddTriangle(subdivided, c, ca, bc);
            AddTriangle(subdivided, ab, bc, ca);
        }

        triangles.Clear();
        triangles.AddRange(subdivided);
    }

    private static int GetIcosphereMidpointIndex(List<Vector3> directions, Dictionary<long, int> midpointCache, int indexA, int indexB)
    {
        int min = Mathf.Min(indexA, indexB);
        int max = Mathf.Max(indexA, indexB);
        long key = ((long)min << 32) | (uint)max;

        if (midpointCache.TryGetValue(key, out int cachedIndex))
            return cachedIndex;

        Vector3 midpoint = (directions[indexA] + directions[indexB]) * 0.5f;
        int newIndex = AddDirection(directions, midpoint);
        midpointCache[key] = newIndex;
        return newIndex;
    }

    private static int AddDirection(List<Vector3> directions, Vector3 direction)
    {
        directions.Add(direction.normalized);
        return directions.Count - 1;
    }

    private static void AddTriangle(List<int> triangles, int a, int b, int c)
    {
        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);
    }

    private static Vector3 EnsurePositiveRadii(Vector3 radii)
    {
        return new Vector3(
            Mathf.Max(0.001f, radii.x),
            Mathf.Max(0.001f, radii.y),
            Mathf.Max(0.001f, radii.z));
    }

    private float ResolveFusedBodySurfaceDistance(
        Vector3 direction,
        Vector3 bodyRadii,
        Vector3 rearRadii,
        Vector3 rearBodyCenterL,
        Vector3 rearBodyCenterR,
        float fieldThreshold)
    {
        float high = Mathf.Max(
            bodyRadii.magnitude,
            Mathf.Max(rearBodyCenterL.magnitude + rearRadii.magnitude, rearBodyCenterR.magnitude + rearRadii.magnitude));
        high = Mathf.Max(high * 1.6f, 0.1f);

        while (EvaluateFusedBodyField(direction * high, bodyRadii, rearRadii, rearBodyCenterL, rearBodyCenterR) > fieldThreshold)
            high *= 1.5f;

        float low = 0f;
        for (int i = 0; i < 18; i++)
        {
            float mid = (low + high) * 0.5f;
            float field = EvaluateFusedBodyField(direction * mid, bodyRadii, rearRadii, rearBodyCenterL, rearBodyCenterR);
            if (field > fieldThreshold)
                low = mid;
            else
                high = mid;
        }

        return (low + high) * 0.5f;
    }

    private float EvaluateFusedBodyField(Vector3 point, Vector3 bodyRadii, Vector3 rearRadii, Vector3 rearBodyCenterL, Vector3 rearBodyCenterR)
    {
        return EvaluateEllipsoidField(point, Vector3.zero, bodyRadii) +
               EvaluateEllipsoidField(point, rearBodyCenterL, rearRadii) +
               EvaluateEllipsoidField(point, rearBodyCenterR, rearRadii);
    }

    private float EvaluateEllipsoidField(Vector3 point, Vector3 center, Vector3 radii)
    {
        Vector3 local = point - center;
        float q =
            (local.x * local.x) / (radii.x * radii.x) +
            (local.y * local.y) / (radii.y * radii.y) +
            (local.z * local.z) / (radii.z * radii.z);
        return Mathf.Exp(-FusedBodyFieldSharpness * q);
    }

    private Vector3 ResolveFusedBodyNormal(Vector3 point, Vector3 bodyRadii, Vector3 rearRadii, Vector3 rearBodyCenterL, Vector3 rearBodyCenterR)
    {
        Vector3 inwardGradient =
            EvaluateEllipsoidGradient(point, Vector3.zero, bodyRadii) +
            EvaluateEllipsoidGradient(point, rearBodyCenterL, rearRadii) +
            EvaluateEllipsoidGradient(point, rearBodyCenterR, rearRadii);

        if (inwardGradient.sqrMagnitude <= 0.000001f)
            return point.sqrMagnitude > 0.000001f ? point.normalized : Vector3.up;

        return (-inwardGradient).normalized;
    }

    private Vector3 EvaluateEllipsoidGradient(Vector3 point, Vector3 center, Vector3 radii)
    {
        Vector3 local = point - center;
        float field = EvaluateEllipsoidField(point, center, radii);
        return new Vector3(
            field * -FusedBodyFieldSharpness * 2f * local.x / (radii.x * radii.x),
            field * -FusedBodyFieldSharpness * 2f * local.y / (radii.y * radii.y),
            field * -FusedBodyFieldSharpness * 2f * local.z / (radii.z * radii.z));
    }

    // Main animation dispatch called every frame.
    // Each state applies its pose on top of the cached neutral bases.
    protected override void UpdateAnimation()
    {
        if (!IsRigReady())
            return;

        float t = Application.isPlaying ? Time.time : (float)System.DateTime.Now.TimeOfDay.TotalSeconds;
        float dt = DeltaTimeSafe();
        CreatureVisualState animationState = GetAnimationState();

        ResetTowardBase(dt);

        switch (animationState)
        {
            case CreatureVisualState.Idle:
                AnimateIdle(t);
                break;

            case CreatureVisualState.Follow:
            case CreatureVisualState.Overtake:
            case CreatureVisualState.Hunt:
            case CreatureVisualState.Recenter:
            case CreatureVisualState.LeashReturn:
                AnimateChase(t);
                break;

            case CreatureVisualState.WaitPlayer:
                AnimateWaitPlayer(t);
                break;

            case CreatureVisualState.EatAttack:
                AnimateEatAttackPose(t);
                break;

            case CreatureVisualState.EatRecovery:
                AnimateEatRecoveryPose(t);
                break;

            case CreatureVisualState.Stunned:
                AnimateStunned(t);
                break;
        }

        UpdateEyes(t, animationState);
    }

    private bool IsRigReady()
    {
        return body != null &&
               headPivot != null &&
               jawPivot != null &&
               tail != null &&
               eyeL != null &&
               eyeR != null;
    }

    private void ResetTowardBase(float dt)
    {
        for (int i = 0; i < legRigs.Count; i++)
        {
            Transform legRoot = legRigs[i].root;
            if (legRoot != null)
                legRoot.localRotation = Quaternion.Slerp(legRoot.localRotation, Quaternion.identity, 8f * dt);
        }

        SetBodyClusterLocalPosition(Vector3.Lerp(body.localPosition, bodyBaseLocalPos, 8f * dt));
        body.localScale = Vector3.Lerp(body.localScale, bodyBaseLocalScale, 8f * dt);

        headPivot.localRotation = Quaternion.Slerp(headPivot.localRotation, headBaseLocalRot, 8f * dt);
        tail.localRotation = Quaternion.Slerp(tail.localRotation, tailBaseLocalRot, 8f * dt);
        jawPivot.localRotation = Quaternion.Slerp(jawPivot.localRotation, jawBaseLocalRot, 10f * dt);
        visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, visualBaseLocalRot, 8f * dt);

        SetEyeScaleImmediate(eyeBaseScale);
    }

    private void AnimateIdle(float t)
    {
        float breath = Mathf.Sin(t * idleBreathSpeed) * idleBreathAmount;
        SetBodyClusterLocalPosition(bodyBaseLocalPos + new Vector3(0f, breath, 0f));

        float headNod = Mathf.Sin(t * idleHeadNodSpeed) * idleHeadNodAngle;
        headPivot.localRotation = headBaseLocalRot * Quaternion.Euler(headNod, 0f, 0f);

        float tailSwing = Mathf.Sin(t * 1.2f) * (tailSwingAngle * idleTailSwingFactor);
        tail.localRotation = tailBaseLocalRot * Quaternion.Euler(0f, tailSwing, 0f);

        jawPivot.localRotation = jawBaseLocalRot;
    }

    private void AnimateChase(float t)
    {
        if (animateWalk)
        {
            float phase = ResolveChasePhase(t);
            float appliedLegAngle = legAngle;
            float appliedBodyBob = bodyBobAmount;
            float appliedHeadSwing = headSwingAngle;
            float appliedTailSwing = tailSwingAngle;

            float gaitSin = Mathf.Sin(phase);
            for (int i = 0; i < legRigs.Count; i++)
            {
                LegRig leg = legRigs[i];
                if (leg.root == null)
                    continue;
                float swingSign = ResolveLegSwingSign(leg);
                float legPitch = gaitSin * appliedLegAngle * swingSign;
                leg.root.localRotation = Quaternion.Euler(legPitch, 0f, 0f);
            }

            float bob = Mathf.Abs(Mathf.Sin(phase * 1.2f)) * appliedBodyBob;
            SetBodyClusterLocalPosition(bodyBaseLocalPos + new Vector3(0f, bob, 0f));

            float headSwing = Mathf.Sin(phase) * appliedHeadSwing;
            headPivot.localRotation = headBaseLocalRot * Quaternion.Euler(headSwing, 0f, 0f);

            float tailSwing = Mathf.Sin(phase + 0.8f) * appliedTailSwing;
            tail.localRotation = tailBaseLocalRot * Quaternion.Euler(0f, tailSwing, 0f);
        }

        jawPivot.localRotation = jawBaseLocalRot * Quaternion.Euler(chaseMouthOpenAngle * 0.12f, 0f, 0f);
    }

    // Computes chase gait phase.
    // Can be time-driven or displacement-driven (distance traveled in world space).
    private float ResolveChasePhase(float t)
    {
        if (!driveChasePhaseFromDisplacement || !Application.isPlaying)
            return t * walkCycleSpeed;

        EnsureChaseCalibration();

        Vector3 currentPosition = transform.position;
        if (!chaseHasLastWorldPosition)
        {
            chaseLastWorldPosition = currentPosition;
            chaseHasLastWorldPosition = true;
            return chaseGaitPhase;
        }

        Vector3 delta = currentPosition - chaseLastWorldPosition;
        chaseLastWorldPosition = currentPosition;
        delta.y = 0f;
        float distance = delta.magnitude;

        float minDistance = chaseMinDisplacement;
        if (distance <= minDistance)
            return chaseGaitPhase;

        float maxDistance = chaseMaxDisplacementPerFrame;
        if (distance > maxDistance)
            return chaseGaitPhase;

        float phaseAdvance = (distance / chaseCycleDistance) * Mathf.PI * 2f;
        chaseGaitPhase = Mathf.Repeat(chaseGaitPhase + phaseAdvance, Mathf.PI * 2f);
        return chaseGaitPhase;
    }

    private void EnsureChaseCalibration()
    {
        if (float.IsNaN(chaseCachedLegAngle) || float.IsInfinity(chaseCachedLegAngle) ||
            Mathf.Abs(chaseCachedLegAngle - legAngle) > 0.001f ||
            chaseCycleDistance <= 0.001f)
        {
            RecomputeChaseCalibration();
        }
    }

    private void RecomputeChaseCalibration()
    {
        chaseLegLengthEstimate = EstimateAverageLegLength();
        float stepDistance = 2f * chaseLegLengthEstimate * Mathf.Sin(legAngle * Mathf.Deg2Rad);
        chaseCycleDistance = stepDistance * 2f;
        chaseCachedLegAngle = legAngle;
    }

    private float EstimateAverageLegLength()
    {
        float total = 0f;
        int count = 0;

        for (int i = 0; i < legRigs.Count; i++)
            TryAccumulateLegLength(legRigs[i].root, ref total, ref count);

        if (count <= 0)
            return 0.2f;

        return total / count;
    }

    private static void TryAccumulateLegLength(Transform legRoot, ref float totalLength, ref int count)
    {
        if (legRoot == null)
            return;

        Transform upper = legRoot.Find("Upper");
        Transform ankle = legRoot.Find("Ankle");
        Transform foot = legRoot.Find("Foot");
        if (upper == null || ankle == null || foot == null)
            return;

        float segmentA = Vector3.Distance(upper.position, ankle.position);
        float segmentB = Vector3.Distance(ankle.position, foot.position);
        float candidate = segmentA + segmentB;
        if (candidate <= 0.001f)
            return;

        totalLength += candidate;
        count++;
    }

    private void ResetChaseDisplacementSampling()
    {
        chaseHasLastWorldPosition = false;
        chaseLastWorldPosition = transform.position;
    }

    private void AnimateEatAttackPose(float t)
    {
        // Keep body anchored: jump motion is handled by controller movement.
        SetBodyClusterLocalPosition(bodyBaseLocalPos);
        body.localScale = bodyBaseLocalScale;

        // Unity local X rotation is positive when pitching down.
        // Negate here so inspector convention stays intuitive: + = up, - = down.
        visualRoot.localRotation = visualBaseLocalRot * Quaternion.Euler(-eatAttackGlobalPitch, 0f, 0f);

        float phase = ResolveChasePhase(t);
        float eatAttackLegSwing = Mathf.Sin(phase) * (legAngle * 0.25f);
        for (int i = 0; i < legRigs.Count; i++)
        {
            LegRig leg = legRigs[i];
            if (leg.root == null)
                continue;
            float swingSign = ResolveLegSwingSign(leg);
            leg.root.localRotation = Quaternion.Euler(eatLegFoldAngle + (eatAttackLegSwing * swingSign), 0f, 0f);
        }

        headPivot.localRotation = headBaseLocalRot * Quaternion.Euler(6f, 0f, 0f);
        float mouthPulse = 0.65f + 0.35f * Mathf.Sin(stateTimer * 14f);
        jawPivot.localRotation = jawBaseLocalRot * Quaternion.Euler(eatAttackMouthOpenAngle * mouthPulse, 0f, 0f);
        tail.localRotation = tailBaseLocalRot * Quaternion.Euler(0f, -tailSwingAngle * 0.4f, 0f);
    }

    private void AnimateEatRecoveryPose(float t)
    {
        SetBodyClusterLocalPosition(bodyBaseLocalPos);
        body.localScale = bodyBaseLocalScale;

        // Same convention as EAT_ATTACK: + = up, - = down from inspector values.
        float blendDuration = eatRecoveryPitchBlendDuration;
        float blendT;
        if (blendDuration <= 0f)
        {
            blendT = 1f;
        }
        else
        {
            blendT = stateTimer / blendDuration;
            if (blendT < 0f)
                blendT = 0f;
            else if (blendT > 1f)
                blendT = 1f;
        }
        float blendedPitch = Mathf.Lerp(eatAttackGlobalPitch, eatRecoveryGlobalPitch, blendT);
        visualRoot.localRotation = visualBaseLocalRot * Quaternion.Euler(-blendedPitch, 0f, 0f);

        float phase = ResolveChasePhase(t);
        float eatRecoveryLegSwing = Mathf.Sin(phase) * (legAngle * 0.35f);
        for (int i = 0; i < legRigs.Count; i++)
        {
            LegRig leg = legRigs[i];
            if (leg.root == null)
                continue;
            float swingSign = ResolveLegSwingSign(leg);
            leg.root.localRotation = Quaternion.Euler(-eatRecoveryLegForwardAngle + (eatRecoveryLegSwing * swingSign), 0f, 0f);
        }

        headPivot.localRotation = headBaseLocalRot * Quaternion.Euler(-6f, 0f, 0f);
        jawPivot.localRotation = jawBaseLocalRot * Quaternion.Euler(eatRecoveryMouthOpenAngle * 0.3f, 0f, 0f);
        tail.localRotation = tailBaseLocalRot * Quaternion.Euler(0f, tailSwingAngle * 0.2f, 0f);
    }

    private void AnimateWaitPlayer(float t)
    {
        float bob = Mathf.Abs(Mathf.Sin(t * waitBodyBobSpeed)) * waitBodyBobAmount;
        SetBodyClusterLocalPosition(bodyBaseLocalPos + new Vector3(0f, bob, 0f));

        float yaw = Mathf.Sin(t * waitHeadYawSpeed) * waitHeadYawAngle;
        float nod = Mathf.Sin(t * (waitHeadYawSpeed * 0.6f + 0.35f)) * (waitHeadYawAngle * 0.25f);
        headPivot.localRotation = headBaseLocalRot * Quaternion.Euler(nod, yaw, 0f);

        // "Trepigner": fast, slightly irregular alternating stomps.
        float stompPhase = t * waitLegStompSpeed;
        int pairCount = ResolveLegPairCount();
        for (int i = 0; i < legRigs.Count; i++)
        {
            LegRig leg = legRigs[i];
            if (leg.root == null)
                continue;

            float pairT = pairCount <= 1 ? 0.5f : leg.pairIndex / (float)(pairCount - 1);
            float stompAmplitude = waitLegStompAngle * Mathf.Lerp(1f, 0.8f, pairT);
            float primary = Mathf.Sin(stompPhase + (pairT * Mathf.PI * 0.7f));
            float jitter = Mathf.Sin(stompPhase * 2.2f + (pairT * 2.9f) + (leg.sideSign > 0 ? 0.9f : 0f)) * waitLegStompJitter;
            float stomp = Mathf.Clamp(primary + jitter, -1f, 1f) * stompAmplitude;
            float sideSign = leg.sideSign < 0 ? 1f : -1f;
            leg.root.localRotation = Quaternion.Euler(stomp * sideSign, 0f, 0f);
        }

        float tailSwing = Mathf.Sin(t * (waitHeadYawSpeed + 0.55f)) * waitTailSwingAngle;
        tail.localRotation = tailBaseLocalRot * Quaternion.Euler(0f, tailSwing, 0f);

        // Angry "talking": wide and quick jaw cycles.
        float mouthPulse = 0.5f + 0.5f * Mathf.Sin(t * waitMouthTalkSpeed);
        float mouthOpen = waitMouthOpenBase + mouthPulse * waitMouthTalkAngle;
        jawPivot.localRotation = jawBaseLocalRot * Quaternion.Euler(mouthOpen, 0f, 0f);
    }

    private static float ResolveLegSwingSign(LegRig leg)
    {
        bool pairEven = (leg.pairIndex & 1) == 0;
        bool isRight = leg.sideSign > 0;
        return pairEven ^ isRight ? 1f : -1f;
    }

    private void SetBodyClusterLocalPosition(Vector3 bodyLocalPosition)
    {
        if (body != null)
            body.localPosition = bodyLocalPosition;

        Vector3 bodyOffset = bodyLocalPosition - bodyBaseLocalPos;

        if (rearBodyAnchorL != null)
            rearBodyAnchorL.localPosition = rearBodyAnchorLBaseLocalPos + bodyOffset;

        if (rearBodyAnchorR != null)
            rearBodyAnchorR.localPosition = rearBodyAnchorRBaseLocalPos + bodyOffset;
    }

    private void AnimateStunned(float t)
    {
        float shake = Mathf.Sin(t * stunnedShakeSpeed) * stunnedShakeAngle;
        visualRoot.localRotation = visualBaseLocalRot * Quaternion.Euler(0f, 0f, shake);

        jawPivot.localRotation = jawBaseLocalRot * Quaternion.Euler(stunnedMouthOpenAngle * 0.08f, 0f, 0f);
    }

    private void UpdateEyes(float t, CreatureVisualState animationState)
    {
        if (animationState != CreatureVisualState.WaitPlayer)
        {
            SetPupilLocalPositionImmediate(pupilBaseLocalPos);
            return;
        }

        float rawLook = Mathf.Sin(t * waitEyeLookSpeed);
        float dartLook = Mathf.Sin(t * waitEyeDartSpeed);
        float snapped = dartLook >= 0f ? 1f : -1f;
        float lookPhase = Mathf.Lerp(rawLook, snapped, waitEyeDartSnap);

        float eyeRadius = 0.5f;
        float appliedPupilScale = pupilBaseScale.sqrMagnitude > 0.000001f ? pupilBaseScale.x : pupilScale;
        float pupilRadius = appliedPupilScale * 0.5f;
        float maxTravelOnSclera = (eyeRadius - pupilRadius) * 0.95f;
        float minTravel = waitPupilLookSideOffset;
        float angleFactor = Mathf.Sin(waitEyeLookSideAngle * Mathf.Deg2Rad);
        float sideAmplitude = Mathf.Lerp(minTravel, maxTravelOnSclera, angleFactor) * waitEyeDartSideMultiplier;
        if (sideAmplitude > maxTravelOnSclera)
            sideAmplitude = maxTravelOnSclera;
        float pupilSide = lookPhase * sideAmplitude;
        Vector3 pupilPos = pupilBaseLocalPos + new Vector3(pupilSide, 0f, 0f);
        SetPupilLocalPositionImmediate(pupilPos);
    }

    private static bool IsChaseLikeState(CreatureVisualState visualState)
    {
        return visualState == CreatureVisualState.Follow ||
               visualState == CreatureVisualState.Overtake ||
               visualState == CreatureVisualState.Hunt ||
               visualState == CreatureVisualState.Recenter ||
               visualState == CreatureVisualState.LeashReturn;
    }

    private void SetEyeScaleImmediate(Vector3 scale)
    {
        if (eyeL != null)
            eyeL.localScale = scale;

        if (eyeR != null)
            eyeR.localScale = scale;
    }

    private void SetEyeLocalPositionsImmediate(Vector3 leftLocalPos, Vector3 rightLocalPos)
    {
        if (eyeL != null)
            eyeL.localPosition = leftLocalPos;

        if (eyeR != null)
            eyeR.localPosition = rightLocalPos;
    }

    private void SetPupilScaleImmediate(Vector3 scale)
    {
        if (pupilL != null)
            pupilL.localScale = scale;

        if (pupilR != null)
            pupilR.localScale = scale;
    }

    private void SetPupilLocalPositionImmediate(Vector3 localPos)
    {
        if (pupilL != null)
            pupilL.localPosition = localPos;

        if (pupilR != null)
            pupilR.localPosition = localPos;
    }

    // Live-apply structural parameters without rebuilding:
    // overall scale, body/head proportions, mouth, legs, ear placement and tail geometry.
    private void ApplyStructurePlacement()
    {
        if (root != null)
            root.localScale = Vector3.one * overallScale;

        if (bodyPivot != null)
        {
            bodyPivot.localPosition = Vector3.zero;
            bodyPivot.localRotation = Quaternion.identity;
            bodyPivot.localScale = Vector3.one;
        }

        Vector3 resolvedBodyLocalPos = ResolveBodyLocalPosition();
        Vector3 resolvedBodyLocalScale = ResolveBodyLocalScale();

        if (body != null)
        {
            bodyBaseLocalPos = resolvedBodyLocalPos;
            bodyBaseLocalScale = Vector3.one;
            body.localPosition = bodyBaseLocalPos;
            body.localScale = bodyBaseLocalScale;

            Vector3 rearBodyScale = ResolveRearBodyLocalScale(resolvedBodyLocalScale);
            ResolveRearBodyAnchorLocalPositions(resolvedBodyLocalPos, resolvedBodyLocalScale, out Vector3 rearAnchorPosL, out Vector3 rearAnchorPosR);
            UpdateFusedBodyMesh(resolvedBodyLocalScale, rearBodyScale, rearAnchorPosL - resolvedBodyLocalPos, rearAnchorPosR - resolvedBodyLocalPos);
        }

        ApplyRearBodyPlacement(resolvedBodyLocalPos, resolvedBodyLocalScale);

        if (headAnchor != null)
        {
            headAnchor.localPosition = ResolveHeadAnchorLocalPosition(resolvedBodyLocalPos, resolvedBodyLocalScale);
            headAnchor.localRotation = Quaternion.identity;
            headAnchor.localScale = Vector3.one;

            if (headPivot != null)
                headPivot.localPosition = Vector3.zero;
        }
        else if (headPivot != null)
        {
            // Legacy fallback position in case an old hierarchy is still present.
            headPivot.localPosition = new Vector3(0f, 1.02f, 2.05f);
        }

        if (head != null)
        {
            head.localPosition = Vector3.zero;
            head.localScale = ResolveHeadLocalScale();
        }

        ApplyMouthStructurePlacement();
        ApplyLegStructurePlacement();
        ApplyTailPlacement();
        ApplyEarPlacement();
    }

    private void ApplyRearBodyPlacement(Vector3 bodyLocalPos, Vector3 bodyLocalScale)
    {
        Vector3 rearBodyScale = ResolveRearBodyLocalScale(bodyLocalScale);
        ResolveRearBodyAnchorLocalPositions(bodyLocalPos, bodyLocalScale, out Vector3 rearAnchorPosL, out Vector3 rearAnchorPosR);

        if (rearBodyAnchorL != null)
        {
            rearBodyAnchorLBaseLocalPos = rearAnchorPosL;
            rearBodyAnchorL.localPosition = rearAnchorPosL;
            rearBodyAnchorL.localRotation = Quaternion.identity;
            rearBodyAnchorL.localScale = Vector3.one;
        }

        if (rearBodyAnchorR != null)
        {
            rearBodyAnchorRBaseLocalPos = rearAnchorPosR;
            rearBodyAnchorR.localPosition = rearAnchorPosR;
            rearBodyAnchorR.localRotation = Quaternion.identity;
            rearBodyAnchorR.localScale = Vector3.one;
        }

        if (rearBodyL != null)
        {
            rearBodyL.localPosition = Vector3.zero;
            rearBodyL.localRotation = Quaternion.identity;
            rearBodyL.localScale = rearBodyScale;
        }

        if (rearBodyR != null)
        {
            rearBodyR.localPosition = Vector3.zero;
            rearBodyR.localRotation = Quaternion.identity;
            rearBodyR.localScale = rearBodyScale;
        }
    }

    // Keep collisions deterministic:
    // - remove physical colliders from generated visual parts
    // - keep a single trigger sphere on Hippo root, auto-fitted to structure
    private void ConfigureCollisionSetup()
    {
        if (removeGeneratedPartColliders && !generatedPartCollidersPurged && root != null)
        {
            RemoveCollidersInHierarchy(root, includeRoot: false);
            generatedPartCollidersPurged = true;
        }
        else if (!removeGeneratedPartColliders)
        {
            generatedPartCollidersPurged = false;
        }

        if (!autoStructureTriggerCollider)
        {
            if (structureTriggerCollider != null)
                RemoveCollider(structureTriggerCollider);

            structureTriggerCollider = null;
            rootColliderCleanupDone = false;
            return;
        }

        EnsureStructureTriggerCollider();
        if (!rootColliderCleanupDone)
        {
            RemoveExtraRootColliders();
            rootColliderCleanupDone = true;
        }
    }

    private void EnsureStructureTriggerCollider()
    {
        if (structureTriggerCollider == null)
            structureTriggerCollider = GetComponent<SphereCollider>();
        if (structureTriggerCollider == null)
            structureTriggerCollider = gameObject.AddComponent<SphereCollider>();

        structureTriggerCollider.isTrigger = true;
        structureTriggerCollider.enabled = true;

        if (!TryComputeStructureBounds(out Vector3 boundsCenter, out Vector3 boundsExtents))
            return;

        Vector3 center = boundsCenter + triggerCenterOffset;
        float baseRadius = Mathf.Max(boundsExtents.x, Mathf.Max(boundsExtents.y, boundsExtents.z));
        float radius = baseRadius * triggerRadiusScale + triggerRadiusPadding;
        if (radius < 0.01f)
            radius = 0.01f;

        structureTriggerCollider.center = center;
        structureTriggerCollider.radius = radius;
    }

    private void RemoveExtraRootColliders()
    {
        Collider[] rootColliders = GetComponents<Collider>();
        for (int i = 0; i < rootColliders.Length; i++)
        {
            Collider collider = rootColliders[i];
            if (collider == null)
                continue;
            if (collider == structureTriggerCollider)
                continue;
            if (collider is CharacterController)
                continue;

            RemoveCollider(collider);
        }
    }

    private bool TryComputeStructureBounds(out Vector3 center, out Vector3 extents)
    {
        center = Vector3.zero;
        extents = Vector3.zero;

        ResolveJawDimensions(
            out float upperJawWidth,
            out float lowerJawWidth,
            out float upperJawHeight,
            out float lowerJawHeight,
            out float upperJawLength,
            out float lowerJawLength);
        ResolveJawVerticalOffsets(out float upperJawYOffset, out float lowerJawYOffset);

        Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        Vector3 bodyLocalPos = ResolveBodyLocalPosition();
        Vector3 bodyLocalScale = ResolveBodyLocalScale();
        Vector3 headPivotPos = ResolveHeadAnchorLocalPosition(bodyLocalPos, bodyLocalScale);
        Vector3 headLocalScale = ResolveHeadLocalScale();
        Vector3 rearBodyScale = ResolveRearBodyLocalScale(bodyLocalScale);
        ResolveRearBodyAnchorLocalPositions(bodyLocalPos, bodyLocalScale, out Vector3 rearAnchorPosL, out Vector3 rearAnchorPosR);

        EncapsulateAabb(ref min, ref max, bodyLocalPos, bodyLocalScale);
        EncapsulateAabb(ref min, ref max, rearAnchorPosL, rearBodyScale);
        EncapsulateAabb(ref min, ref max, rearAnchorPosR, rearBodyScale);
        EncapsulateAabb(ref min, ref max, headPivotPos, headLocalScale);

        Vector3 mouthAnchorPos = headPivotPos + ResolveMouthAnchorLocalPosition(headLocalScale);
        EncapsulateAabb(ref min, ref max, mouthAnchorPos + new Vector3(0f, upperJawYOffset, 0f), new Vector3(upperJawWidth, upperJawHeight, upperJawLength));
        EncapsulateAabb(ref min, ref max, mouthAnchorPos + new Vector3(0f, lowerJawYOffset, 0f), new Vector3(lowerJawWidth, lowerJawHeight, lowerJawLength));

        ResolveEarAnchorLocalPositions(headLocalScale, out Vector3 earAnchorPosL, out Vector3 earAnchorPosR);
        Vector3 earSize = ResolveEarLocalScale();
        EncapsulateAabb(ref min, ref max, headPivotPos + earAnchorPosL, earSize);
        EncapsulateAabb(ref min, ref max, headPivotPos + earAnchorPosR, earSize);

        ResolveEyeAnchorLocalPositions(headLocalScale, out Vector3 eyeAnchorPosL, out Vector3 eyeAnchorPosR);
        Vector3 eyeSize = ResolveEyeLocalScale();
        EncapsulateAabb(ref min, ref max, headPivotPos + eyeAnchorPosL, eyeSize);
        EncapsulateAabb(ref min, ref max, headPivotPos + eyeAnchorPosR, eyeSize);

        Vector3 tailAnchorPos = ResolveTailAnchorLocalPosition(bodyLocalPos, bodyLocalScale);
        EncapsulateAabb(ref min, ref max, tailAnchorPos, GetTailEllipsoidScale());

        float upperCenterY = upperLegLength * UpperLegCenterYRatio;
        float ankleCenterY = upperLegLength * AnkleCenterYRatio;
        float footCenterY = upperLegLength * FootCenterYRatio;
        float ankleForward = footWidth * AnkleForwardFromFootWidthRatio;
        float footForward = footWidth * FootForwardFromFootWidthRatio;
        float footDepth = footWidth * FootDepthFromFootWidthRatio;

        Vector3 upperSize = new Vector3(upperLegThickness, upperLegLength, upperLegThickness);
        Vector3 ankleSize = new Vector3(ankleDiameter, ankleDiameter, ankleDiameter);
        Vector3 footSize = new Vector3(footWidth, footHeight, footDepth);
        int pairCount = ResolveLegPairCount();
        for (int pairIndex = 0; pairIndex < pairCount; pairIndex++)
        {
            Vector3 leftAnchor = ResolveLegAnchorLocalPosition(pairIndex, -1, bodyLocalPos, bodyLocalScale);
            Vector3 rightAnchor = ResolveLegAnchorLocalPosition(pairIndex, 1, bodyLocalPos, bodyLocalScale);
            EncapsulateLeg(ref min, ref max, leftAnchor, upperCenterY, ankleCenterY, footCenterY, ankleForward, footForward, upperSize, ankleSize, footSize);
            EncapsulateLeg(ref min, ref max, rightAnchor, upperCenterY, ankleCenterY, footCenterY, ankleForward, footForward, upperSize, ankleSize, footSize);
        }

        if (float.IsPositiveInfinity(min.x) || float.IsNegativeInfinity(max.x))
            return false;

        min *= overallScale;
        max *= overallScale;
        center = (min + max) * 0.5f;
        extents = (max - min) * 0.5f;
        return true;
    }

    private static void EncapsulateAabb(ref Vector3 min, ref Vector3 max, Vector3 center, Vector3 size)
    {
        Vector3 half = size * 0.5f;
        Vector3 localMin = center - half;
        Vector3 localMax = center + half;
        min = Vector3.Min(min, localMin);
        max = Vector3.Max(max, localMax);
    }

    private static void EncapsulateLeg(
        ref Vector3 min,
        ref Vector3 max,
        Vector3 legRootPosition,
        float upperCenterY,
        float ankleCenterY,
        float footCenterY,
        float ankleForward,
        float footForward,
        Vector3 upperSize,
        Vector3 ankleSize,
        Vector3 footSize)
    {
        EncapsulateAabb(ref min, ref max, legRootPosition + new Vector3(0f, upperCenterY, 0f), upperSize);
        EncapsulateAabb(ref min, ref max, legRootPosition + new Vector3(0f, ankleCenterY, ankleForward), ankleSize);
        EncapsulateAabb(ref min, ref max, legRootPosition + new Vector3(0f, footCenterY, footForward), footSize);
    }

    // Live-update mouth structure (upper jaw, lower jaw and pivot/rest orientation).
    private void ApplyMouthStructurePlacement()
    {
        ResolveJawDimensions(
            out float upperJawResolvedWidth,
            out float lowerJawResolvedWidth,
            out float upperJawResolvedHeight,
            out float lowerJawResolvedHeight,
            out float upperJawResolvedLength,
            out float lowerJawResolvedLength);
        ResolveJawVerticalOffsets(out float upperJawYOffset, out float lowerJawYOffset);

        Vector3 headLocalScale = ResolveHeadLocalScale();

        if (upperJaw != null)
        {
            upperJaw.localPosition = new Vector3(0f, upperJawYOffset, 0f);
            upperJaw.localScale = new Vector3(upperJawResolvedWidth, upperJawResolvedHeight, upperJawResolvedLength);
        }

        if (mouthAnchor != null)
        {
            mouthAnchor.localPosition = ResolveMouthAnchorLocalPosition(headLocalScale);
            mouthAnchor.localRotation = Quaternion.identity;
            mouthAnchor.localScale = Vector3.one;
        }

        if (jawPivot != null)
        {
            jawPivot.localPosition = Vector3.zero;
            jawBaseLocalRot = Quaternion.Euler(jawBasePitch, 0f, 0f);
            jawPivot.localRotation = jawBaseLocalRot;
        }

        if (lowerJaw != null)
        {
            lowerJaw.localPosition = new Vector3(0f, lowerJawYOffset, 0f);
            lowerJaw.localScale = new Vector3(lowerJawResolvedWidth, lowerJawResolvedHeight, lowerJawResolvedLength);
        }
    }

    private void RecoverLegReferences()
    {
        legRigs.Clear();
        if (bodyPivot == null)
            return;

        int maxPairCount = 6;
        for (int pairIndex = 0; pairIndex < maxPairCount; pairIndex++)
        {
            TryRecoverLegRig(pairIndex, -1);
            TryRecoverLegRig(pairIndex, 1);
        }

        // Backward-compat fallback (old 4-leg hierarchy without anchors):
        if (legRigs.Count > 0)
            return;

        Transform legacyFL = visualRoot != null ? visualRoot.Find("Leg_FL") : null;
        Transform legacyFR = visualRoot != null ? visualRoot.Find("Leg_FR") : null;
        Transform legacyBL = visualRoot != null ? visualRoot.Find("Leg_BL") : null;
        Transform legacyBR = visualRoot != null ? visualRoot.Find("Leg_BR") : null;

        if (legacyFL != null)
            legRigs.Add(new LegRig { anchor = null, root = legacyFL, pairIndex = 0, sideSign = -1 });
        if (legacyFR != null)
            legRigs.Add(new LegRig { anchor = null, root = legacyFR, pairIndex = 0, sideSign = 1 });
        if (legacyBL != null)
            legRigs.Add(new LegRig { anchor = null, root = legacyBL, pairIndex = 1, sideSign = -1 });
        if (legacyBR != null)
            legRigs.Add(new LegRig { anchor = null, root = legacyBR, pairIndex = 1, sideSign = 1 });
    }

    private void TryRecoverLegRig(int pairIndex, int sideSign)
    {
        string anchorName = GetLegAnchorName(pairIndex, sideSign);
        Transform anchor = bodyPivot.Find(anchorName);
        if (anchor == null)
            return;

        string legName = GetLegName(pairIndex, sideSign);
        Transform legRoot = anchor.Find(legName);
        if (legRoot == null)
        {
            // Fallback for renamed children: first child named "Leg_*".
            for (int i = 0; i < anchor.childCount; i++)
            {
                Transform child = anchor.GetChild(i);
                if (child != null && child.name.StartsWith("Leg_"))
                {
                    legRoot = child;
                    break;
                }
            }
        }

        if (legRoot == null)
            return;

        legRigs.Add(new LegRig
        {
            anchor = anchor,
            root = legRoot,
            pairIndex = pairIndex,
            sideSign = sideSign
        });
    }

    private void ResolveJawDimensions(
        out float upperWidth,
        out float lowerWidth,
        out float upperHeight,
        out float lowerHeight,
        out float upperLength,
        out float lowerLength)
    {
        SplitUpperLowerByRatio(jawWidth, jawWidthUpperToLowerRatio, out upperWidth, out lowerWidth);
        SplitUpperLowerByRatio(jawHeight, jawHeightUpperToLowerRatio, out upperHeight, out lowerHeight);
        SplitUpperLowerByRatio(jawLength, jawLengthUpperToLowerRatio, out upperLength, out lowerLength);
    }

    private void ResolveJawVerticalOffsets(out float upperYOffset, out float lowerYOffset)
    {
        float halfSeparation = jawVerticalSeparation * 0.5f;
        upperYOffset = halfSeparation;
        lowerYOffset = -halfSeparation;
    }

    private static void SplitUpperLowerByRatio(
        float lowerBaseValue,
        float upperToLowerRatio,
        out float upper,
        out float lower)
    {
        lower = lowerBaseValue;
        upper = lowerBaseValue * upperToLowerRatio;
    }

    // Live-update leg structure (anchor positions + upper/ankle/foot dimensions).
    private void ApplyLegStructurePlacement()
    {
        Vector3 bodyLocalPos = ResolveBodyLocalPosition();
        Vector3 bodyLocalScale = ResolveBodyLocalScale();

        for (int i = 0; i < legRigs.Count; i++)
        {
            LegRig leg = legRigs[i];

            if (leg.anchor != null)
            {
                leg.anchor.localPosition = ResolveLegAnchorLocalPosition(leg.pairIndex, leg.sideSign, bodyLocalPos, bodyLocalScale);
                leg.anchor.localRotation = Quaternion.identity;
                leg.anchor.localScale = Vector3.one;
            }

            if (leg.root != null)
            {
                leg.root.localPosition = Vector3.zero;
                ApplyLegPartPlacement(leg.root);
            }
        }

        if (driveChasePhaseFromDisplacement)
            RecomputeChaseCalibration();
    }

    private void ApplyLegPartPlacement(Transform legRoot)
    {
        if (legRoot == null)
            return;

        Transform upper = legRoot.Find("Upper");
        Transform ankle = legRoot.Find("Ankle");
        Transform foot = legRoot.Find("Foot");

        float upperCenterY = upperLegLength * UpperLegCenterYRatio;
        float ankleCenterY = upperLegLength * AnkleCenterYRatio;
        float footCenterY = upperLegLength * FootCenterYRatio;
        float ankleForward = footWidth * AnkleForwardFromFootWidthRatio;
        float footForward = footWidth * FootForwardFromFootWidthRatio;
        float footDepth = footWidth * FootDepthFromFootWidthRatio;

        if (upper != null)
        {
            upper.localPosition = new Vector3(0f, upperCenterY, 0f);
            upper.localScale = new Vector3(upperLegThickness, upperLegLength, upperLegThickness);
        }

        if (ankle != null)
        {
            ankle.localPosition = new Vector3(0f, ankleCenterY, ankleForward);
            ankle.localScale = new Vector3(ankleDiameter, ankleDiameter, ankleDiameter);
        }

        if (foot != null)
        {
            foot.localPosition = new Vector3(0f, footCenterY, footForward);
            foot.localScale = new Vector3(footWidth, footHeight, footDepth);
        }
    }

    // Live-update tail transform from inspector parameters.
    private void ApplyTailPlacement()
    {
        Vector3 bodyLocalPos = ResolveBodyLocalPosition();
        Vector3 bodyLocalScale = ResolveBodyLocalScale();

        if (tailAnchor != null)
        {
            tailAnchor.localPosition = ResolveTailAnchorLocalPosition(bodyLocalPos, bodyLocalScale);
            tailAnchor.localRotation = Quaternion.identity;
            tailAnchor.localScale = Vector3.one;
        }

        if (tail == null)
            return;

        tail.localPosition = Vector3.zero;
        tailBaseLocalRot = Quaternion.Euler(tailPitchDegrees, 0f, 0f);
        tail.localRotation = tailBaseLocalRot;
        tail.localScale = GetTailEllipsoidScale();
    }

    private Vector3 GetTailEllipsoidScale()
    {
        // Tail dimensions are direct local scales per axis (same convention as other Width/Height/Length groups).
        return new Vector3(tailWidth, tailLength, tailHeight);
    }

    // Live-update ear transforms from inspector parameters.
    private void ApplyEarPlacement()
    {
        if (headPivot != null)
        {
            if (earAnchorL == null)
                earAnchorL = headPivot.Find("EarAnchor_L");
            if (earAnchorR == null)
                earAnchorR = headPivot.Find("EarAnchor_R");

            if (earL == null)
            {
                if (earAnchorL != null)
                    earL = earAnchorL.Find("Ear_L");
                if (earL == null)
                    earL = headPivot.Find("Ear_L");
            }

            if (earR == null)
            {
                if (earAnchorR != null)
                    earR = earAnchorR.Find("Ear_R");
                if (earR == null)
                    earR = headPivot.Find("Ear_R");
            }
        }

        Vector3 headLocalScale = ResolveHeadLocalScale();
        ResolveEarAnchorLocalPositions(headLocalScale, out Vector3 earAnchorPosL, out Vector3 earAnchorPosR);
        Vector3 earScale = ResolveEarLocalScale();

        if (earAnchorL != null)
        {
            earAnchorL.localPosition = earAnchorPosL;
            earAnchorL.localRotation = Quaternion.identity;
            earAnchorL.localScale = Vector3.one;
        }

        if (earAnchorR != null)
        {
            earAnchorR.localPosition = earAnchorPosR;
            earAnchorR.localRotation = Quaternion.identity;
            earAnchorR.localScale = Vector3.one;
        }

        if (earL != null)
        {
            earL.localPosition = Vector3.zero;
            earL.localRotation = Quaternion.Euler(0f, 0f, 18f);
            earL.localScale = earScale;
        }

        if (earR != null)
        {
            earR.localPosition = Vector3.zero;
            earR.localRotation = Quaternion.Euler(0f, 0f, -18f);
            earR.localScale = earScale;
        }
    }

    // Live-update eyes and pupils from inspector parameters.
    // This is intentionally separate from full rig rebuild for fast iteration.
    private void ApplyEyePlacement()
    {
        if (headPivot != null)
        {
            if (eyeAnchorL == null)
                eyeAnchorL = headPivot.Find("EyeAnchor_L");
            if (eyeAnchorR == null)
                eyeAnchorR = headPivot.Find("EyeAnchor_R");

            if (eyeL == null)
            {
                if (eyeAnchorL != null)
                    eyeL = eyeAnchorL.Find("Eye_L");
                if (eyeL == null)
                    eyeL = headPivot.Find("Eye_L");
            }

            if (eyeR == null)
            {
                if (eyeAnchorR != null)
                    eyeR = eyeAnchorR.Find("Eye_R");
                if (eyeR == null)
                    eyeR = headPivot.Find("Eye_R");
            }
        }

        Vector3 headLocalScale = ResolveHeadLocalScale();
        ResolveEyeAnchorLocalPositions(headLocalScale, out Vector3 eyeAnchorPosL, out Vector3 eyeAnchorPosR);

        if (eyeAnchorL != null)
        {
            eyeAnchorL.localPosition = eyeAnchorPosL;
            eyeAnchorL.localRotation = Quaternion.identity;
            eyeAnchorL.localScale = Vector3.one;
        }

        if (eyeAnchorR != null)
        {
            eyeAnchorR.localPosition = eyeAnchorPosR;
            eyeAnchorR.localRotation = Quaternion.identity;
            eyeAnchorR.localScale = Vector3.one;
        }

        eyeLBaseLocalPos = Vector3.zero;
        eyeRBaseLocalPos = Vector3.zero;
        SetEyeLocalPositionsImmediate(eyeLBaseLocalPos, eyeRBaseLocalPos);

        eyeBaseScale = ResolveEyeLocalScale();
        SetEyeScaleImmediate(eyeBaseScale);

        // Pupil values are expressed in eye local space (normalized to a unit sphere).
        pupilBaseScale = new Vector3(pupilScale, pupilScale, pupilScale);
        SetPupilScaleImmediate(pupilBaseScale);

        pupilBaseLocalPos = new Vector3(0f, 0f, pupilForwardOffset);
        SetPupilLocalPositionImmediate(pupilBaseLocalPos);
    }

    private void BuildLegRig(Vector3 bodyLocalPos, Vector3 bodyLocalScale)
    {
        legRigs.Clear();
        int pairCount = ResolveLegPairCount();
        for (int pairIndex = 0; pairIndex < pairCount; pairIndex++)
        {
            CreateLegRig(pairIndex, -1, bodyLocalPos, bodyLocalScale);
            CreateLegRig(pairIndex, 1, bodyLocalPos, bodyLocalScale);
        }
    }

    private void CreateLegRig(int pairIndex, int sideSign, Vector3 bodyLocalPos, Vector3 bodyLocalScale)
    {
        Transform anchor = CreateNode(GetLegAnchorName(pairIndex, sideSign), bodyPivot);
        anchor.localPosition = ResolveLegAnchorLocalPosition(pairIndex, sideSign, bodyLocalPos, bodyLocalScale);
        anchor.localRotation = Quaternion.identity;
        anchor.localScale = Vector3.one;

        Transform legRoot = CreateLeg(GetLegName(pairIndex, sideSign), anchor, Vector3.zero);
        legRigs.Add(new LegRig
        {
            anchor = anchor,
            root = legRoot,
            pairIndex = pairIndex,
            sideSign = sideSign
        });
    }

    private static string GetLegAnchorName(int pairIndex, int sideSign)
    {
        return $"LegAnchor_{pairIndex:00}_{(sideSign < 0 ? "L" : "R")}";
    }

    private static string GetLegName(int pairIndex, int sideSign)
    {
        return $"Leg_{pairIndex:00}_{(sideSign < 0 ? "L" : "R")}";
    }

    private Transform CreateLeg(string name, Transform parent, Vector3 localPos)
    {
        Transform legRoot = CreateNode(name, parent);
        legRoot.localPosition = localPos;
        legRoot.localRotation = Quaternion.identity;
        legRoot.localScale = Vector3.one;

        Transform upper = CreatePart(
            "Upper",
            PrimitiveType.Sphere,
            legRoot,
            new Vector3(0f, upperLegLength * UpperLegCenterYRatio, 0f),
            Vector3.zero,
            new Vector3(upperLegThickness, upperLegLength, upperLegThickness),
            CreatureMaterialSlot.Body);

        Transform ankle = CreatePart(
            "Ankle",
            PrimitiveType.Sphere,
            legRoot,
            new Vector3(0f, upperLegLength * AnkleCenterYRatio, footWidth * AnkleForwardFromFootWidthRatio),
            Vector3.zero,
            new Vector3(ankleDiameter, ankleDiameter, ankleDiameter),
            CreatureMaterialSlot.Body);

        Transform foot = CreatePart(
            "Foot",
            PrimitiveType.Sphere,
            legRoot,
            new Vector3(0f, upperLegLength * FootCenterYRatio, footWidth * FootForwardFromFootWidthRatio),
            Vector3.zero,
            new Vector3(footWidth, footHeight, footWidth * FootDepthFromFootWidthRatio),
            CreatureMaterialSlot.Body);

        RemoveCollider(upper.gameObject);
        RemoveCollider(ankle.gameObject);
        RemoveCollider(foot.gameObject);

        return legRoot;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebug)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.2f, 0.25f);
    }
}
