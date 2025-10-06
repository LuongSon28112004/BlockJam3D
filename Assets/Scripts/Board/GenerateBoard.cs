using DG.Tweening;
using UnityEngine;

public class GenerateBoard : MonoBehaviour
{
    [Header("Level Configuration")]
    public LevelData levelData; // Gán LevelData.asset vào đây trong Inspector
    public string prefabFolder = "Prefabs"; // Thư mục chứa tất cả prefab

    [Header("Parent Container")]
    public Transform gridParent; // Gán GameObject Grid ở đây

    private void Start()
    {
        if (levelData == null)
        {
            Debug.LogError("Chưa gán LevelData!");
            return;
        }

        if (gridParent == null)
        {
            Debug.LogError("Chưa gán Grid để chứa các ô!");
            return;
        }

        LoadLevel();
    }

    private void LoadLevel()
    {
        float offsetX = 1.25f;
        float offsetZ = 1.25f;

        // Xóa các con cũ trong Grid nếu có
        for (int i = gridParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(gridParent.GetChild(i).gameObject);
        }

        // Sinh level mới
        for (int y = 0; y < levelData.height; y++)
        {
            for (int x = 0; x < levelData.width; x++)
            {
                string prefabName = levelData.prefabNames[y * levelData.width + x];
                if (string.IsNullOrEmpty(prefabName)) continue;

                // 🔹 Load prefab trong thư mục Resources/Prefabs/
                GameObject prefab = Resources.Load<GameObject>($"{prefabFolder}/{prefabName}");
                if (prefab == null)
                {
                    Debug.LogWarning($"Không tìm thấy prefab: {prefabName}");
                    continue;
                }

                // 🔹 Tính vị trí (X tăng dần, Z giảm dần)
                float posX = x * offsetX;
                float posZ = -y * offsetZ;

                GameObject obj = Instantiate(prefab, new Vector3(gridParent.position.x + posX, 0f, gridParent.position.z + posZ), Quaternion.identity, gridParent);
                obj.name = prefabName; // Đặt lại tên cho dễ nhìn trong Hierarchy
            }
        }

        Debug.Log($"Level '{levelData.name}' loaded successfully under {gridParent.name}!");
    }
}
