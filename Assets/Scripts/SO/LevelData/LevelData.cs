using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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

    public void CopyFrom(LevelData other)
    {
        this.width = other.width;
        this.height = other.height;
        this.prefabNames = other.prefabNames;
        this.alignment = other.alignment;
        this.totalUnits = other.totalUnits;
        this.gsp = other.gsp;
    }

    /// <summary>
    /// Tạo một mảng prefabNames mới được shuffle, giữ nguyên Wall, Container, và ô có GSP.
    /// </summary>
    public string[] ShufflePrefabs()
    {
        if (prefabNames == null || prefabNames.Length == 0)
            return prefabNames;

        // Sao chép mảng gốc để không làm thay đổi dữ liệu gốc
        string[] newPrefabs = (string[])prefabNames.Clone();

        // Xác định index cần shuffle
        List<int> shuffleIndices = new List<int>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int idx = y * width + x;
                string prefab = prefabNames[idx];

                if (string.IsNullOrEmpty(prefab))
                    continue;

                // Nếu là Wall, Container hoặc có GSP thì bỏ qua
                string lower = prefab.ToLower();
                bool isWall = lower.Contains("wall");
                bool isContainer = lower.Contains("container");
                bool isGSP = lower.Contains("gsp");
                bool isBarrel = lower.Contains("b");

                if (!isWall && !isContainer && !isGSP && !isBarrel)
                    shuffleIndices.Add(idx);
            }
        }

        // Lấy danh sách prefab sẽ shuffle
        List<string> shufflePrefabs = shuffleIndices
            .Select(i => prefabNames[i])
            .ToList();

        // Trộn ngẫu nhiên danh sách này
        System.Random rand = new System.Random();
        shufflePrefabs = shufflePrefabs.OrderBy(_ => rand.Next()).ToList();

        // Gán lại vào mảng kết quả
        for (int i = 0; i < shuffleIndices.Count; i++)
        {
            newPrefabs[shuffleIndices[i]] = shufflePrefabs[i];
        }

        return newPrefabs;
    }



}
