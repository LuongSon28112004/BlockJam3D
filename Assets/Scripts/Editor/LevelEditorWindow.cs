using UnityEngine;
using UnityEditor;
using System.IO;

public class LevelEditorWindow : EditorWindow
{
    private int width = 5;  // số cột (ngang)
    private int height = 3; // số hàng (dọc)
    private string[] prefabNames;
    private int selectedTileIndex = 0;

    private LevelData currentLevel;

    private static readonly string[] TileOptions = new string[]
    {
        "Empty",
        "Wall",
        "Container",
        "1",
        "2",
        "3",
        "4"
    };

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

    private void OnGUI()
    {
        GUILayout.Label("⚙️ Level Settings", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        width = Mathf.Max(1, EditorGUILayout.IntField("Columns (ngang)", width));
        height = Mathf.Max(1, EditorGUILayout.IntField("Rows (dọc)", height));
        if (EditorGUI.EndChangeCheck())
        {
            ResizeGrid(width, height);
        }

        GUILayout.Space(10);
        GUILayout.Label("🎨 Tile Types", EditorStyles.boldLabel);

        selectedTileIndex = GUILayout.Toolbar(selectedTileIndex, TileOptions);

        GUILayout.Space(10);
        DrawGrid();

        GUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🆕 New Level")) prefabNames = new string[width * height];
        if (GUILayout.Button("💾 Save Level")) SaveLevel();
        if (GUILayout.Button("📂 Open Level")) OpenLevel();
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);
        EditorGUILayout.HelpBox("Chế độ này chỉ để vẽ layout (Empty / Wall / Container / 1 / 2 / 3). Không có chức năng generate trong scene.", MessageType.Info);
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
        const float cellWidth = 40f;
        const float cellHeight = 20f;

        GUILayout.Label("🧱 Level Grid", EditorStyles.boldLabel);

        for (int row = 0; row < height; row++) // mỗi hàng
        {
            EditorGUILayout.BeginHorizontal();
            for (int col = 0; col < width; col++) // mỗi cột
            {
                int index = row * width + col;
                string display = "Empty";

                if (prefabNames != null && index < prefabNames.Length && !string.IsNullOrEmpty(prefabNames[index]))
                    display = prefabNames[index];

                if (GUILayout.Button(display, GUILayout.Width(cellWidth), GUILayout.Height(cellHeight)))
                {
                    prefabNames[index] = TileOptions[selectedTileIndex];
                }
            }
            EditorGUILayout.EndHorizontal();
        }
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
        string absPath = EditorUtility.OpenFilePanel("Open Level", Application.dataPath, "asset");
        if (string.IsNullOrEmpty(absPath)) return;

        string path = FileUtil.GetProjectRelativePath(absPath);
        currentLevel = AssetDatabase.LoadAssetAtPath<LevelData>(path);
        if (currentLevel != null)
        {
            width = currentLevel.width;
            height = currentLevel.height;
            prefabNames = (string[])currentLevel.prefabNames.Clone();
            Debug.Log($"📂 Loaded level: {path}");
        }
        else
        {
            Debug.LogWarning("⚠️ Không thể load LevelData từ đường dẫn đã chọn.");
        }
    }
}
