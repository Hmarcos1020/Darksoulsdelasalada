using System.Collections.Generic;
using UnityEngine;

/// <summary>Consulta frontal y bloqueo por obstáculos, común a ambos ataques.</summary>
public static class PrototypeMeleeHitQuery
{
    public static void ApplyDamage(Transform attacker, Vector3 offset, Vector3 halfExtents, int damage,
        HashSet<PrototypeHealth> hitTargets)
    {
        Vector3 origin = attacker.position + Vector3.up * offset.y;
        Collider[] contacts = Physics.OverlapBox(attacker.TransformPoint(offset), halfExtents, attacker.rotation,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        foreach (Collider contact in contacts)
        {
            var target = contact.GetComponentInParent<PrototypeHealth>();
            if (target == null || target.IsDefeated || hitTargets.Contains(target)
                || target.transform.IsChildOf(attacker) || IsObstructed(attacker, origin, contact, target))
                continue;
            if (target.TakeDamage(damage))
                hitTargets.Add(target);
        }
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
