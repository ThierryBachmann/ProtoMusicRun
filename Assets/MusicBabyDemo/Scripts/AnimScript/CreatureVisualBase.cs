using UnityEngine;

public enum CreatureVisualState
{
    Idle = 0,
    Chase = 1,
    Eat = 2,
    Stunned = 3,
    Wait = 4
}

public abstract class CreatureVisualBase : MonoBehaviour
{
    [Header("External State")]
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
            case MusicRun.CreatureState.EAT_ATTACK:
            case MusicRun.CreatureState.EAT_RECOVERY:
                return CreatureVisualState.Eat;
            case MusicRun.CreatureState.WAIT_PLAYER:
                return CreatureVisualState.Wait;
            case MusicRun.CreatureState.FOLLOW:
            case MusicRun.CreatureState.OVERTAKE:
            case MusicRun.CreatureState.HUNT:
            case MusicRun.CreatureState.RECENTER:
            case MusicRun.CreatureState.LEASH_RETURN:
                return CreatureVisualState.Chase;
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
