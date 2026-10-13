using UnityEngine;

/// <summary>Vida, fases y resultado del ejercicio de duelo.</summary>
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
        GUI.Box(new Rect(12f, 12f, 460f, 155f), "Duelo provisional · Enemigo inmóvil");
        GUI.Label(new Rect(24f, 38f, 435f, 24f), "WASD / flechas: mover · Clic / E: atacar · R: reiniciar duelo");
        GUI.Label(new Rect(24f, 63f, 435f, 24f), "Tu vida: " + (player == null ? "—" : player.Health + "/" + player.MaximumHealth)
            + " · Enemigo: " + target.Health + "/" + target.MaximumHealth);
        GUI.Label(new Rect(24f, 88f, 435f, 24f), "Tu ataque: " + PhaseLabel(attack.Phase) + " · Enemigo: "
            + (enemyAttack == null ? "Inmóvil" : PhaseLabel(enemyAttack.Phase)));
        string message = player != null && player.IsDefeated ? "DERROTA · R para reiniciar"
            : target.IsDefeated ? "VICTORIA · R para reiniciar"
            : enemyAttack != null && enemyAttack.Phase == PrototypeAttackPhase.Windup ? "¡Apartate! El área violeta anuncia el golpe enemigo."
            : enemyAttack != null && enemyAttack.Phase == PrototypeAttackPhase.Recovery ? "El enemigo se recupera: oportunidad para atacar."
            : "Acercate al enemigo verde para comenzar.";
        GUI.Label(new Rect(24f, 116f, 435f, 35f), message);
    }

    private static string PhaseLabel(PrototypeAttackPhase phase) => phase == PrototypeAttackPhase.Idle ? "Listo"
        : phase == PrototypeAttackPhase.Windup ? "Preparación" : phase == PrototypeAttackPhase.Active ? "Impacto" : "Recuperación";
}
