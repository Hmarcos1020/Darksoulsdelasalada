using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>Regresión de combate y comprobaciones de presentación con pasos controlados.</summary>
public static class PrototypeCombatVisualValidation
{
    [MenuItem("Tools/Soulslike/Validar lectura visual H1")]
    public static void Validate()
    {
        PrototypeCombatValidation.Validate();
        Scene previous = SceneManager.GetActiveScene();
        Scene temporary = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(temporary);
        try
        {
            GameObject player = new GameObject("Prueba de presentación");
            player.transform.position = new Vector3(200f, 1f, 200f);
            player.AddComponent<CharacterController>();
            var attack = player.AddComponent<PrototypePlayerAttack>();
            Transform pivot = new GameObject("Pivote de prueba").transform;
            pivot.SetParent(player.transform, false);
            var presentation = player.AddComponent<PrototypeAttackPresentation>();
            presentation.Configure(attack, pivot);
            Vector3 restPosition = pivot.localPosition;
            Quaternion restRotation = pivot.localRotation;
            GameObject targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            targetObject.transform.position = player.transform.position + Vector3.forward * 1.5f;
            targetObject.transform.localScale = Vector3.one * 0.6f;
            var target = targetObject.AddComponent<PrototypeHealth>();
            var feedback = targetObject.AddComponent<PrototypeHitPresentation>();
            Transform graphic = new GameObject("Señal de prueba").transform;
            graphic.SetParent(target.transform, false);
            feedback.Configure(target, graphic);
            target.ResetHealth();
            int notifications = 0;
            int actualDamage = 0;
            target.Damaged += amount => { notifications++; actualDamage = amount; };
            Physics.SyncTransforms();

            Check(attack.TryAttack(), "No inicia ataque");
            Invoke(attack, "Advance", 0.09f);
            Invoke(presentation, "ApplyPose");
            Check(attack.PhaseProgress > 0.49f && attack.PhaseProgress < 0.51f, "Progreso no usa reloj del ataque");
            Check(Quaternion.Angle(restRotation, pivot.localRotation) > 5f, "Arma inmóvil durante preparación");
            Check(target.Health == 100 && !graphic.gameObject.activeSelf, "Señal de impacto antes de recibir daño");
            Quaternion raisedRotation = pivot.localRotation;
            Invoke(attack, "Advance", 0.1f);
            Invoke(presentation, "ApplyPose");
            Check(target.Health == 75 && graphic.gameObject.activeSelf && notifications == 1 && actualDamage == 25, "Señal de impacto o evento incorrectos");
            Check(Quaternion.Angle(raisedRotation, pivot.localRotation) > 1f, "Pose no cambia al golpear");

            presentation.enabled = false;
            Invoke(presentation, "OnDisable");
            Invoke(attack, "Advance", 1f);
            Check(target.Health == 75 && !attack.IsAttacking, "Presentación modifica el combate");
            Invoke(feedback, "AdvanceVisual", 0.5f);
            Check(!graphic.gameObject.activeSelf, "Señal no expira");
            presentation.enabled = true;
            for (int iteration = 0; iteration < 6; iteration++)
            {
                target.ResetHealth();
                Check(attack.TryAttack(), "Ataque repetido rechazado");
                Invoke(attack, "Advance", 1f);
                Invoke(presentation, "ApplyPose");
                Check(Vector3.Distance(restPosition, pivot.localPosition) < 0.001f && Quaternion.Angle(restRotation, pivot.localRotation) < 0.01f, "Deriva de pose entre ataques");
            }
            target.ResetHealth();
            Check(!graphic.gameObject.activeSelf, "R no limpia señal de impacto");
            int before = notifications;
            target.TakeDamage(-10);
            Check(notifications == before && !graphic.gameObject.activeSelf, "Daño inválido emite señal");
            target.TakeDamage(200);
            Check(actualDamage == 100 && graphic.gameObject.activeSelf, "Evento no informa daño efectivo");
            before = notifications;
            target.TakeDamage(25);
            Check(notifications == before, "Objetivo derrotado emite impactos adicionales");
            feedback.enabled = false;
            Invoke(feedback, "OnDisable");
            target.ResetHealth();
            target.TakeDamage(25);
            Check(!graphic.gameObject.activeSelf, "Presentación desactivada conserva suscripción");
            feedback.enabled = true;
            Invoke(feedback, "OnEnable");
            target.ResetHealth();
            target.TakeDamage(25);
            Check(graphic.gameObject.activeSelf, "Reactivar pierde suscripción");
            Debug.Log("H1_VISUAL_VALIDATION_SUCCEEDED: progreso, poses, ausencia de deriva, daño independiente de presentación, señales, reinicio y suscripciones.");
        }
        finally
        {
            EditorSceneManager.CloseScene(temporary, true);
            if (previous.IsValid() && previous.isLoaded)
                SceneManager.SetActiveScene(previous);
        }
    }

    public static void PrepareValidateCaptureAndBuild()
    {
        PrototypeCombatVisualBuilder.PrepareVisuals();
        PrototypeCombatVisualBuilder.PrepareVisuals();
        Check(UnityEngine.Object.FindObjectsByType<PrototypeAttackPresentation>(FindObjectsSortMode.None).Length == (UnityEngine.Object.FindFirstObjectByType<PrototypeEnemyAttack>() == null ? 1 : 2), "Preparación duplica presentación del arma");
        Check(UnityEngine.Object.FindObjectsByType<PrototypeHitPresentation>(FindObjectsSortMode.None).Length == (UnityEngine.Object.FindFirstObjectByType<PrototypeEnemyAttack>() == null ? 1 : 2), "Preparación duplica presentación del impacto");
        var attack = UnityEngine.Object.FindFirstObjectByType<PrototypePlayerAttack>();
        var target = UnityEngine.Object.FindFirstObjectByType<PrototypeTrainingTarget>().GetComponent<PrototypeHealth>();
        Check(attack.transform.Find("Arma provisional H1").GetComponentsInChildren<Collider>(true).Length == 0, "Arma visual tiene collider");
        Check(target.transform.Find("Señal de impacto H1").GetComponentsInChildren<Collider>(true).Length == 0, "Señal visual tiene collider");
        Check(attack.GetComponent<Renderer>().sharedMaterial.HasProperty("_BaseColor")
            && target.GetComponent<Renderer>().sharedMaterial.HasProperty("_BaseColor")
            && attack.transform.Find("Alcance del ataque H1").GetComponent<Renderer>().sharedMaterial.HasProperty("_BaseColor"), "Material visual incompatible con colores de fase/daño");
        Validate();
        try { CapturePhases(); }
        catch (Exception exception) { Debug.LogWarning("H1_VISUAL_CAPTURE_UNAVAILABLE: " + exception.Message); }
        PrototypeWindowsBuilder.BuildWindows();
    }

    private static void CapturePhases()
    {
        string scenePath = SceneManager.GetActiveScene().path;
        var attack = UnityEngine.Object.FindFirstObjectByType<PrototypePlayerAttack>();
        var target = UnityEngine.Object.FindFirstObjectByType<PrototypeTrainingTarget>().GetComponent<PrototypeHealth>();
        var presentation = attack.GetComponent<PrototypeAttackPresentation>();
        Camera camera = Camera.main;
        try
        {
            attack.transform.position = target.transform.position + Vector3.back * 1.5f + Vector3.up * 0.05f;
            camera.GetComponent<PrototypeFollowCamera>().Configure(attack.transform);
            var feedback = target.GetComponent<PrototypeHitPresentation>();
            Invoke(feedback, "OnEnable");
            target.ResetHealth();
            Physics.SyncTransforms();
            Invoke(presentation, "ApplyPose");
            Capture(camera, "01-Reposo");
            attack.TryAttack();
            Invoke(attack, "Advance", 0.12f);
            Invoke(presentation, "ApplyPose");
            Capture(camera, "02-Preparacion");
            Invoke(attack, "Advance", 0.12f);
            Invoke(presentation, "ApplyPose");
            Capture(camera, "03-Impacto");
            Invoke(attack, "Advance", 0.15f);
            Invoke(presentation, "ApplyPose");
            Invoke(feedback, "AdvanceVisual", 0.15f);
            Capture(camera, "04-Recuperacion");
            Debug.Log("H1_VISUAL_CAPTURE_SUCCEEDED: capturas de geometría y fases en Logs/H1Visual.");
        }
        finally
        {
            // Las poses de captura son cambios propios y no deben entrar en la escena guardada/build.
            EditorSceneManager.OpenScene(scenePath);
        }
    }

    public static void Capture(Camera camera, string name, string folder = "Logs/H1Visual")
    {
        var texture = new RenderTexture(1280, 720, 24);
        var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        RenderTexture previous = RenderTexture.active;
        float previousAspect = camera.aspect;
        try
        {
            texture.Create();
            camera.aspect = 1280f / 720f;
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = texture };
            if (!RenderPipeline.SupportsRenderRequest(camera, request))
                throw new NotSupportedException("No hay render request URP disponible en esta sesión.");
            RenderPipeline.SubmitRenderRequest(camera, request);
            RenderTexture.active = texture;
            pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            pixels.Apply();
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), pixels.EncodeToPNG());
        }
        finally
        {
            camera.aspect = previousAspect;
            RenderTexture.active = previous;
            texture.Release();
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(pixels);
        }
    }

    private static void Invoke(object component, string method, params object[] arguments)
        => component.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component, arguments);
    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("H1_VISUAL_VALIDATION_FAILED: " + message);
    }
}
