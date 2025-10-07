using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Game/Level Data (Prefab Mode)")]
public class LevelData : ScriptableObject
{
    public int width;
    public int height;
    public string[] prefabNames; // Lưu tên prefab tại từng ô
    public bool alignment;

    // optional helper
    public string GetPrefabName(int x, int y)
    {
        if (prefabNames == null) return null;
        if (x < 0 || y < 0 || x >= width || y >= height) return null;
        int idx = y * width + x;
        if (idx < 0 || idx >= prefabNames.Length) return null;
        return prefabNames[idx];
    }
}
