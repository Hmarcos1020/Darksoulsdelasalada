using UnityEngine;

public enum PrototypeParryPhase { Ready, Windup, Active, Recovery }
public enum PrototypeParryResult { None, Pending, Success, Failed }

/// <summary>Ventana temporal frontal y contacto defensivo, independientes de la presentación.</summary>
[DefaultExecutionOrder(-130)]
public sealed class PrototypePlayerParry : MonoBehaviour
{
    [SerializeField, Min(0.01f), Tooltip("Tiempo desde la pulsación hasta habilitar defensa, en segundos de juego.")]
    private float windupDuration = 0.06f;
    [SerializeField, Min(0.01f), Tooltip("Duración de defensa válida. Valor inicial: 120 ms; ajustar después de probar.")]
    private float activeDuration = 0.12f;
    [SerializeField, Min(0.01f)] private float recoveryDuration = 0.30f;
    [SerializeField, Range(0f, 90f)] private float halfAngle = 60f;
    [SerializeField] private Renderer indicator;
    [SerializeField] private Renderer volumeOutline;
    [SerializeField] private BoxCollider defensiveVolume;
    private MaterialPropertyBlock properties;
    private double evaluatedAt, startedAt, activeStartsAt, activeEndsAt, actionEndsAt;
    private bool hasAttempt, consumed;
    public event System.Action<bool> Attempted;
    public event System.Action Succeeded;
    public event System.Action Failed;
    public PrototypeParryPhase Phase { get; private set; }
    public PrototypeParryResult Result { get; private set; }
    // No reemplazar una ventana histórica hasta resolver los contactos del frame actual.
    public bool IsBusy => Phase != PrototypeParryPhase.Ready || (hasAttempt && !consumed && evaluatedAt >= actionEndsAt);
    public bool LastSucceeded => Result == PrototypeParryResult.Success;
    public string FailureReason { get; private set; } = "";
    public double LastContactTime { get; private set; }
    public double PressLeadSeconds => LastContactTime - startedAt;
    public BoxCollider DefensiveVolume => defensiveVolume;

    public void Configure(Renderer renderer, BoxCollider volume, Renderer outline)
    {
        indicator = renderer; defensiveVolume = volume; volumeOutline = outline; CancelParry();
    }

    private bool CannotAct()
    {
        var health = GetComponent<PrototypeHealth>();
        var session = GetComponent<PrototypeDuelSession>();
        return !isActiveAndEnabled || (health != null && health.IsDefeated) || (session != null && session.HasEnded);
    }

    private void Update()
    {
        double now = Time.timeAsDouble;
        AdvanceTo(now);
        var input = GetComponent<PrototypePlayerInput>();
        // La pulsación pertenece a ahora: no se le presta el delta anterior del frame.
        if (input != null && input.ParryPressed) TryParryAt(now);
    }

    public bool TryParry() => TryParryAt(Time.timeAsDouble);

    private bool TryParryAt(double now)
    {
        var attack = GetComponent<PrototypePlayerAttack>();
        var dodge = GetComponent<PrototypePlayerDodge>();
        bool accepted = !CannotAct() && !IsBusy && (attack == null || !attack.IsAttacking) && (dodge == null || !dodge.IsBusy);
        var stamina = GetComponent<PrototypeStamina>();
        if (accepted && stamina != null) accepted = stamina.TrySpend(PrototypeStaminaAction.Parry);
        if (accepted)
        {
            startedAt = evaluatedAt = now;
            activeStartsAt = now + windupDuration;
            activeEndsAt = activeStartsAt + activeDuration;
            actionEndsAt = activeEndsAt + recoveryDuration;
            hasAttempt = true; consumed = false;
            Result = PrototypeParryResult.Pending; FailureReason = ""; LastContactTime = 0d;
            SetPhase(PrototypeParryPhase.Windup);
        }
        Attempted?.Invoke(accepted);
        return accepted;
    }

    // Comprueba el instante del contacto, no solo el estado al terminar el frame.
    public bool TryDeflect(PrototypeEnemyAttack attacker, double contactTime)
    {
        if (CannotAct() || !hasAttempt || consumed || attacker == null
            || contactTime < activeStartsAt || contactTime >= activeEndsAt) return false;
        Vector3 direction = Vector3.ProjectOnPlane(attacker.transform.position - transform.position, Vector3.up);
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        if (direction.sqrMagnitude < 0.000001f || Vector3.Dot(forward, direction.normalized) < Mathf.Cos(halfAngle * Mathf.Deg2Rad)) return false;
        if (!HasDefensiveContact(attacker) || !attacker.ReceiveParry()) return false;
        consumed = true; Result = PrototypeParryResult.Success;
        LastContactTime = contactTime;
        actionEndsAt = contactTime + recoveryDuration;
        AdvanceTo(evaluatedAt);
        Succeeded?.Invoke();
        return true;
    }

    private bool HasDefensiveContact(PrototypeEnemyAttack attacker)
    {
        if (defensiveVolume == null || !defensiveVolume.enabled || !defensiveVolume.gameObject.activeInHierarchy) return false;
        foreach (Collider contact in Physics.OverlapBox(attacker.HitboxCenter, attacker.HitboxHalfExtents,
            attacker.transform.rotation, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            if (contact == defensiveVolume) return true;
        return false;
    }

    public void NotifyHit(double contactTime)
    {
        if (!hasAttempt || consumed || contactTime < startedAt || contactTime >= actionEndsAt) return;
        LastContactTime = contactTime;
        actionEndsAt = contactTime + recoveryDuration;
        ResolveFailure("Golpe recibido");
        AdvanceTo(evaluatedAt);
    }

    private void Advance(float deltaTime) => AdvanceTo(evaluatedAt + Mathf.Max(0f, deltaTime));

    private void AdvanceTo(double now)
    {
        now = System.Math.Max(evaluatedAt, now);
        evaluatedAt = now;
        if (CannotAct()) { CancelParry(); return; }
        if (!hasAttempt) return;
        if (now >= actionEndsAt) SetPhase(PrototypeParryPhase.Ready);
        else if (consumed || now >= activeEndsAt) SetPhase(PrototypeParryPhase.Recovery);
        else if (now >= activeStartsAt) SetPhase(PrototypeParryPhase.Active);
        else SetPhase(PrototypeParryPhase.Windup);
    }

    // Espera a que el enemigo resuelva contactos de este frame antes de anunciar un fallo.
    private void LateUpdate() => FinalizeFeedback();
    private void FinalizeFeedback()
    {
        if (hasAttempt && !consumed && evaluatedAt >= activeEndsAt)
            ResolveFailure("Ventana agotada sin desvío");
    }

    private void ResolveFailure(string reason)
    {
        consumed = true; Result = PrototypeParryResult.Failed; FailureReason = reason;
        SetPhase(Phase);
        Failed?.Invoke();
    }

    public void CancelParry()
    {
        hasAttempt = consumed = false; Result = PrototypeParryResult.None; FailureReason = "";
        evaluatedAt = Time.timeAsDouble; LastContactTime = 0d;
        SetPhase(PrototypeParryPhase.Ready);
    }
    private void OnDisable() => CancelParry();

    private void SetPhase(PrototypeParryPhase phase)
    {
        Phase = phase;
        if (properties == null) properties = new MaterialPropertyBlock();
        Color color = phase == PrototypeParryPhase.Windup ? Color.yellow
            : phase == PrototypeParryPhase.Active ? new Color(0.1f, 0.5f, 1f)
            : LastSucceeded ? Color.white : new Color(1f, 0.25f, 0.1f);
        properties.SetColor("_BaseColor", color);
        if (indicator != null) { indicator.enabled = IsBusy; indicator.SetPropertyBlock(properties); }
        if (volumeOutline != null) { volumeOutline.enabled = IsBusy; volumeOutline.SetPropertyBlock(properties); }
    }

    private void OnDrawGizmosSelected()
    {
        if (defensiveVolume == null) return;
        Gizmos.color = Color.blue;
        Gizmos.matrix = defensiveVolume.transform.localToWorldMatrix;
        Gizmos.DrawWireCube(defensiveVolume.center, defensiveVolume.size);
    }
}
