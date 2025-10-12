using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class BoardCtrl : MonoBehaviour
{

    [Header("Level Configuration")]
    [SerializeField] private ItemClickCtrl itemClickCtrl;
    private const int MAXTYPE = 3;
    public LevelData levelData; 
    public string prefabFolder = "Prefabs"; 

    [Header("Parent Container")]
    public Transform gridParent; 
    public List<BoardCell> boardCells;
    public Dictionary<string, TypeItem> DictIdType;

    [Header("Action Event")]
    public Action<BoardCell> checkAndSavePosAction;
    public Func<Vector3,Task> MoveToPosAction;
    public Func<Task> MoveToCellPlay;
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

    // public void SetIdTypeRandom()
    // {
    //     // Xóa dict cũ
    //     DictIdType = new Dictionary<string, TypeItem>();

    //     // Danh sách ID (từ 1 đến MAXTYPE)
    //     List<string> ids = new List<string>();
    //     for (int i = 1; i <= MAXTYPE; i++)
    //     {
    //         ids.Add(i.ToString());
    //     }

    //     // Danh sách TypeItem
    //     List<TypeItem> allTypes = new List<TypeItem>((TypeItem[])System.Enum.GetValues(typeof(TypeItem)));

    //     // Shuffle danh sách type để random
    //     for (int i = allTypes.Count - 1; i > 0; i--)
    //     {
    //         int randIndex = UnityEngine.Random.Range(0, i + 1);
    //         (allTypes[i], allTypes[randIndex]) = (allTypes[randIndex], allTypes[i]);
    //     }

    //     // Gán từng ID với TypeItem tương ứng
    //     for (int i = 0; i < MAXTYPE; i++)
    //     {
    //         DictIdType.Add(ids[i], allTypes[i]);
    //     }

    //     // // Debug kiểm tra
    //     // foreach (var kvp in DictIdType)
    //     // {
    //     //     Debug.Log($"ID: {kvp.Key} → Type: {kvp.Value}");
    //     // }
    // }



   public void LoadLevel(LevelData levelData)
    {
        // random ngẫu nhiên để các level không trùng type
        //SetIdTypeRandom();

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
        itemClickCtrl.FindingPath.SetCapacity(levelData.height, levelData.width); // ✅ sửa lại đúng width, height
        itemClickCtrl.FindingPath.containers.Clear();

        // Tính điểm bắt đầu sao cho bàn cờ nằm chính giữa
        float startX = gridParent.position.x - (levelData.width - 1) * offsetX / 2f;
        float startZ = gridParent.position.z + (levelData.height - 1) * offsetZ / 2f;

        BoardCell[,] grid = new BoardCell[levelData.height, levelData.width];
        bool[,] IsWall = new bool[levelData.height, levelData.width];

        // ======== TẠO CÁC Ô ========
        for (int row = 0; row < levelData.height; row++) // hàng (y)
        {
            for (int col = 0; col < levelData.width; col++) // cột (x)
            {
                int index = row * levelData.width + col;
                //==================================================
                string prefabName = levelData.prefabNames[index];
                //================================================= repair
                if (string.IsNullOrEmpty(prefabName))
                {
                    //findingPath.containers.Add(null);
                    continue;
                }

                // Chọn prefab
                GameObject prefab = null;
                if (prefabName != "Wall" && prefabName != "Container")
                {
                    string name = Enum.GetName(typeof(TypeItem), int.Parse(prefabName) -1);
                    prefab = Resources.Load<GameObject>($"{prefabFolder}/{name}");
                }
                else
                {
                    prefab = Resources.Load<GameObject>($"{prefabFolder}/{prefabName}");
                    if(prefabName == "Wall")
                    {
                        IsWall[row, col] = true;
                    }
                }

                if (prefab == null)
                {
                    Debug.LogWarning($"Không tìm thấy prefab: {prefabName}");
                    // findingPath.containers.Add(null);
                    continue;
                }

                // Tính vị trí
                float posX = startX + col * offsetX;
                float posZ = startZ - row * offsetZ; // hàng 0 ở trên, hàng cuối ở dưới

                GameObject obj = Instantiate(prefab, new Vector3(posX, 0f, posZ), Quaternion.identity, gridParent);
                obj.name = prefabName;
                if (prefabName != "Wall" && prefabName != "Container")
                {
                    GameObject prefabTemp = Resources.Load<GameObject>($"{prefabFolder}/{"Container"}");
                    GameObject objj = Instantiate(prefabTemp, new Vector3(posX, 0f, posZ), Quaternion.identity, gridParent);
                    objj.name = "Container";
                    objj.GetComponent<Container>().IsContaining = true;
                    objj.GetComponent<Container>().Pos = new Vector3(posX, 0f, posZ);
                    itemClickCtrl.FindingPath.containers.Add(objj.GetComponent<Container>());
                    // BoardCell
                    if (obj.TryGetComponent(out BoardCell boardCell))
                    {
                        boardCell.Pos = new Vector3(posX, 0f, posZ);
                        boardCells.Add(boardCell);
                        boardCell.IdType = prefabName;
                        boardCell.TypeItem = (TypeItem)(int.Parse(prefabName.ToString()) - 1);
                        //////////////////////////////////////////
                        //boardCell.ChangItemFromId(DictIdType);
                        /////////////////////////////////////////
                        boardCell.Container = objj.GetComponent<Container>();
                        grid[row, col] = boardCell;
                    }
                }


                // Container
                Container containerToAdd = null;

                if (prefabName == "Wall")
                    containerToAdd = null;
                else if (prefabName == "Container")
                    containerToAdd = obj.GetComponent<Container>();
                // else
                //     containerToAdd = obj.GetComponent<Container>();

                if (containerToAdd != null)
                {
                    containerToAdd.Pos = new Vector3(posX, 0f, posZ);
                }

                if(prefabName == "Wall" || prefabName == "Container")
                    itemClickCtrl.FindingPath.containers.Add(containerToAdd);
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

                // Chỉ 4 hướng: Trên, Dưới, Trái, Phải
                int[,] directions = new int[,]
                {
                    { 0, 1 },   // trên
                    { 0, -1 },  // dưới
                    { -1, 0 },  // trái
                    { 1, 0 }    // phải
                };

                for (int d = 0; d < directions.GetLength(0); d++)
                {
                    int dx = directions[d, 0];
                    int dy = directions[d, 1];

                    int nx = col + dx;
                    int ny = row + dy;

                    if (nx >= 0 && nx < levelData.width && ny >= 0 && ny < levelData.height)
                    {
                        BoardCell neighbor = grid[ny, nx];
                        if (neighbor != null)
                            current.AddNeighbor(neighbor);
                    }
                }

                // Kiểm tra hàng xóm phía dưới (row + 1)
                int belowRow = row + 1;
                if (belowRow >= levelData.height || (grid[belowRow, col] == null && !IsWall[belowRow,col]) )
                {
                    current.BoardCellAnimation.SetActive();
                    current.HasClick = true;
                }
                else
                {
                    current.HasClick = false;
                }
            }
        }


        Debug.Log($"Level '{levelData.name}' loaded successfully under {gridParent.name}!");
    }

   public void UpdateBoardCell()
    {
        for (int i = 0; i < boardCells.Count; i++)
        {
            if (boardCells[i] == null)
            {
                Debug.Log($"Ô tại vị trí {i} bị missing");
                // Xử lý nếu cần, ví dụ: xóa khỏi danh sách
                boardCells.RemoveAt(i);
                i--; // Giảm i để không bỏ qua phần tử kế tiếp
            }
        }
    }

}
