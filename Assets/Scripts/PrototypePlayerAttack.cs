using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Ataque frontal: preparación, impacto y recuperación sin encadenar entradas.</summary>
[DefaultExecutionOrder(-100)]
public sealed class PrototypePlayerAttack : MonoBehaviour
{
    public enum AttackPhase { Idle, Windup, Active, Recovery }

    [SerializeField, Min(0.01f)] private float windupDuration = 0.18f;
    [SerializeField, Min(0.01f)] private float activeDuration = 0.12f;
    [SerializeField, Min(0.01f)] private float recoveryDuration = 0.35f;
    [SerializeField, Min(1)] private int damage = 25;
    [SerializeField] private Vector3 hitboxOffset = new Vector3(0f, -0.2f, 1.15f);
    [SerializeField] private Vector3 hitboxHalfExtents = new Vector3(0.7f, 0.85f, 0.7f);
    [SerializeField] private Renderer rangeIndicator;

    private readonly HashSet<PrototypeTrainingTarget> hitTargets = new HashSet<PrototypeTrainingTarget>();
    private MaterialPropertyBlock indicatorProperties;
    private float remaining;
    public AttackPhase Phase { get; private set; }
    public bool IsAttacking => Phase != AttackPhase.Idle;

    public void Configure(Renderer indicator) => rangeIndicator = indicator;

    private void Update()
    {
        bool pressed = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            || (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame);
        if (pressed)
            TryAttack();
        Advance(Time.deltaTime);
    }

    public bool TryAttack()
    {
        if (!isActiveAndEnabled || IsAttacking)
            return false;
        hitTargets.Clear();
        SetPhase(AttackPhase.Windup, windupDuration);
        return true;
    }

    // Separado de la lectura de entrada para validar tiempos con pasos controlados.
    private void Advance(float deltaTime)
    {
        if (!IsAttacking)
            return;
        if (Phase == AttackPhase.Active)
            DetectHits();
        remaining -= Mathf.Max(0f, deltaTime);
        while (IsAttacking && remaining <= 0f)
        {
            float overflow = -remaining;
            switch (Phase)
            {
                case AttackPhase.Windup:
                    SetPhase(AttackPhase.Active, activeDuration);
                    // También muestrea si un fotograma largo atraviesa toda la ventana activa.
                    DetectHits();
                    break;
                case AttackPhase.Active:
                    SetPhase(AttackPhase.Recovery, recoveryDuration);
                    break;
                default:
                    SetPhase(AttackPhase.Idle, 0f);
                    break;
            }
            remaining -= overflow;
        }
    }

    private void DetectHits()
    {
        Vector3 origin = transform.position + Vector3.up * hitboxOffset.y;
        Vector3 center = transform.TransformPoint(hitboxOffset);
        Collider[] contacts = Physics.OverlapBox(center, hitboxHalfExtents, transform.rotation,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        foreach (Collider contact in contacts)
        {
            PrototypeTrainingTarget target = contact.GetComponentInParent<PrototypeTrainingTarget>();
            if (target == null || target.IsDefeated || hitTargets.Contains(target)
                || target.transform.IsChildOf(transform))
                continue;
            if (IsObstructed(origin, contact, target))
                continue;
            hitTargets.Add(target);
            target.TakeDamage(damage);
        }
    }

    private bool IsObstructed(Vector3 origin, Collider contact, PrototypeTrainingTarget target)
    {
        Vector3 segment = contact.ClosestPoint(origin) - origin;
        float distance = segment.magnitude;
        if (distance < 0.001f)
            return false;
        foreach (RaycastHit hit in Physics.RaycastAll(origin, segment / distance, distance,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(transform)
                || hit.collider.GetComponentInParent<PrototypeTrainingTarget>() == target)
                continue;
            return true;
        }
        return false;
    }

    private void SetPhase(AttackPhase phase, float duration)
    {
        Phase = phase;
        remaining = Mathf.Max(0.01f, duration);
        if (rangeIndicator == null)
            return;
        rangeIndicator.enabled = phase != AttackPhase.Idle;
        if (indicatorProperties == null)
            indicatorProperties = new MaterialPropertyBlock();
        Color color = phase == AttackPhase.Windup ? Color.yellow
            : phase == AttackPhase.Active ? Color.red : new Color(0.2f, 0.6f, 1f);
        indicatorProperties.SetColor("_BaseColor", color);
        rangeIndicator.SetPropertyBlock(indicatorProperties);
    }

    private void OnDisable()
    {
        hitTargets.Clear();
        SetPhase(AttackPhase.Idle, 0f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(hitboxOffset, hitboxHalfExtents * 2f);
    }
}
