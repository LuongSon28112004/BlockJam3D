using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class LevelEditorWindow : EditorWindow
{
    private int width = 5;
    private int height = 3;
    private string[] prefabNames;
    private int selectedTileIndex = 0;

    private LevelData currentLevel;

    private int totalUnits = 0;
    private Dictionary<Vector2Int, GSPInfo> gspDownData = new Dictionary<Vector2Int, GSPInfo>();

    private static readonly string[] TileOptions = new string[]
    {
        "Empty", "Wall", "Container",
        "1","1B","2","2B","3","3B","4","4B", "5","5B","6", "6B","7","7B",
        "GSPDown","GSPUp","GSPLeft","GSPRight","GSPBottomRight"
    };

    [System.Serializable]
    private class GSPInfo
    {
        public string type;
        public int spawnCount;
    }

    [MenuItem("Game Tools/Level Editor (Simple Mode)")]
    public static void ShowWindow()
    {
        GetWindow<LevelEditorWindow>("Simple Level Editor");
    }

    private void OnEnable()
    {
        if (prefabNames == null || prefabNames.Length != width * height)
            prefabNames = new string[width * height];
    }

    private Vector2 scrollPos;

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.Label("⚙️ Level Settings", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        width = Mathf.Max(1, EditorGUILayout.IntField("Columns (ngang)", width));
        height = Mathf.Max(1, EditorGUILayout.IntField("Rows (dọc)", height));
        if (EditorGUI.EndChangeCheck())
            ResizeGrid(width, height);

        GUILayout.Space(10);
        totalUnits = EditorGUILayout.IntField("🎯 Total Units", totalUnits);

        GUILayout.Space(10);
        GUILayout.Label("🎨 Tile Types", EditorStyles.boldLabel);
        selectedTileIndex = GUILayout.Toolbar(selectedTileIndex, TileOptions);

        GUILayout.Space(10);
        DrawGrid();

        GUILayout.Space(10);
        DrawGSPSettings();

        GUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🆕 New Level"))
        {
            prefabNames = new string[width * height];
            gspDownData.Clear();
            totalUnits = 0;
        }
        if (GUILayout.Button("💾 Save Level")) SaveLevel();
        if (GUILayout.Button("📂 Open Level")) OpenLevel();
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Chế độ này chỉ để vẽ layout (Empty / Wall / Container / 1 / 2 / 3 / GSP...).",
            MessageType.Info
        );

        EditorGUILayout.EndScrollView();
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
        const float cellWidth = 50f;
        const float cellHeight = 25f;

        GUILayout.Label("🧱 Level Grid", EditorStyles.boldLabel);

        for (int row = 0; row < height; row++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int col = 0; col < width; col++)
            {
                int index = row * width + col;
                string display = prefabNames[index] ?? "Empty";

                if (GUILayout.Button(display, GUILayout.Width(cellWidth), GUILayout.Height(cellHeight)))
                {
                    string selected = TileOptions[selectedTileIndex];
                    prefabNames[index] = selected;

                    Vector2Int pos = new Vector2Int(col, row); // x = col, y = row

                    if (IsGSPTile(selected))
                    {
                        if (!gspDownData.ContainsKey(pos))
                            gspDownData[pos] = new GSPInfo { type = selected, spawnCount = 1 };
                        else
                            gspDownData[pos].type = selected;
                    }
                    else
                    {
                        gspDownData.Remove(pos);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawGSPSettings()
    {
        if (gspDownData.Count == 0) return;

        GUILayout.Label("🎯 GSP Spawn Settings", EditorStyles.boldLabel);

        List<Vector2Int> keys = new List<Vector2Int>(gspDownData.Keys);
        foreach (var pos in keys)
        {
            var info = gspDownData[pos];
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"{info.type} ({pos.x},{pos.y})", GUILayout.Width(150));
            info.spawnCount = EditorGUILayout.IntField("Spawn Count", info.spawnCount);
            if (GUILayout.Button("❌", GUILayout.Width(30)))
            {
                gspDownData.Remove(pos);
                prefabNames[pos.y * width + pos.x] = "Empty";
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private bool IsGSPTile(string name)
    {
        return name == "GSPDown" || name == "GSPUp" ||
               name == "GSPLeft" || name == "GSPRight" ||
               name == "GSPBottomRight";
    }

    private void SaveLevel()
    {
        string path = EditorUtility.SaveFilePanelInProject("Save Level", "NewLevel", "asset", "Save LevelData");
        if (string.IsNullOrEmpty(path)) return;

        LevelData data = ScriptableObject.CreateInstance<LevelData>();
        data.width = width;
        data.height = height;
        data.prefabNames = prefabNames;
        data.totalUnits = totalUnits;
        data.gsp = new List<LevelData.GSPData>();

        foreach (var kv in gspDownData)
        {
            // 🧩 Giữ đúng trục Unity 2D: x = col, y = row
            data.gsp.Add(new LevelData.GSPData
            {
                x = kv.Key.x,
                y = kv.Key.y,
                spawnCount = kv.Value.spawnCount,
                type = kv.Value.type
            });
        }

        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();

        currentLevel = data;
        Debug.Log($"✅ Saved level to: {path}");
    }

    private void OpenLevel()
    {
        string absPath = EditorUtility.OpenFilePanel("Open Level", Application.dataPath, "asset");
        if (string.IsNullOrEmpty(absPath)) return;

        string path = FileUtil.GetProjectRelativePath(absPath);
        currentLevel = AssetDatabase.LoadAssetAtPath<LevelData>(path);
        if (currentLevel != null)
        {
            width = currentLevel.width;
            height = currentLevel.height;
            prefabNames = (string[])currentLevel.prefabNames.Clone();
            totalUnits = currentLevel.totalUnits;

            gspDownData.Clear();
            foreach (var g in currentLevel.gsp)
            {
                // 🧩 Giữ đúng trục Unity 2D: x = col, y = row
                Vector2Int pos = new Vector2Int(g.x, g.y);
                gspDownData[pos] = new GSPInfo
                {
                    spawnCount = g.spawnCount,
                    type = g.type
                };
            }

            Debug.Log($"📂 Loaded level: {path}");
        }
        else
        {
            Debug.LogWarning("⚠️ Không thể load LevelData từ đường dẫn đã chọn.");
        }
    }
}
