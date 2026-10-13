using UnityEngine;

/// <summary>Reinicio del ejercicio y cancelación de acciones al terminar el duelo.</summary>
[DefaultExecutionOrder(-200)]
public sealed class PrototypeDuelSession : MonoBehaviour
{
    [SerializeField] private PrototypeHealth player;
    [SerializeField] private PrototypeHealth enemy;
    [SerializeField] private PrototypePlayerAttack playerAttack;
    [SerializeField] private PrototypeEnemyAttack enemyAttack;
    [SerializeField] private PrototypeFollowCamera followCamera;
    [SerializeField] private Vector3 playerSpawn;
    [SerializeField] private Quaternion playerRotation;
    [SerializeField] private Vector3 enemySpawn;
    [SerializeField] private Quaternion enemyRotation;
    public bool HasEnded => player != null && enemy != null && (player.IsDefeated || enemy.IsDefeated);

    public void Configure(PrototypeHealth playerHealth, PrototypeHealth enemyHealth, PrototypeFollowCamera camera)
    {
        player = playerHealth;
        enemy = enemyHealth;
        playerAttack = player.GetComponent<PrototypePlayerAttack>();
        enemyAttack = enemy.GetComponent<PrototypeEnemyAttack>();
        followCamera = camera;
        playerSpawn = player.transform.position;
        playerRotation = player.transform.rotation;
        enemySpawn = enemy.transform.position;
        enemyRotation = enemy.transform.rotation;
    }

    private void Start() => Restart();

    private void Update()
    {
        var commands = player != null ? player.GetComponent<PrototypePlayerInput>() : null;
        if (commands != null && commands.RestartPressed)
            Restart();
        if (player != null && enemy != null && (player.IsDefeated || enemy.IsDefeated))
        {
            player.GetComponent<PrototypePlayerDodge>()?.CancelDodge();
            player.GetComponent<PrototypePlayerParry>()?.CancelParry();
            playerAttack.CancelAttack();
            enemyAttack.CancelAttack();
            enemy.GetComponent<PrototypeEnemyParry>()?.CancelDefense();
        }
    }

    public void Restart()
    {
        if (player == null || enemy == null)
            return;
        player.GetComponent<PrototypePlayerDodge>()?.CancelDodge();
        player.GetComponent<PrototypePlayerParry>()?.CancelParry();
        playerAttack.CancelAttack();
        enemyAttack.CancelAttack();
        var controller = player.GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = false;
        player.transform.SetPositionAndRotation(playerSpawn, playerRotation);
        if (controller != null)
            controller.enabled = true;
        enemy.transform.SetPositionAndRotation(enemySpawn, enemyRotation);
        player.ResetHealth();
        enemy.ResetHealth();
        var movement = player.GetComponent<PrototypePlayerMovement>();
        if (movement != null)
            movement.ResetMovement();
        if (followCamera != null)
            followCamera.Configure(player.transform);
    }
}
