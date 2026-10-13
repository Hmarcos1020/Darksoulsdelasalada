using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Regresión de desplazamiento con CharacterController en escena temporal.</summary>
public static class PrototypeDodgeValidation
{
    [MenuItem("Tools/Soulslike/Validar primera esquiva")]
    public static void Validate()
    {
        var previous = SceneManager.GetActiveScene();
        var temporary = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(temporary);
        SimulationMode previousSimulation = Physics.simulationMode;
        try
        {
            // En edición no hay FixedUpdate. Un paso físico publica las poses nativas del controller.
            Physics.simulationMode = SimulationMode.Script;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(300f, -0.5f, 300f);
            floor.transform.localScale = new Vector3(30f, 1f, 30f);
            var actor = new GameObject("Jugador de prueba de esquiva");
            var controller = actor.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.center = Vector3.zero;
            controller.skinWidth = 0.02f;
            var health = actor.AddComponent<PrototypeHealth>();
            var attack = actor.AddComponent<PrototypePlayerAttack>();
            var movement = actor.AddComponent<PrototypePlayerMovement>();
            var dodge = actor.AddComponent<PrototypePlayerDodge>();
            var camera = new GameObject("Cámara de prueba de esquiva");
            movement.Configure(camera.transform);
            Invoke(movement, "Awake");
            Invoke(dodge, "Awake");
            Vector3 origin = new Vector3(300f, 1.05f, 300f);

            foreach (float dt in new[] { 1f / 30f, 1f / 120f, 0.7f })
            {
                ResetActor(actor, controller, health, attack, movement, dodge, origin);
                Check(dodge.TryDodge(Vector2.right), "No inicia esquiva libre");
                for (int frame = 0; frame < Mathf.CeilToInt(0.5f / dt); frame++)
                    Move(movement, Vector2.zero, dt);
                Check(Mathf.Abs(actor.transform.position.x - origin.x - 3f) < 0.035f && !dodge.IsBusy,
                    "Distancia/duración depende de FPS o pierde tiempo en paso largo: " + dt);
            }
            ResetActor(actor, controller, health, attack, movement, dodge, origin);
            camera.transform.rotation = Quaternion.Euler(30f, 90f, 0f);
            Check(dodge.TryDodge(Vector2.up), "No inicia respecto a cámara");
            Move(movement, Vector2.down, 0.25f);
            Check(Mathf.Abs(actor.transform.position.x - origin.x - 3f) < 0.035f && Mathf.Abs(actor.transform.position.z - origin.z) < 0.035f,
                "Dirección no corresponde a cámara o puede redirigirse");
            Check(dodge.Phase == PrototypeDodgePhase.Recovery && !dodge.TryDodge(Vector2.up) && !attack.TryAttack(), "Recuperación permite cancelar o encadenar");
            Vector3 end = actor.transform.position;
            Move(movement, Vector2.up, 0.21f);
            Check(Vector3.ProjectOnPlane(actor.transform.position - end, Vector3.up).magnitude < 0.01f && !dodge.IsBusy,
                "Camina dentro del frame de recuperación");
            Check(attack.TryAttack() && !dodge.TryDodge(Vector2.up), "Ataque no bloquea esquiva");
            attack.CancelAttack();
            camera.transform.rotation = Quaternion.identity;
            ResetActor(actor, controller, health, attack, movement, dodge, origin);
            Check(dodge.TryDodge(Vector2.zero), "No inicia retroceso");
            Move(movement, Vector2.zero, 0.5f);
            Check(Mathf.Abs(actor.transform.position.z - origin.z + 3f) < 0.035f && Quaternion.Angle(actor.transform.rotation, Quaternion.identity) < 0.01f,
                "Retroceso no mantiene orientación o distancia");

            ResetActor(actor, controller, health, attack, movement, dodge, origin);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(origin.x + 1.8f, 1.5f, origin.z);
            wall.transform.localScale = new Vector3(0.2f, 3f, 10f);
            Physics.SyncTransforms();
            dodge.TryDodge(Vector2.right);
            Move(movement, Vector2.zero, 0.25f);
            Check(actor.transform.position.x > origin.x + 0.5f && actor.transform.position.x < origin.x + 1.3f, "Esquiva atraviesa pared");
            float stoppedX = actor.transform.position.x;
            wall.SetActive(false);
            Move(movement, Vector2.zero, 0.21f);
            Check(Mathf.Abs(actor.transform.position.x - stoppedX) < 0.01f, "Colisión conserva distancia pendiente");
            ResetActor(actor, controller, health, attack, movement, dodge, origin + Vector3.up * 4f);
            dodge.TryDodge(Vector2.right);
            Move(movement, Vector2.zero, 0.1f);
            Check(actor.transform.position.y < origin.y + 3.9f, "Esquiva detiene gravedad");
            health.TakeDamage(20);
            Check(health.Health == 80 && dodge.IsBusy, "Esquiva otorga invulnerabilidad");
            health.TakeDamage(80);
            Vector3 death = actor.transform.position;
            Move(movement, Vector2.up, 0.1f);
            Check(!dodge.IsBusy && Vector3.ProjectOnPlane(actor.transform.position - death, Vector3.up).magnitude < 0.01f
                && !dodge.TryDodge(Vector2.right), "Derrota no cancela/bloquea esquiva");

            var enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyObject.transform.position = origin + Vector3.back * 1.5f;
            var enemy = enemyObject.AddComponent<PrototypeEnemyAttack>();
            var enemyHealth = enemyObject.GetComponent<PrototypeHealth>();
            enemyHealth.ResetHealth();
            enemy.Configure(health, null);
            ResetActor(actor, controller, health, attack, movement, dodge, origin);
            Physics.SyncTransforms();
            Invoke(enemy, "Advance", 0.3f);
            Check(health.Health == 100 && enemy.Phase == PrototypeAttackPhase.Windup, "Preparación enemiga inválida");
            Check(dodge.TryDodge(Vector2.right), "No inicia esquiva en preparación enemiga");
            Move(movement, Vector2.zero, 0.25f);
            Physics.SyncTransforms();
            Physics.Simulate(0.02f);
            Check(health.Health == 100, "Cambió vida durante movimiento");
            Collider[] afterDodge = Physics.OverlapBox(enemy.transform.TransformPoint(new Vector3(0f, -0.2f, 1.15f)),
                new Vector3(0.7f, 0.85f, 0.7f), enemy.transform.rotation);
            Check(!Array.Exists(afterDodge, contact => contact == controller), "Overlap conserva contacto tras Move: "
                + controller.bounds + ", centro golpe=" + enemy.transform.TransformPoint(new Vector3(0f, -0.2f, 1.15f)) + ", giro=" + enemy.transform.eulerAngles);
            Invoke(enemy, "Advance", 0.51f);
            Check(health.Health == 100, "Reposicionamiento lateral no evita golpe real: vida=" + health.Health
                + ", posición=" + actor.transform.position + ", bounds=" + controller.bounds + ", enemigo=" + enemy.transform.position + ", fase=" + dodge.Phase);
            enemy.CancelAttack();
            ResetActor(actor, controller, health, attack, movement, dodge, origin);
            wall.SetActive(true);
            wall.transform.position = new Vector3(origin.x + 0.9f, 1.5f, origin.z);
            Physics.SyncTransforms();
            Invoke(enemy, "Advance", 0.3f);
            dodge.TryDodge(Vector2.right);
            Move(movement, Vector2.zero, 0.25f);
            Physics.SyncTransforms();
            Physics.Simulate(0.02f);
            Invoke(enemy, "Advance", 0.51f);
            Check(health.Health == 80, "Esquiva bloqueada dentro del volumen evita daño indebidamente");
            wall.SetActive(false);
            enemy.CancelAttack();

            ResetActor(actor, controller, health, attack, movement, dodge, origin);
            var session = actor.AddComponent<PrototypeDuelSession>();
            session.Configure(health, enemyHealth, null);
            Invoke(dodge, "Awake");
            dodge.TryDodge(Vector2.right);
            Move(movement, Vector2.zero, 0.1f);
            session.Restart();
            Check(!dodge.IsBusy && health.Health == 100 && enemyHealth.Health == 100
                && Vector3.Distance(actor.transform.position, origin) < 0.01f, "R no restaura/cancela esquiva");
            Check(dodge.TryDodge(Vector2.right), "R no devuelve esquiva");
            enemyHealth.TakeDamage(100);
            Move(movement, Vector2.zero, 0.1f);
            Check(!dodge.IsBusy && !dodge.TryDodge(Vector2.right), "Victoria no cancela/bloquea esquiva");
            session.Restart();
            dodge.TryDodge(Vector2.right);
            dodge.enabled = false;
            Invoke(dodge, "OnDisable");
            Check(!dodge.IsBusy && !dodge.TryDodge(Vector2.right), "Deshabilitar conserva esquiva");
            Debug.Log("DODGE_VALIDATION_SUCCEEDED: 30/120 FPS/paso largo, cámara, retroceso, exclusión ataque, recuperación, paredes, gravedad, daño real sin inmunidad, muerte, victoria y reinicio.");
        }
        finally
        {
            Physics.simulationMode = previousSimulation;
            EditorSceneManager.CloseScene(temporary, true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }
    }

    public static void CaptureDodge()
    {
        const string scenePath = "Assets/Scenes/PrototypeArena.unity";
        EditorSceneManager.OpenScene(scenePath);
        try
        {
            var dodge = UnityEngine.Object.FindFirstObjectByType<PrototypePlayerDodge>();
            var movement = dodge.GetComponent<PrototypePlayerMovement>();
            var session = dodge.GetComponent<PrototypeDuelSession>();
            session.Restart();
            Invoke(movement, "Awake");
            Invoke(dodge, "Awake");
            Check(dodge.TryDodge(Vector2.right), "No inicia esquiva para captura");
            Move(movement, Vector2.zero, 0.12f);
            Camera camera = Camera.main;
            camera.GetComponent<PrototypeFollowCamera>().Configure(dodge.transform);
            PrototypeCombatVisualValidation.Capture(camera, "01-Desplazamiento", "Logs/Dodge");
            Move(movement, Vector2.zero, 0.14f);
            camera.GetComponent<PrototypeFollowCamera>().Configure(dodge.transform);
            PrototypeCombatVisualValidation.Capture(camera, "02-Recuperacion", "Logs/Dodge");
            Debug.Log("DODGE_CAPTURE_SUCCEEDED: desplazamiento y recuperación en Logs/Dodge; sin UI OnGUI.");
        }
        finally
        {
            EditorSceneManager.OpenScene(scenePath);
        }
    }

    private static void ResetActor(GameObject actor, CharacterController controller, PrototypeHealth health,
        PrototypePlayerAttack attack, PrototypePlayerMovement movement, PrototypePlayerDodge dodge, Vector3 position)
    {
        attack.CancelAttack();
        dodge.CancelDodge();
        movement.ResetMovement();
        health.ResetHealth();
        controller.enabled = false;
        actor.transform.SetPositionAndRotation(position, Quaternion.identity);
        controller.enabled = true;
        Physics.SyncTransforms();
    }
    private static void Move(PrototypePlayerMovement movement, Vector2 input, float dt) => Invoke(movement, "AdvanceMovement", input, dt);
    private static void Invoke(object component, string method, params object[] arguments)
        => component.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component, arguments);
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("DODGE_VALIDATION_FAILED: " + message);
    }
}
