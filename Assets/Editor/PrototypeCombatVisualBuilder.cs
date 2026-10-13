using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Agrega geometría visual al ejercicio H1 y conserva sus objetos existentes.</summary>
public static class PrototypeCombatVisualBuilder
{
    [MenuItem("Tools/Soulslike/Preparar lectura visual H1")]
    public static void PrepareVisuals()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Salí de Play antes de preparar la representación visual.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        PrototypeCombatBuilder.PrepareCombat();
        var attack = UnityEngine.Object.FindFirstObjectByType<PrototypePlayerAttack>();
        var target = UnityEngine.Object.FindFirstObjectByType<PrototypeTrainingTarget>();
        if (attack == null || target == null)
            throw new InvalidOperationException("Faltan jugador u objetivo H1.");
        Material blade = GetMaterial("ArmaProvisional", new Color(0.8f, 0.9f, 1f));
        Material grip = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Prototype/Obstaculos.mat");
        Material spark = GetMaterial("ImpactoProvisional", new Color(1f, 0.85f, 0.1f));
        // El material heredado puede usar otro shader; la presentación requiere _BaseColor URP.
        Renderer playerRenderer = attack.GetComponent<Renderer>();
        Debug.Log("Material heredado del jugador: " + playerRenderer.sharedMaterial.shader.name
            + "; soporte _BaseColor: " + playerRenderer.sharedMaterial.HasProperty("_BaseColor"));
        playerRenderer.sharedMaterial = GetMaterial("JugadorVisual", new Color(1f, 0.55f, 0.08f));
        target.GetComponent<Renderer>().sharedMaterial = GetMaterial("ObjetivoVisual", new Color(0.1f, 0.8f, 0.45f));
        attack.transform.Find("Alcance del ataque H1").GetComponent<Renderer>().sharedMaterial = blade;

        Transform pivot = attack.transform.Find("Arma provisional H1");
        if (pivot == null)
        {
            pivot = new GameObject("Arma provisional H1").transform;
            pivot.SetParent(attack.transform, false);
            CreatePart(pivot, "Empuñadura", new Vector3(0f, 0f, 0.05f), new Vector3(0.13f, 0.13f, 0.35f), grip);
            CreatePart(pivot, "Guarda", new Vector3(0f, 0f, 0.25f), new Vector3(0.5f, 0.1f, 0.1f), blade);
            CreatePart(pivot, "Hoja", new Vector3(0f, 0f, 0.85f), new Vector3(0.22f, 0.07f, 1.1f), blade);
        }
        var presentation = attack.GetComponent<PrototypeAttackPresentation>();
        if (presentation == null)
            presentation = attack.gameObject.AddComponent<PrototypeAttackPresentation>();
        presentation.Configure(attack, pivot);

        Transform graphic = target.transform.Find("Señal de impacto H1");
        if (graphic == null)
        {
            graphic = new GameObject("Señal de impacto H1").transform;
            graphic.SetParent(target.transform, false);
            for (int index = 0; index < 4; index++)
            {
                Transform ray = CreatePart(graphic, "Rayo " + index, Vector3.zero, new Vector3(0.65f, 0.065f, 0.065f), spark);
                ray.localRotation = Quaternion.Euler(0f, 0f, index * 45f);
            }
        }
        var feedback = target.GetComponent<PrototypeHitPresentation>();
        if (feedback == null)
            feedback = target.gameObject.AddComponent<PrototypeHitPresentation>();
        feedback.Configure(target.GetComponent<PrototypeHealth>(), graphic);
        EditorUtility.SetDirty(presentation);
        EditorUtility.SetDirty(feedback);
        AssetDatabase.SaveAssets();
        var scene = attack.gameObject.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("No se pudo guardar la representación visual H1.");
        Debug.Log("Representación visual H1 guardada: arma procedural y señal de daño real.");
    }

    private static Material GetMaterial(string name, Color color)
    {
        string path = "Assets/Materials/Prototype/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
            return material;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            throw new InvalidOperationException("Falta el shader URP/Lit.");
        material = new Material(shader) { name = name };
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", 0.3f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Transform CreatePart(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        return part.transform;
    }
}
