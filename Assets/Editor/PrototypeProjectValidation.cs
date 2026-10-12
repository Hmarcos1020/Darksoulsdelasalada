using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Comprueba la escena configurada y reutiliza la regresión existente sin regenerar la arena.</summary>
public static class PrototypeProjectValidation
{
    private const string ArenaPath = "Assets/Scenes/PrototypeArena.unity";
    private const string SceneListOutput = "Builds/WindowsSceneList/SoulslikeCombatPrototype.exe";

    [MenuItem("Tools/Soulslike/Validar proyecto y regresión")]
    public static void Validate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer)
            throw new InvalidOperationException("Salí de Play y esperá que terminen importación/compilación.");
        CheckSceneList(EditorBuildSettings.globalScenes, "lista global");
        CheckSceneList(EditorBuildSettings.scenes, "lista del perfil activo");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ArenaPath) == null)
            throw new InvalidOperationException("No se encontró el asset de la arena.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        // Las pruebas aditivas requieren una escena guardada; batch puede iniciar con Untitled.
        EditorSceneManager.OpenScene(ArenaPath, OpenSceneMode.Single);
        PrototypeCombatVisualValidation.Validate();
        Debug.Log("PROJECT_VALIDATION_SUCCEEDED: listas global y activa apuntan a la arena; regresión H1 y visual aprobadas.");
    }

    // Valida el camino de compilación que consulta la lista, no el builder con escena fija de H0.
    public static void ValidateAndBuildFromSceneList()
    {
        Validate();
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64
            || !BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
            throw new InvalidOperationException("Activá Windows x86_64 con su soporte de compilación antes de validar la build.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            throw new OperationCanceledException("Compilación cancelada antes de guardar escenas.");

        string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        Debug.Log("SCENE_LIST_BUILD_INPUT: " + string.Join(", ", scenes));
        var target = NamedBuildTarget.Standalone;
        ScriptingImplementation previousBackend = PlayerSettings.GetScriptingBackend(target);
        try
        {
            if (previousBackend != ScriptingImplementation.Mono2x)
                PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.Mono2x);
            Directory.CreateDirectory(Path.GetDirectoryName(SceneListOutput));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = SceneListOutput,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            });
            if (report == null || report.summary.result != BuildResult.Succeeded || !File.Exists(SceneListOutput))
                throw new InvalidOperationException("SCENE_LIST_BUILD_FAILED: revisá el informe y los errores del log.");
            Debug.Log("SCENE_LIST_BUILD_SUCCEEDED: " + SceneListOutput + "; errores=" + report.summary.totalErrors
                + "; advertencias=" + report.summary.totalWarnings + "; duración=" + report.summary.totalTime);
        }
        finally
        {
            if (PlayerSettings.GetScriptingBackend(target) != previousBackend)
                PlayerSettings.SetScriptingBackend(target, previousBackend);
        }
    }

    private static void CheckSceneList(EditorBuildSettingsScene[] scenes, string label)
    {
        var enabled = scenes.Where(scene => scene.enabled).ToArray();
        if (enabled.Length != 1 || enabled[0].path != ArenaPath
            || enabled[0].guid.ToString() != AssetDatabase.AssetPathToGUID(ArenaPath))
            throw new InvalidOperationException("SCENE_LIST_VALIDATION_FAILED: " + label
                + " debe contener únicamente PrototypeArena habilitada, con su GUID correcto.");
    }
}
