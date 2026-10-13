using System.Collections.Generic;
using UnityEngine;

/// <summary>Enemigo inmóvil: fija dirección, anuncia un golpe y deja una recuperación castigable.</summary>
[RequireComponent(typeof(PrototypeHealth))]
[DefaultExecutionOrder(-90)]
public sealed class PrototypeEnemyAttack : MonoBehaviour
{
    [SerializeField] private PrototypeHealth player;
    [SerializeField] private Renderer rangeIndicator;
    [SerializeField, Min(0.1f)] private float activationDistance = 2.2f;
    [SerializeField, Min(0.01f)] private float windupDuration = 0.8f;
    [SerializeField, Min(0.01f)] private float activeDuration = 0.15f;
    [SerializeField, Min(0.01f)] private float recoveryDuration = 1.1f;
    [SerializeField, Min(1)] private int damage = 20;
    [SerializeField] private Vector3 hitboxOffset = new Vector3(0f, -0.2f, 1.15f);
    [SerializeField] private Vector3 hitboxHalfExtents = new Vector3(0.7f, 0.85f, 0.7f);
    private readonly HashSet<PrototypeHealth> hitTargets = new HashSet<PrototypeHealth>();
    private MaterialPropertyBlock properties;
    private float remaining;
    private float phaseDuration;
    [SerializeField, Min(0.01f)] private float parryOpeningDuration = 1.2f;
    private float openingRemaining;
    public bool IsExposed => openingRemaining > 0f;
    public float ExposureElapsed => IsExposed ? parryOpeningDuration - openingRemaining : 0f;
    public Vector3 HitboxCenter => transform.TransformPoint(hitboxOffset);
    public Vector3 HitboxHalfExtents => hitboxHalfExtents;
    public float ActivationDistance => activationDistance;
    public PrototypeAttackPhase Phase { get; private set; }
    public float PhaseProgress => Phase == PrototypeAttackPhase.Idle ? 0f : Mathf.Clamp01(1f - remaining / phaseDuration);

    public void Configure(PrototypeHealth opponent, Renderer indicator)
    {
        player = opponent;
        rangeIndicator = indicator;
        CancelAttack();
    }

    private void Update() => AdvanceAt(Time.deltaTime, Time.timeAsDouble);

    private void Advance(float deltaTime) => AdvanceAt(deltaTime, Time.timeAsDouble);

    private void AdvanceAt(float deltaTime, double frameEndTime)
    {
        deltaTime = Mathf.Max(0f, deltaTime);
        double frameStartTime = frameEndTime - deltaTime;
        if (player == null || player.IsDefeated || GetComponent<PrototypeHealth>().IsDefeated)
        {
            CancelAttack();
            return;
        }
        if (IsExposed)
        {
            openingRemaining = Mathf.Max(0f, openingRemaining - Mathf.Max(0f, deltaTime));
            if (!IsExposed) SetPhase(PrototypeAttackPhase.Idle, 0f);
            return;
        }
        var defense = GetComponent<PrototypeEnemyParry>();
        if (defense != null && defense.isActiveAndEnabled)
        {
            if (defense.IsBusy) return;
            if (defense.TakeCounterStart(out double contactTime))
            {
                Vector3 direction = Vector3.ProjectOnPlane(player.transform.position - transform.position, Vector3.up);
                if (direction.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
                hitTargets.Clear(); SetPhase(PrototypeAttackPhase.Windup, windupDuration);
                deltaTime = Mathf.Clamp((float)(frameEndTime - contactTime), 0f, deltaTime);
            }
        }
        if (Phase == PrototypeAttackPhase.Idle)
        {
            Vector3 direction = Vector3.ProjectOnPlane(player.transform.position - transform.position, Vector3.up);
            if (direction.sqrMagnitude > activationDistance * activationDistance || direction.sqrMagnitude < 0.001f)
                return;
            if (defense != null && defense.isActiveAndEnabled)
            {
                if (defense.TryBeginAt(frameEndTime)) return;
                deltaTime = Mathf.Min(deltaTime, Mathf.Max(0f, (float)(frameEndTime - defense.BusyUntil)));
            }
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            hitTargets.Clear();
            SetPhase(PrototypeAttackPhase.Windup, windupDuration);
        }
        if (Phase == PrototypeAttackPhase.Active)
            DetectHits(frameStartTime);
        if (IsExposed) return;
        remaining -= Mathf.Max(0f, deltaTime);
        while (Phase != PrototypeAttackPhase.Idle && remaining <= 0f)
        {
            float overflow = -remaining;
            switch (Phase)
            {
                case PrototypeAttackPhase.Windup:
                    SetPhase(PrototypeAttackPhase.Active, activeDuration);
                    DetectHits(frameEndTime - overflow);
                    // La recepción puede cancelar este golpe. No consumir su overflow en la apertura nueva.
                    if (IsExposed) return;
                    break;
                case PrototypeAttackPhase.Active:
                    SetPhase(PrototypeAttackPhase.Recovery, recoveryDuration);
                    break;
                default:
                    SetPhase(PrototypeAttackPhase.Idle, 0f);
                    defense?.Rearm();
                    break;
            }
            remaining -= overflow;
        }
    }

    private void DetectHits(double contactTime) => PrototypeMeleeHitQuery.ApplyDamage(transform, hitboxOffset, hitboxHalfExtents, damage, hitTargets, contactTime);

    public void CancelAttack()
    {
        openingRemaining = 0f;
        hitTargets.Clear();
        SetPhase(PrototypeAttackPhase.Idle, 0f);
    }

    public bool ReceiveParry()
    {
        if (!isActiveAndEnabled || Phase != PrototypeAttackPhase.Active || IsExposed
            || GetComponent<PrototypeHealth>().IsDefeated) return false;
        CancelAttack();
        openingRemaining = parryOpeningDuration;
        GetComponent<PrototypeEnemyParry>()?.Rearm();
        if (rangeIndicator != null)
        {
            rangeIndicator.enabled = true;
            if (properties == null) properties = new MaterialPropertyBlock();
            properties.SetColor("_BaseColor", new Color(1f, 0.55f, 0.05f));
            rangeIndicator.SetPropertyBlock(properties);
        }
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
        if (properties == null)
            properties = new MaterialPropertyBlock();
        Color color = phase == PrototypeAttackPhase.Windup ? new Color(0.8f, 0.2f, 1f)
            : phase == PrototypeAttackPhase.Active ? new Color(1f, 0.15f, 0.15f) : new Color(0.2f, 0.7f, 0.9f);
        properties.SetColor("_BaseColor", color);
        rangeIndicator.SetPropertyBlock(properties);
    }

    private void OnDisable() => CancelAttack();
}
