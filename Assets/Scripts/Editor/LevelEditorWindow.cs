using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
public class LevelEditorWindow : EditorWindow
{
    private int width = 5;
    private int height = 3;
    private string[] prefabNames; // Lưu tên prefab theo từng ô
    private int selectedTileIndex = 0;

    public TileEntry[] tileEntries; // Danh sách prefab tự động load

    private LevelData currentLevel;

    [MenuItem("Game Tools/Level Editor (Prefab Mode)")]
    public static void ShowWindow()
    {
        GetWindow<LevelEditorWindow>("Prefab Level Editor");
    }

    private void OnEnable()
    {
        // Load sẵn prefab khi mở tool
        LoadPrefabs();

        if (prefabNames == null || prefabNames.Length != width * height)
            prefabNames = new string[width * height];
    }

    private void OnGUI()
    {
        GUILayout.Label("⚙️ Level Settings", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        width = EditorGUILayout.IntField("Width", width);
        height = EditorGUILayout.IntField("Height", height);
        if (EditorGUI.EndChangeCheck())
        {
            ResizeGrid(width, height);
        }

        GUILayout.Space(10);
        GUILayout.Label("🎨 Tile Prefabs (Auto Loaded from Assets/Prefabs)", EditorStyles.boldLabel);

        if (GUILayout.Button("🔄 Reload Prefabs"))
        {
            LoadPrefabs();
        }

        if (tileEntries == null || tileEntries.Length == 0)
        {
            EditorGUILayout.HelpBox("⚠️ Không tìm thấy prefab nào trong thư mục Assets/Prefabs!", MessageType.Warning);
            return;
        }

        GUILayout.Space(10);
        selectedTileIndex = GUILayout.Toolbar(selectedTileIndex, GetTileNames());

        GUILayout.Space(10);
        DrawGrid();

        GUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🆕 New Level")) prefabNames = new string[width * height];
        if (GUILayout.Button("💾 Save Level")) SaveLevel();
        if (GUILayout.Button("📂 Open Level")) OpenLevel();
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);
        if (GUILayout.Button("🚀 Generate Level in Scene"))
            GenerateLevelInScene();
    }

    // 🔹 Tự động load prefab trong thư mục Assets/Prefabs
    private void LoadPrefabs()
    {
        string[] prefabGUIDs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources/Prefabs" });
        List<TileEntry> entries = new List<TileEntry>();

        foreach (string guid in prefabGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                entries.Add(new TileEntry { name = prefab.name, prefab = prefab });
            }
        }

        tileEntries = entries.ToArray();
        Debug.Log($"✅ Loaded {tileEntries.Length} prefabs from Assets/Prefabs/");
    }

    private void ResizeGrid(int newWidth, int newHeight)
    {
        string[] newGrid = new string[newWidth * newHeight];
        if (prefabNames != null)
        {
            for (int y = 0; y < Mathf.Min(height, newHeight); y++)
            {
                for (int x = 0; x < Mathf.Min(width, newWidth); x++)
                {
                    int oldIndex = y * width + x;
                    int newIndex = y * newWidth + x;
                    if (oldIndex < prefabNames.Length)
                        newGrid[newIndex] = prefabNames[oldIndex];
                }
            }
        }
        prefabNames = newGrid;
        width = newWidth;
        height = newHeight;
    }

    private void DrawGrid()
    {
        for (int y = 0; y < height; y++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                string display = string.IsNullOrEmpty(prefabNames[index]) ? "Empty" : prefabNames[index];
                if (GUILayout.Button(display, GUILayout.Width(70), GUILayout.Height(30)))
                {
                    prefabNames[index] = tileEntries[selectedTileIndex].name;
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private string[] GetTileNames()
    {
        string[] names = new string[tileEntries.Length];
        for (int i = 0; i < names.Length; i++)
            names[i] = tileEntries[i].name;
        return names;
    }

    private void SaveLevel()
    {
        string path = EditorUtility.SaveFilePanelInProject("Save Level", "NewLevel", "asset", "Save LevelData");
        if (string.IsNullOrEmpty(path)) return;

        LevelData data = ScriptableObject.CreateInstance<LevelData>();
        data.width = width;
        data.height = height;
        data.prefabNames = prefabNames;

        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();

        currentLevel = data;
        Debug.Log($"✅ Saved level to: {path}");
    }

    private void OpenLevel()
    {
        string path = EditorUtility.OpenFilePanel("Open Level", "Assets", "asset");
        if (string.IsNullOrEmpty(path)) return;

        path = FileUtil.GetProjectRelativePath(path);
        currentLevel = AssetDatabase.LoadAssetAtPath<LevelData>(path);
        if (currentLevel != null)
        {
            width = currentLevel.width;
            height = currentLevel.height;
            prefabNames = (string[])currentLevel.prefabNames.Clone();
            Debug.Log($"📂 Loaded level: {path}");
        }
    }

    private void GenerateLevelInScene()
    {
        if (tileEntries == null || tileEntries.Length == 0)
        {
            Debug.LogWarning("⚠️ No tile prefabs assigned!");
            return;
        }

        GameObject root = new GameObject("GeneratedLevel");

        float offsetX = 1.25f; // khoảng cách giữa các ô theo trục X
        float offsetZ = 1.25f; // khoảng cách giữa các ô theo trục Z

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                string name = prefabNames[y * width + x];
                if (string.IsNullOrEmpty(name)) continue;

                TileEntry entry = System.Array.Find(tileEntries, t => t.name == name);
                if (entry != null && entry.prefab != null)
                {
                    GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(entry.prefab);

                    // ✅ Tính vị trí: X tăng dần, Z giảm dần
                    float posX = x * offsetX;
                    float posZ = -y * offsetZ;

                    obj.transform.position = new Vector3(posX, 0f, posZ);
                    obj.transform.SetParent(root.transform);
                }
            }
        }

        Debug.Log("✅ Level generated in scene with prefab grid layout!");
    }
}
