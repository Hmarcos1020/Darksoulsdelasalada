using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Añade el recurso a la arena aprobada, conservando sus parámetros de combate.</summary>
public static class PrototypeStaminaBuilder
{
    [MenuItem("Tools/Soulslike/Preparar stamina del jugador")]
    public static void Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Salí de Play antes de preparar stamina.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/Scenes/PrototypeArena.unity");
        var player = UnityEngine.Object.FindFirstObjectByType<PrototypePlayerParry>();
        if (player == null) throw new InvalidOperationException("Se requiere la arena con parry aprobado.");
        var stamina = player.GetComponent<PrototypeStamina>();
        if (stamina == null) stamina = player.gameObject.AddComponent<PrototypeStamina>();
        EditorUtility.SetDirty(stamina);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        if (!EditorSceneManager.SaveScene(player.gameObject.scene)) throw new InvalidOperationException("No se pudo guardar stamina.");
        Debug.Log("Stamina preparada: máximo 100, ataque 20, esquiva 30, parry 15; parámetros conservados al repetir.");
    }
    public static void PrepareValidateAndBuild()
    {
        Prepare(); Prepare();
        if (UnityEngine.Object.FindObjectsByType<PrototypeStamina>(FindObjectsSortMode.None).Length != 1)
            throw new InvalidOperationException("Preparación duplicó stamina.");
        PrototypeInputValidation.ValidateAndBuild();
    }
}
