using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Regresión del comienzo del daño y continuidad de la pose para ambos atacantes.</summary>
public static class PrototypeImpactSyncValidation
{
    [MenuItem("Tools/Soulslike/Validar sincronización de impacto")]
    public static void Validate()
    {
        var previous = SceneManager.GetActiveScene();
        var temporary = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(temporary);
        try
        {
            Verify(false, new Vector3(220f, 1f, 220f), 0.18f, 0.12f, 25);
            Verify(true, new Vector3(230f, 1f, 230f), 0.8f, 0.15f, 20);
            Debug.Log("IMPACT_SYNC_VALIDATION_SUCCEEDED: jugador/enemigo, preparación sin daño, pose de contacto al primer daño, continuidad preparación/activo/recuperación, 30/120 FPS y reposo.");
        }
        finally
        {
            EditorSceneManager.CloseScene(temporary, true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }
    }

    private static void Verify(bool enemyAttacker, Vector3 origin, float windup, float active, int damage)
    {
        var actor = new GameObject(enemyAttacker ? "Enemigo para sincronización" : "Jugador para sincronización");
        actor.transform.position = origin;
        actor.AddComponent<PrototypeHealth>().ResetHealth();
        var targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        targetObject.transform.position = origin + Vector3.forward * 1.5f;
        targetObject.transform.localScale = Vector3.one * 0.6f;
        var target = targetObject.AddComponent<PrototypeHealth>();
        target.ResetHealth();
        Transform pivot = new GameObject("Espada de prueba").transform;
        pivot.SetParent(actor.transform, false);
        var presentation = actor.AddComponent<PrototypeAttackPresentation>();
        PrototypePlayerAttack player = null;
        PrototypeEnemyAttack enemy = null;
        object source;
        if (enemyAttacker)
        {
            enemy = actor.AddComponent<PrototypeEnemyAttack>();
            enemy.Configure(target, null);
            presentation.Configure(enemy, pivot);
            source = enemy;
        }
        else
        {
            player = actor.AddComponent<PrototypePlayerAttack>();
            presentation.Configure(player, pivot);
            source = player;
        }
        Vector3 rest = pivot.localPosition;
        Quaternion restRotation = pivot.localRotation;
        Vector3 contact = new Vector3(0.65f, -0.15f, 0.5f);
        Quaternion contactRotation = Quaternion.Euler(5f, -20f, 0f);
        Physics.SyncTransforms();

        if (player != null) Check(player.TryAttack(), "No inicia ataque del jugador");
        Invoke(source, "Advance", windup * 0.7f);
        Invoke(presentation, "ApplyPose");
        Quaternion raised = pivot.localRotation;
        Check(target.Health == 100 && Quaternion.Angle(raised, contactRotation) > 30f, "Preparación debe levantar arma sin causar daño");
        Invoke(source, "Advance", Remaining(source) - 0.0001f);
        Invoke(presentation, "ApplyPose");
        Vector3 beforeContact = pivot.localPosition;
        Quaternion beforeContactRotation = pivot.localRotation;
        Check(target.Health == 100 && Quaternion.Angle(beforeContactRotation, contactRotation) < 0.1f,
            "La preparación debe aproximarse al contacto sin daño anticipado");
        Invoke(source, "Advance", Remaining(source));
        Invoke(presentation, "ApplyPose");
        Check(target.Health == 100 - damage && Mathf.Abs(Progress(source)) < 0.0001f, "Cambió el comienzo o el daño del ataque");
        Check(Vector3.Distance(pivot.localPosition, contact) < 0.001f && Quaternion.Angle(pivot.localRotation, contactRotation) < 0.1f,
            "El primer frame activo aún usa pose elevada");
        Check(Vector3.Distance(beforeContact, pivot.localPosition) < 0.001f && Quaternion.Angle(beforeContactRotation, pivot.localRotation) < 0.1f,
            "Salto visual al habilitar daño");

        Invoke(source, "Advance", active - 0.0001f);
        Invoke(presentation, "ApplyPose");
        Vector3 beforeRecovery = pivot.localPosition;
        Quaternion beforeRecoveryRotation = pivot.localRotation;
        Invoke(source, "Advance", Remaining(source));
        Invoke(presentation, "ApplyPose");
        Check(Vector3.Distance(beforeRecovery, pivot.localPosition) < 0.001f && Quaternion.Angle(beforeRecoveryRotation, pivot.localRotation) < 0.1f,
            "Salto visual al recuperar");
        Check(target.Health == 100 - damage, "La presentación agregó daño");
        Invoke(source, "Advance", Remaining(source));
        Invoke(presentation, "ApplyPose");
        Check(Vector3.Distance(rest, pivot.localPosition) < 0.001f && Quaternion.Angle(restRotation, pivot.localRotation) < 0.1f,
            "No restaura reposo");

        foreach (float dt in new[] { 1f / 30f, 1f / 120f })
        {
            if (player != null) player.CancelAttack(); else enemy.CancelAttack();
            target.ResetHealth();
            if (player != null) Check(player.TryAttack(), "No reinicia ataque");
            Invoke(source, "Advance", windup - dt * 0.5f);
            Invoke(presentation, "ApplyPose");
            Check(target.Health == 100, "Daño antes de la ventana activa");
            Invoke(source, "Advance", dt);
            Invoke(presentation, "ApplyPose");
            Check(target.Health == 100 - damage && Quaternion.Angle(pivot.localRotation, contactRotation) < 5f,
                "Al cruzar a Active, daño y pose no coinciden a dt=" + dt);
        }
    }

    public static void Capture()
    {
        const string scenePath = "Assets/Scenes/PrototypeArena.unity";
        EditorSceneManager.OpenScene(scenePath);
        SimulationMode previousSimulation = Physics.simulationMode;
        var observer = UnityEngine.Object.FindFirstObjectByType<PrototypeCombatDiagnostics>();
        try
        {
            Physics.simulationMode = SimulationMode.Script;
            var session = observer.GetComponent<PrototypeDuelSession>();
            var enemy = UnityEngine.Object.FindFirstObjectByType<PrototypeEnemyAttack>();
            var presentation = enemy.GetComponent<PrototypeAttackPresentation>();
            session.Restart();
            Invoke(observer, "Awake");
            Invoke(observer, "OnEnable");
            observer.SetVisible(true);
            var controller = observer.GetComponent<CharacterController>();
            controller.enabled = false;
            observer.transform.position = enemy.transform.position + Vector3.back * 1.4f + Vector3.up * 0.08f;
            controller.enabled = true;
            Camera camera = Camera.main;
            camera.GetComponent<PrototypeFollowCamera>().Configure(observer.transform);
            Physics.SyncTransforms();
            Physics.Simulate(0.02f);
            Invoke(enemy, "Advance", 0.56f);
            Invoke(presentation, "ApplyPose");
            Invoke(observer, "LateUpdate");
            PrototypeCombatVisualValidation.Capture(camera, "01-ArmaElevada", "Logs/ImpactSync");
            Invoke(enemy, "Advance", Remaining(enemy) - 0.0001f);
            Invoke(presentation, "ApplyPose");
            Invoke(observer, "LateUpdate");
            Check(observer.GetComponent<PrototypeHealth>().Health == 100, "La captura previa al contacto causó daño");
            PrototypeCombatVisualValidation.Capture(camera, "02-AntesDelImpacto", "Logs/ImpactSync");
            Invoke(enemy, "Advance", Remaining(enemy));
            Invoke(presentation, "ApplyPose");
            Invoke(observer, "LateUpdate");
            Check(observer.GetComponent<PrototypeHealth>().Health == 80, "La captura activa no aplicó daño");
            PrototypeCombatVisualValidation.Capture(camera, "03-PrimerDaño", "Logs/ImpactSync");
            Debug.Log("IMPACT_SYNC_CAPTURE_SUCCEEDED: preparación elevada, aproximación sin daño y primer daño en pose de contacto; sin UI OnGUI.");
        }
        finally
        {
            if (observer != null) Invoke(observer, "OnDisable");
            Physics.simulationMode = previousSimulation;
            EditorSceneManager.OpenScene(scenePath);
        }
    }

    private static float Remaining(object source) => (float)source.GetType().GetField("remaining", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(source);
    private static float Progress(object source) => (float)source.GetType().GetProperty("PhaseProgress").GetValue(source);
    private static void Invoke(object component, string method, params object[] arguments)
        => component.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component, arguments);
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("IMPACT_SYNC_VALIDATION_FAILED: " + message);
    }
}
