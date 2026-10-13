using UnityEngine;

/// <summary>Controles, vida, acciones y resultado del ejercicio.</summary>
public sealed class PrototypeCombatHUD : MonoBehaviour
{
    [SerializeField] private PrototypePlayerAttack attack;
    [SerializeField] private PrototypeHealth player;
    [SerializeField] private PrototypeHealth target;
    [SerializeField] private PrototypeEnemyAttack enemyAttack;

    public void Configure(PrototypePlayerAttack playerAttack, PrototypeHealth targetHealth)
    {
        attack = playerAttack;
        player = attack.GetComponent<PrototypeHealth>();
        target = targetHealth;
        enemyAttack = target.GetComponent<PrototypeEnemyAttack>();
    }

    private void OnGUI()
    {
        if (attack == null || target == null)
            return;
        var dodge = attack.GetComponent<PrototypePlayerDodge>();
        GUI.Box(new Rect(12f, 12f, 710f, 256f), "Duelo provisional · Parry de ambos");
        GUI.Label(new Rect(24f, 38f, 525f, 24f), "WASD / flechas: mover · Clic / E: atacar · R: reiniciar duelo");
        GUI.Label(new Rect(24f, 63f, 525f, 24f), "Espacio: esquivar · Sin dirección: retroceso · Sin invulnerabilidad");
        GUI.Label(new Rect(24f, 88f, 525f, 24f), "Tu vida: " + (player == null ? "—" : player.Health + "/" + player.MaximumHealth)
            + " · Enemigo: " + target.Health + "/" + target.MaximumHealth);
        var enemyDefense = enemyAttack != null ? enemyAttack.GetComponent<PrototypeEnemyParry>() : null;
        GUI.Label(new Rect(24f, 113f, 680f, 24f), "Tu ataque: " + (attack.IsStaggered ? "Desviado / recuperando" : PhaseLabel(attack.Phase)) + " · Enemigo: "
            + (enemyAttack == null ? "Inmóvil" : enemyAttack.IsExposed ? "Expuesto"
                : enemyDefense != null && enemyDefense.IsBusy ? "Defensa: " + ParryLabel(enemyDefense.Phase) : PhaseLabel(enemyAttack.Phase)));
        var parry = attack.GetComponent<PrototypePlayerParry>();
        GUI.Label(new Rect(24f, 138f, 680f, 24f), "Q / clic derecho: parry · " + (parry == null ? "Sin configurar" : ParryLabel(parry.Phase))
            + (parry != null && parry.LastSucceeded ? " · ¡ACIERTO!" : "")
            + (parry != null && parry.Result == PrototypeParryResult.Failed ? " · FALLO" : "")
            + " · Esquiva: " + (dodge == null || dodge.Phase == PrototypeDodgePhase.Ready ? "Lista"
                : dodge.Phase == PrototypeDodgePhase.Moving ? "Desplazamiento" : "Recuperación"));
        string message = player != null && player.IsDefeated ? "DERROTA · R para reiniciar"
            : target.IsDefeated ? "VICTORIA · R para reiniciar"
            : attack.IsStaggered ? "Tu golpe fue desviado: recuperate y prepará tu respuesta."
            : enemyAttack != null && enemyAttack.IsExposed ? "¡Parry! Enemigo expuesto: recuperate y atacá."
            : enemyDefense != null && enemyDefense.Phase == PrototypeParryPhase.Active ? "Defensa cian: esperá su recuperación o cambiá el ángulo."
            : enemyDefense != null && enemyDefense.Phase == PrototypeParryPhase.Windup ? "El enemigo prepara una defensa frontal."
            : enemyDefense != null && enemyDefense.Phase == PrototypeParryPhase.Recovery ? "La defensa se recupera: oportunidad para atacar."
            : enemyAttack != null && enemyAttack.Phase == PrototypeAttackPhase.Windup ? "¡Apartate! El área violeta anuncia el golpe enemigo."
            : enemyAttack != null && enemyAttack.Phase == PrototypeAttackPhase.Recovery ? "El enemigo se recupera: oportunidad para atacar."
            : "Acercate al enemigo verde para comenzar.";
        var stamina = attack.GetComponent<PrototypeStamina>();
        if (stamina != null)
        {
            Color previousColor = GUI.color;
            GUI.color = new Color(0.15f, 0.18f, 0.2f);
            GUI.DrawTexture(new Rect(24f, 168f, 295f, 14f), Texture2D.whiteTexture);
            float cheapest = Mathf.Min(stamina.Cost(PrototypeStaminaAction.Attack), stamina.Cost(PrototypeStaminaAction.Dodge), stamina.Cost(PrototypeStaminaAction.Parry));
            float highest = Mathf.Max(stamina.Cost(PrototypeStaminaAction.Attack), stamina.Cost(PrototypeStaminaAction.Dodge), stamina.Cost(PrototypeStaminaAction.Parry));
            GUI.color = stamina.Current < cheapest ? new Color(1f, 0.3f, 0.15f)
                : stamina.Current < highest ? new Color(1f, 0.8f, 0.2f) : new Color(0.25f, 0.9f, 0.35f);
            GUI.DrawTexture(new Rect(24f, 168f, 295f * Mathf.Clamp01(stamina.Current / stamina.Maximum), 14f), Texture2D.whiteTexture);
            GUI.color = previousColor;
            GUI.Label(new Rect(334f, 163f, 376f, 24f), "Stamina: " + Mathf.FloorToInt(stamina.Current) + "/" + stamina.Maximum.ToString("0")
                + " · E:" + stamina.Cost(PrototypeStaminaAction.Attack).ToString("0")
                + " Esp:" + stamina.Cost(PrototypeStaminaAction.Dodge).ToString("0")
                + " Q:" + stamina.Cost(PrototypeStaminaAction.Parry).ToString("0"));
            GUI.Label(new Rect(24f, 216f, 680f, 24f), stamina.RefusalMessage);
        }
        GUI.Label(new Rect(24f, 191f, 680f, 24f), message);
        var diagnostics = attack.GetComponent<PrototypeCombatDiagnostics>();
        if (diagnostics != null)
            GUI.Label(new Rect(24f, 241f, 680f, 24f), "F3: diagnóstico · F4: tiempo 0,2x · F5: guardar registro");
    }

    private static string PhaseLabel(PrototypeAttackPhase phase) => phase == PrototypeAttackPhase.Idle ? "Listo"
        : phase == PrototypeAttackPhase.Windup ? "Preparación" : phase == PrototypeAttackPhase.Active ? "Impacto" : "Recuperación";
    private static string ParryLabel(PrototypeParryPhase phase) => phase == PrototypeParryPhase.Ready ? "Listo"
        : phase == PrototypeParryPhase.Windup ? "Preparación" : phase == PrototypeParryPhase.Active ? "Ventana activa" : "Recuperación";
}
