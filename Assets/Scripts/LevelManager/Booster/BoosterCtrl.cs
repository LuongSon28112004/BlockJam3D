using System;
using System.Collections;
using System.Collections.Generic;
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

    public bool IsMatch3 { get => isMatch3; set => isMatch3 = value; }
    public Queue<KeyValuePair<BoardCell, Container>> UndoQueue { get => undoQueue; set => undoQueue = value; }
    public (BoardCell cell, Container container, List<Vector3> path) LastMove { get => lastMove; set => lastMove = value; }
    public Container ContainerLastMove { get => containerLastMove; set => containerLastMove = value; }

    //[Header("Add Booster")]

    void Start()
    {
        lastMove = (new BoardCell(), new Container(), new List<Vector3>());
        undoQueue = new Queue<KeyValuePair<BoardCell, Container>>();
    }

    #region Undo
    public IEnumerator Undo()
    {
        if (!isMatch3)
            yield return StartCoroutine(UndoNormalMove());
        else
            yield return StartCoroutine(UndoMatch3Move());
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

        // 2 Di chuyển ngược lại đường đi
        yield return StartCoroutine(MoveBackward(cell, path));

        // 3 Cập nhật trạng thái cuối cùng
        yield return StartCoroutine(LevelManager.Instance.cellPlayCtrl.RearrangeCellsAfterRemove()); //tat tam
        ResetCellAfterUndo(cell, container);

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
        yield return StartCoroutine(movement.MovementToPos(path[^1]));

        path.RemoveAt(path.Count - 1);
        for (int i = path.Count - 1; i >= 0; i--)
        {
            yield return StartCoroutine(movement.MovementToPos(path[i]));
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
}