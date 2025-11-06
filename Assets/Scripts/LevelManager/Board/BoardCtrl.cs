using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class BoardCtrl : MonoBehaviour
{

    [Header("Level Configuration")]
    [SerializeField] public ItemClickCtrl itemClickCtrl;
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
    public BoardCell[,] grid;
    public bool[,] IsWall;
    public Container[,] gridContainerSpot;

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
    public Dictionary<TypeItem, int> initialTypeCounts = new Dictionary<TypeItem, int>();

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

    public IEnumerator CheckSpawnBlock(Container container, BoardCell boardCell = null)//neu sau nay muon truyen index thi them vao
    {
        if (gridSpotSpawns.Count == 0) yield break;
        for (int i = 0; i < gridSpotSpawns.Count; i++)
        {
            if (gridSpotSpawns[i].CheckContainer(container))
            {
                TypeItem typeItem = GetNextRandomType(levelData.totalUnits);
                GameObject obj = AddressableManager.Instance.GetPrefab(Enum.GetName(typeof(TypeItem), typeItem));
                StartCoroutine(gridSpotSpawns[i].SpawnBlock(obj, container, typeItem, (boardCell) =>
                {
                    boardCells.Add(boardCell);
                    Vector2 index = FindContainer(container);
                    int row = Mathf.FloorToInt(index.x);
                    int col = Mathf.FloorToInt(index.y);
                    if (row < 0 || col < 0)
                    {
                        boardAlls.Add(boardCell.gameObject);
                    }
                    else
                    {
                        int linearIndex = row * (levelData != null ? levelData.width : 0) + col;
                        linearIndex = Mathf.Clamp(linearIndex, 0, boardAlls.Count);
                        boardAlls.Insert(linearIndex, boardCell.gameObject);
                    }
                    if (gridSpotSpawns[i].CurrentPointSpawn > 0)
                    {
                        boardCell.HasSpawn = true;
                    }
                }));
                break;
            }
        }
        yield break;
    }

    public void RemoveContainer(int index)
    {

        List<Container> temp = new List<Container>();
        for (int i = 0; i < levelData.height; i++)
        {
            for (int j = 0; j < levelData.width; j++)
            {
                temp.Add(gridContainerSpot[i, j]);
            }
        }

        if (index >= 0 && index < temp.Count)
            temp.RemoveAt(index);

        // Tạo lại grid mới nếu bạn muốn
        Container[,] newGrid = new Container[levelData.height, levelData.width]; // hoặc tính lại kích thước mới
        for (int i = 0; i < temp.Count; i++)
        {
            int row = i / levelData.width;
            int col = i % levelData.width;
            newGrid[row, col] = temp[i];
        }

        gridContainerSpot = newGrid;
    }


    private Vector2 FindContainer(Container container)
    {
        for (int x = 0; x < gridContainerSpot.GetLength(0); x++)
        {
            for (int y = 0; y < gridContainerSpot.GetLength(1); y++)
            {
                if (gridContainerSpot[x, y] == container)
                {
                    return new Vector2(x, y);
                }
            }
        }
        return new Vector2(-1, -1);
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
    public IEnumerator LoadLevel(LevelData levelData, bool isSlide = true)
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


        // Xóa grid cũ
        for (int i = gridParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(gridParent.GetChild(i).gameObject);
        }

        boardCells.Clear();
        // Khởi tạo lại danh sách container trong findPath
        itemClickCtrl.FindingPath.SetCapacity(levelData.height, levelData.width);
        itemClickCtrl.FindingPath.containers.Clear();

        // khởi tạo các mảng phục vụ cho gán giá trị các block trong leaderBoard
        grid = new BoardCell[levelData.height, levelData.width];
        IsWall = new bool[levelData.height, levelData.width];
        gridContainerSpot = new Container[levelData.height, levelData.width];
        //tạo bàn cờ
        SpawnLeaderBoard(grid, IsWall, gridContainerSpot);

        //gán các hàng xóm của các khối block
        AddNeighbor(grid, IsWall);

        //gán các container cho các máy random pipe nếu có trên leaderboard
        AlignContainer(gridContainerSpot);

        //trượt leaderboard khi qua round tiếp theo
        StartCoroutine(SlideLeaderBoard(isSlide));

        // chỉnh camera để bàn cờ vào giữa trung tâm của màn hình
        FolowCamera();

        //khởi tạo để đếm các block được spawn ra trên leaderboard
        InitTypeCounts();



        Debug.Log($"Level '{levelData.name}' loaded successfully under {gridParent.name}!");
    }

    public void SpawnLeaderBoard(BoardCell[,] grid, bool[,] IsWall, Container[,] gridContainerSpot)
    {
        // ======== TẠO CÁC Ô ========
        float offsetX = 1.25f;
        float offsetZ = 1.25f;
        // Tính điểm bắt đầu sao cho bàn cờ nằm chính giữa
        float startX = gridParent.position.x - (levelData.width - 1) * offsetX / 2f;
        float startZ = gridParent.position.z + (levelData.height - 1) * offsetZ / 2f;
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
                if (prefabName != "Wall" && prefabName != "Container" && prefabName != "GSPDown" && prefabName != "GSPBottomRight" && prefabName != "GSPRight")
                {
                    //Sửa lỗi tham chiếu: Sử dụng Enum.GetName(typeof(TypeItem), int.Parse(prefabName))
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

                // if (prefab == null)
                // {
                //     Debug.LogWarning($"Không tìm thấy prefab: {prefabName}");
                //     continue;
                // }

                // Tính vị trí
                float posX = startX + col * offsetX;
                float posZ = startZ - row * offsetZ; // hàng 0 ở trên, hàng cuối ở dưới

                GameObject obj = null;
                if (prefabName != "Wall" && prefabName != "Container" && prefabName != "GSPDown" && prefabName != "GSPBottomRight" && prefabName != "GSPRight" && prefabName.Length < 2)
                {
                    string name = Enum.GetName(typeof(TypeItem), int.Parse(prefabName[0].ToString()) - 1);
                    obj = BlockItemSpawner.Instance.spawnCellItem(name, new Vector3(posX, 0f, posZ), Quaternion.identity).gameObject;
                    // if (prefabName.Length > 1)
                    // {
                    //     BoardCell boardCell = obj.GetComponent<BoardCell>();
                    //     boardCell.Barrel.gameObject.SetActive(true);
                    //     boardCell.BarrelCell.BarrelCelAnimation.PlayBarrelDefault();
                    // }
                    obj.transform.SetParent(gridParent);
                }
                else
                {
                    obj = Instantiate(prefab, new Vector3(posX, 0f, posZ), Quaternion.identity, gridParent);
                }
                obj.name = prefabName;
                if (prefabName != "Wall" && prefabName != "Container" && prefabName != "GSPDown" && prefabName != "GSPBottomRight" && prefabName != "GSPRight")
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
                if (prefabName == "GSPDown" || prefabName == "GSPBottomRight" || prefabName == "GSPRight")
                {
                    if (obj.TryGetComponent(out GridSpotSpawn gridSpotSpawn))
                    {
                        var gspData = levelData.GetGSPAt(row, col, prefabName);
                        if (gspData != null)
                        {
                            gridSpotSpawns.Add(gridSpotSpawn);
                            gridSpotSpawn.CurrentPointSpawn = gspData.spawnCount;
                            gridSpotSpawn.MaxPointSpawn = gspData.spawnCount;
                        }
                        else
                        {
                            Debug.LogWarning($"Không tìm thấy GSPDownData tại ({row}, {col}) trong LevelData!");
                            gridSpotSpawn.CurrentPointSpawn = 1; // hoặc gán giá trị mặc định nếu cần
                        }
                    }
                }


                boardAlls.Add(obj);


                // Container
                Container containerToAdd = null;

                // o đoạn này, nếu là Block thì không thêm vì container của block đã được thêm ở trên rồi
                if (prefabName == "Wall" || prefabName == "GSPDown" || prefabName == "GSPBottomRight" || prefabName == "GSPRight")
                    containerToAdd = null;
                else if (prefabName == "Container")
                    containerToAdd = obj.GetComponent<Container>();

                if (containerToAdd != null)
                {
                    containerToAdd.Pos = new Vector3(posX, 0f, posZ);
                }

                if (prefabName == "Wall" || prefabName == "Container" || prefabName == "GSPDown" || prefabName == "GSPBottomRight" || prefabName == "GSPRight")
                    itemClickCtrl.FindingPath.containers.Add(containerToAdd);
            }
        }

    }

    public void FolowCamera()
    {
        Camera camera = Camera.main;
        if (levelData.alignment)
        {
            camera.transform.position = new Vector3(-0.6f, camera.transform.position.y, camera.transform.position.z);
        }
        else camera.transform.position = new Vector3(0f, camera.transform.position.y, camera.transform.position.z);

        Debug.Log("Hoàn tất di chuyển!");
    }

    public IEnumerator SlideLeaderBoard(bool isSlide)
    {
        if (isSlide)
        {
            // Đặt vị trí ban đầu
            transform.position = new Vector3(4f, transform.position.y, transform.position.z);

            // Di chuyển từ x = 4f đến x = 0 trong 0.25 giây
            Tween tween = transform.DOMoveX(0f, 0.25f).SetEase(Ease.Linear);

            // Đợi tween chạy xong
            yield return tween.WaitForCompletion();
        }
    }

    // BoardCtrl.cs (thêm vào class BoardCtrl)
    public void RebuildGridFromBoardAlls()
    {
        if (levelData == null)
        {
            Debug.LogWarning("RebuildGridFromBoardAlls: levelData là null.");
            return;
        }

        // đảm bảo boardAlls chiều dài đúng (height * width) nếu có thể

        // Tạo lại mảng grid và gridContainerSpot, IsWall
        grid = new BoardCell[levelData.height, levelData.width];

        for (int row = 0; row < levelData.height; row++)
        {
            for (int col = 0; col < levelData.width; col++)
            {
                int index = row * levelData.width + col;
                if (index < 0 || index >= boardAlls.Count)
                {
                    // thiếu phần tử tại vị trí này — coi như null
                    grid[row, col] = null;
                    continue;
                }

                GameObject obj = boardAlls[index];
                if (obj == null)
                {
                    grid[row, col] = null;
                    continue;
                }

                // Nếu là BoardCell
                if (obj.TryGetComponent(out BoardCell boardCell))
                {
                    // đảm bảo boardCell.Pos / Container / IdType / TypeItem là chính xác theo transform / tên
                    boardCell.Pos = obj.transform.position;
                    grid[row, col] = boardCell;

                    // nếu có container đính kèm (trong SpawnLeaderBoard bạn gán Container riêng)
                    boardCell.Container = gridContainerSpot[row, col];
                    // mặc định không cho nó spawn
                    boardCell.HasSpawn = false;
                }
                else
                {
                    grid[row, col] = null;
                }

            }
        }

        // Sau khi rebuild grid[,] và boardCells, gọi AddNeighbor để cập nhật neighbors
        AddNeighbor(grid, IsWall);
        AlignContainer(gridContainerSpot);

    }

    public void AddNeighbor(BoardCell[,] grid, bool[,] IsWall)
    {
        // ======== GÁN NEIGHBOR ========
        for (int row = 0; row < levelData.height; row++)
        {
            for (int col = 0; col < levelData.width; col++)
            {
                BoardCell current = grid[row, col];
                if (current == null) continue;

                current.ClearNeighbors();

                // 4 hướng: trên, dưới, trái, phải
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

                    int nx = col + dx; // Cột mới
                    int ny = row + dy; // Hàng mới

                    if (nx >= 0 && nx < levelData.width && ny >= 0 && ny < levelData.height)
                    {
                        BoardCell neighbor = grid[ny, nx];
                        if (neighbor != null)
                        {
                            DirectionNeighBor directionNeighBor = DirectionNeighBor.Top;
                            if (d == 0) directionNeighBor = DirectionNeighBor.Top;
                            if (d == 1) directionNeighBor = DirectionNeighBor.Bottom;
                            if (d == 2) directionNeighBor = DirectionNeighBor.Left;
                            if (d == 3) directionNeighBor = DirectionNeighBor.Right;

                            current.AddNeighbor(neighbor, directionNeighBor);
                        }
                    }
                }

                // --- Kiểm tra 4 hướng để quyết định SetActive ---
                string parentName = current.transform.name;
                if (parentName != "1" && parentName != "2" && parentName != "3" &&
                    parentName != "4" && parentName != "5" && parentName != "6" &&
                    parentName != "7")
                {
                    continue;
                }
                bool canActivate = false;

                // 4 hướng tương tự
                for (int d = 0; d < directions.GetLength(0); d++)
                {
                    int dx = directions[d, 0];
                    int dy = directions[d, 1];

                    int nx = col + dx;
                    int ny = row + dy;

                    // Nếu vượt ra ngoài biên => coi như trống (được phép)
                    if (nx < 0 || nx >= levelData.width || ny < 0 || ny >= levelData.height)
                    {
                        canActivate = true;
                        break;
                    }

                    // Nếu ô kế bên là null hoặc không phải Wall => có thể kích hoạt
                    if (grid[ny, nx] == null && !IsWall[ny, nx])
                    {
                        canActivate = true;
                        break;
                    }
                }

                if (canActivate)
                {
                    current.BoardCellAnimation.SetActive();
                    current.IsActive = true;
                    current.HasClick = true;
                }
                else
                {
                    current.HasClick = false;
                    // them 11/4/2025
                    current.IsActive = false;
                    current.BoardCellAnimation.SetInActive();
                }
            }
        }
    }
    public void AlignContainer(Container[,] gridContainerSpot)
    {
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
                        gridSpotSpawn.ClearContainer();
                        bool isBottom = gridSpotSpawn.CheckDirection(Direction.Down);
                        if (isBottom && bottom < levelData.height)
                        {
                            gridSpotSpawn.AddContainer(gridContainerSpot[row, col], Direction.Down);
                            BoardCell boardCell = boardAlls[row * levelData.width + col].GetComponent<BoardCell>();
                            if (boardCell != null)
                            {
                                boardCell.HasSpawn = true;
                            }
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
                        gridSpotSpawn1.ClearContainer();
                        bool isTop = gridSpotSpawn1.CheckDirection(Direction.Up);
                        if (isTop && top >= 0)
                        {
                            gridSpotSpawn1.AddContainer(gridContainerSpot[row, col], Direction.Up);
                            BoardCell boardCell = boardAlls[row * levelData.width + col].GetComponent<BoardCell>();
                            if (boardCell != null)
                            {
                                boardCell.HasSpawn = true;
                            }
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
                        gridSpotSpawn2.ClearContainer();
                        bool isRight = gridSpotSpawn2.CheckDirection(Direction.Right);
                        if (isRight && right < levelData.width)
                        {
                            gridSpotSpawn2.AddContainer(gridContainerSpot[row, col], Direction.Right);
                            BoardCell boardCell = boardAlls[row * levelData.width + col].GetComponent<BoardCell>();
                            if (boardCell != null)
                            {
                                boardCell.HasSpawn = true;
                            }
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
                        gridSpotSpawn3.ClearContainer();
                        bool isLeft = gridSpotSpawn3.CheckDirection(Direction.Left);
                        if (isLeft && left >= 0)
                        {
                            gridSpotSpawn3.AddContainer(gridContainerSpot[row, col], Direction.Left);
                            BoardCell boardCell = boardAlls[row * levelData.width + col].GetComponent<BoardCell>();
                            if (boardCell != null)
                            {
                                boardCell.HasSpawn = true;
                            }
                        }
                    }
                }
            }
        }
    }

    public void UpdateBoardCell(BoardCell boardCell)
    {
        boardCells.Remove(boardCell);
    }

    public void AddBlockInLeaderBoard(Container container, BoardCell boardCell)
    {
        for (int i = 0; i < boardAlls.Count; i++)
        {
            if (boardAlls[i].TryGetComponent<Container>(out var c) && c == container)
            {
                // Insert the new boardCell's gameObject at this position and keep boardCells in sync
                boardAlls[i] = boardCell.gameObject;
                // if (!boardCells.Contains(boardCell))
                //     boardCells.Add(boardCell);
                return;
            }
            if (boardAlls[i].TryGetComponent<BoardCell>(out BoardCell cell) && cell.Container == container)
            {
                boardAlls[i] = boardCell.gameObject;
                return;
            }
        }
    }
}