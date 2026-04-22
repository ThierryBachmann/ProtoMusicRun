using UnityEngine;

public enum CreatureVisualState
{
    Idle = 0,
    Follow = 1,
    Overtake = 2,
    Hunt = 3,
    Recenter = 4,
    WaitPlayer = 5,
    EatAttack = 6,
    EatRecovery = 7,
    LeashReturn = 8,
    Stunned = 9,
}

public abstract class CreatureVisualBase : MonoBehaviour
{
    [Header("External State")]
    [Tooltip("Visual state used by the animation graph (1:1 mapped from controller state when available).")]
    public CreatureVisualState state = CreatureVisualState.Idle;

    [Header("Runtime Context (Debug)")]
    [SerializeField] protected float runtimeSpeed;
    [SerializeField] protected bool runtimeGrounded;
    [SerializeField] protected bool runtimeHasTarget;
    [SerializeField] protected Vector3 runtimeGroundNormal = Vector3.up;

    protected CreatureVisualState previousState;
    protected float stateTimer;
    protected bool materialsDirty = true;

    protected virtual void Start()
    {
        materialsDirty = true;
        BuildIfNeeded(false);
        SyncPreviousState();
    }

    protected virtual void OnEnable()
    {
        materialsDirty = true;
        BuildIfNeeded(false);
        SyncPreviousState();
    }

    protected virtual void OnValidate()
    {
        materialsDirty = true;
        BuildIfNeeded(false);
    }

    protected virtual void Update()
    {
        BuildIfNeeded(false);

        if (Application.isPlaying)
        {
            if (previousState != state)
            {
                OnStateChanged(previousState, state);
                previousState = state;
            }

            stateTimer += Time.deltaTime;
        }

        UpdateAnimation();
    }

    public virtual void SetState(CreatureVisualState newState)
    {
        if (state == newState)
            return;

        CreatureVisualState old = state;
        state = newState;

        if (Application.isPlaying)
        {
            OnStateChanged(old, newState);
            previousState = newState;
        }
        else
        {
            previousState = newState;
        }
    }

    public virtual void SetControllerState(MusicRun.CreatureState controllerState)
    {
        SetState(MapControllerState(controllerState));
    }

    public virtual void SetMotionContext(float speed, bool grounded, Vector3 groundNormal)
    {
        runtimeSpeed = Mathf.Max(0f, speed);
        runtimeGrounded = grounded;
        runtimeGroundNormal = groundNormal.sqrMagnitude > 0.0001f ? groundNormal.normalized : Vector3.up;
    }

    public virtual void SetTargetContext(bool hasTarget)
    {
        runtimeHasTarget = hasTarget;
    }

    public virtual void RebuildVisual()
    {
        BuildIfNeeded(true);
        SyncPreviousState();
    }

    protected virtual CreatureVisualState MapControllerState(MusicRun.CreatureState controllerState)
    {
        switch (controllerState)
        {
            case MusicRun.CreatureState.FOLLOW:
                return CreatureVisualState.Follow;
            case MusicRun.CreatureState.OVERTAKE:
                return CreatureVisualState.Overtake;
            case MusicRun.CreatureState.HUNT:
                return CreatureVisualState.Hunt;
            case MusicRun.CreatureState.RECENTER:
                return CreatureVisualState.Recenter;
            case MusicRun.CreatureState.WAIT_PLAYER:
                return CreatureVisualState.WaitPlayer;
            case MusicRun.CreatureState.EAT_ATTACK:
                return CreatureVisualState.EatAttack;
            case MusicRun.CreatureState.EAT_RECOVERY:
                return CreatureVisualState.EatRecovery;
            case MusicRun.CreatureState.LEASH_RETURN:
                return CreatureVisualState.LeashReturn;
            default:
                return CreatureVisualState.Idle;
        }
    }

    protected void SyncPreviousState()
    {
        previousState = state;
        stateTimer = 0f;
    }

    protected virtual void OnStateChanged(CreatureVisualState oldState, CreatureVisualState newState)
    {
        stateTimer = 0f;
    }

    protected float DeltaTimeSafe()
    {
        return Application.isPlaying ? Time.deltaTime : 0.016f;
    }

    protected abstract void BuildIfNeeded(bool force);
    protected abstract void UpdateAnimation();
}
