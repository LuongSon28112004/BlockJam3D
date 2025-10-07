using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class GenerateBoard : MonoBehaviour
{
    [Header("Level Configuration")]
    public LevelData levelData; 
    public string prefabFolder = "Prefabs"; 

    [Header("Parent Container")]
    public Transform gridParent; 
    public List<BoardCell> boardCells;

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

        // align ban co
        if (levelData.alignment)
        {
            Vector3 parentPos = gridParent.parent.position;
            parentPos.x = -0.6f;
            gridParent.parent.position = parentPos;
        }
        LoadLevel();
    }

   private void LoadLevel()
    {
        float offsetX = 1.25f;
        float offsetZ = 1.25f;

        for (int i = gridParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(gridParent.GetChild(i).gameObject);
        }

        boardCells.Clear();

        float startX = gridParent.position.x - (levelData.width - 1) * offsetX / 2f;
        float startZ = gridParent.position.z + (levelData.height - 1) * offsetZ / 2f;

        BoardCell[,] grid = new BoardCell[levelData.height, levelData.width];

        // Tạo các ô
        for (int y = 0; y < levelData.height; y++)
        {
            for (int x = 0; x < levelData.width; x++)
            {
                string prefabName = levelData.prefabNames[y * levelData.width + x];
                if (string.IsNullOrEmpty(prefabName)) continue;

                GameObject prefab = Resources.Load<GameObject>($"{prefabFolder}/{prefabName}");
                if (prefab == null)
                {
                    Debug.LogWarning($"Không tìm thấy prefab: {prefabName}");
                    continue;
                }

                float posX = startX + x * offsetX;
                float posZ = startZ - y * offsetZ;

                GameObject obj = Instantiate(prefab, new Vector3(posX, 0f, posZ), Quaternion.identity, gridParent);
                obj.name = prefabName;

                if (obj.TryGetComponent(out BoardCell boardCell))
                {
                    boardCell.Pos = new Vector3(posX, 0f, posZ);
                    boardCells.Add(boardCell);
                    grid[y, x] = boardCell;
                }
            }
        }

        for (int y = 0; y < levelData.height; y++)
        {
            for (int x = 0; x < levelData.width; x++)
            {
                BoardCell current = grid[y, x];
                if (current == null) continue;

                current.NeighBor.Clear(); // Xóa danh sách cũ (chút nữa ta thêm hàm này trong BoardCell)

                // 8 hướng
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue; // bỏ chính nó

                        int nx = x + dx;
                        int ny = y + dy;

                        // kiểm tra hợp lệ
                        if (nx >= 0 && nx < levelData.width && ny >= 0 && ny < levelData.height)
                        {
                            BoardCell neighbor = grid[ny, nx];
                            if (neighbor != null)
                                current.NeighBor.Add(neighbor);
                        }
                    }
                }
            }
        }

        Debug.Log($"Level '{levelData.name}' loaded successfully under {gridParent.name}!");
    }

}
