using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Unity.Collections.LowLevel.Unsafe;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;

public class BoosterCtrl : MonoBehaviour
{
    [Header("Booster Component")]
     [SerializeField] private Transform parentBoard;
    [Header("Undo Booster")]
    [SerializeField] private bool isMatch3 = false;
    [SerializeField] private Queue<KeyValuePair<BoardCell, Container>> undoQueue;
    [SerializeField] private (BoardCell cell, Container container, List<Vector3> path) lastMove;
    [SerializeField] private Container containerLastMove;
    [Header("Add Booster")]
    [SerializeField] private BoosterAddPos boosterAddPos;
    [SerializeField] private BoosterMagnetPos boosterMagnetPos;

    public bool IsMatch3 { get => isMatch3; set => isMatch3 = value; }
    public Queue<KeyValuePair<BoardCell, Container>> UndoQueue { get => undoQueue; set => undoQueue = value; }
    public (BoardCell cell, Container container, List<Vector3> path) LastMove { get => lastMove; set => lastMove = value; }
    public Container ContainerLastMove { get => containerLastMove; set => containerLastMove = value; }

    [Header("Busy")]
    public bool IsBusy = false;

    void Start()
    {
        lastMove = (new BoardCell(), new Container(), new List<Vector3>());
        undoQueue = new Queue<KeyValuePair<BoardCell, Container>>();
    }

    #region Undo
    public IEnumerator Undo()
    {
        if (!isMatch3)
        {
            IsBusy = true;
            yield return StartCoroutine(UndoNormalMove());

        }
        else
        {
            IsBusy = true;
            yield return StartCoroutine(UndoMatch3Move());
        }
        IsBusy = false;
    }

    #region --- Undo Logic Helpers ---

    // Undo khi KHÔNG có Match-3
    private IEnumerator UndoNormalMove()
    {
        if (lastMove == (null, null, null)) yield break;

        var (cell, container, path) = lastMove;

        // 1 Gỡ cell khỏi danh sách và cập nhật trạng thái
        int index = LevelManager.Instance.cellPlayCtrl.BoardCells.IndexOf(cell);
        if (index != -1)
        {
            LevelManager.Instance.cellPlayCtrl.BoardCells.RemoveAt(index);
            LevelManager.Instance.cellPlayCtrl.CountCellType[cell.TypeItem].Remove(cell);
            LevelManager.Instance.cellPlayCtrl.CellPlays[index].IsContaining = false;
        }

        // xoa block justSpawn
        List<GridSpotSpawn> gridSpotSpawns = LevelManager.Instance.BoardCtrl.gridSpotSpawns;
        for (int i = 0; i < gridSpotSpawns.Count; i++)
        {
            if (gridSpotSpawns[i].CheckContainer(container))
            {
                gridSpotSpawns[i].DestroyBoardCellJustSpawn();
                break;
            }
        }
        // 2 Di chuyển ngược lại đường đi
        yield return StartCoroutine(MoveBackward(cell, path));

        // 3 Cập nhật trạng thái cuối cùng
        yield return StartCoroutine(LevelManager.Instance.cellPlayCtrl.RearrangeCellsAfterRemove()); //tat tam
        ResetCellAfterUndo(cell, container);

        //Audio sound
        AudioManager.Instance.PlayOneShot("BLJ_Boosters_Undo_01", 1f);


        //convert BoardCell

        // inactive lại các hàng xóm
        cell.SetInActiveNeighBor();
        lastMove = (null, null, null);
    }

    // Undo khi CÓ Match-3

    Vector3 posUndoLast;
    private IEnumerator UndoMatch3Move()
    {
        if (undoQueue.Count == 0) yield break;

        // 1 Tạo lại các ô bị phá
        yield return StartCoroutine(RecreateMatchedCells());

        // 2 Tạo lại ô di chuyển cuối cùng
        yield return StartCoroutine(RecreateLastMovedCell());
        
        //Audio sound
        AudioManager.Instance.PlayOneShot("BLJ_Boosters_Undo_01", 1f);


        // 3 Reset lại trạng thái
        isMatch3 = false;
        undoQueue.Clear();
    }

    #endregion


    #region --- Undo Sub Methods ---

    // Di chuyển ngược lại theo path
    private IEnumerator MoveBackward(BoardCell cell, List<Vector3> path)
    {
        if (path == null || path.Count == 0) yield break;

        var movement = cell.BoardCellMovement;
        var animation = cell.BoardCellAnimation;

        cell.transform.localRotation = Quaternion.Euler(0, 180, 0);
        animation.SetRunning();
        yield return StartCoroutine(movement.MovementToPosNormal(path[^1]));

        path.RemoveAt(path.Count - 1);
        for (int i = path.Count - 1; i >= 0; i--)
        {
            yield return StartCoroutine(movement.MovementToPosNormal(path[i]));
        }

        animation.SetIdle();
        cell.transform.localRotation = Quaternion.Euler(0, 0, 0);
    }

    // Reset trạng thái sau khi Undo thường
    private void ResetCellAfterUndo(BoardCell cell, Container container)
    {
        cell.HasClick = true;
        cell.Container = container;
        cell.IsInCellPlay = false;
        container.IsContaining = true;
    }

    // Tạo lại các ô cùng loại đã bị phá khi match-3
    private IEnumerator RecreateMatchedCells()
    {
        foreach (var pair in undoQueue)
        {
            var (undoCell, undoContainer) = (pair.Key, pair.Value);
            yield return StartCoroutine(RecreateCellAt(undoCell, undoContainer));
            posUndoLast = undoContainer.Pos;
        }
    }

    // Tạo lại cell tại vị trí cụ thể
    private IEnumerator RecreateCellAt(BoardCell sourceCell, Container container)
    {
        string prefabName = Enum.GetName(typeof(TypeItem), sourceCell.TypeItem);
        GameObject prefab = AddressableManager.Instance.GetPrefab(prefabName);
        GameObject block = Instantiate(prefab, Vector3.zero, Quaternion.identity, parentBoard);

        BoardCell newCell = block.GetComponent<BoardCell>();
        //config data
        newCell.TypeItem = sourceCell.TypeItem;
        newCell.HasClick = false;
        newCell.Container = container;
        newCell.Pos = container.Pos;
        newCell.IsInCellPlay = true;
        newCell.BoardCellAnimation = block.GetComponentInChildren<BoardCellAnimation>();
        newCell.BoardCellMovement = block.GetComponentInChildren<BoardCellMovement>();
        newCell.BoardCellAnimation.SetActive();
        newCell.Barrel.SetActive(false);

        int posCell = LevelManager.Instance.cellPlayCtrl.CellPlays.IndexOf(container);
        yield return StartCoroutine(LevelManager.Instance.cellPlayCtrl.ShiftCellsRight(posCell));

        LevelManager.Instance.cellPlayCtrl.BoardCells[posCell] = newCell;
        LevelManager.Instance.cellPlayCtrl.CellPlays[posCell].IsContaining = true;
        LevelManager.Instance.cellPlayCtrl.CountCellType[newCell.TypeItem].Add(newCell);
        newCell.transform.position = container.Pos;
    }

    // Tạo lại ô di chuyển cuối cùng sau khi match-3
    private IEnumerator RecreateLastMovedCell()
    {
        var (cellData, container, path) = lastMove;

        string prefabName = Enum.GetName(typeof(TypeItem), cellData.TypeItem);
        GameObject prefab = AddressableManager.Instance.GetPrefab(prefabName);
        GameObject block = Instantiate(prefab, Vector3.zero, Quaternion.identity, parentBoard);

        BoardCell recreatedCell = block.GetComponent<BoardCell>();
        //config data
        recreatedCell.TypeItem = cellData.TypeItem;
        recreatedCell.HasClick = false;
        recreatedCell.Neighbors = cellData.Neighbors;
        recreatedCell.Container = container;
        recreatedCell.Pos = container.Pos;
        recreatedCell.IsInCellPlay = false;
        recreatedCell.BoardCellAnimation = block.GetComponentInChildren<BoardCellAnimation>();
        recreatedCell.BoardCellMovement = block.GetComponentInChildren<BoardCellMovement>();
        recreatedCell.BoardCellAnimation.SetActive();
        recreatedCell.Barrel.SetActive(false);
        recreatedCell.transform.localPosition = posUndoLast;

        int index = LevelManager.Instance.cellPlayCtrl.CellPlays.IndexOf(container);
        if (index != -1)
        {
            LevelManager.Instance.cellPlayCtrl.BoardCells[index] = recreatedCell;
            LevelManager.Instance.cellPlayCtrl.CellPlays[index].IsContaining = true;
            LevelManager.Instance.cellPlayCtrl.CountCellType[recreatedCell.TypeItem].Add(recreatedCell);
        }

        // xoa block justSpawn
        List<GridSpotSpawn> gridSpotSpawns = LevelManager.Instance.BoardCtrl.gridSpotSpawns;
        for (int i = 0; i < gridSpotSpawns.Count; i++)
        {
            if (gridSpotSpawns[i].CheckContainer(container))
            {
                gridSpotSpawns[i].DestroyBoardCellJustSpawn();
                break;
            }
        }

        yield return StartCoroutine(MoveBackward(recreatedCell, path));
        //reset lai cac hang xom
        recreatedCell.SetInActiveNeighBor();
        recreatedCell.IsInCellPlay = false;

        recreatedCell.HasClick = true;
        container.IsContaining = true;
    }

    #endregion
    #endregion

    #region Add
    public IEnumerator Add()
    {
        if (LevelManager.Instance.cellPlayCtrl.BoardCells.Count == 0) yield break;
        int pos = 0;
        for (int i = LevelManager.Instance.cellPlayCtrl.BoardCells.Count - 1; i >= LevelManager.Instance.cellPlayCtrl.BoardCells.Count - 3; i--)
        {
            BoardCellMovement boardCellMovement = LevelManager.Instance.cellPlayCtrl.BoardCells[i].BoardCellMovement;
            BoardCellAnimation boardCellAnimation = LevelManager.Instance.cellPlayCtrl.BoardCells[i].BoardCellAnimation;
            LevelManager.Instance.cellPlayCtrl.CountCellType[LevelManager.Instance.cellPlayCtrl.BoardCells[i].TypeItem].Remove(LevelManager.Instance.cellPlayCtrl.BoardCells[i]);
            if (boardCellMovement == null) yield break;
            if (boardCellAnimation == null) yield break;
            boardCellAnimation.SetRunning();
            yield return boardCellMovement.MovementToPos(boosterAddPos.ListPosBoosterAdd[pos]);
            boardCellAnimation.SetIdle();
            LevelManager.Instance.cellPlayCtrl.CellPlays[i].IsContaining = false;
            LevelManager.Instance.cellPlayCtrl.BoardCells[i].HasClick = true;
            LevelManager.Instance.cellPlayCtrl.BoardCells[i].IsBoosterAdd = true;
            pos++;
        }

        var boardCells = LevelManager.Instance.cellPlayCtrl.BoardCells;

        int removeCount = 3;
        int startIndex = Mathf.Max(0, boardCells.Count - removeCount);

        boardCells.RemoveRange(startIndex, boardCells.Count - startIndex);


    }
    #endregion

    #region Shuffle
    public IEnumerator Shuffle(List<GameObject> LeaderBoards)
    {
        IsBusy = true;
        CustomeEventSystem.Instance.ActiveBooster(new List<int> { -1, -1, 1, -1 });
        // Lấy reference gốc
        LevelData original = LevelManager.Instance.BoardCtrl.levelData;

        // Tạo bản sao runtime (không ảnh hưởng file gốc)
        LevelData levelData = ScriptableObject.CreateInstance<LevelData>();
        levelData.CopyFrom(original);

        // Shuffle và load lại
        string[] prefabNames = levelData.ShufflePrefabs();
        levelData.prefabNames = prefabNames;
        LevelManager.Instance.cellPlayCtrl.ResetCellPlay();
        yield return StartCoroutine(LevelManager.Instance.BoardCtrl.LoadLevel(levelData, false));
        if (LevelManager.Instance.cellPlayCtrl.BoardCells.Count == 0)
        {
            CustomeEventSystem.Instance.ActiveBooster(new List<int> { -1, -1, 1, 1 });
            LevelManager.Instance.BoardCtrl.itemClickCtrl.isStart = false;
        }
        else
        {
            CustomeEventSystem.Instance.ActiveBooster(new List<int> { -1, 1, 1, 1 });
            LevelManager.Instance.BoardCtrl.itemClickCtrl.isStart = false;
        }
        IsBusy = false;
    }

    #endregion

    #region Magnet
    Dictionary<TypeItem, int> countDict = new Dictionary<TypeItem, int>();
    public IEnumerator Magnet()
    {
        IsBusy = true;
        CustomeEventSystem.Instance.ActiveBooster(new List<int> { -1, -1, -1, 1 });
        yield return new WaitForSeconds(0.3f);
        TypeItem type = FindTypeFirstAppearMany();
        int maxMagnet = 0;
        if (countDict.TryGetValue(type, out int currentCount))
        {
            maxMagnet = 3 - currentCount;
        }
        else
        {
            maxMagnet = 3;
            type = RandomTypeFromBoardCellLeaderBoard();
        }


        List<BoardCell> boardCells = LevelManager.Instance.BoardCtrl.BoardCells;
        List<BoardCell> ListChoices = new List<BoardCell>();

        List<BoardCell> allBoardCells = new List<BoardCell>();

        int count = 0;
        for (int i = 0; i < boardCells.Count; i++)
        {
            // kiểm tra các ô cùng loại và nó phải đang không ở thanh bên dưới
            if (boardCells[i].TypeItem == type && !boardCells[i].IsInCellPlay)
            {
                count++;
                ListChoices.Add(boardCells[i]);
                allBoardCells.Add(boardCells[i]);
            }
            if (count == maxMagnet) break;
        }

        // Nếu trên board không đủ quân thì phải sinh ra 1 quân từ các máy Pipe random
            while(count < maxMagnet)
            {
                List<GridSpotSpawn> gridSpotSpawns = LevelManager.Instance.BoardCtrl.gridSpotSpawns;
                BoardCell boardCell = null;
                GameObject obj = AddressableManager.Instance.GetPrefab(Enum.GetName(typeof(TypeItem), type));
                for(int i = 0; i < gridSpotSpawns.Count; i++)
                {
                    if(gridSpotSpawns[i].MaxPointSpawn > 0)
                    {
                        StartCoroutine(gridSpotSpawns[i].SpawnBlockMagnet(obj, gridSpotSpawns[i].transform, type, (result) =>
                        {
                            boardCell = result;
                        }));
                        yield return new WaitUntil(() => boardCell != null);
                        ListChoices.Add(boardCell);
                        allBoardCells.Add(boardCell);
                        // cộng data 
                        LevelManager.Instance.BoardCtrl.initialTypeCounts[type] += 1;
                        count += 1;
                        if (count >= maxMagnet) break;
                    }
                }
            }

        count = 1;
        Sequence seqKnob = DOTween.Sequence();

        // --- Giai đoạn Knob ---
        for (int i = 0; i < ListChoices.Count; i++)
        {
            BoardCellMovement bc = ListChoices[i].BoardCellMovement;
            BoardCellAnimation boardCellAnimation = ListChoices[i].BoardCellAnimation;
            StartCoroutine(LevelManager.Instance.BoardCtrl.CheckSpawnBlock(ListChoices[i].Container));
            StartCoroutine(ListChoices[i].SetActiveNeighBor());
            boardCellAnimation.SetActive();
            seqKnob.Join(bc.Knob());
        }

        List<BoardCell> boardCellss = LevelManager.Instance.cellPlayCtrl.BoardCells;
        for (int i = 0; i < boardCellss.Count; i++)
        {
            if (boardCellss[i].TypeItem == type)
            {
                BoardCellMovement bc = boardCellss[i].BoardCellMovement;
                allBoardCells.Add(boardCellss[i]);
                seqKnob.Join(bc.Knob());
            }
        }

        // Chờ tất cả Knob xong
        yield return seqKnob.WaitForCompletion();

        // Giai đoạn Move
        Sequence seqMove = DOTween.Sequence();
        count = 1;

        for (int i = 0; i < ListChoices.Count; i++)
        {
            if (count > 3) break;
            BoardCellMovement bc = ListChoices[i].BoardCellMovement;
            seqMove.Join(bc.MovementToPosTween(boosterMagnetPos.ListPosBoosterMagnet[3 - count]));
            count++;
        }

        for (int i = 0; i < boardCellss.Count; i++)
        {
            if (boardCellss[i].TypeItem == type)
            {
                BoardCellMovement bc = boardCellss[i].BoardCellMovement;
                seqMove.Join(bc.MovementToPosTween(boosterMagnetPos.ListPosBoosterMagnet[3 - count]));
                count++;
            }
        }

        //yield return new WaitForSeconds(0.8f);
        yield return seqMove.WaitForCompletion();
        for (int i = 0; i < ListChoices.Count; i++)
        {
            BoardCellAnimation bc = ListChoices[i].BoardCellAnimation;
            if (bc == null) yield break;
            bc.SetIdle();
            ListChoices[i].transform.DOLocalRotate(Vector3.zero, 0.15f).SetEase(Ease.InSine);
        }

        for (int i = 0; i < boardCellss.Count; i++)
        {
            if (boardCellss[i].TypeItem == type)
            {
                BoardCellAnimation bc = boardCellss[i].BoardCellAnimation;
                if (bc == null) yield break;
                bc.SetIdle();
                boardCellss[i].transform.DOLocalRotate(Vector3.zero, 0.15f).SetEase(Ease.InSine);
            }
        }

        //yield return new WaitForSeconds(0.3f);
        List<BoardCell> filteredCells = allBoardCells.Where(cell => !cell.IsInCellPlay).ToList();
        for (int i = 0; i < boardCellss.Count; i++)
        {
            if (boardCellss[i].TypeItem == type)
            {
                filteredCells.Add(boardCellss[i]);
            }
        }
        LevelManager.Instance.cellPlayCtrl.RemoveCellData(filteredCells, type);
        StartCoroutine(LevelManager.Instance.cellPlayCtrl.RearrangeCellsAfterRemove());
        yield return StartCoroutine(LevelManager.Instance.cellPlayCtrl.SetAnimMerge(allBoardCells));
        countDict = new Dictionary<TypeItem, int>();
        if (LevelManager.Instance.cellPlayCtrl.BoardCells.Count == 0)
        {
            CustomeEventSystem.Instance.ActiveBooster(new List<int> { -1, -1, 1, 1 });
            LevelManager.Instance.BoardCtrl.itemClickCtrl.isStart = false;
        }
        else
        {
            CustomeEventSystem.Instance.ActiveBooster(new List<int> { -1, 1, 1, 1 });
            LevelManager.Instance.BoardCtrl.itemClickCtrl.isStart = false;
        }
        IsBusy = false;
    }

    private TypeItem RandomTypeFromBoardCellLeaderBoard()
    {
        var boardCells = LevelManager.Instance.BoardCtrl.BoardCells;
        List<TypeItem> typeItems = new List<TypeItem>();

        // Lấy danh sách các TypeItem không trùng
        foreach (var cell in boardCells)
        {
            if (cell != null && !typeItems.Contains(cell.TypeItem))
            {
                typeItems.Add(cell.TypeItem);
            }
        }

        // Kiểm tra nếu không có TypeItem nào
        if (typeItems.Count == 0)
        {
            Debug.LogWarning("Không có TypeItem nào trong BoardCells!");
            return default;
        }

        // Chọn ngẫu nhiên 1 TypeItem
        int randomIndex = UnityEngine.Random.Range(0, typeItems.Count);
        return typeItems[randomIndex];
    }


    private TypeItem FindTypeFirstAppearMany()
    {
        List<TypeItem> orderTypeInCellPlay = LevelManager.Instance.cellPlayCtrl.orderPlayInCellPlay;
        List<BoardCell> boardCells = LevelManager.Instance.cellPlayCtrl.BoardCells;

        // Đếm số lần xuất hiện của mỗi TypeItem trong boardCells
        foreach (var cell in boardCells)
        {
            if (cell == null || cell.TypeItem == null)
                continue;

            TypeItem type = cell.TypeItem;
            if (countDict.ContainsKey(type))
                countDict[type]++;
            else
                countDict[type] = 1;
        }

        // Nếu không có ô nào => trả về mặc định
        if (countDict.Count == 0)
            return default;

        TypeItem bestType = default;
        int maxCount = -1;
        int earliestOrderIndex = int.MaxValue;

        // Duyệt theo thứ tự orderTypeInCellPlay để ưu tiên loại nào có thứ tự sớm hơn
        for (int i = 0; i < orderTypeInCellPlay.Count; i++)
        {
            var type = orderTypeInCellPlay[i];
            if (!countDict.TryGetValue(type, out int count))
                continue; // loại này không còn trên bàn

            if (count > maxCount)
            {
                maxCount = count;
                earliestOrderIndex = i;
                bestType = type;
            }
            else if (count == maxCount && i < earliestOrderIndex)
            {
                // Cùng số lượng nhưng xuất hiện sớm hơn trong orderTypeInCellPlay
                earliestOrderIndex = i;
                bestType = type;
            }
        }

        return bestType;
    }

    #endregion


}