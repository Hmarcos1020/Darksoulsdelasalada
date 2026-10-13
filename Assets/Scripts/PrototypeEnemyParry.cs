using UnityEngine;

/// <summary>Turno defensivo anunciado y fijo; no observa botones ni aprende repeticiones.</summary>
[RequireComponent(typeof(PrototypeEnemyAttack))]
[DefaultExecutionOrder(-110)]
public sealed class PrototypeEnemyParry : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float windupDuration = 0.25f;
    [SerializeField, Min(0.01f)] private float activeDuration = 0.20f;
    [SerializeField, Min(0.01f)] private float recoveryDuration = 0.55f;
    [SerializeField, Range(0f, 90f)] private float halfAngle = 60f;
    [SerializeField] private Renderer indicator;
    [SerializeField] private Renderer volumeOutline;
    [SerializeField] private BoxCollider defensiveVolume;
    [SerializeField] private PrototypeHealth opponent;
    private PrototypeHealth health;
    private MaterialPropertyBlock properties;
    private double startedAt, activeStartsAt, activeEndsAt, actionEndsAt, evaluatedAt, successAt;
    private bool hasAttempt, consumed, wantsTurn = true, pendingCounter;
    public event System.Action Started;
    public event System.Action Succeeded;
    public event System.Action Failed;
    public PrototypeParryPhase Phase { get; private set; }
    public PrototypeParryResult Result { get; private set; }
    public BoxCollider DefensiveVolume => defensiveVolume;
    public bool IsBusy => Phase != PrototypeParryPhase.Ready || (hasAttempt && !consumed && evaluatedAt >= actionEndsAt);
    public bool IsShowingSuccess => Result == PrototypeParryResult.Success && SuccessElapsed < 0.20f;
    public float SuccessElapsed => Result == PrototypeParryResult.Success ? Mathf.Max(0f, (float)(System.Math.Max(evaluatedAt, Time.timeAsDouble) - successAt)) : float.PositiveInfinity;
    public double ContactTime => successAt;
    public double BusyUntil => actionEndsAt;
    public float PhaseProgress => Phase == PrototypeParryPhase.Windup ? Progress(startedAt, activeStartsAt)
        : Phase == PrototypeParryPhase.Active ? Progress(activeStartsAt, activeEndsAt)
        : Phase == PrototypeParryPhase.Recovery ? Progress(activeEndsAt, actionEndsAt) : 0f;
    private float Progress(double start, double end) => Mathf.Clamp01((float)((evaluatedAt - start) / System.Math.Max(0.0001d, end - start)));

    private void Awake() { health = GetComponent<PrototypeHealth>(); CancelDefense(); }
    private void OnEnable()
    {
        if (health == null) health = GetComponent<PrototypeHealth>();
        health.ResetCompleted += CancelDefense;
    }
    private void OnDisable() { if (health != null) health.ResetCompleted -= CancelDefense; CancelDefense(); }
    public void Configure(PrototypeHealth player, Renderer cue, BoxCollider volume, Renderer outline)
    {
        opponent = player; indicator = cue; defensiveVolume = volume; volumeOutline = outline; CancelDefense();
    }
    private bool CannotAct() => !isActiveAndEnabled || opponent == null || opponent.IsDefeated
        || GetComponent<PrototypeHealth>().IsDefeated;
    private void Update() => AdvanceTo(Time.timeAsDouble);

    public bool TryBeginAt(double now)
    {
        var attack = GetComponent<PrototypeEnemyAttack>();
        if (CannotAct() || !wantsTurn || IsBusy || attack.Phase != PrototypeAttackPhase.Idle || attack.IsExposed) return false;
        Vector3 direction = Vector3.ProjectOnPlane(opponent.transform.position - transform.position, Vector3.up);
        if (direction.sqrMagnitude < 0.001f || direction.sqrMagnitude > attack.ActivationDistance * attack.ActivationDistance) return false;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        startedAt = evaluatedAt = now;
        activeStartsAt = now + windupDuration; activeEndsAt = activeStartsAt + activeDuration;
        actionEndsAt = activeEndsAt + recoveryDuration;
        hasAttempt = true; consumed = false; pendingCounter = false; wantsTurn = false;
        Result = PrototypeParryResult.Pending; Phase = PrototypeParryPhase.Windup;
        DrawSignals(); Started?.Invoke(); return true;
    }

    public bool TryDeflect(PrototypePlayerAttack attacker, double contactTime)
    {
        var attack = GetComponent<PrototypeEnemyAttack>();
        if (CannotAct() || !hasAttempt || consumed || attacker == null || attack.IsExposed
            || attack.Phase != PrototypeAttackPhase.Idle || contactTime < activeStartsAt || contactTime >= activeEndsAt
            || defensiveVolume == null || !defensiveVolume.enabled || !defensiveVolume.gameObject.activeInHierarchy) return false;
        Vector3 direction = Vector3.ProjectOnPlane(attacker.transform.position - transform.position, Vector3.up);
        if (direction.sqrMagnitude < 0.000001f || Vector3.Dot(transform.forward, direction.normalized) < Mathf.Cos(halfAngle * Mathf.Deg2Rad)) return false;
        bool contactFound = false;
        foreach (Collider contact in Physics.OverlapBox(attacker.HitboxCenter, attacker.HitboxHalfExtents, attacker.transform.rotation,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            if (contact == defensiveVolume) { contactFound = true; break; }
        if (!contactFound || !attacker.ReceiveParry(contactTime)) return false;
        consumed = true; Result = PrototypeParryResult.Success; successAt = contactTime;
        actionEndsAt = contactTime; pendingCounter = true;
        Phase = PrototypeParryPhase.Ready; DrawSignals(); Succeeded?.Invoke(); return true;
    }

    public bool TakeCounterStart(out double time)
    {
        time = successAt;
        if (!pendingCounter || CannotAct()) return false;
        pendingCounter = false; return true;
    }
    public void Rearm() { wantsTurn = true; }

    private void AdvanceTo(double now)
    {
        evaluatedAt = System.Math.Max(evaluatedAt, now);
        if (CannotAct()) { CancelDefense(); return; }
        if (hasAttempt && !consumed)
            Phase = evaluatedAt >= actionEndsAt ? PrototypeParryPhase.Ready
                : evaluatedAt >= activeEndsAt ? PrototypeParryPhase.Recovery
                : evaluatedAt >= activeStartsAt ? PrototypeParryPhase.Active : PrototypeParryPhase.Windup;
        else if (Result == PrototypeParryResult.Failed)
            Phase = evaluatedAt >= actionEndsAt ? PrototypeParryPhase.Ready : PrototypeParryPhase.Recovery;
        DrawSignals();
    }
    private void LateUpdate() => FinalizeFeedback();
    private void FinalizeFeedback()
    {
        if (!hasAttempt || consumed || evaluatedAt < activeEndsAt) return;
        consumed = true; Result = PrototypeParryResult.Failed; DrawSignals(); Failed?.Invoke();
    }
    public void CancelDefense()
    {
        hasAttempt = consumed = pendingCounter = false; wantsTurn = true;
        startedAt = activeStartsAt = activeEndsAt = actionEndsAt = successAt = 0d;
        evaluatedAt = Time.timeAsDouble; Phase = PrototypeParryPhase.Ready; Result = PrototypeParryResult.None; DrawSignals();
    }
    private void DrawSignals()
    {
        if (properties == null) properties = new MaterialPropertyBlock();
        Color color = IsShowingSuccess ? Color.white : Phase == PrototypeParryPhase.Windup ? new Color(1f, 0.7f, 0.1f)
            : Phase == PrototypeParryPhase.Active ? new Color(0.1f, 0.95f, 1f) : new Color(0.45f, 0.45f, 0.5f);
        properties.SetColor("_BaseColor", color);
        if (indicator != null) { indicator.enabled = IsBusy || IsShowingSuccess; indicator.SetPropertyBlock(properties); }
        if (volumeOutline != null) { volumeOutline.enabled = IsBusy || IsShowingSuccess; volumeOutline.SetPropertyBlock(properties); }
    }
    private void OnDrawGizmosSelected()
    {
        if (defensiveVolume == null) return;
        Gizmos.color = Color.cyan; Gizmos.matrix = defensiveVolume.transform.localToWorldMatrix;
        Gizmos.DrawWireCube(defensiveVolume.center, defensiveVolume.size);
    }
}
