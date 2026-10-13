using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Agrega la primera esquiva al duelo sin reconstruir la arena.</summary>
public static class PrototypeDodgeBuilder
{
    [MenuItem("Tools/Soulslike/Preparar primera esquiva")]
    public static void PrepareDodge()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Salí de Play antes de preparar la esquiva.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        if (AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/PrototypeCombat.inputactions") == null)
            throw new InvalidOperationException("Falta importar PrototypeCombat.inputactions.");
        PrototypeDuelBuilder.PrepareDuel();
        var player = UnityEngine.Object.FindFirstObjectByType<PrototypePlayerMovement>();
        var dodge = player.GetComponent<PrototypePlayerDodge>();
        if (dodge == null)
            dodge = player.gameObject.AddComponent<PrototypePlayerDodge>();
        Transform cue = player.transform.Find("Señal de esquiva");
        if (cue == null)
        {
            var graphic = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            graphic.name = "Señal de esquiva";
            UnityEngine.Object.DestroyImmediate(graphic.GetComponent<Collider>());
            graphic.transform.SetParent(player.transform, false);
            graphic.transform.localPosition = new Vector3(0f, -0.9f, 0f);
            graphic.transform.localScale = new Vector3(1.5f, 0.015f, 1.5f);
            cue = graphic.transform;
        }
        var renderer = cue.GetComponent<Renderer>();
        renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Prototype/ArmaProvisional.mat");
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        dodge.Configure(renderer);
        EditorUtility.SetDirty(dodge);
        var scene = player.gameObject.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("No se pudo guardar la primera esquiva.");
        Debug.Log("Primera esquiva guardada: Espacio, 3 m / 0,25 s + 0,20 s de recuperación; sin invulnerabilidad.");
    }

    public static void PrepareValidateAndBuild()
    {
        PrepareDodge();
        PrepareDodge();
        if (UnityEngine.Object.FindObjectsByType<PrototypePlayerDodge>(FindObjectsSortMode.None).Length != 1
            || UnityEngine.Object.FindObjectsByType<PrototypePlayerInput>(FindObjectsSortMode.None).Length != 1)
            throw new InvalidOperationException("La preparación duplicó esquiva o entrada.");
        var dodge = UnityEngine.Object.FindFirstObjectByType<PrototypePlayerDodge>();
        if (dodge.transform.Find("Señal de esquiva").GetComponent<Collider>() != null)
            throw new InvalidOperationException("La señal visual no debe afectar colisiones.");
        PrototypeInputValidation.ValidateAndBuild();
    }
}
