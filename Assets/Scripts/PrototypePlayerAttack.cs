using System.Collections.Generic;
using UnityEngine;

/// <summary>Ataque frontal: preparación, impacto y recuperación sin encadenar entradas.</summary>
[DefaultExecutionOrder(-100)]
public sealed class PrototypePlayerAttack : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float windupDuration = 0.18f;
    [SerializeField, Min(0.01f)] private float activeDuration = 0.12f;
    [SerializeField, Min(0.01f)] private float recoveryDuration = 0.35f;
    [SerializeField, Min(1)] private int damage = 25;
    [SerializeField] private Vector3 hitboxOffset = new Vector3(0f, -0.2f, 1.15f);
    [SerializeField] private Vector3 hitboxHalfExtents = new Vector3(0.7f, 0.85f, 0.7f);
    [SerializeField] private Renderer rangeIndicator;

    private readonly HashSet<PrototypeHealth> hitTargets = new HashSet<PrototypeHealth>();
    private MaterialPropertyBlock indicatorProperties;
    private float remaining;
    private float phaseDuration;
    [SerializeField, Min(0.01f)] private float deflectedRecoveryDuration = 0.60f;
    private bool staggered;
    private double staggerStart, staggerUntil, evaluatedAt;
    private int attackSerial;
    public bool IsStaggered => staggered && System.Math.Max(evaluatedAt, Time.timeAsDouble) < staggerUntil;
    public float StaggerElapsed => IsStaggered ? Mathf.Max(0f, (float)(System.Math.Max(evaluatedAt, Time.timeAsDouble) - staggerStart)) : 0f;
    public float StaggerDuration => deflectedRecoveryDuration;
    public Vector3 HitboxCenter => transform.TransformPoint(hitboxOffset);
    public Vector3 HitboxHalfExtents => hitboxHalfExtents;
    public PrototypeAttackPhase Phase { get; private set; }
    public bool IsAttacking => Phase != PrototypeAttackPhase.Idle || IsStaggered;
    // Lectura para presentación: usa el mismo reloj que gobierna los impactos.
    public float PhaseProgress => Phase != PrototypeAttackPhase.Idle ? Mathf.Clamp01(1f - remaining / phaseDuration) : 0f;

    public void Configure(Renderer indicator) => rangeIndicator = indicator;

    private void Update()
    {
        double now = Time.timeAsDouble;
        RefreshStagger(now);
        var commands = GetComponent<PrototypePlayerInput>();
        bool started = commands != null && commands.AttackPressed && TryAttack();
        AdvanceAt(started ? 0f : Time.deltaTime, now);
    }

    public bool TryAttack()
    {
        var health = GetComponent<PrototypeHealth>();
        var duel = GetComponent<PrototypeDuelSession>();
        var dodge = GetComponent<PrototypePlayerDodge>();
        if (GetComponent<PrototypePlayerParry>() is PrototypePlayerParry parry && parry.IsBusy) return false;
        if (!isActiveAndEnabled || IsAttacking || (dodge != null && dodge.IsBusy) || (health != null && health.IsDefeated) || (duel != null && duel.HasEnded))
            return false;
        var stamina = GetComponent<PrototypeStamina>();
        if (stamina != null && !stamina.TrySpend(PrototypeStaminaAction.Attack)) return false;
        hitTargets.Clear();
        SetPhase(PrototypeAttackPhase.Windup, windupDuration);
        return true;
    }

    // Separado de la lectura de entrada para validar tiempos con pasos controlados.
    private void Advance(float deltaTime) => AdvanceAt(deltaTime, Time.timeAsDouble);

    private void AdvanceAt(float deltaTime, double frameEndTime)
    {
        deltaTime = Mathf.Max(0f, deltaTime);
        RefreshStagger(frameEndTime);
        var health = GetComponent<PrototypeHealth>();
        if (health != null && health.IsDefeated)
        {
            CancelAttack();
            return;
        }
        if (IsStaggered || Phase == PrototypeAttackPhase.Idle)
            return;
        if (Phase == PrototypeAttackPhase.Active)
            if (DetectHits(frameEndTime - deltaTime)) return;
        remaining -= Mathf.Max(0f, deltaTime);
        while (IsAttacking && remaining <= 0f)
        {
            float overflow = -remaining;
            switch (Phase)
            {
                case PrototypeAttackPhase.Windup:
                    SetPhase(PrototypeAttackPhase.Active, activeDuration);
                    // También muestrea si un fotograma largo atraviesa toda la ventana activa.
                    if (DetectHits(frameEndTime - overflow)) return;
                    break;
                case PrototypeAttackPhase.Active:
                    SetPhase(PrototypeAttackPhase.Recovery, recoveryDuration);
                    break;
                default:
                    SetPhase(PrototypeAttackPhase.Idle, 0f);
                    break;
            }
            remaining -= overflow;
        }
    }

    private bool DetectHits(double contactTime)
    {
        int serial = attackSerial;
        PrototypeMeleeHitQuery.ApplyDamage(transform, hitboxOffset, hitboxHalfExtents, damage, hitTargets, contactTime);
        return serial != attackSerial;
    }
    private void RefreshStagger(double now)
    {
        evaluatedAt = now;
        if (staggered && now >= staggerUntil) staggered = false;
    }
    public bool ReceiveParry(double contactTime)
    {
        var health = GetComponent<PrototypeHealth>();
        if (!isActiveAndEnabled || Phase != PrototypeAttackPhase.Active || (health != null && health.IsDefeated)) return false;
        CancelAttack();
        staggerStart = contactTime; staggerUntil = contactTime + deflectedRecoveryDuration; staggered = true;
        RefreshStagger(evaluatedAt);
        return true;
    }

    private void SetPhase(PrototypeAttackPhase phase, float duration)
    {
        Phase = phase;
        phaseDuration = Mathf.Max(0.01f, duration);
        remaining = phaseDuration;
        if (rangeIndicator == null)
            return;
        rangeIndicator.enabled = phase != PrototypeAttackPhase.Idle;
        if (indicatorProperties == null)
            indicatorProperties = new MaterialPropertyBlock();
        Color color = phase == PrototypeAttackPhase.Windup ? Color.yellow
            : phase == PrototypeAttackPhase.Active ? Color.red : new Color(0.2f, 0.6f, 1f);
        indicatorProperties.SetColor("_BaseColor", color);
        rangeIndicator.SetPropertyBlock(indicatorProperties);
    }

    public void CancelAttack()
    {
        attackSerial++; staggered = false;
        hitTargets.Clear();
        SetPhase(PrototypeAttackPhase.Idle, 0f);
    }

    private void OnDisable() => CancelAttack();

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(hitboxOffset, hitboxHalfExtents * 2f);
    }
}
