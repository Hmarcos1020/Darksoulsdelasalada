using System.Collections.Generic;
using UnityEngine;

/// <summary>Consulta frontal y bloqueo por obstáculos, común a ambos ataques.</summary>
public static class PrototypeMeleeHitQuery
{
    public static event System.Action<PrototypeMeleeQuerySample> Completed;

    public static void ApplyDamage(Transform attacker, Vector3 offset, Vector3 halfExtents, int damage,
        HashSet<PrototypeHealth> hitTargets, double contactTime = double.NaN)
    {
        if (double.IsNaN(contactTime)) contactTime = Time.timeAsDouble;
        // Publica también la pose del trigger defensivo hijo antes de consultar contactos.
        Physics.SyncTransforms();
        Vector3 origin = attacker.position + Vector3.up * offset.y;
        Vector3 center = attacker.TransformPoint(offset);
        Quaternion rotation = attacker.rotation;
        int hitCount = 0;
        int appliedDamage = 0;
        int parryCount = 0;
        Collider[] contacts = Physics.OverlapBox(center, halfExtents, rotation,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        var enemyAttack = attacker.GetComponent<PrototypeEnemyAttack>();
        var playerAttack = attacker.GetComponent<PrototypePlayerAttack>();
        if (damage > 0 && (enemyAttack != null || playerAttack != null))
        {
            foreach (Collider contact in contacts)
            {
                var target = contact.GetComponentInParent<PrototypeHealth>();
                if (target == null || target.IsDefeated || hitTargets.Contains(target) || target.transform.IsChildOf(attacker)
                    || IsObstructed(attacker, origin, contact, target)) continue;
                bool deflected = false;
                var playerDefense = target.GetComponent<PrototypePlayerParry>();
                var enemyDefense = target.GetComponent<PrototypeEnemyParry>();
                if (enemyAttack != null && playerDefense != null && contact == playerDefense.DefensiveVolume)
                    deflected = playerDefense.TryDeflect(enemyAttack, contactTime);
                if (playerAttack != null && enemyDefense != null && contact == enemyDefense.DefensiveVolume)
                    deflected = enemyDefense.TryDeflect(playerAttack, contactTime);
                if (!deflected) continue;
                hitTargets.Add(target);
                Completed?.Invoke(new PrototypeMeleeQuerySample(attacker, center, halfExtents, rotation, 0, 0, 1, contactTime));
                return; // Desvío antes de daño corporal; el golpe completo quedó cancelado.
            }
        }
        foreach (Collider contact in contacts)
        {
            if (contact.isTrigger) continue; // Un volumen defensivo inactivo no amplía la hurtbox.
            var target = contact.GetComponentInParent<PrototypeHealth>();
            if (target == null || target.IsDefeated || hitTargets.Contains(target)
                || target.transform.IsChildOf(attacker) || IsObstructed(attacker, origin, contact, target))
                continue;
            var defense = target.GetComponent<PrototypePlayerParry>();
            int previousHealth = target.Health;
            if (target.TakeDamage(damage))
            {
                hitTargets.Add(target);
                hitCount++;
                appliedDamage += previousHealth - target.Health;
                defense?.NotifyHit(contactTime);
            }
        }
        Completed?.Invoke(new PrototypeMeleeQuerySample(attacker, center, halfExtents, rotation, hitCount, appliedDamage, parryCount, contactTime));
    }

    private static bool IsObstructed(Transform attacker, Vector3 origin, Collider contact, PrototypeHealth target)
    {
        Vector3 segment = contact.ClosestPoint(origin) - origin;
        float distance = segment.magnitude;
        if (distance < 0.001f)
            return false;
        foreach (RaycastHit hit in Physics.RaycastAll(origin, segment / distance, distance,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(attacker)
                || hit.collider.GetComponentInParent<PrototypeHealth>() == target)
                continue;
            return true;
        }
        return false;
    }
}

/// <summary>Snapshot del volumen realmente consultado y el daño efectivo de esa consulta.</summary>
public readonly struct PrototypeMeleeQuerySample
{
    public readonly Transform Attacker;
    public readonly Vector3 Center;
    public readonly Vector3 HalfExtents;
    public readonly Quaternion Rotation;
    public readonly int HitCount;
    public readonly int AppliedDamage;
    public readonly int ParryCount;
    public readonly double ContactTime;
    public PrototypeMeleeQuerySample(Transform attacker, Vector3 center, Vector3 halfExtents, Quaternion rotation, int hitCount, int damage, int parryCount = 0, double contactTime = double.NaN)
    {
        Attacker = attacker; Center = center; HalfExtents = halfExtents;
        Rotation = rotation; HitCount = hitCount; AppliedDamage = damage;
        ParryCount = parryCount;
        ContactTime = contactTime;
    }
}
