using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LevelData", menuName = "Game/Level Data (Prefab Mode)")]
public class LevelData : ScriptableObject
{
    public int width;
    public int height;
    public string[] prefabNames; // Lưu tên prefab tại từng ô
    public bool alignment;

    public int totalUnits; // Tổng số quân
    public List<GSPData> gsp = new List<GSPData>(); // Danh sách các ô GSPDown

    [System.Serializable]
    public class GSPData
    {
        public int x;
        public int y;
        public int spawnCount;
        public string type; // thêm dòng này
    }

    public string GetPrefabName(int x, int y)
    {
        if (prefabNames == null) return null;
        if (x < 0 || y < 0 || x >= width || y >= height) return null;
        int idx = y * width + x;
        if (idx < 0 || idx >= prefabNames.Length) return null;
        return prefabNames[idx];
    }

    public GSPData GetGSPAt(int x, int y, string type)
    {
        return gsp.Find(g => g.x == x && g.y == y && g.type == type);
    }


}
