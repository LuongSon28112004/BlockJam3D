using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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
            if(gridSpotSpawns[i].CheckContainer(container))
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
        }
    }

    // Tạo lại cell tại vị trí cụ thể
    private IEnumerator RecreateCellAt(BoardCell sourceCell, Container container)
    {
        string prefabName = Enum.GetName(typeof(TypeItem), sourceCell.TypeItem);
        GameObject prefab = AddressableManager.Instance.GetPrefab(prefabName);
        GameObject block = Instantiate(prefab, Vector3.zero, Quaternion.identity, parentBoard);

        BoardCell newCell = block.GetComponent<BoardCell>();
        newCell.TypeItem = sourceCell.TypeItem;
        newCell.HasClick = false;
        newCell.Container = container;
        newCell.Pos = container.Pos;
        newCell.IsInCellPlay = false;
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
        recreatedCell.transform.localPosition = containerLastMove.Pos;

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
            if(gridSpotSpawns[i].CheckContainer(container))
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
        IsBusy = false;
    }

    #endregion

    #region 
    Dictionary<TypeItem, int> countDict = new Dictionary<TypeItem, int>();
    public IEnumerator Magnet()
    {

        TypeItem type = FindTypeFirstAppearMany();
        int maxMagnet = 3 - countDict[type];
        List<BoardCell> boardCells = LevelManager.Instance.BoardCtrl.BoardCells;
        List<BoardCell> ListChoices = new List<BoardCell>();
        int count = 0;
        for (int i = 0; i < boardCells.Count; i++)
        {
            if (boardCells[i].TypeItem == type)
            {
                count++;
                ListChoices.Add(boardCells[i]);

            }
            if (count == maxMagnet) break;
        }

        count = 1;

        for (int i = 0; i < ListChoices.Count; i++)
        {
            BoardCellMovement bc = ListChoices[i].BoardCellMovement;
            BoardCellAnimation boardCellAnimation = ListChoices[i].BoardCellAnimation;
            StartCoroutine(ListChoices[i].SetActiveNeighBor());
            boardCellAnimation.SetActive();
            Vector3 pos = ListChoices[i].Pos;
            yield return ListChoices[i].transform.DOMoveY(4f, 0.2f);
            StartCoroutine(bc.MovementToPos(boosterMagnetPos.ListPosBoosterMagnet[3 - count]));
            yield return ListChoices[i].transform.DOMoveY(pos.y, 0.2f);
            count += 1;
        }
        
        List<BoardCell> boardCellss = LevelManager.Instance.cellPlayCtrl.BoardCells;
        for(int i = 0; i < boardCellss.Count; i++)
        {
            if(boardCellss[i].TypeItem == type)
            {
                BoardCellMovement bc = boardCellss[i].BoardCellMovement;
                StartCoroutine(bc.MovementToPos(boosterMagnetPos.ListPosBoosterMagnet[3 - count]));
            }
        }
        yield break;
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