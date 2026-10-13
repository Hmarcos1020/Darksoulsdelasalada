using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Pruebas de lógica y consultas físicas reales sin guardar la escena de prueba.</summary>
public static class PrototypeCombatValidation
{
    private static readonly MethodInfo Advance = typeof(PrototypePlayerAttack).GetMethod("Advance", BindingFlags.Instance | BindingFlags.NonPublic);

    [MenuItem("Tools/Soulslike/Validar lógica de ataque H1")]
    public static void Validate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Salí de Play antes de validar.");
        Scene previous = SceneManager.GetActiveScene();
        Scene testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(testScene);
        try
        {
            var player = new GameObject("Jugador de prueba");
            player.transform.position = new Vector3(100f, 1f, 100f);
            player.AddComponent<CharacterController>();
            var attack = player.AddComponent<PrototypePlayerAttack>();
            GameObject targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            targetObject.transform.localScale = Vector3.one * 0.6f;
            var target = targetObject.AddComponent<PrototypeHealth>();
            targetObject.transform.position = player.transform.position + Vector3.forward * 1.5f;
            target.ResetHealth();
            // Dos colliders del mismo objetivo deben producir un solo daño.
            targetObject.AddComponent<SphereCollider>().radius = 0.5f;
            Physics.SyncTransforms();
            Check(attack.TryAttack(), "No inicia el primer ataque");
            Check(!attack.TryAttack(), "Acepta spam durante preparación");
            Step(attack, 0.1f);
            Check(target.Health == 100, "Daño antes de la ventana activa");
            Step(attack, 0.09f);
            Check(target.Health == 75, "Daño frontal incorrecto");
            Step(attack, 0.02f);
            Check(target.Health == 75, "Daño repetido por fotograma o collider");
            Step(attack, 0.15f);
            Check(attack.Phase == PrototypeAttackPhase.Recovery && !attack.TryAttack(), "Recuperación no bloquea entradas");
            Step(attack, 0.4f);
            Check(!attack.IsAttacking, "No vuelve a reposo");

            foreach (float frameTime in new[] { 1f / 30f, 1f / 120f })
            {
                target.ResetHealth();
                Check(attack.TryAttack(), "No inicia prueba de framerate");
                for (int frame = 0; frame < Mathf.CeilToInt(0.8f / frameTime); frame++)
                    Step(attack, frameTime);
                Check(target.Health == 75 && !attack.IsAttacking, "Daño o duración depende del framerate");
            }

            target.ResetHealth();
            FullAttack(attack);
            Check(target.Health == 75, "Fotograma largo pierde o duplica el impacto");
            targetObject.transform.position = player.transform.position + Vector3.forward * 4f;
            Physics.SyncTransforms();
            target.ResetHealth();
            FullAttack(attack);
            Check(target.Health == 100, "Golpea fuera de alcance");
            targetObject.transform.position = player.transform.position - Vector3.forward * 1.5f;
            Physics.SyncTransforms();
            FullAttack(attack);
            Check(target.Health == 100, "Golpea hacia atrás");

            targetObject.transform.position = player.transform.position + Vector3.forward * 1.5f;
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = player.transform.position + Vector3.forward * 0.8f;
            wall.transform.localScale = new Vector3(2f, 2f, 0.15f);
            Physics.SyncTransforms();
            FullAttack(attack);
            Check(target.Health == 100, "Golpea a través de obstáculos");
            wall.SetActive(false);
            Physics.SyncTransforms();
            for (int index = 0; index < 4; index++)
                FullAttack(attack);
            Check(target.Health == 0 && target.IsDefeated, "Cuatro golpes no derrotan al objetivo");
            FullAttack(attack);
            Check(target.Health == 0, "Vida negativa tras derrota");
            target.ResetHealth();
            Check(target.Health == 100 && !target.IsDefeated, "Reinicio incorrecto");
            Check(attack.TryAttack(), "No inicia prueba de desactivación");
            attack.enabled = false;
            // En edición los callbacks de MonoBehaviour no tienen el ciclo de Play.
            typeof(PrototypePlayerAttack).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(attack, null);
            Check(!attack.IsAttacking, "Desactivar deja el movimiento bloqueado");
            Debug.Log("H1_VALIDATION_SUCCEEDED: fases, spam, daño único, 30/120 FPS, fotograma largo, alcance, dirección, obstáculos, derrota y reinicio.");
        }
        finally
        {
            EditorSceneManager.CloseScene(testScene, true);
            if (previous.IsValid() && previous.isLoaded)
                SceneManager.SetActiveScene(previous);
        }
    }

    // Punto de entrada batch; una excepción provoca salida de Unity con fallo.
    public static void PrepareAndValidate()
    {
        PrototypeCombatBuilder.PrepareCombat();
        PrototypeCombatBuilder.PrepareCombat();
        Check(UnityEngine.Object.FindObjectsByType<PrototypePlayerAttack>(FindObjectsSortMode.None).Length == 1, "Preparación duplica ataques");
        Check(UnityEngine.Object.FindObjectsByType<PrototypeTrainingTarget>(FindObjectsSortMode.None).Length == 1, "Preparación duplica objetivos");
        Validate();
    }

    private static void Step(PrototypePlayerAttack attack, float deltaTime) => Advance.Invoke(attack, new object[] { deltaTime });
    private static void FullAttack(PrototypePlayerAttack attack)
    {
        Check(attack.TryAttack(), "Ataque completo rechazado");
        Step(attack, 1f);
    }
    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("H1_VALIDATION_FAILED: " + message);
    }
}
