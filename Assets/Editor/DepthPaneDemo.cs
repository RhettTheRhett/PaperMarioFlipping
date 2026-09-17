using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DepthPaneDemo
{
    // Run in batch mode, or from Tools after saving your current scene.
    [MenuItem("Tools/Paper Mario/Create Depth Pane Demo")]
    public static void Create()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("WorldStateManager").AddComponent<WorldStateManager>();
        PaneManager manager = new GameObject("PaneManager").AddComponent<PaneManager>();
        manager.createFallbackPane = false;
        manager.graphicsRange = 0;

        MakeArea("Front area", 0, 0, new Color(0.45f, 0.7f, 0.35f));
        MakeArea("Back area", 12, 1, new Color(0.35f, 0.55f, 0.85f));
        // Shared floor links the two slices when walking along depth in 3D.
        MakeBox("Shared crossing", null, new Vector3(-7, -0.5f, 6), new Vector3(5, 1, 24), Color.gray);
        GameObject holder = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PREFABS/PlayerHolder.prefab"));
        PlayerStateManager player = holder.GetComponentInChildren<PlayerStateManager>();
        player.transform.position = new Vector3(-7, 1.2f, 0);
        player.ground = 1 << LayerMask.NameToLayer("ground");
        MoveCamera camera = holder.GetComponentInChildren<MoveCamera>();
        camera.target = player.transform;
        camera.transform.position = player.transform.position + camera.offset;
        Light light = new GameObject("Sun").AddComponent<Light>();
        light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(45, -30, 0);
        RenderSettings.ambientLight = Color.gray;
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/DepthPaneDemo.unity");
    }

    private static void MakeArea(string name, float depth, int index, Color color)
    {
        GameObject area = new GameObject(name);
        area.transform.position = new Vector3(0, 0, depth);
        Pane pane = area.AddComponent<Pane>();
        pane.paneIndex = index;
        pane.thickness = 12;
        MakeBox("Floor", area.transform, new Vector3(0, -0.5f, 0), new Vector3(24, 1, 12), color);
        MakeBox("Walk behind me in 3D", area.transform, new Vector3(0, 1.5f, 0), new Vector3(2, 3, 2), color);
        MakeBox("Platform", area.transform, new Vector3(5, 0.75f, 1), new Vector3(3, 1.5f, 2), color);
    }

    private static void MakeBox(string name, Transform parent, Vector3 position, Vector3 size, Color color)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.layer = LayerMask.NameToLayer("ground");
        box.transform.SetParent(parent, false);
        box.transform.localPosition = position;
        box.transform.localScale = size;
        // Use a saved material so the demo also retains its colors after reopening.
        string path = "Assets/ART/PaneDemo-" + ColorUtility.ToHtmlStringRGB(color) + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Standard")) { color = color };
            AssetDatabase.CreateAsset(material, path);
        }
        box.GetComponent<Renderer>().sharedMaterial = material;
    }
}
