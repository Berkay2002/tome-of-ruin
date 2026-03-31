#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cinemachine;

public static class SceneGenerator
{
    [MenuItem("Tools/Generate Scenes")]
    public static void GenerateAll()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("Cannot generate scenes during Play Mode. Stop Play Mode first.");
            return;
        }

        EnsureDirectory("Assets/Scenes");

        CreateZoneScene("ZoneA", "Starting Ruins", new string[] { "Hollow", "Hollow", "Wraith", "Knight" });
        CreateZoneScene("ZoneB", "Catacombs", new string[] { "Wraith", "Knight", "Caster", "Hollow" });
        CreateZoneScene("ZoneC", "Cursed Chapel", new string[] { "Knight", "Caster", "Wraith", "Caster" });
        CreateBossArena();
        CreateMainMenu();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("All scenes generated with player, enemies, camera follow, and interactables!");
    }

    private static void EnsureDirectory(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            string folder = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }

    private static GameObject SetupCamera()
    {
        var cameraObj = new GameObject("Main Camera");
        cameraObj.tag = "MainCamera";
        var cam = cameraObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.1f);
        cameraObj.transform.position = new Vector3(0, 0, -10);

        // Add CinemachineBrain so virtual cameras work
        cameraObj.AddComponent<CinemachineBrain>();

        return cameraObj;
    }

    private static GameObject SetupVirtualCamera(Transform followTarget)
    {
        var vcamObj = new GameObject("CM vcam1");
        var vcam = vcamObj.AddComponent<CinemachineVirtualCamera>();
        vcam.m_Lens.OrthographicSize = 5f;
        vcam.m_Lens.NearClipPlane = 0.1f;
        vcam.m_Lens.FarClipPlane = 100f;
        vcam.Follow = followTarget;

        // Use framing transposer for 2D follow
        var body = vcam.AddCinemachineComponent<CinemachineFramingTransposer>();
        body.m_LookaheadTime = 0f;
        body.m_DeadZoneWidth = 0.1f;
        body.m_DeadZoneHeight = 0.1f;
        body.m_SoftZoneWidth = 0.5f;
        body.m_SoftZoneHeight = 0.5f;
        body.m_CameraDistance = 10f;

        // Add impulse listener for screen shake
        vcamObj.AddComponent<CinemachineImpulseListener>();

        return vcamObj;
    }

    private static GameObject SpawnPrefab(string prefabPath, Vector3 position)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"Prefab not found: {prefabPath}. Run Tools > Generate Prefabs first.");
            // Create a placeholder
            var placeholder = new GameObject(System.IO.Path.GetFileNameWithoutExtension(prefabPath));
            placeholder.transform.position = position;
            return placeholder;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = position;
        return instance;
    }

    private static void CreateZoneScene(string sceneName, string zoneName, string[] enemyNames)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera with solid background
        SetupCamera();

        // Player instance
        var player = SpawnPrefab("Assets/Prefabs/Player/Player.prefab", Vector3.zero);

        // Cinemachine virtual camera following the player
        SetupVirtualCamera(player.transform);

        // Screen shake source on player
        var shakeObj = new GameObject("ScreenShake");
        shakeObj.transform.SetParent(player.transform);
        shakeObj.AddComponent<CinemachineImpulseSource>();
        shakeObj.AddComponent<ScreenShake>();

        // Zone Label (for reference)
        new GameObject($"--- {zoneName} ---");

        // Enemy instances
        for (int i = 0; i < enemyNames.Length; i++)
        {
            string enemyName = enemyNames[i];
            Vector3 pos = new Vector3(5 + i * 4, Random.Range(-3f, 3f), 0);
            SpawnPrefab($"Assets/Prefabs/Enemies/{enemyName}.prefab", pos);
        }

        // Shrine
        SpawnPrefab("Assets/Prefabs/Interactables/Shrine.prefab", new Vector3(-3, 0, 0));

        // Scene transition trigger
        var transitionOut = new GameObject("SceneTransition_Out");
        transitionOut.transform.position = new Vector3(25, 0, 0);

        EditorSceneManager.SaveScene(scene, $"Assets/Scenes/{sceneName}.unity");
    }

    private static void CreateBossArena()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera with dark red background
        var cameraObj = new GameObject("Main Camera");
        cameraObj.tag = "MainCamera";
        var cam = cameraObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.1f, 0.02f, 0.02f);
        cameraObj.transform.position = new Vector3(0, 0, -10);
        cameraObj.AddComponent<CinemachineBrain>();

        // Player
        var player = SpawnPrefab("Assets/Prefabs/Player/Player.prefab", new Vector3(0, -4, 0));

        // Virtual camera
        SetupVirtualCamera(player.transform);

        // Screen shake
        var shakeObj = new GameObject("ScreenShake");
        shakeObj.transform.SetParent(player.transform);
        shakeObj.AddComponent<CinemachineImpulseSource>();
        shakeObj.AddComponent<ScreenShake>();

        // Boss spawn point (boss not instantiated — spawned by BossController)
        var bossSpawn = new GameObject("BossSpawn");
        bossSpawn.transform.position = new Vector3(0, 3, 0);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/BossArena.unity");
    }

    private static void CreateMainMenu()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cameraObj = new GameObject("Main Camera");
        cameraObj.tag = "MainCamera";
        var cam = cameraObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.1f);
        cameraObj.transform.position = new Vector3(0, 0, -10);

        // Canvas
        var canvasObj = new GameObject("Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Title text
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(canvasObj.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 100);
        titleRect.sizeDelta = new Vector2(600, 100);
        var titleText = titleObj.AddComponent<Text>();
        titleText.text = "Tome of Ruin";
        titleText.fontSize = 48;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;

        // Start Button
        var buttonObj = new GameObject("StartButton");
        buttonObj.transform.SetParent(canvasObj.transform, false);
        var buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchoredPosition = new Vector2(0, -50);
        buttonRect.sizeDelta = new Vector2(200, 60);
        var buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.3f, 0.3f, 0.3f);
        buttonObj.AddComponent<Button>();

        // Button text
        var btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(buttonObj.transform, false);
        var btnTextRect = btnTextObj.AddComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.sizeDelta = Vector2.zero;
        var btnText = btnTextObj.AddComponent<Text>();
        btnText.text = "Start Game";
        btnText.fontSize = 24;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.color = Color.white;

        // EventSystem
        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
    }
}
#endif
