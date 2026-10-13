using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Convierte el objetivo de práctica en un atacante inmóvil y configura el reinicio.</summary>
public static class PrototypeDuelBuilder
{
    [MenuItem("Tools/Soulslike/Preparar duelo inmóvil")]
    public static void PrepareDuel()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Salí de Play antes de preparar el duelo.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        PrototypeCombatVisualBuilder.PrepareVisuals();
        var playerAttack = UnityEngine.Object.FindFirstObjectByType<PrototypePlayerAttack>();
        var target = UnityEngine.Object.FindFirstObjectByType<PrototypeTrainingTarget>();
        var player = playerAttack.GetComponent<PrototypeHealth>();
        var enemy = target.GetComponent<PrototypeHealth>();
        var enemyAttack = enemy.GetComponent<PrototypeEnemyAttack>();
        if (enemyAttack == null)
            enemyAttack = enemy.gameObject.AddComponent<PrototypeEnemyAttack>();

        Transform indicator = enemy.transform.Find("Alcance enemigo");
        if (indicator == null)
        {
            GameObject area = GameObject.CreatePrimitive(PrimitiveType.Cube);
            area.name = "Alcance enemigo";
            UnityEngine.Object.DestroyImmediate(area.GetComponent<Collider>());
            area.transform.SetParent(enemy.transform, false);
            area.transform.localPosition = new Vector3(0f, -0.9f, 1.15f);
            area.transform.localScale = new Vector3(1.4f, 0.04f, 1.4f);
            indicator = area.transform;
        }
        var areaRenderer = indicator.GetComponent<Renderer>();
        areaRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Prototype/ArmaProvisional.mat");
        areaRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        enemyAttack.Configure(player, areaRenderer);

        Transform weapon = enemy.transform.Find("Arma enemiga");
        if (weapon == null)
        {
            var weaponObject = UnityEngine.Object.Instantiate(player.transform.Find("Arma provisional H1").gameObject, enemy.transform);
            weaponObject.name = "Arma enemiga";
            weapon = weaponObject.transform;
        }
        string materialPath = "Assets/Materials/Prototype/ArmaEnemiga.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "ArmaEnemiga" };
            material.SetColor("_BaseColor", new Color(1f, 0.25f, 0.5f));
            AssetDatabase.CreateAsset(material, materialPath);
        }
        foreach (Renderer renderer in weapon.GetComponentsInChildren<Renderer>())
            renderer.sharedMaterial = material;
        var enemyPresentation = enemy.GetComponent<PrototypeAttackPresentation>();
        if (enemyPresentation == null)
            enemyPresentation = enemy.gameObject.AddComponent<PrototypeAttackPresentation>();
        enemyPresentation.Configure(enemyAttack, weapon);

        Transform playerGraphic = player.transform.Find("Señal de daño jugador");
        if (playerGraphic == null)
        {
            var graphicObject = UnityEngine.Object.Instantiate(enemy.transform.Find("Señal de impacto H1").gameObject, player.transform);
            graphicObject.name = "Señal de daño jugador";
            playerGraphic = graphicObject.transform;
        }
        var playerFeedback = player.GetComponent<PrototypeHitPresentation>();
        if (playerFeedback == null)
            playerFeedback = player.gameObject.AddComponent<PrototypeHitPresentation>();
        playerFeedback.Configure(player, playerGraphic);
        var session = player.GetComponent<PrototypeDuelSession>();
        if (session == null)
            session = player.gameObject.AddComponent<PrototypeDuelSession>();
        session.Configure(player, enemy, Camera.main.GetComponent<PrototypeFollowCamera>());
        player.GetComponent<PrototypeCombatHUD>().Configure(playerAttack, enemy);
        target.Configure(enemy);
        EditorUtility.SetDirty(enemyAttack);
        EditorUtility.SetDirty(enemyPresentation);
        EditorUtility.SetDirty(playerFeedback);
        EditorUtility.SetDirty(session);
        EditorUtility.SetDirty(target);
        EditorUtility.SetDirty(player.GetComponent<PrototypeCombatHUD>());
        AssetDatabase.SaveAssets();
        var scene = player.gameObject.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("No se pudo guardar el duelo.");
        Debug.Log("Duelo inmóvil guardado: preparación violeta, golpe de 20, R reinicia ambos personajes.");
    }

    public static void PrepareValidateAndBuild()
    {
        PrepareDuel();
        PrepareDuel();
        if (UnityEngine.Object.FindObjectsByType<PrototypeEnemyAttack>(FindObjectsSortMode.None).Length != 1
            || UnityEngine.Object.FindObjectsByType<PrototypeDuelSession>(FindObjectsSortMode.None).Length != 1
            || UnityEngine.Object.FindObjectsByType<PrototypeHealth>(FindObjectsSortMode.None).Length != 2)
            throw new InvalidOperationException("La preparación duplicó componentes del duelo.");
        PrototypeProjectValidation.ValidateAndBuildFromSceneList();
    }
}
