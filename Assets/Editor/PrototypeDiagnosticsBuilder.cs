using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PrototypeDiagnosticsBuilder
{
    [MenuItem("Tools/Soulslike/Preparar diagnóstico de combate")]
    public static void Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Salí de Play antes de preparar diagnóstico.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/Scenes/PrototypeArena.unity");
        var player = UnityEngine.Object.FindFirstObjectByType<PrototypePlayerDodge>();
        var enemy = UnityEngine.Object.FindFirstObjectByType<PrototypeEnemyAttack>();
        if (player == null || enemy == null) throw new InvalidOperationException("Prepará el duelo con esquiva antes del diagnóstico.");
        var observer = player.GetComponent<PrototypeCombatDiagnostics>();
        if (observer == null) observer = player.gameObject.AddComponent<PrototypeCombatDiagnostics>();
        const string materialPath = "Assets/Materials/Prototype/DiagnosticoVolumen.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) throw new InvalidOperationException("No se encontró URP/Unlit para el contorno.");
            material = new Material(shader) { name = "DiagnosticoVolumen" };
            AssetDatabase.CreateAsset(material, materialPath);
        }
        observer.Configure(enemy, enemy.transform.Find("Arma enemiga"), material);
        EditorUtility.SetDirty(observer);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        if (!EditorSceneManager.SaveScene(player.gameObject.scene)) throw new InvalidOperationException("No se pudo guardar diagnóstico.");
        Debug.Log("Diagnóstico preparado: F3 mostrar, F4 tiempo 0,2x, F5 guardar; las reglas del duelo se conservan.");
    }

    public static void PrepareValidateAndBuild()
    {
        Prepare();
        Prepare();
        if (UnityEngine.Object.FindObjectsByType<PrototypeCombatDiagnostics>(FindObjectsSortMode.None).Length != 1)
            throw new InvalidOperationException("La preparación duplicó el diagnóstico.");
        PrototypeInputValidation.ValidateAndBuild();
    }
}
