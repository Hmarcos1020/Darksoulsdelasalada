using System;
using System.IO;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Comprueba observación de un golpe real, orden del frame y restauración del diagnóstico.</summary>
public static class PrototypeDiagnosticsValidation
{
    public static void Validate()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("El diagnóstico se prueba dentro de Play.");
        var observer = UnityEngine.Object.FindFirstObjectByType<PrototypeCombatDiagnostics>();
        var movement = observer.GetComponent<PrototypePlayerMovement>();
        var dodge = observer.GetComponent<PrototypePlayerDodge>();
        var session = observer.GetComponent<PrototypeDuelSession>();
        var health = observer.GetComponent<PrototypeHealth>();
        var controller = observer.GetComponent<CharacterController>();
        var enemy = UnityEngine.Object.FindFirstObjectByType<PrototypeEnemyAttack>();
        SimulationMode previousSimulation = Physics.simulationMode;
        float previousScale = Time.timeScale;
        float previousStep = Time.fixedDeltaTime;
        GameObject floor = null;
        GameObject wall = null;
        try
        {
            Physics.simulationMode = SimulationMode.Script;
            Vector3 origin = new Vector3(300f, 1.05f, 300f);
            floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Suelo temporal de prueba de diagnóstico";
            floor.transform.position = new Vector3(300f, -0.5f, 300f);
            floor.transform.localScale = new Vector3(30f, 1f, 30f);
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Pared temporal de prueba de diagnóstico";
            wall.transform.position = origin + Vector3.right * 0.9f;
            wall.transform.localScale = new Vector3(0.2f, 3f, 10f);
            wall.SetActive(false);
            observer.SetVisible(true);

            foreach (float dt in new[] { 1f / 30f, 1f / 120f })
            {
                PlaceActors(session, controller, observer.transform, enemy.transform, origin);
                // Cruza el comienzo de Active dentro del próximo paso, a ambas frecuencias.
                Invoke(enemy, "Advance", 0.8f - dt * 0.5f);
                Check(dodge.TryDodge(Vector2.right), "No inicia intento tardío");
                // Orden real de Update: esquiva -120, enemigo -90, motor 0; pose en LateUpdate.
                Invoke(enemy, "Advance", dt);
                Check(health.Health == 80, "Observador altera daño o no detecta golpe de 20");
                Invoke(movement, "AdvanceMovement", Vector2.zero, dt);
                Invoke(enemy.GetComponent<PrototypeAttackPresentation>(), "ApplyPose");
                Invoke(observer, "LateUpdate");
                string trace = observer.TraceSnapshot;
                int accepted = trace.IndexOf("ESQUIVA aceptada", StringComparison.Ordinal);
                int damage = trace.IndexOf("DAÑO recibido=20", StringComparison.Ordinal);
                int query = trace.IndexOf("CONSULTA enemigo", StringComparison.Ordinal);
                int end = trace.IndexOf("CIERRE frame", StringComparison.Ordinal);
                Check(accepted >= 0 && damage > accepted && query > damage && end > query, "Registro no conserva orden de consulta/movimiento");
                Check(Vector3.Distance(observer.LastDamagePosition, origin) < 0.02f
                    && observer.LastDamageEndPosition.x > observer.LastDamagePosition.x + 0.05f,
                    "No distingue posición usada para daño y posición al cierre");
                Check(observer.LastImpact.Contains("espada=Active"), "No vincula impacto con pose de cierre");
                Debug.Log("DIAGNOSTICS_ORDER_SAMPLE: dt=" + dt + "; " + observer.LastImpact.Replace("\n", " | "));
            }

            PlaceActors(session, controller, observer.transform, enemy.transform, origin);
            Invoke(enemy, "Advance", 0.3f);
            dodge.TryDodge(Vector2.right);
            Invoke(movement, "AdvanceMovement", Vector2.zero, 0.25f);
            Physics.Simulate(0.02f);
            Invoke(enemy, "Advance", 0.51f);
            Invoke(observer, "LateUpdate");
            Check(health.Health == 100 && observer.TraceSnapshot.Contains("impactos=0; daño=0"), "Esquiva temprana no evita impacto o no registra fallo");

            PlaceActors(session, controller, observer.transform, enemy.transform, origin);
            wall.SetActive(true);
            Physics.SyncTransforms();
            Invoke(enemy, "Advance", 0.3f);
            dodge.TryDodge(Vector2.right);
            Invoke(movement, "AdvanceMovement", Vector2.zero, 0.25f);
            Physics.Simulate(0.02f);
            Invoke(enemy, "Advance", 0.51f);
            Invoke(observer, "LateUpdate");
            Check(health.Health == 80, "Observador vuelve invulnerable una esquiva bloqueada");
            string exported = observer.ExportTrace("Logs/DiagnosticsValidation");
            Check(File.ReadAllText(exported).Contains("DAÑO recibido=20"), "Exportación no conserva evidencia");
            session.Restart();
            Check(observer.RecordedCount == 0, "R no limpia diagnóstico");

            Time.timeScale = 0.7f;
            Time.fixedDeltaTime = 0.018f;
            observer.SetSlowMotion(true);
            Check(Mathf.Abs(Time.timeScale - 0.14f) < 0.0001f && Mathf.Abs(Time.fixedDeltaTime - 0.0036f) < 0.0001f, "Lento no adapta el paso físico");
            observer.enabled = false;
            Check(Mathf.Abs(Time.timeScale - 0.7f) < 0.0001f && Mathf.Abs(Time.fixedDeltaTime - 0.018f) < 0.0001f, "Deshabilitar no restaura valores previos");
            health.TakeDamage(10);
            Check(observer.RecordedCount == 0, "Observador deshabilitado conserva suscripciones");
            observer.enabled = true;
            observer.SetVisible(true);
            health.TakeDamage(10);
            Check(observer.RecordedCount == 1, "Rehabilitar duplica suscripciones de daño");
            observer.ClearRecords();
            for (int attempt = 0; attempt < 150; attempt++) dodge.TryDodge(Vector2.right);
            Check(observer.RecordedCount == 128, "Registro crece sin límite");
            Debug.Log("DIAGNOSTICS_VALIDATION_SUCCEEDED: orden 30/120 FPS, posiciones de daño/cierre, pose, fallo temprano, pared, exportación, R, tiempo previo, suscripciones y límite del registro.");
        }
        finally
        {
            observer.SetSlowMotion(false);
            observer.enabled = true;
            observer.SetVisible(false);
            Time.timeScale = previousScale;
            Time.fixedDeltaTime = previousStep;
            Physics.simulationMode = previousSimulation;
            if (floor != null) UnityEngine.Object.DestroyImmediate(floor);
            if (wall != null) UnityEngine.Object.DestroyImmediate(wall);
            session.Restart();
        }
    }

    private static void PlaceActors(PrototypeDuelSession session, CharacterController controller, Transform player, Transform enemy, Vector3 origin)
    {
        session.Restart();
        controller.enabled = false;
        player.SetPositionAndRotation(origin, Quaternion.identity);
        controller.enabled = true;
        enemy.SetPositionAndRotation(origin + Vector3.back * 1.5f, Quaternion.identity);
        Physics.SyncTransforms();
        Physics.Simulate(0.02f);
    }

    public static void Capture()
    {
        const string scenePath = "Assets/Scenes/PrototypeArena.unity";
        EditorSceneManager.OpenScene(scenePath);
        var observer = UnityEngine.Object.FindFirstObjectByType<PrototypeCombatDiagnostics>();
        SimulationMode previousSimulation = Physics.simulationMode;
        try
        {
            Physics.simulationMode = SimulationMode.Script;
            var session = observer.GetComponent<PrototypeDuelSession>();
            var enemy = UnityEngine.Object.FindFirstObjectByType<PrototypeEnemyAttack>();
            session.Restart();
            Invoke(observer, "Awake");
            Invoke(observer, "OnEnable");
            observer.SetVisible(true);
            var controller = observer.GetComponent<CharacterController>();
            controller.enabled = false;
            observer.transform.position = enemy.transform.position + Vector3.back * 1.4f;
            controller.enabled = true;
            Camera camera = Camera.main;
            camera.GetComponent<PrototypeFollowCamera>().Configure(observer.transform);
            Physics.SyncTransforms();
            Physics.Simulate(0.02f);
            Invoke(enemy, "Advance", 0.3f);
            Invoke(enemy.GetComponent<PrototypeAttackPresentation>(), "ApplyPose");
            Invoke(observer, "LateUpdate");
            PrototypeCombatVisualValidation.Capture(camera, "01-Preparacion", "Logs/Diagnostics");
            Invoke(enemy, "Advance", 0.51f);
            Check(observer.GetComponent<PrototypeHealth>().Health == 80 && observer.TraceSnapshot.Contains("DAÑO recibido=20"),
                "La captura debe incluir daño real y sus marcas históricas");
            Invoke(enemy.GetComponent<PrototypeAttackPresentation>(), "ApplyPose");
            Invoke(observer, "LateUpdate");
            PrototypeCombatVisualValidation.Capture(camera, "02-Impacto", "Logs/Diagnostics");
            Debug.Log("DIAGNOSTICS_CAPTURE_SUCCEEDED: volumen de preparación/impacto y posición de daño, sin UI OnGUI.");
        }
        finally
        {
            if (observer != null) Invoke(observer, "OnDisable");
            Physics.simulationMode = previousSimulation;
            EditorSceneManager.OpenScene(scenePath);
        }
    }

    private static void Invoke(object component, string method, params object[] arguments)
        => component.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component, arguments);
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("DIAGNOSTICS_VALIDATION_FAILED: " + message);
    }
}
