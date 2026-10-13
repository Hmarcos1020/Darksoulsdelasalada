using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Añade la defensa a la arena guardada sin reconstruir el duelo.</summary>
public static class PrototypeParryBuilder
{
    [MenuItem("Tools/Soulslike/Preparar parry del jugador")]
    public static void Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Salí de Play antes de preparar parry.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/Scenes/PrototypeArena.unity");
        var player = UnityEngine.Object.FindFirstObjectByType<PrototypePlayerAttack>();
        if (player == null || UnityEngine.Object.FindFirstObjectByType<PrototypeEnemyAttack>() == null)
            throw new InvalidOperationException("Se necesita el duelo guardado.");
        var parry = player.GetComponent<PrototypePlayerParry>();
        if (parry == null) parry = player.gameObject.AddComponent<PrototypePlayerParry>();
        Transform graphic = player.transform.Find("Señal de parry");
        if (graphic == null)
        {
            GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "Señal de parry";
            UnityEngine.Object.DestroyImmediate(plate.GetComponent<Collider>());
            plate.transform.SetParent(player.transform, false);
            plate.transform.localPosition = new Vector3(0.65f, 0.1f, 0.65f);
            plate.transform.localScale = new Vector3(0.85f, 0.85f, 0.06f);
            plate.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            graphic = plate.transform;
        }
        // Rombo lateral de estado; el contorno frontal representa el volumen real.
        graphic.localPosition = new Vector3(0.8f, 0.6f, 0.25f);
        graphic.localScale = new Vector3(0.4f, 0.4f, 0.06f);
        Renderer renderer = graphic.GetComponent<Renderer>();
        renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Prototype/DiagnosticoVolumen.mat");
        if (renderer.sharedMaterial == null) throw new InvalidOperationException("Falta el material Unlit del diagnóstico.");
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.enabled = false;
        Transform volumeObject = player.transform.Find("Volumen defensivo parry");
        if (volumeObject == null)
        {
            volumeObject = new GameObject("Volumen defensivo parry").transform;
            volumeObject.SetParent(player.transform, false);
        }
        volumeObject.localPosition = new Vector3(0f, 0.1f, 0.65f);
        var volume = volumeObject.GetComponent<BoxCollider>();
        if (volume == null) volume = volumeObject.gameObject.AddComponent<BoxCollider>();
        volume.isTrigger = true; volume.size = new Vector3(0.8f, 1.3f, 0.3f);
        var outline = volumeObject.GetComponent<LineRenderer>();
        if (outline == null) outline = volumeObject.gameObject.AddComponent<LineRenderer>();
        outline.sharedMaterial = renderer.sharedMaterial;
        outline.useWorldSpace = false; outline.widthMultiplier = 0.035f;
        outline.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        int[] path = { 0, 1, 3, 2, 0, 4, 5, 1, 5, 7, 3, 7, 6, 2, 6, 4, 0 };
        outline.positionCount = path.Length;
        Vector3 half = volume.size * 0.5f;
        for (int i = 0; i < path.Length; i++)
        {
            int vertex = path[i];
            outline.SetPosition(i, new Vector3((vertex & 1) == 0 ? -half.x : half.x,
                (vertex & 2) == 0 ? -half.y : half.y, (vertex & 4) == 0 ? -half.z : half.z));
        }
        bool upgrade = parry.DefensiveVolume == null;
        parry.Configure(renderer, volume, outline);
        var settings = new SerializedObject(parry);
        if (upgrade) settings.FindProperty("activeDuration").floatValue = 0.12f;
        settings.ApplyModifiedPropertiesWithoutUndo();
        if (player.GetComponent<PrototypeParryFeedback>() == null) player.gameObject.AddComponent<PrototypeParryFeedback>();
        var audio = player.GetComponent<AudioSource>();
        audio.playOnAwake = false;
        EditorUtility.SetDirty(parry);
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        AssetDatabase.SaveAssets();
        if (!EditorSceneManager.SaveScene(player.gameObject.scene)) throw new InvalidOperationException("No se pudo guardar parry.");
        Debug.Log("Parry preparado: Q/clic derecho, azul activo, blanco acierto, naranja apertura enemiga.");
    }

    public static void PrepareValidateAndBuild()
    {
        Prepare();
        Prepare();
        if (UnityEngine.Object.FindObjectsByType<PrototypePlayerParry>(FindObjectsSortMode.None).Length != 1)
            throw new InvalidOperationException("La preparación duplicó el parry.");
        PrototypeInputValidation.ValidateAndBuild();
    }

    public static void PrepareCaptureValidateAndBuild()
    {
        Prepare();
        PrototypeParryValidation.Capture();
        PrepareValidateAndBuild();
    }
}
