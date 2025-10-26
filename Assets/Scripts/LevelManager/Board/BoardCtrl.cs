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
    [SerializeField] private List<BoardCell> boardCells;
    public List<GameObject> boardAlls;
    public List<GridSpotSpawn> gridSpotSpawns;

    [Header("Action Event")]
   
    [Header("Action spawn Block to GridSpotSpawn")]
    public Func<Container, BoardCell, IEnumerator> SpawnBlockToGSPAction;


    //getter & setter
    public List<BoardCell> BoardCells { get => boardCells; set => boardCells = value; }

    void Start()
    {
        SpawnBlockToGSPAction += CheckSpawnBlock;
        BoardCells = new List<BoardCell>();
    }

    void OnDisable()
    {
        SpawnBlockToGSPAction -= CheckSpawnBlock;
    }

    [Header("random")]
    private Dictionary<TypeItem, int> initialTypeCounts = new Dictionary<TypeItem, int>();

    private void InitTypeCounts()
    {
        initialTypeCounts.Clear();
        foreach (TypeItem type in Enum.GetValues(typeof(TypeItem)))
        {
            initialTypeCounts[type] = boardCells.Count(c => c.TypeItem == type);
        }

        Debug.Log("Initial type counts:");
        foreach (var kv in initialTypeCounts)
        {
            Debug.Log($" - {kv.Key}: {kv.Value}");
        }
    }

    private IEnumerator CheckSpawnBlock(Container container, BoardCell boardCell = null)//neu sau nay muon truyen index thi them vao
    {
        if (gridSpotSpawns.Count == 0) yield break;
        for (int i = 0; i < gridSpotSpawns.Count; i++)
        {
            if (gridSpotSpawns[i].CheckContainer(container))
            {
                TypeItem typeItem = GetNextRandomType(levelData.totalUnits);
                GameObject obj = AddressableManager.Instance.GetPrefab(Enum.GetName(typeof(TypeItem), typeItem));
                StartCoroutine(gridSpotSpawns[i].SpawnBlock(obj, container,typeItem, (boardCell) =>
                {
                    boardCells.Add(boardCell);
                    
                }));
                //int index = boardCells.IndexOf(boardCell);
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
        // Nếu chưa có thống kê thì khởi tạo
        if (initialTypeCounts == null || initialTypeCounts.Count == 0)
        {
            InitTypeCounts();
        }

        // Tổng số quân hiện tại
        int currentTotal = initialTypeCounts.Values.Sum();

        // Nếu đã đạt giới hạn tổng, reset lại vòng (nếu muốn)
        if (currentTotal >= totalCount)
        {
            Debug.Log("Đã đạt tổng quân tối đa, reset bộ đếm!");
            InitTypeCounts(); // reset nếu cần tạo batch mới
        }

        // B1: Lấy danh sách các loại chưa đạt bội 3
        List<TypeItem> notMultipleOfThree = new List<TypeItem>();
        foreach (var kv in initialTypeCounts)
        {
            int remainder = kv.Value % 3;
            if (remainder != 0) notMultipleOfThree.Add(kv.Key);
        }

        TypeItem chosenType;

        // B2: Nếu có loại chưa đủ bội 3 → ưu tiên chọn từ đó
        if (notMultipleOfThree.Count > 0)
        {
            chosenType = notMultipleOfThree[UnityEngine.Random.Range(0, notMultipleOfThree.Count)];
        }
        else
        {
            // Nếu tất cả đều đủ bội 3 → chọn ngẫu nhiên đều giữa tất cả
            Array values = Enum.GetValues(typeof(TypeItem));
            chosenType = (TypeItem)values.GetValue(UnityEngine.Random.Range(0, values.Length));
        }

        // B3: Cập nhật bộ đếm
        initialTypeCounts[chosenType]++;

        Debug.Log($"[RandomType] Chọn {chosenType}, hiện có {initialTypeCounts[chosenType]} quân.");

        return chosenType;
    }



    private void ResetList()
    {
        boardAlls.Clear();
        boardCells.Clear();
        gridSpotSpawns.Clear();
    }
    public IEnumerator LoadLevel(LevelData levelData)
    {
        // random ngẫu nhiên để các level không trùng type
        //SetIdTypeRandom();
        ResetList();

        this.levelData = levelData;
        if (levelData == null)
        {
            Debug.LogError("Chưa gán LevelData!");
            yield break; ;
        }

        if (gridParent == null)
        {
            Debug.LogError("Chưa gán Grid để chứa các ô!");
            yield break;
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
                        if (prefabName.Length > 1)
                        {
                            boardCell.Barrel.SetActive(true);
                            boardCell.BarrelCell.BarrelCelAnimation.PlayBarrelDefault();
                        }
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
                        {
                            DirectionNeighBor directionNeighBor = DirectionNeighBor.Top;
                            if (d == 0) directionNeighBor = DirectionNeighBor.Bottom;
                            if (d == 1) directionNeighBor = DirectionNeighBor.Top;
                            if (d == 2) directionNeighBor = DirectionNeighBor.Left;
                            if (d == 3) directionNeighBor = DirectionNeighBor.Right;
                            current.AddNeighbor(neighbor, directionNeighBor);
                        }
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
                if (gridContainerSpot[row, col] != null && gridContainerSpot[row, col] is Container)
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
                            gridSpotSpawn.AddContainer(gridContainerSpot[row, col], Direction.Down);
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
                            gridSpotSpawn1.AddContainer(gridContainerSpot[row, col], Direction.Up);
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
                            gridSpotSpawn2.AddContainer(gridContainerSpot[row, col], Direction.Right);
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
                            gridSpotSpawn3.AddContainer(gridContainerSpot[row, col], Direction.Left);
                        }
                    }
                }
            }
        }


        // Đặt vị trí ban đầu
        transform.position = new Vector3(4f, transform.position.y, transform.position.z);

        // Di chuyển từ x = 4f đến x = 0 trong 0.25 giây
        Tween tween = transform.DOMoveX(0f, 0.25f).SetEase(Ease.Linear);

        // Đợi tween chạy xong
        yield return tween.WaitForCompletion();

        Debug.Log("Hoàn tất di chuyển!");
        //=============================
        // Sau khi hoàn tất spawn toàn bộ boardCells ban đầu
        InitTypeCounts();



        Debug.Log($"Level '{levelData.name}' loaded successfully under {gridParent.name}!");
    }
    


     public void UpdateBoardCell(BoardCell boardCell)
    {
        boardCells.Remove(boardCell);
    }
}