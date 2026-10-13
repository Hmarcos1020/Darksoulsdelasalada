using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Agrega el primer ejercicio de combate conservando la arena existente.</summary>
public static class PrototypeCombatBuilder
{
    private const string ScenePath = "Assets/Scenes/PrototypeArena.unity";

    [MenuItem("Tools/Soulslike/Preparar primer ataque H1")]
    public static void PrepareCombat()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Salí de Play antes de preparar H1.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        var scene = EditorSceneManager.OpenScene(ScenePath);
        PrototypePlayerMovement movement = UnityEngine.Object.FindFirstObjectByType<PrototypePlayerMovement>();
        if (movement == null || movement.gameObject.scene != scene)
            throw new InvalidOperationException("No se encontró el jugador de la arena.");
        var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/PrototypeCombat.inputactions");
        if (inputAsset == null)
            throw new InvalidOperationException("Falta PrototypeCombat.inputactions.");
        var commands = movement.GetComponent<PrototypePlayerInput>();
        if (commands == null)
            commands = movement.gameObject.AddComponent<PrototypePlayerInput>();
        commands.Configure(inputAsset);
        EditorUtility.SetDirty(commands);
        var attack = movement.GetComponent<PrototypePlayerAttack>();
        if (attack == null)
            attack = movement.gameObject.AddComponent<PrototypePlayerAttack>();

        var playerHealth = movement.GetComponent<PrototypeHealth>();
        if (playerHealth == null)
            playerHealth = movement.gameObject.AddComponent<PrototypeHealth>();
        Transform indicatorTransform = movement.transform.Find("Alcance del ataque H1");
        if (indicatorTransform == null)
        {
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            indicator.name = "Alcance del ataque H1";
            UnityEngine.Object.DestroyImmediate(indicator.GetComponent<Collider>());
            indicator.transform.SetParent(movement.transform, false);
            indicator.transform.localPosition = new Vector3(0f, -0.9f, 1.15f);
            indicator.transform.localScale = new Vector3(1.4f, 0.04f, 1.4f);
            indicatorTransform = indicator.transform;
        }
        Renderer indicatorRenderer = indicatorTransform.GetComponent<Renderer>();
        indicatorRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Prototype/Jugador.mat");
        indicatorRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        indicatorRenderer.enabled = false;
        attack.Configure(indicatorRenderer);

        PrototypeTrainingTarget target = UnityEngine.Object.FindFirstObjectByType<PrototypeTrainingTarget>();
        if (target == null)
        {
            GameObject targetObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            targetObject.name = "Objetivo de práctica H1";
            targetObject.transform.position = new Vector3(0f, 1f, -1.5f);
            targetObject.GetComponent<Renderer>().sharedMaterial = indicatorRenderer.sharedMaterial;
            target = targetObject.AddComponent<PrototypeTrainingTarget>();
        }
        var targetHealth = target.GetComponent<PrototypeHealth>();
        if (targetHealth == null)
            targetHealth = target.gameObject.AddComponent<PrototypeHealth>();
        target.Configure(targetHealth);
        var hud = movement.GetComponent<PrototypeCombatHUD>();
        if (hud == null)
            hud = movement.gameObject.AddComponent<PrototypeCombatHUD>();
        hud.Configure(attack, targetHealth);
        EditorUtility.SetDirty(attack);
        EditorUtility.SetDirty(hud);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException("No se pudo guardar la arena H1.");
        Debug.Log("Arena H1 guardada. Clic izquierdo/E: ataque; R: reiniciar objetivo.");
    }
}
