#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
// using UnityEngine.U2D; // Re-enable when SpriteShapeController visuals are added
using UnityEngine.Rendering.Universal;
using Cinemachine;
using System.Collections.Generic;

public static class LevelGenerator
{
    [MenuItem("Tools/Generate Levels")]
    public static void GenerateAll()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("LevelGenerator: Cannot generate levels during Play Mode.");
            return;
        }

        EnsureDirectory("Assets/Scenes");

        GenerateZoneA();
        GenerateZoneB();
        GenerateZoneC();
        GenerateBossArena();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("LevelGenerator: All zones generated with SpriteShape rooms, lighting, and enemies!");
    }

    // ----------------------------------------------------------------
    // ZoneA — Starting Ruins
    // ----------------------------------------------------------------
    private static void GenerateZoneA()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        string zone = "ZoneA";

        SetupSceneInfrastructure(zone,
            new Color(0.05f, 0.05f, 0.1f),
            0.15f,
            new Color(0.6f, 0.7f, 0.9f));

        var player = SetupPlayerAndCamera(Vector3.zero);

        // Rooms
        CreateRoom("Hub", zone, new Vector2[]
        {
            new Vector2(-8, -6), new Vector2(-8, 6), new Vector2(-2, 8),
            new Vector2(8, 6), new Vector2(10, 2), new Vector2(10, -2),
            new Vector2(8, -6), new Vector2(-2, -8)
        }, Vector2.zero);

        CreateRoom("SideA", zone, new Vector2[]
        {
            new Vector2(-5, -4), new Vector2(-5, 4), new Vector2(5, 4), new Vector2(5, -4)
        }, new Vector2(-22, 8));

        CreateRoom("SideB", zone, new Vector2[]
        {
            new Vector2(-4, -5), new Vector2(-4, 5), new Vector2(4, 5),
            new Vector2(6, 3), new Vector2(6, -3), new Vector2(4, -5)
        }, new Vector2(22, -8));

        CreateRoom("ShrineAlcove", zone, new Vector2[]
        {
            new Vector2(-3, -3), new Vector2(-3, 3), new Vector2(3, 3), new Vector2(3, -3)
        }, new Vector2(0, -20));

        CreateRoom("ExitB", zone, new Vector2[]
        {
            new Vector2(-4, -3), new Vector2(-4, 3), new Vector2(4, 3), new Vector2(4, -3)
        }, new Vector2(-22, -10));

        // Enemies
        SpawnPrefab("Assets/Prefabs/Enemies/Hollow.prefab", new Vector3(5, 3, 0));
        SpawnPrefab("Assets/Prefabs/Enemies/Hollow.prefab", new Vector3(-4, -3, 0));
        SpawnPrefab("Assets/Prefabs/Enemies/Wraith.prefab", new Vector3(7, -2, 0));
        SpawnPrefab("Assets/Prefabs/Enemies/Knight.prefab", new Vector3(-20, 8, 0));

        // Torches
        CreateTorchLight(new Vector3(-6, 0, 0), 2f, 0.3f);
        CreateTorchLight(new Vector3(6, 0, 0), 2f, 0.3f);
        CreateTorchLight(new Vector3(0, 5, 0), 2f, 0.3f);
        CreateTorchLight(new Vector3(-20, 9, 0), 2f, 0.3f);

        // Scene transitions
        CreateSceneTransition("ToZoneB", new Vector3(-26, -10, 0), "ZoneB");
        CreateSceneTransition("ToZoneC", new Vector3(26, 8, 0), "ZoneC");

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/ZoneA.unity");
    }

    // ----------------------------------------------------------------
    // ZoneB — Catacombs
    // ----------------------------------------------------------------
    private static void GenerateZoneB()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        string zone = "ZoneB";

        SetupSceneInfrastructure(zone,
            new Color(0.02f, 0.05f, 0.05f),
            0.1f,
            new Color(0.3f, 0.8f, 0.7f));

        var player = SetupPlayerAndCamera(Vector3.zero);

        // Rooms
        CreateRoom("Entry", zone, new Vector2[]
        {
            new Vector2(-5, -4), new Vector2(-5, 4), new Vector2(5, 5),
            new Vector2(7, 2), new Vector2(7, -2), new Vector2(5, -4)
        }, Vector2.zero);

        CreateRoom("Branch", zone, new Vector2[]
        {
            new Vector2(-6, -3), new Vector2(-6, 3), new Vector2(6, 3), new Vector2(6, -3)
        }, new Vector2(16, 0));

        CreateRoom("DeadEnd", zone, new Vector2[]
        {
            new Vector2(-3, -3), new Vector2(-3, 3), new Vector2(3, 4),
            new Vector2(4, 2), new Vector2(4, -2), new Vector2(3, -3)
        }, new Vector2(16, 12));

        CreateRoom("MiniBoss", zone, new Vector2[]
        {
            new Vector2(-7, -5), new Vector2(-7, 5), new Vector2(7, 5), new Vector2(7, -5)
        }, new Vector2(30, 0));

        CreateRoom("Shortcut", zone, new Vector2[]
        {
            new Vector2(-3, -3), new Vector2(-3, 3), new Vector2(3, 3), new Vector2(3, -3)
        }, new Vector2(-10, -12));

        // Enemies
        SpawnPrefab("Assets/Prefabs/Enemies/Wraith.prefab", new Vector3(3, 2, 0));
        SpawnPrefab("Assets/Prefabs/Enemies/Knight.prefab", new Vector3(18, 1, 0));
        SpawnPrefab("Assets/Prefabs/Enemies/Caster.prefab", new Vector3(16, 13, 0));
        SpawnPrefab("Assets/Prefabs/Enemies/Hollow.prefab", new Vector3(28, 3, 0));

        // Freeform lights (blue-green glow)
        CreateFreeformLight(new Vector3(5, 0, 0), new Color(0.3f, 1f, 0.8f), 0.5f);
        CreateFreeformLight(new Vector3(20, 2, 0), new Color(0.3f, 1f, 0.8f), 0.4f);
        CreateFreeformLight(new Vector3(16, 14, 0), new Color(0.3f, 1f, 0.8f), 0.6f);

        // Scene transitions
        CreateSceneTransition("ToZoneA", new Vector3(-8, 0, 0), "ZoneA");
        CreateSceneTransition("ToZoneC", new Vector3(37, 0, 0), "ZoneC");

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/ZoneB.unity");
    }

    // ----------------------------------------------------------------
    // ZoneC — Cursed Chapel
    // ----------------------------------------------------------------
    private static void GenerateZoneC()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        string zone = "ZoneC";

        SetupSceneInfrastructure(zone,
            new Color(0.08f, 0.03f, 0.05f),
            0.2f,
            new Color(0.6f, 0.2f, 0.3f));

        var player = SetupPlayerAndCamera(Vector3.zero);

        // Rooms
        CreateRoom("Entry", zone, new Vector2[]
        {
            new Vector2(-6, -5), new Vector2(-6, 5), new Vector2(6, 5), new Vector2(6, -5)
        }, Vector2.zero);

        CreateRoom("Hall", zone, new Vector2[]
        {
            new Vector2(-8, -6), new Vector2(-8, 6), new Vector2(8, 6), new Vector2(8, -6)
        }, new Vector2(0, 16));

        CreateRoom("MiniBoss", zone, new Vector2[]
        {
            new Vector2(-7, -6), new Vector2(-7, 6), new Vector2(7, 6), new Vector2(7, -6)
        }, new Vector2(0, 32));

        CreateRoom("Side", zone, new Vector2[]
        {
            new Vector2(-4, -4), new Vector2(-4, 4), new Vector2(4, 4), new Vector2(4, -4)
        }, new Vector2(16, 16));

        CreateRoom("Shortcut", zone, new Vector2[]
        {
            new Vector2(-3, -3), new Vector2(-3, 3), new Vector2(3, 3), new Vector2(3, -3)
        }, new Vector2(-16, 16));

        // Enemies
        SpawnPrefab("Assets/Prefabs/Enemies/Knight.prefab", new Vector3(3, 17, 0));
        SpawnPrefab("Assets/Prefabs/Enemies/Caster.prefab", new Vector3(-5, 18, 0));
        SpawnPrefab("Assets/Prefabs/Enemies/Wraith.prefab", new Vector3(2, 30, 0));
        SpawnPrefab("Assets/Prefabs/Enemies/Caster.prefab", new Vector3(17, 17, 0));

        // Crimson point lights
        CreatePointLight(new Vector3(0, 16, 0), new Color(0.8f, 0.1f, 0.15f), 2f, 8f);
        CreatePointLight(new Vector3(0, 32, 0), new Color(0.8f, 0.1f, 0.15f), 2f, 8f);

        // Freeform crimson light
        CreateFreeformLight(new Vector3(0, 20, 0), new Color(0.6f, 0.1f, 0.2f), 0.5f);

        // Scene transitions
        CreateSceneTransition("ToZoneA", new Vector3(0, -8, 0), "ZoneA");
        CreateSceneTransition("ToZoneB", new Vector3(-19, 16, 0), "ZoneB");
        CreateSceneTransition("ToBossArena", new Vector3(0, 40, 0), "BossArena");

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/ZoneC.unity");
    }

    // ----------------------------------------------------------------
    // BossArena
    // ----------------------------------------------------------------
    private static void GenerateBossArena()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        string zone = "BossArena";

        var globalLight = SetupSceneInfrastructure(zone,
            new Color(0.03f, 0.02f, 0.03f),
            0.2f,
            new Color(0.7f, 0.7f, 0.7f));

        var player = SetupPlayerAndCamera(new Vector3(0, -8, 0));

        // Circular arena — 12-point polygon, radius 12
        float radius = 12f;
        int pointCount = 12;
        var vertices = new Vector2[pointCount];
        for (int i = 0; i < pointCount; i++)
        {
            float angle = (2f * Mathf.PI * i) / pointCount;
            vertices[i] = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
        }

        CreateRoom("Arena", zone, vertices, Vector2.zero);

        // Boss spawn
        SpawnPrefab("Assets/Prefabs/Enemies/Boss.prefab", new Vector3(0, 4, 0));

        // 6 perimeter lights around circle (radius 10), start disabled
        var perimeterLights = new Light2D[6];
        for (int i = 0; i < 6; i++)
        {
            float angle = (2f * Mathf.PI * i) / 6f;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * 10f, Mathf.Sin(angle) * 10f, 0);

            var lightObj = new GameObject($"PerimeterLight_{i}");
            lightObj.transform.position = pos;
            var light = lightObj.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = new Color(1f, 0.3f, 0.2f);
            light.intensity = 0f;
            light.pointLightOuterRadius = 6f;
            lightObj.SetActive(false);
            perimeterLights[i] = light;
        }

        // BossArenaLighting trigger
        var triggerObj = new GameObject("BossArenaLighting");
        triggerObj.transform.position = new Vector3(0, -8, 0);
        var triggerCollider = triggerObj.AddComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;
        triggerCollider.size = new Vector2(6, 2);
        var bossLighting = triggerObj.AddComponent<BossArenaLighting>();
        bossLighting.globalLight = globalLight;
        bossLighting.perimeterLights = perimeterLights;

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/BossArena.unity");
    }

    // ----------------------------------------------------------------
    // Helper: Scene Infrastructure
    // ----------------------------------------------------------------
    private static Light2D SetupSceneInfrastructure(string zoneName, Color bgColor, float globalLightIntensity, Color globalLightColor)
    {
        // Main Camera
        var cameraObj = new GameObject("Main Camera");
        cameraObj.tag = "MainCamera";
        var cam = cameraObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = bgColor;
        cameraObj.transform.position = new Vector3(0, 0, -10);
        cameraObj.AddComponent<CinemachineBrain>();

        // Global Light 2D
        var lightObj = new GameObject("Global Light 2D");
        var globalLight = lightObj.AddComponent<Light2D>();
        globalLight.lightType = Light2D.LightType.Global;
        globalLight.intensity = globalLightIntensity;
        globalLight.color = globalLightColor;

        return globalLight;
    }

    // ----------------------------------------------------------------
    // Helper: Player + Virtual Camera
    // ----------------------------------------------------------------
    private static GameObject SetupPlayerAndCamera(Vector3 playerPos)
    {
        var player = SpawnPrefab("Assets/Prefabs/Player/Player.prefab", playerPos);

        // Cinemachine virtual camera
        var vcamObj = new GameObject("CM vcam1");
        var vcam = vcamObj.AddComponent<CinemachineVirtualCamera>();
        vcam.m_Lens.OrthographicSize = 5f;
        vcam.m_Lens.NearClipPlane = 0.1f;
        vcam.m_Lens.FarClipPlane = 100f;
        vcam.Follow = player.transform;

        var body = vcam.AddCinemachineComponent<CinemachineFramingTransposer>();
        body.m_LookaheadTime = 0f;
        body.m_DeadZoneWidth = 0.1f;
        body.m_DeadZoneHeight = 0.1f;
        body.m_SoftZoneWidth = 0.5f;
        body.m_SoftZoneHeight = 0.5f;
        body.m_CameraDistance = 10f;

        vcamObj.AddComponent<CinemachineConfiner>();
        vcamObj.AddComponent<CinemachineImpulseListener>();

        // ScreenShake child on player
        var shakeObj = new GameObject("ScreenShake");
        shakeObj.transform.SetParent(player.transform);
        shakeObj.AddComponent<CinemachineImpulseSource>();
        shakeObj.AddComponent<ScreenShake>();

        return player;
    }

    // ----------------------------------------------------------------
    // Helper: Create Room
    // ----------------------------------------------------------------
    private static void CreateRoom(string name, string zoneName, Vector2[] vertices, Vector2 position)
    {
        var roomObj = new GameObject($"Room_{name}");
        roomObj.transform.position = new Vector3(position.x, position.y, 0);

        // --- Walls (EdgeCollider2D only) ---
        // SpriteShapeController visuals deferred until art assets exist.
        // For now, walls are invisible colliders — functional for prototyping.
        var wallsObj = new GameObject("Walls");
        wallsObj.transform.SetParent(roomObj.transform, false);

        var edgeCollider = wallsObj.AddComponent<EdgeCollider2D>();
        var edgePoints = new Vector2[vertices.Length + 1];
        for (int i = 0; i < vertices.Length; i++)
            edgePoints[i] = vertices[i];
        edgePoints[vertices.Length] = vertices[0]; // Close the loop
        edgeCollider.points = edgePoints;

        // --- Floor (polygon mesh matching room shape) ---
        var floorObj = new GameObject("Floor");
        floorObj.transform.SetParent(roomObj.transform, false);

        var meshFilter = floorObj.AddComponent<MeshFilter>();
        var meshRenderer = floorObj.AddComponent<MeshRenderer>();
        meshRenderer.sortingLayerName = "Floor";
        meshRenderer.sortingOrder = 0;

        int[] triangles = TriangulatePolygon(vertices);
        if (triangles.Length > 0)
        {
            var mesh = new Mesh();
            var verts3d = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            float tileScale = 0.1f; // UV scale: 1 tile per 10 units
            for (int i = 0; i < vertices.Length; i++)
            {
                verts3d[i] = new Vector3(vertices[i].x, vertices[i].y, 0);
                uvs[i] = new Vector2(vertices[i].x * tileScale, vertices[i].y * tileScale);
            }
            mesh.vertices = verts3d;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            meshFilter.mesh = mesh;
        }

        var floorMat = AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/Floor_{zoneName}.mat");
        if (floorMat != null)
            meshRenderer.material = floorMat;

        // --- Camera Confiner ---
        var confinerObj = new GameObject("CameraConfiner");
        confinerObj.transform.SetParent(roomObj.transform, false);
        var confinerCollider = confinerObj.AddComponent<PolygonCollider2D>();
        confinerCollider.isTrigger = true;
        confinerCollider.points = vertices;

        // --- Room Trigger ---
        var bounds = CalculateBounds(vertices);
        var triggerObj = new GameObject("RoomTrigger");
        triggerObj.transform.SetParent(roomObj.transform, false);
        triggerObj.transform.localPosition = new Vector3(bounds.center.x, bounds.center.y, 0);
        var triggerCollider = triggerObj.AddComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;
        triggerCollider.size = bounds.size;
    }

    // ----------------------------------------------------------------
    // Helper: Torch Light (Point Light 2D + LightFlicker)
    // ----------------------------------------------------------------
    private static void CreateTorchLight(Vector3 position, float intensity, float flickerAmount)
    {
        var obj = new GameObject("TorchLight");
        obj.transform.position = position;
        var light = obj.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = new Color(1f, 0.7f, 0.3f);
        light.intensity = intensity;
        light.pointLightOuterRadius = 6f;

        var flicker = obj.AddComponent<LightFlicker>();
        flicker.baseIntensity = intensity;
        flicker.flickerAmount = flickerAmount;
    }

    // ----------------------------------------------------------------
    // Helper: Point Light 2D (no flicker)
    // ----------------------------------------------------------------
    private static void CreatePointLight(Vector3 position, Color color, float intensity, float radius)
    {
        var obj = new GameObject("PointLight");
        obj.transform.position = position;
        var light = obj.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.pointLightOuterRadius = radius;
    }

    // ----------------------------------------------------------------
    // Helper: Freeform Light 2D
    // ----------------------------------------------------------------
    private static void CreateFreeformLight(Vector3 position, Color color, float intensity)
    {
        var obj = new GameObject("FreeformLight");
        obj.transform.position = position;
        var light = obj.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Freeform;
        light.color = color;
        light.intensity = intensity;
    }

    // ----------------------------------------------------------------
    // Helper: Scene Transition
    // ----------------------------------------------------------------
    private static void CreateSceneTransition(string name, Vector3 position, string targetScene)
    {
        var obj = new GameObject($"SceneTransition_{name}");
        obj.transform.position = position;
        var collider = obj.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(2, 2);
        var transition = obj.AddComponent<SceneTransition>();
        transition.targetScene = targetScene;
    }

    // ----------------------------------------------------------------
    // Helper: Spawn Prefab
    // ----------------------------------------------------------------
    private static GameObject SpawnPrefab(string prefabPath, Vector3 position)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"LevelGenerator: Prefab not found: {prefabPath}. Run Tools > Generate Prefabs first.");
            var placeholder = new GameObject(System.IO.Path.GetFileNameWithoutExtension(prefabPath));
            placeholder.transform.position = position;
            return placeholder;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = position;
        return instance;
    }

    // ----------------------------------------------------------------
    // Helper: Calculate Bounds
    // ----------------------------------------------------------------
    // ----------------------------------------------------------------
    // Helper: Ear-clipping polygon triangulation
    // ----------------------------------------------------------------
    private static int[] TriangulatePolygon(Vector2[] polygon)
    {
        // Ensure counter-clockwise winding (ear-clipping requires CCW)
        float signedArea = 0;
        for (int i = 0; i < polygon.Length; i++)
        {
            int j = (i + 1) % polygon.Length;
            signedArea += polygon[i].x * polygon[j].y;
            signedArea -= polygon[j].x * polygon[i].y;
        }
        if (signedArea < 0)
        {
            // Clockwise — reverse to make CCW
            polygon = (Vector2[])polygon.Clone();
            System.Array.Reverse(polygon);
        }

        var indices = new List<int>();
        var remaining = new List<int>();
        for (int i = 0; i < polygon.Length; i++)
            remaining.Add(i);

        int safety = polygon.Length * polygon.Length;
        while (remaining.Count > 2 && safety-- > 0)
        {
            bool earFound = false;
            for (int i = 0; i < remaining.Count; i++)
            {
                int prevIdx = (i - 1 + remaining.Count) % remaining.Count;
                int nextIdx = (i + 1) % remaining.Count;
                int prev = remaining[prevIdx];
                int curr = remaining[i];
                int next = remaining[nextIdx];

                if (!IsConvex(polygon[prev], polygon[curr], polygon[next]))
                    continue;

                bool containsPoint = false;
                for (int j = 0; j < remaining.Count; j++)
                {
                    if (j == prevIdx || j == i || j == nextIdx) continue;
                    if (PointInTriangle(polygon[remaining[j]], polygon[prev], polygon[curr], polygon[next]))
                    {
                        containsPoint = true;
                        break;
                    }
                }

                if (!containsPoint)
                {
                    indices.Add(prev);
                    indices.Add(curr);
                    indices.Add(next);
                    remaining.RemoveAt(i);
                    earFound = true;
                    break;
                }
            }
            if (!earFound) break;
        }
        return indices.ToArray();
    }

    private static bool IsConvex(Vector2 a, Vector2 b, Vector2 c)
    {
        return ((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x)) > 0;
    }

    private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
        float d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y);
        float d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
        return !(hasNeg && hasPos);
    }

    private static Rect CalculateBounds(Vector2[] vertices)
    {
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        for (int i = 0; i < vertices.Length; i++)
        {
            if (vertices[i].x < minX) minX = vertices[i].x;
            if (vertices[i].y < minY) minY = vertices[i].y;
            if (vertices[i].x > maxX) maxX = vertices[i].x;
            if (vertices[i].y > maxY) maxY = vertices[i].y;
        }

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    // ----------------------------------------------------------------
    // Helper: Ensure Directory
    // ----------------------------------------------------------------
    private static void EnsureDirectory(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            string folder = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
#endif
