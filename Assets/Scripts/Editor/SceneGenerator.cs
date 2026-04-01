#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

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

        LevelGenerator.GenerateAll();
        CreateMainMenu();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("All scenes generated successfully.");
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
