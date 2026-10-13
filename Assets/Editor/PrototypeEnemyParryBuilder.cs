using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PrototypeEnemyParryBuilder
{
    [MenuItem("Tools/Soulslike/Preparar parry del enemigo")]
    public static void Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Salí de Play antes de preparar defensa enemiga.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/Scenes/PrototypeArena.unity");
        var enemy = UnityEngine.Object.FindFirstObjectByType<PrototypeEnemyAttack>();
        var player = UnityEngine.Object.FindFirstObjectByType<PrototypePlayerParry>();
        if (enemy == null || player == null || player.GetComponent<PrototypeStamina>() == null)
            throw new InvalidOperationException("Se requiere el duelo con parry y stamina.");
        var defense = enemy.GetComponent<PrototypeEnemyParry>();
        if (defense == null) defense = enemy.gameObject.AddComponent<PrototypeEnemyParry>();
        Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Prototype/DiagnosticoVolumen.mat");
        if (material == null) throw new InvalidOperationException("Falta el material Unlit del diagnóstico.");
        Transform cue = enemy.transform.Find("Señal de parry enemigo");
        if (cue == null)
        {
            GameObject graphic = GameObject.CreatePrimitive(PrimitiveType.Cube);
            graphic.name = "Señal de parry enemigo";
            UnityEngine.Object.DestroyImmediate(graphic.GetComponent<Collider>());
            graphic.transform.SetParent(enemy.transform, false); cue = graphic.transform;
            cue.localPosition = new Vector3(0.8f, 0.6f, 0.25f);
            cue.localScale = new Vector3(0.4f, 0.4f, 0.06f); cue.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }
        var renderer = cue.GetComponent<Renderer>(); renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Transform volumeObject = enemy.transform.Find("Volumen defensivo enemigo");
        if (volumeObject == null)
        {
            volumeObject = new GameObject("Volumen defensivo enemigo").transform;
            volumeObject.SetParent(enemy.transform, false); volumeObject.localPosition = new Vector3(0f, 0.1f, 0.65f);
        }
        var volume = volumeObject.GetComponent<BoxCollider>();
        if (volume == null) { volume = volumeObject.gameObject.AddComponent<BoxCollider>(); volume.size = new Vector3(0.8f, 1.3f, 0.3f); }
        volume.isTrigger = true;
        var outline = volumeObject.GetComponent<LineRenderer>();
        if (outline == null) outline = volumeObject.gameObject.AddComponent<LineRenderer>();
        outline.useWorldSpace = false; outline.widthMultiplier = 0.035f; outline.sharedMaterial = material;
        outline.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        int[] path = { 0, 1, 3, 2, 0, 4, 5, 1, 5, 7, 3, 7, 6, 2, 6, 4, 0 };
        outline.positionCount = path.Length; Vector3 half = volume.size * 0.5f;
        for (int i = 0; i < path.Length; i++)
        {
            int vertex = path[i];
            outline.SetPosition(i, volume.center + new Vector3((vertex & 1) == 0 ? -half.x : half.x,
                (vertex & 2) == 0 ? -half.y : half.y, (vertex & 4) == 0 ? -half.z : half.z));
        }
        defense.Configure(player.GetComponent<PrototypeHealth>(), renderer, volume, outline);
        if (enemy.GetComponent<PrototypeParryFeedback>() == null) enemy.gameObject.AddComponent<PrototypeParryFeedback>();
        enemy.GetComponent<AudioSource>().playOnAwake = false;
        EditorUtility.SetDirty(defense); AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(enemy.gameObject.scene);
        if (!EditorSceneManager.SaveScene(enemy.gameObject.scene)) throw new InvalidOperationException("No se pudo guardar defensa enemiga.");
        Debug.Log("Parry enemigo preparado: aviso ámbar, ventana cian y recuperación; secuencia fija con ataque normal.");
    }
    public static void PrepareValidateAndBuild()
    {
        Prepare(); Prepare();
        if (UnityEngine.Object.FindObjectsByType<PrototypeEnemyParry>(FindObjectsSortMode.None).Length != 1)
            throw new InvalidOperationException("La preparación duplicó la defensa.");
        PrototypeInputValidation.ValidateAndBuild();
    }
    public static void PrepareCaptureValidateAndBuild()
    {
        Prepare(); PrototypeEnemyParryValidation.Capture(); PrepareValidateAndBuild();
    }
}
