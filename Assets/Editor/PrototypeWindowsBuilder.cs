using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Compila únicamente la arena H0 para Windows x86_64 con Mono.</summary>
public static class PrototypeWindowsBuilder
{
    private const string ScenePath = "Assets/Scenes/PrototypeArena.unity";
    private const string OutputPath = "Builds/Windows/SoulslikeCombatPrototype.exe";

    [MenuItem("Tools/Soulslike/Compilar Windows H0")]
    public static void BuildWindows()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer)
        {
            ReportFailure("Salí de Play y esperá que terminen la importación y compilación antes de compilar Windows.");
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
        {
            ReportFailure("No existe " + ScenePath + ". Generá y guardá la arena H0 primero.");
            return;
        }
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
        {
            ReportFailure("Unity no tiene disponible Windows Build Support para este editor. No se inició la compilación.");
            return;
        }
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
        {
            ReportFailure("Activá Windows x86_64 en File > Build Profiles y esperá la importación antes de volver a usar este menú.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.Log("Compilación Windows H0 cancelada antes de iniciar: no se guardaron las escenas.");
            return;
        }

        NamedBuildTarget target = NamedBuildTarget.Standalone;
        ScriptingImplementation previousBackend = PlayerSettings.GetScriptingBackend(target);
        try
        {
            // Mono se aplica solo a esta operación; se restaura incluso si falla.
            if (previousBackend != ScriptingImplementation.Mono2x)
            {
                Debug.Log("Windows H0: se usa temporalmente Mono en lugar de " + previousBackend + ".");
                PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.Mono2x);
            }
            if (PlayerSettings.GetScriptingBackend(target) != ScriptingImplementation.Mono2x)
                throw new InvalidOperationException("No se pudo configurar Mono para la compilación.");

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report == null)
            {
                ReportFailure("BuildPipeline.BuildPlayer no devolvió un informe. No se puede confirmar el resultado.");
                return;
            }

            BuildSummary summary = report.summary;
            string details = "Resultado: " + summary.result + ". Errores: " + summary.totalErrors
                + ". Advertencias: " + summary.totalWarnings + ". Tiempo: " + summary.totalTime + ".";
            if (summary.result != BuildResult.Succeeded)
            {
                ReportFailure(details + " Revisá los errores de compilación en la consola. Un ejecutable anterior no confirma éxito.");
                return;
            }
            if (!File.Exists(OutputPath))
            {
                ReportFailure(details + " No se encontró el ejecutable esperado en " + OutputPath + ".");
                return;
            }

            string message = "Compilación Windows H0 exitosa.\n" + details + "\n" + Path.GetFullPath(OutputPath);
            Debug.Log(message);
            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("Windows H0: éxito", message, "Aceptar");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            ReportFailure("La compilación Windows H0 falló por una excepción: " + exception.Message);
        }
        finally
        {
            if (PlayerSettings.GetScriptingBackend(target) != previousBackend)
                PlayerSettings.SetScriptingBackend(target, previousBackend);
        }
    }

    private static void ReportFailure(string message)
    {
        Debug.LogError("Windows H0: fallo. " + message);
        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("Windows H0: fallo", message, "Aceptar");
    }
}
