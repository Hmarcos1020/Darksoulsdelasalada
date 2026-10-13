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
    public PrototypeAttackPhase Phase { get; private set; }
    public float PhaseProgress => Phase == PrototypeAttackPhase.Idle ? 0f : Mathf.Clamp01(1f - remaining / phaseDuration);

    public void Configure(PrototypeHealth opponent, Renderer indicator)
    {
        player = opponent;
        rangeIndicator = indicator;
        CancelAttack();
    }

    private void Update() => Advance(Time.deltaTime);

    private void Advance(float deltaTime)
    {
        if (player == null || player.IsDefeated || GetComponent<PrototypeHealth>().IsDefeated)
        {
            CancelAttack();
            return;
        }
        if (Phase == PrototypeAttackPhase.Idle)
        {
            Vector3 direction = Vector3.ProjectOnPlane(player.transform.position - transform.position, Vector3.up);
            if (direction.sqrMagnitude > activationDistance * activationDistance || direction.sqrMagnitude < 0.001f)
                return;
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            hitTargets.Clear();
            SetPhase(PrototypeAttackPhase.Windup, windupDuration);
        }
        if (Phase == PrototypeAttackPhase.Active)
            DetectHits();
        remaining -= Mathf.Max(0f, deltaTime);
        while (Phase != PrototypeAttackPhase.Idle && remaining <= 0f)
        {
            float overflow = -remaining;
            switch (Phase)
            {
                case PrototypeAttackPhase.Windup:
                    SetPhase(PrototypeAttackPhase.Active, activeDuration);
                    DetectHits();
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

    private void DetectHits() => PrototypeMeleeHitQuery.ApplyDamage(transform, hitboxOffset, hitboxHalfExtents, damage, hitTargets);

    public void CancelAttack()
    {
        hitTargets.Clear();
        SetPhase(PrototypeAttackPhase.Idle, 0f);
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
