using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Pruebas del atacante inmóvil, daño compartido, derrota y reinicio.</summary>
public static class PrototypeDuelValidation
{
    [MenuItem("Tools/Soulslike/Validar duelo inmóvil")]
    public static void Validate()
    {
        var previous = SceneManager.GetActiveScene();
        var temporary = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(temporary);
        try
        {
            var enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyObject.transform.position = new Vector3(200f, 1f, 200f);
            var enemy = enemyObject.AddComponent<PrototypeEnemyAttack>();
            var enemyHealth = enemyObject.GetComponent<PrototypeHealth>();
            enemyHealth.ResetHealth();
            var playerObject = new GameObject("Jugador de prueba");
            var controller = playerObject.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.center = Vector3.zero;
            var player = playerObject.AddComponent<PrototypeHealth>();
            player.ResetHealth();
            var playerAttack = playerObject.AddComponent<PrototypePlayerAttack>();
            var movement = playerObject.AddComponent<PrototypePlayerMovement>();
            var cameraObject = new GameObject("Cámara de prueba");
            movement.Configure(cameraObject.transform);
            Invoke(movement, "Awake");
            playerObject.transform.position = enemyObject.transform.position + Vector3.forward * 4f;
            enemy.Configure(player, null);
            Physics.SyncTransforms();
            Step(enemy, 1f);
            Check(enemy.Phase == PrototypeAttackPhase.Idle && player.Health == 100, "Ataca fuera de activación");
            playerObject.transform.position = enemyObject.transform.position + Vector3.forward * 1.5f;
            Physics.SyncTransforms();
            Step(enemy, 0.3f);
            Check(enemy.Phase == PrototypeAttackPhase.Windup && player.Health == 100, "No anuncia el golpe");
            Step(enemy, 0.51f);
            Check(player.Health == 80 && enemyHealth.Health == 100, "Daño incorrecto o daño propio");
            Step(enemy, 0.03f);
            Check(player.Health == 80, "Daño repetido durante ventana activa");
            Step(enemy, 0.2f);
            Check(enemy.Phase == PrototypeAttackPhase.Recovery, "Sin recuperación");
            Step(enemy, 1.2f);
            Check(enemy.Phase == PrototypeAttackPhase.Idle, "No finaliza ataque");

            player.ResetHealth();
            Step(enemy, 0.3f);
            Quaternion lockedAim = enemyObject.transform.rotation;
            playerObject.transform.position = enemyObject.transform.position + Vector3.right * 1.5f;
            Physics.SyncTransforms();
            Step(enemy, 1.8f);
            Check(player.Health == 100 && Quaternion.Angle(lockedAim, enemyObject.transform.rotation) < 0.01f, "Sigue al jugador durante preparación o golpea fuera de dirección");
            playerObject.transform.position = enemyObject.transform.position + Vector3.forward * 1.5f;
            Physics.SyncTransforms();
            Step(enemy, 0.3f);
            playerObject.transform.position = enemyObject.transform.position + Vector3.forward * 4f;
            Physics.SyncTransforms();
            Step(enemy, 1.8f);
            Check(player.Health == 100, "Salir de alcance no evita el impacto");

            playerObject.transform.position = enemyObject.transform.position + Vector3.forward * 1.5f;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = enemyObject.transform.position + Vector3.forward * 0.8f;
            wall.transform.localScale = new Vector3(2f, 2f, 0.15f);
            Physics.SyncTransforms();
            Step(enemy, 2.1f);
            Check(player.Health == 100, "Ataca a través de una pared");
            wall.SetActive(false);
            Physics.SyncTransforms();
            foreach (float dt in new[] { 1f / 30f, 1f / 120f })
            {
                enemy.CancelAttack();
                player.ResetHealth();
                for (int frame = 0; frame < Mathf.CeilToInt(2.1f / dt); frame++)
                    Step(enemy, dt);
                Check(player.Health == 80, "Daño depende del framerate");
            }
            enemy.CancelAttack();
            player.ResetHealth();
            Step(enemy, 0.3f);
            enemyHealth.TakeDamage(100);
            Step(enemy, 2f);
            Check(player.Health == 100 && enemy.Phase == PrototypeAttackPhase.Idle, "Enemigo derrotado conserva ataque pendiente");
            enemyHealth.ResetHealth();
            for (int hit = 0; hit < 5; hit++)
                Step(enemy, 2.1f);
            Check(player.IsDefeated && player.Health == 0 && !playerAttack.TryAttack(), "Derrota no bloquea el ataque del jugador");
            Step(enemy, 2.1f);
            Check(player.Health == 0 && enemy.Phase == PrototypeAttackPhase.Idle, "Sigue atacando al jugador derrotado");
            Vector3 before = playerObject.transform.position;
            Invoke(movement, "AdvanceMovement", Vector2.up, 0.1f);
            Check(Vector3.ProjectOnPlane(playerObject.transform.position - before, Vector3.up).sqrMagnitude < 0.0001f, "Jugador derrotado puede desplazarse");
            var session = playerObject.AddComponent<PrototypeDuelSession>();
            session.Configure(player, enemyHealth, null);
            Vector3 spawn = playerObject.transform.position;
            playerObject.transform.position += Vector3.right * 2f;
            session.Restart();
            Physics.SyncTransforms();
            Check(player.Health == 100 && enemyHealth.Health == 100 && Vector3.Distance(playerObject.transform.position, spawn) < 0.01f
                && enemy.Phase == PrototypeAttackPhase.Idle && !playerAttack.IsAttacking && !session.HasEnded, "Reinicio incompleto");
            before = playerObject.transform.position;
            Invoke(movement, "AdvanceMovement", Vector2.up, 0.1f);
            Check(Vector3.ProjectOnPlane(playerObject.transform.position - before, Vector3.up).sqrMagnitude > 0.01f, "Reinicio no devuelve movimiento");
            Check(playerAttack.TryAttack(), "Reinicio no devuelve ataque");
            enemyHealth.TakeDamage(100);
            playerAttack.CancelAttack();
            Check(session.HasEnded && !playerAttack.TryAttack(), "Victoria no bloquea ataques adicionales");
            Debug.Log("DUEL_VALIDATION_SUCCEEDED: activación, aviso, daño único, dirección fija, evasión caminando, paredes, 30/120 FPS, derrota, movimiento y reinicio.");
        }
        finally
        {
            EditorSceneManager.CloseScene(temporary, true);
            if (previous.IsValid() && previous.isLoaded)
                SceneManager.SetActiveScene(previous);
        }
    }

    private static void Step(PrototypeEnemyAttack enemy, float dt) => Invoke(enemy, "Advance", dt);
    public static void CaptureEnemyPhases()
    {
        const string scenePath = "Assets/Scenes/PrototypeArena.unity";
        EditorSceneManager.OpenScene(scenePath);
        var session = UnityEngine.Object.FindFirstObjectByType<PrototypeDuelSession>();
        var enemy = UnityEngine.Object.FindFirstObjectByType<PrototypeEnemyAttack>();
        var player = UnityEngine.Object.FindFirstObjectByType<PrototypePlayerAttack>();
        var enemyHealth = enemy.GetComponent<PrototypeHealth>();
        var targetColor = enemy.GetComponent<PrototypeTrainingTarget>();
        try
        {
            targetColor.Configure(enemyHealth);
            foreach (var feedback in UnityEngine.Object.FindObjectsByType<PrototypeHitPresentation>(FindObjectsSortMode.None))
                Invoke(feedback, "OnEnable");
            session.Restart();
            var controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = enemy.transform.position + Vector3.back * 1.4f + Vector3.up * 0.05f;
            controller.enabled = true;
            Camera camera = Camera.main;
            camera.GetComponent<PrototypeFollowCamera>().Configure(player.transform);
            Physics.SyncTransforms();
            Step(enemy, 0.35f);
            Invoke(enemy.GetComponent<PrototypeAttackPresentation>(), "ApplyPose");
            PrototypeCombatVisualValidation.Capture(camera, "01-Aviso", "Logs/Duel");
            Step(enemy, 0.5f);
            Invoke(enemy.GetComponent<PrototypeAttackPresentation>(), "ApplyPose");
            PrototypeCombatVisualValidation.Capture(camera, "02-Impacto", "Logs/Duel");
            Step(enemy, 0.2f);
            Invoke(enemy.GetComponent<PrototypeAttackPresentation>(), "ApplyPose");
            PrototypeCombatVisualValidation.Capture(camera, "03-Recuperacion", "Logs/Duel");
            Debug.Log("DUEL_CAPTURE_SUCCEEDED: aviso, impacto y recuperación en Logs/Duel; sin UI OnGUI.");
        }
        finally
        {
            EditorSceneManager.OpenScene(scenePath);
        }
    }

    private static void Invoke(object component, string method, params object[] arguments)
        => component.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component, arguments);
    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("DUEL_VALIDATION_FAILED: " + message);
    }
}
