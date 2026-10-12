using UnityEngine;

/// <summary>Información mínima para probar el combate sin recursos de UI adicionales.</summary>
public sealed class PrototypeCombatHUD : MonoBehaviour
{
    [SerializeField] private PrototypePlayerAttack attack;
    [SerializeField] private PrototypeTrainingTarget target;

    public void Configure(PrototypePlayerAttack playerAttack, PrototypeTrainingTarget trainingTarget)
    {
        attack = playerAttack;
        target = trainingTarget;
    }

    private void OnGUI()
    {
        if (attack == null || target == null)
            return;
        string phase = attack.Phase == PrototypePlayerAttack.AttackPhase.Idle ? "Listo"
            : attack.Phase == PrototypePlayerAttack.AttackPhase.Windup ? "Preparación"
            : attack.Phase == PrototypePlayerAttack.AttackPhase.Active ? "Impacto" : "Recuperación";
        GUI.Box(new Rect(12f, 12f, 380f, 110f), "Primer ataque · H1");
        GUI.Label(new Rect(24f, 38f, 350f, 24f), "WASD / flechas: mover · Clic izquierdo / E: atacar");
        GUI.Label(new Rect(24f, 63f, 350f, 24f), "Ataque: " + phase + " · Objetivo: " + target.Health + "/" + target.MaximumHealth);
        GUI.Label(new Rect(24f, 88f, 350f, 24f), target.IsDefeated
            ? "Objetivo derrotado · R: reiniciar" : "Acercate y orientate al objetivo verde · R: reiniciar");
    }
}
