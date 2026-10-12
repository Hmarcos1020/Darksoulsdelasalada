using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>Genera la arena una sola vez desde el editor y conserva escenas existentes.</summary>
public static class PrototypeArenaBuilder
{
    private const string ScenePath = "Assets/Scenes/PrototypeArena.unity";
    private const string MaterialsFolder = "Assets/Materials/Prototype";

    [MenuItem("Tools/Soulslike/Crear arena H0")]
    public static void CreateArena()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Salí del modo Play antes de crear o abrir la arena.");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        if (File.Exists(ScenePath))
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            return;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            Debug.LogError("No se encontró el shader URP/Lit. No se creó la arena.");
            return;
        }

        EnsureFolder("Assets", "Scenes");
        EnsureFolder("Assets", "Materials");
        EnsureFolder("Assets/Materials", "Prototype");
        Material ground = GetOrCreateMaterial("Suelo", shader, new Color(0.45f, 0.47f, 0.5f));
        Material dark = GetOrCreateMaterial("Obstaculos", shader, new Color(0.12f, 0.15f, 0.19f));
        Material visible = GetOrCreateMaterial("Jugador", shader, new Color(1f, 0.55f, 0.08f));

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCube("Suelo", new Vector3(0f, -0.25f, 0f), new Vector3(20f, 0.5f, 20f), ground);
        CreateCube("Pared norte", new Vector3(0f, 1.5f, 10f), new Vector3(21f, 3f, 1f), dark);
        CreateCube("Pared sur", new Vector3(0f, 1.5f, -10f), new Vector3(21f, 3f, 1f), dark);
        CreateCube("Pared este", new Vector3(10f, 1.5f, 0f), new Vector3(1f, 3f, 20f), dark);
        CreateCube("Pared oeste", new Vector3(-10f, 1.5f, 0f), new Vector3(1f, 3f, 20f), dark);
        CreateCube("Obstáculo 1", new Vector3(-4f, 1f, 2f), new Vector3(2f, 2f, 2f), dark);
        CreateCube("Obstáculo 2", new Vector3(3f, 0.75f, 3f), new Vector3(3f, 1.5f, 2f), dark);
        CreateCube("Obstáculo 3", new Vector3(4f, 1.25f, -3f), new Vector3(2f, 2.5f, 2f), dark);

        GameObject lightObject = new GameObject("Luz direccional");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.5f;
        light.shadows = LightShadows.Soft;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        lightObject.AddComponent<UniversalAdditionalLightData>();
        RenderSettings.sun = light;
        RenderSettings.ambientLight = new Color(0.4f, 0.4f, 0.4f);

        // El CharacterController es el único collider del jugador.
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Jugador";
        Object.DestroyImmediate(player.GetComponent<Collider>());
        player.transform.position = new Vector3(0f, 1.05f, -4f);
        player.GetComponent<Renderer>().sharedMaterial = visible;
        CharacterController controller = player.AddComponent<CharacterController>();
        controller.height = 2f;
        controller.radius = 0.5f;
        controller.center = Vector3.zero;
        controller.stepOffset = 0.3f;
        PrototypePlayerMovement movement = player.AddComponent<PrototypePlayerMovement>();

        GameObject cameraObject = new GameObject("Cámara principal");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 60f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.12f, 0.17f, 0.23f);
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<UniversalAdditionalCameraData>();
        PrototypeFollowCamera follow = cameraObject.AddComponent<PrototypeFollowCamera>();
        follow.Configure(player.transform);
        movement.Configure(cameraObject.transform);

        AssetDatabase.SaveAssets();
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            Debug.LogError("No se pudo guardar la arena en " + ScenePath);
            return;
        }
        Selection.activeGameObject = player;
        Debug.Log("Arena H0 guardada en " + ScenePath + ". Entrá en Play para probarla.");
    }

    private static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            AssetDatabase.CreateFolder(parent, name);
    }

    private static Material GetOrCreateMaterial(string name, Shader shader, Color color)
    {
        string path = MaterialsFolder + "/" + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
            return existing;
        Material material = new Material(shader) { name = name };
        material.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void CreateCube(string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.position = position;
        cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = material;
    }
}
