using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class BoardCtrl : MonoBehaviour
{

    [Header("Level Configuration")]
    public FindingPath findingPath;
    private const int MAXTYPE = 3;
    public LevelData levelData; 
    public string prefabFolder = "Prefabs"; 

    [Header("Parent Container")]
    public Transform gridParent; 
    public List<BoardCell> boardCells;
    public Dictionary<string, TypeItem> DictIdType;

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

        // //init findpath
        // findingPath.SetCapacity(levelData.width, levelData.height);

        //LoadLevel();
    }

    public void SetIdTypeRandom()
    {
        // Xóa dict cũ
        DictIdType = new Dictionary<string, TypeItem>();

        // Danh sách ID (từ 1 đến MAXTYPE)
        List<string> ids = new List<string>();
        for (int i = 1; i <= MAXTYPE; i++)
        {
            ids.Add(i.ToString());
        }

        // Danh sách TypeItem
        List<TypeItem> allTypes = new List<TypeItem>((TypeItem[])System.Enum.GetValues(typeof(TypeItem)));

        // Shuffle danh sách type để random
        for (int i = allTypes.Count - 1; i > 0; i--)
        {
            int randIndex = Random.Range(0, i + 1);
            (allTypes[i], allTypes[randIndex]) = (allTypes[randIndex], allTypes[i]);
        }

        // Gán từng ID với TypeItem tương ứng
        for (int i = 0; i < MAXTYPE; i++)
        {
            DictIdType.Add(ids[i], allTypes[i]);
        }

        // // Debug kiểm tra
        // foreach (var kvp in DictIdType)
        // {
        //     Debug.Log($"ID: {kvp.Key} → Type: {kvp.Value}");
        // }
    }



   public void LoadLevel(LevelData levelData)
    {
        // random ngẫu nhiên để các level không trùng type
        SetIdTypeRandom();

        this.levelData = levelData;

        float offsetX = 1.25f;
        float offsetZ = 1.25f;

        // Xóa grid cũ
        for (int i = gridParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(gridParent.GetChild(i).gameObject);
        }

        boardCells.Clear();

        // Khởi tạo lại danh sách container trong findPath
        findingPath.SetCapacity(levelData.height, levelData.width); // ✅ sửa lại đúng width, height
        findingPath.containers.Clear();

        // Tính điểm bắt đầu sao cho bàn cờ nằm chính giữa
        float startX = gridParent.position.x - (levelData.width - 1) * offsetX / 2f;
        float startZ = gridParent.position.z + (levelData.height - 1) * offsetZ / 2f;

        BoardCell[,] grid = new BoardCell[levelData.height, levelData.width];

        // ======== TẠO CÁC Ô ========
        for (int row = 0; row < levelData.height; row++) // hàng (y)
        {
            for (int col = 0; col < levelData.width; col++) // cột (x)
            {
                int index = row * levelData.width + col;
                string prefabName = levelData.prefabNames[index];

                if (string.IsNullOrEmpty(prefabName))
                {
                    findingPath.containers.Add(null);
                    continue;
                }

                // Chọn prefab
                GameObject prefab;
                if (prefabName != "Wall" && prefabName != "Container")
                    prefab = Resources.Load<GameObject>($"{prefabFolder}/{"BoardCell"}");
                else
                    prefab = Resources.Load<GameObject>($"{prefabFolder}/{prefabName}");

                if (prefab == null)
                {
                    Debug.LogWarning($"Không tìm thấy prefab: {prefabName}");
                    findingPath.containers.Add(null);
                    continue;
                }

                // Tính vị trí
                float posX = startX + col * offsetX;
                float posZ = startZ - row * offsetZ; // hàng 0 ở trên, hàng cuối ở dưới

                GameObject obj = Instantiate(prefab, new Vector3(posX, 0f, posZ), Quaternion.identity, gridParent);
                obj.name = prefabName;
                // if (prefabName == "BoardCell")
                // {
                //     GameObject prefabTemp = Resources.Load<GameObject>($"{prefabFolder}/{"Container"}");
                //     GameObject objj = Instantiate(prefab, new Vector3(posX, 0f, posZ), Quaternion.identity, gridParent);
                //     objj.name = "Container";
                //     objj.GetComponent<Container>().IsContaining = true;
                // }

                // BoardCell
                if (obj.TryGetComponent(out BoardCell boardCell))
                {
                    boardCell.Pos = new Vector3(posX, 0f, posZ);
                    boardCells.Add(boardCell);
                    boardCell.IdType = prefabName;
                    boardCell.ChangItemFromId(DictIdType);
                    grid[row, col] = boardCell;
                }

                // Container
                Container containerToAdd = null;

                if (prefabName == "Wall")
                    containerToAdd = null;
                else if (prefabName == "Container")
                    containerToAdd = obj.GetComponent<Container>();
                // else
                //     containerToAdd = obj.GetComponentInChildren<Container>();

                if (containerToAdd != null)
                {
                    containerToAdd.Pos = new Vector3(posX, 0f, posZ);
                }

                findingPath.containers.Add(containerToAdd);
            }
        }

        // ======== GÁN NEIGHBOR ========
        for (int row = 0; row < levelData.height; row++)
        {
            for (int col = 0; col < levelData.width; col++)
            {
                BoardCell current = grid[row, col];
                if (current == null) continue;

                current.ClearNeighbors();

                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        int nx = col + dx;
                        int ny = row + dy;

                        if (nx >= 0 && nx < levelData.width && ny >= 0 && ny < levelData.height)
                        {
                            BoardCell neighbor = grid[ny, nx];
                            if (neighbor != null)
                                current.AddNeighbor(neighbor);
                        }
                    }
                }
            }
        }

        Debug.Log($"✅ Level '{levelData.name}' loaded successfully under {gridParent.name}!");
    }



}
