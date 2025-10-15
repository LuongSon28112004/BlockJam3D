using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;

public class BoardCtrl : MonoBehaviour
{

    [Header("Level Configuration")]
    [SerializeField] private ItemClickCtrl itemClickCtrl;
    private const int MAXTYPE = 4;
    public LevelData levelData;
    public string prefabFolder = "Prefabs";

    [Header("Parent Container")]
    public Transform gridParent;
    public List<BoardCell> boardCells;
    public List<GameObject> boardAlls;
    public List<GridSpotSpawn> gridSpotSpawns;
    //public Dictionary<string, TypeItem> DictIdType;

    [Header("Action Event")]
    [Header("Action Move maxtrix and move to cell play")]
    public Func<BoardCell,IEnumerator> checkAndSavePosAction;
    public Func<IEnumerator> MoveToCellPlay;
    // Thay đổi: MoveToPosAction nên trả về IEnumerator để được yield return
    public Func<Vector3, IEnumerator> MoveToPosAction;
    [Header("Action spawn Block to GridSpotSpawn")]
    public Func<Container,BoardCell, IEnumerator> SpawnBlockToGSPAction;

    void Start()
    {
        SpawnBlockToGSPAction += CheckSpawnBlock;
    }

    void OnDisable()
    {
        SpawnBlockToGSPAction -= CheckSpawnBlock;
    }

    private IEnumerator CheckSpawnBlock(Container container, BoardCell boardCell = null)//neu sau nay muon truyen index thi them vao
    {
        for (int i = 0; i < gridSpotSpawns.Count; i++)
        {
            if (gridSpotSpawns[i].CheckContainer(container))
            {
                TypeItem typeItem = GetNextRandomType(levelData.totalUnits);
                GameObject obj = AddressableManager.Instance.GetPrefab(Enum.GetName(typeof(TypeItem), typeItem));
                StartCoroutine(gridSpotSpawns[i].SpawnBlock(obj, container,typeItem));
                //int index = boardCells.IndexOf(boardCell);
                boardCells.Add(obj.GetComponent<BoardCell>());
                break;
            }
        }
        yield break;
    }
    
    /// <summary>
    /// Random TypeItem tiếp theo theo trọng số, đảm bảo mỗi loại có ít nhất 3 quân khi đạt totalCount.
    /// </summary>
    public TypeItem GetNextRandomType(int totalCount)
    {
        // Đếm số lượng từng loại hiện có
        Dictionary<TypeItem, int> typeCounts = new Dictionary<TypeItem, int>();
        foreach (TypeItem type in Enum.GetValues(typeof(TypeItem)))
        {
            typeCounts[type] = boardCells.Count(c => c.TypeItem == type);
        }

        // 🔹 1. Nếu gần đầy (còn lại ít hơn số loại * 3)
        int remainingSlots = totalCount - boardCells.Count;

        // Kiểm tra xem có loại nào chưa đủ 3
        List<TypeItem> mustFillTypes = typeCounts
            .Where(kv => kv.Value < 3)
            .Select(kv => kv.Key)
            .ToList();

        // Nếu còn ít slot mà vẫn có loại chưa đủ 3 → ép random loại đó
        if (remainingSlots <= mustFillTypes.Count * 3 && mustFillTypes.Count > 0)
        {
            return mustFillTypes[UnityEngine.Random.Range(0, mustFillTypes.Count)];
        }

        // 🔹 2. Ngược lại → random theo trọng số (bình thường)
        int maxCount = typeCounts.Values.Max();
        Dictionary<TypeItem, float> weights = new Dictionary<TypeItem, float>();

        foreach (var kv in typeCounts)
        {
            // Loại xuất hiện ít → trọng số cao hơn
            weights[kv.Key] = (maxCount - kv.Value + 1);
        }

        // Random theo trọng số
        float totalWeight = weights.Values.Sum();
        float rand = UnityEngine.Random.Range(0, totalWeight);
        float cumulative = 0;

        foreach (var kv in weights)
        {
            cumulative += kv.Value;
            if (rand <= cumulative)
                return kv.Key;
        }

        return TypeItem.BlueBase; // fallback
    }


    public async Task LoadLevel(LevelData levelData)
    {
        // random ngẫu nhiên để các level không trùng type
        //SetIdTypeRandom();

        this.levelData = levelData;
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

        float offsetX = 1.25f;
        float offsetZ = 1.25f;

        // Xóa grid cũ
        for (int i = gridParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(gridParent.GetChild(i).gameObject);
        }

        boardCells.Clear();

        // Khởi tạo lại danh sách container trong findPath
        itemClickCtrl.FindingPath.SetCapacity(levelData.height, levelData.width);
        itemClickCtrl.FindingPath.containers.Clear();

        // Tính điểm bắt đầu sao cho bàn cờ nằm chính giữa
        float startX = gridParent.position.x - (levelData.width - 1) * offsetX / 2f;
        float startZ = gridParent.position.z + (levelData.height - 1) * offsetZ / 2f;

        BoardCell[,] grid = new BoardCell[levelData.height, levelData.width];
        bool[,] IsWall = new bool[levelData.height, levelData.width];
        Container[,] gridContainerSpot = new Container[levelData.height, levelData.width];

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
                if (prefabName != "Wall" && prefabName != "Container" && prefabName != "GSPDown" && prefabName != "GSPBottomRight")
                {
                    // Sửa lỗi tham chiếu: Sử dụng Enum.GetName(typeof(TypeItem), int.Parse(prefabName))
                    string name = Enum.GetName(typeof(TypeItem), int.Parse(prefabName[0].ToString()) - 1);
                    prefab = AddressableManager.Instance.GetPrefab($"{name}");
                }
                else
                {
    
                    prefab = AddressableManager.Instance.GetPrefab($"{prefabName}");
                    if (prefabName == "Wall")
                    {
                        IsWall[row, col] = true;
                    }
                }

                if (prefab == null)
                {
                    Debug.LogWarning($"Không tìm thấy prefab: {prefabName}");
                    continue;
                }

                // Tính vị trí
                float posX = startX + col * offsetX;
                float posZ = startZ - row * offsetZ; // hàng 0 ở trên, hàng cuối ở dưới

                GameObject obj = Instantiate(prefab, new Vector3(posX, 0f, posZ), Quaternion.identity, gridParent);
                obj.name = prefabName;
                if (prefabName != "Wall" && prefabName != "Container" && prefabName != "GSPDown" && prefabName != "GSPBottomRight")
                {
                    GameObject prefabTemp = AddressableManager.Instance.GetPrefab("Container");
                    GameObject objj = Instantiate(prefabTemp, new Vector3(posX, 0f, posZ), Quaternion.identity, gridParent);
                    objj.name = "Container";
                    objj.GetComponent<Container>().IsContaining = true;
                    objj.GetComponent<Container>().Pos = new Vector3(posX, 0f, posZ);
                    itemClickCtrl.FindingPath.containers.Add(objj.GetComponent<Container>());
                    gridContainerSpot[row, col] = objj.GetComponent<Container>();
                    // BoardCell
                    if (obj.TryGetComponent(out BoardCell boardCell))
                    {
                        boardCell.Pos = new Vector3(posX, 0f, posZ);
                        boardCells.Add(boardCell);
                        boardCell.IdType = prefabName[0].ToString();
                        boardCell.TypeItem = (TypeItem)(int.Parse(prefabName[0].ToString()) - 1);
                        if (prefabName.Length > 1) boardCell.Barrel.SetActive(true);
                        else boardCell.Barrel.SetActive(false);
                        //boardCell.ChangItemFromId(DictIdType);
                        boardCell.Container = objj.GetComponent<Container>();
                        grid[row, col] = boardCell;
                    }
                }
                if (prefabName == "GSPDown" || prefabName == "GSPBottomRight")
                {
                    if (obj.TryGetComponent(out GridSpotSpawn gridSpotSpawn))
                    {
                        var gspData = levelData.GetGSPAt(row, col, prefabName);
                        if (gspData != null)
                        {
                            gridSpotSpawns.Add(gridSpotSpawn);
                            gridSpotSpawn.MaxPointSpawn = gspData.spawnCount;
                        }
                        else
                        {
                            Debug.LogWarning($"Không tìm thấy GSPDownData tại ({row}, {col}) trong LevelData!");
                            gridSpotSpawn.MaxPointSpawn = 1; // hoặc gán giá trị mặc định nếu cần
                        }
                    }
                }

                
                boardAlls.Add(obj);


                // Container
                Container containerToAdd = null;

                if (prefabName == "Wall" || prefabName == "GSPDown" || prefabName == "GSPBottomRight")
                    containerToAdd = null;
                else if (prefabName == "Container")
                    containerToAdd = obj.GetComponent<Container>();

                if (containerToAdd != null)
                {
                    containerToAdd.Pos = new Vector3(posX, 0f, posZ);
                }

                if (prefabName == "Wall" || prefabName == "Container" || prefabName == "GSPDown" || prefabName == "GSPBottomRight")
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
                    { 0, 1 },   // trên (col, row + 1)
                    { 0, -1 },  // dưới (col, row - 1)
                    { -1, 0 },  // trái (col - 1, row)
                    { 1, 0 }    // phải (col + 1, row)
                };

                for (int d = 0; d < directions.GetLength(0); d++)
                {
                    int dx = directions[d, 0];
                    int dy = directions[d, 1];

                    int nx = col + dx; // Cột mới
                    int ny = row + dy; // Hàng mới

                    if (nx >= 0 && nx < levelData.width && ny >= 0 && ny < levelData.height)
                    {
                        BoardCell neighbor = grid[ny, nx];
                        if (neighbor != null)
                            current.AddNeighbor(neighbor);
                    }
                }

                // Kiểm tra hàng xóm phía dưới (row + 1)
                int belowRow = row + 1;
                // Nếu dưới cùng hoặc ô dưới là null (không có item) và không phải là Wall
                if (belowRow >= levelData.height || (grid[belowRow, col] == null && !IsWall[belowRow, col]))
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

        //============assign container to Spot=================
        for (int row = 0; row < levelData.height; row++)
        {
            for (int col = 0; col < levelData.width; col++)
            {
                if (gridContainerSpot[row, col] != null && gridContainerSpot[row,col] is Container)
                {
                    int top = row - 1;
                    int bottom = row + 1;
                    int left = col - 1;
                    int right = col + 1;
                    GameObject obj = null;
                    int index = top * levelData.width + col;
                    if (index >= 0 && index < boardAlls.Count)
                    {
                        obj = boardAlls[index];
                    }
                    if (obj != null && obj.TryGetComponent<GridSpotSpawn>(out GridSpotSpawn gridSpotSpawn))
                    {
                        bool isBottom = gridSpotSpawn.CheckDirection(Direction.Down);
                        if (isBottom && bottom < levelData.height)
                        {
                            gridSpotSpawn.AddContainer(gridContainerSpot[row, col],Direction.Down);
                        }
                    }
                    
                    index = bottom * levelData.width + col;
                    obj = null;
                    if (index >= 0 && index < boardAlls.Count)
                    {
                        obj = boardAlls[index];
                    }
                    if (obj != null && obj.TryGetComponent<GridSpotSpawn>(out GridSpotSpawn gridSpotSpawn1))
                    {
                        bool isTop = gridSpotSpawn1.CheckDirection(Direction.Up);
                        if (isTop && top >= 0)
                        {
                            gridSpotSpawn1.AddContainer(gridContainerSpot[row, col],Direction.Up);
                        }
                    }
                    index = row * levelData.width + left;
                    obj = null;
                    if (index >= 0 && index < boardAlls.Count)
                    {
                        obj = boardAlls[index];
                    }
                    if (obj != null && obj.TryGetComponent<GridSpotSpawn>(out GridSpotSpawn gridSpotSpawn2))
                    {
                        bool isRight = gridSpotSpawn2.CheckDirection(Direction.Right);
                        if (isRight && right < levelData.width)
                        {
                            gridSpotSpawn2.AddContainer(gridContainerSpot[row, col],Direction.Right);
                        }
                    }
                    index = row * levelData.width + right;
                    obj = null;
                    if (index >= 0 && index < boardAlls.Count)
                    {
                        obj = boardAlls[index];
                    }
                    if (obj != null && obj.TryGetComponent<GridSpotSpawn>(out GridSpotSpawn gridSpotSpawn3))
                    {
                        bool isLeft = gridSpotSpawn3.CheckDirection(Direction.Left);
                        if (isLeft && left >= 0)
                        {
                            gridSpotSpawn3.AddContainer(gridContainerSpot[row, col],Direction.Left);
                        }
                    }
                }
            }
        }


        transform.position = new Vector3(4f, transform.position.y, transform.position.z);
        // Di chuyển từ x = 4f đến x = 0 trong 0.25 giây
        var tween = transform.DOMoveX(0f, 0.25f).SetEase(Ease.Linear);
        var tcs = new TaskCompletionSource<bool>();
        tween.OnComplete(() => tcs.TrySetResult(true));
        tween.OnKill(() => tcs.TrySetResult(true));
        await tcs.Task;


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
        if(boardCells.Count == 0)
        {
            LevelManager.Instance.NextRound.Invoke();
        }
    }
}