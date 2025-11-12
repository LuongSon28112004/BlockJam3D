using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoosterUndo : MonoBehaviour
{
    [Header("Undo Booster")]
    [SerializeField] private Stack<bool> isMatch3s;
    [SerializeField] private Stack<Queue<KeyValuePair<BoardCell, Container>>> undoQueue;
    [SerializeField] private Stack<(BoardCell cell, Container container, List<Vector3> path)> lastMove;
    public Stack<Queue<KeyValuePair<BoardCell, Container>>> UndoQueue { get => undoQueue; set => undoQueue = value; }
    public Stack<(BoardCell cell, Container container, List<Vector3> path)> LastMove { get => lastMove; set => lastMove = value; }

    public Stack<bool> IsMatch3s { get => isMatch3s; set => isMatch3s = value; }
    void Start()
    {
        lastMove = new Stack<(BoardCell cell, Container container, List<Vector3> path)>();
        undoQueue = new Stack<Queue<KeyValuePair<BoardCell, Container>>>();
        isMatch3s = new Stack<bool>();
    }
    #region Undo

    public void ResetStackUndo()
    {
        lastMove.Clear();
        undoQueue.Clear();
        isMatch3s.Clear();
    }

    public void AddStack(BoardCell boardCell, Container container, List<Vector3> path)
    {
        LevelManager.Instance.boosterCtrl.BoosterUndo.LastMove.Push((boardCell, container, path));
        Queue<KeyValuePair<BoardCell, Container>> temp = new Queue<KeyValuePair<BoardCell, Container>>();
        for (int i = 0; i < LevelManager.Instance.cellPlayCtrl.BoardCells.Count; i++)
        {
            if (LevelManager.Instance.cellPlayCtrl.BoardCells[i].TypeItem == boardCell.TypeItem && LevelManager.Instance.cellPlayCtrl.BoardCells[i] != boardCell)
            {
                temp.Enqueue(new KeyValuePair<BoardCell, Container>(LevelManager.Instance.cellPlayCtrl.BoardCells[i], LevelManager.Instance.cellPlayCtrl.CellPlays[i]));
            }
        }
        if (temp.Count != 0 && temp.Count == 2) LevelManager.Instance.boosterCtrl.BoosterUndo.UndoQueue.Push(temp);
    }
    public IEnumerator Undo()
    {
        if (isMatch3s.Count == 0) yield break;
        bool isMatch3 = isMatch3s.Pop();
        if (!isMatch3)
        {
            BoosterCtrl.Instance.IsBusy = true;
            yield return StartCoroutine(UndoNormalMove());
        }
        else
        {
            BoosterCtrl.Instance.IsBusy = true;
            yield return StartCoroutine(UndoMatch3Move());
        }
        int isUndo = IsMatch3s.Count == 0 ? -1 : 1;
        CustomeEventSystem.Instance.ActiveBooster(new List<int> { isUndo, (LevelManager.Instance.cellPlayCtrl.BoardCells.Count == 0 ? -1 : 1), 1, 1 });
        LevelManager.Instance.BoardCtrl.itemClickCtrl.isStart = false;
        yield return new WaitForSeconds(1f);
        BoosterCtrl.Instance.IsBusy = false;
    }

    #region --- Undo Logic Helpers ---

    // Undo khi KHÔNG có Match-3
    private IEnumerator UndoNormalMove()
    {
        // kiểm tra stack có phần tử không
        if (lastMove == null || lastMove.Count == 0) yield break;

        var (cell, container, path) = lastMove.Pop();

        // if (cell == null || container == null)
        //     yield break;
        if (!cell.IsBoosterAdd)
        {
            StartCoroutine(MoveToLeaderBoard(cell, container, path));
        }
        else
        {
            StartCoroutine(MoveToCellAdd(cell, container, path));
        }


    }

    private IEnumerator MoveToCellAdd(BoardCell cell, Container container, List<Vector3> path)
    {
        BoosterCtrl.Instance.BoosterAdd.BoosterAddPos.AddBoardCell(cell, container);
        cell.HasClick = true;
        cell.IsBoosterAdd = true;
        int index = LevelManager.Instance.cellPlayCtrl.BoardCells.IndexOf(cell);
        if (index != -1)
        {
            // đặt IsContaining trước khi remove nếu CellPlays vẫn song song với BoardCells
            if (index < LevelManager.Instance.cellPlayCtrl.CellPlays.Count)
                LevelManager.Instance.cellPlayCtrl.CellPlays[index].IsContaining = false;

            LevelManager.Instance.cellPlayCtrl.BoardCells.RemoveAt(index);
            if (LevelManager.Instance.cellPlayCtrl.CountCellType.ContainsKey(cell.TypeItem))
                LevelManager.Instance.cellPlayCtrl.CountCellType[cell.TypeItem].Remove(cell);
            yield return StartCoroutine(LevelManager.Instance.cellPlayCtrl.RearrangeCellsAfterRemove());
        }

        yield break;
    }

    private IEnumerator MoveToLeaderBoard(BoardCell cell, Container container, List<Vector3> path)
    {
        // 1 Gỡ cell khỏi danh sách và cập nhật trạng thái
        LevelManager.Instance.BoardCtrl.AddBlockInLeaderBoard(container, cell);
        int index = LevelManager.Instance.cellPlayCtrl.BoardCells.IndexOf(cell);
        if (index != -1)
        {
            // đặt IsContaining trước khi remove nếu CellPlays vẫn song song với BoardCells
            if (index < LevelManager.Instance.cellPlayCtrl.CellPlays.Count)
                LevelManager.Instance.cellPlayCtrl.CellPlays[index].IsContaining = false;

            LevelManager.Instance.cellPlayCtrl.BoardCells.RemoveAt(index);
            if (LevelManager.Instance.cellPlayCtrl.CountCellType.ContainsKey(cell.TypeItem))
                LevelManager.Instance.cellPlayCtrl.CountCellType[cell.TypeItem].Remove(cell);
            //xóa block justSpawn nếu có
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
            yield return StartCoroutine(LevelManager.Instance.cellPlayCtrl.RearrangeCellsAfterRemove());
            ResetCellAfterUndo(cell, container);


            // inactive lại các hàng xóm
            cell.SetInActiveNeighBor();
        }
    }

    // Undo khi CÓ Match-3
    Vector3 posUndoLast;
    private IEnumerator UndoMatch3Move()
    {
        if (undoQueue == null || undoQueue.Count == 0) yield break;

        // 1 Tạo lại các ô bị phá
        yield return StartCoroutine(RecreateMatchedCells());

        // 2 Tạo lại ô di chuyển cuối cùng (nếu có)
        if (lastMove != null && lastMove.Count > 0)
            yield return StartCoroutine(RecreateLastMovedCell());
    }

    #endregion


    #region --- Undo Sub Methods ---

    // Di chuyển ngược lại theo path
    private IEnumerator MoveBackward(BoardCell cell, List<Vector3> path)
    {
        if (path == null || path.Count == 0 || cell == null) yield break;

        var movement = cell.BoardCellMovement;
        var animation = cell.BoardCellAnimation;

        if (movement == null || animation == null) yield break;

        cell.transform.localRotation = Quaternion.Euler(0, 180, 0);
        //gán lại Pos của BoardCell
        cell.Pos = path[0];
        //setAnimaion Running
        animation.SetRunning();
        // đi đến điểm cuối cùng trước
        yield return StartCoroutine(movement.MovementToPosNormal(path[^1]));

        // xóa phần tử cuối (đã đến)
        path.RemoveAt(path.Count - 1);
        // di chuyển ngược lại đường đi ban đầu
        yield return MovePath(path, movement);

        animation.SetIdle();
        cell.transform.localRotation = Quaternion.Euler(0, 0, 0);
    }

    private IEnumerator MovePath(List<Vector3> path, BoardCellMovement movement)
    {
        for (int i = path.Count - 1; i >= 0; i--)
        {
            yield return StartCoroutine(movement.MovementToPosNormal(path[i]));
        }

    }

    // Reset trạng thái sau khi Undo thường
    private void ResetCellAfterUndo(BoardCell cell, Container container)
    {
        if (cell == null || container == null) return;
        cell.HasClick = true;
        cell.Container = container;
        cell.IsInCellPlay = false;
        container.IsContaining = true;
    }

    // Tạo lại các ô cùng loại đã bị phá khi match-3
    private IEnumerator RecreateMatchedCells()
    {
        if (undoQueue == null || undoQueue.Count == 0) yield break;

        var queue = undoQueue.Pop();
        foreach (var pair in queue)
        {
            var (undoCell, undoContainer) = (pair.Key, pair.Value);
            //if (undoCell == null || undoContainer == null) continue;
            yield return StartCoroutine(RecreateCellAt(undoCell, undoContainer));
            posUndoLast = undoContainer.Pos;
        }
    }

    // Tạo lại cell tại vị trí cụ thể
    private IEnumerator RecreateCellAt(BoardCell sourceCell, Container container)
    {
        //if (sourceCell == null || container == null) yield break;

        // string prefabName = Enum.GetName(typeof(TypeItem), sourceCell.TypeItem);
        // GameObject prefab = AddressableManager.Instance.GetPrefab(prefabName);
        int index = LevelManager.Instance.cellPlayCtrl.BoardCellMatch_3.IndexOf(sourceCell);
        if (index == -1)
        {
            Debug.Log("not found gameobject");
            yield break;
        }
        GameObject block = LevelManager.Instance.cellPlayCtrl.BoardCellMatch_3[index].gameObject;
        block.SetActive(true);
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

        //lưu lại data của block vừa undo vào BoardCtrl để việc check win hoặc lose
        // trường hợp normal không cần lưu vì code của tôi chưa xóa các class BoardCell đấy
        LevelManager.Instance.BoardCtrl.BoardCells.Add(sourceCell);

        int posCell = LevelManager.Instance.cellPlayCtrl.CellPlays.IndexOf(container);
        yield return StartCoroutine(LevelManager.Instance.cellPlayCtrl.ShiftCellsRight(posCell));

        if (posCell >= 0 && posCell < LevelManager.Instance.cellPlayCtrl.BoardCells.Count)
            LevelManager.Instance.cellPlayCtrl.BoardCells[posCell] = newCell;
        else
            LevelManager.Instance.cellPlayCtrl.BoardCells.Insert(posCell, newCell);

        if (posCell >= 0 && posCell < LevelManager.Instance.cellPlayCtrl.CellPlays.Count)
            LevelManager.Instance.cellPlayCtrl.CellPlays[posCell].IsContaining = true;

        if (!LevelManager.Instance.cellPlayCtrl.CountCellType.ContainsKey(newCell.TypeItem))
            LevelManager.Instance.cellPlayCtrl.CountCellType[newCell.TypeItem] = new List<BoardCell>();

        LevelManager.Instance.cellPlayCtrl.CountCellType[newCell.TypeItem].Add(newCell);
        newCell.transform.position = container.Pos;
    }

    // Tạo lại ô di chuyển cuối cùng sau khi match-3
    private IEnumerator RecreateLastMovedCell()
    {
        if (lastMove == null || lastMove.Count == 0) yield break;

        var (cellData, container, path) = lastMove.Pop();
        //if (cellData == null || container == null) yield break;

        // string prefabName = Enum.GetName(typeof(TypeItem), cellData.TypeItem);
        // GameObject prefab = AddressableManager.Instance.GetPrefab(prefabName);
        // GameObject block = Instantiate(prefab, Vector3.zero, Quaternion.identity, parentBoard);
        if (!cellData.IsBoosterAdd)
        {
            LevelManager.Instance.BoardCtrl.AddBlockInLeaderBoard(container, cellData);
            int indexx = LevelManager.Instance.cellPlayCtrl.BoardCellMatch_3.IndexOf(cellData);
            if (indexx == -1)
            {
                Debug.Log("not found gameobject");
                yield break;
            }
            GameObject block = LevelManager.Instance.cellPlayCtrl.BoardCellMatch_3[indexx].gameObject;
            block.SetActive(true);

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
                // đảm bảo danh sách đủ dài
                if (index < LevelManager.Instance.cellPlayCtrl.BoardCells.Count)
                    LevelManager.Instance.cellPlayCtrl.BoardCells[index] = recreatedCell;
                else
                    LevelManager.Instance.cellPlayCtrl.BoardCells.Insert(index, recreatedCell);

                if (index < LevelManager.Instance.cellPlayCtrl.CellPlays.Count)
                    LevelManager.Instance.cellPlayCtrl.CellPlays[index].IsContaining = true;

                if (!LevelManager.Instance.cellPlayCtrl.CountCellType.ContainsKey(recreatedCell.TypeItem))
                    LevelManager.Instance.cellPlayCtrl.CountCellType[recreatedCell.TypeItem] = new List<BoardCell>();

                LevelManager.Instance.cellPlayCtrl.CountCellType[recreatedCell.TypeItem].Add(recreatedCell);
            }

            //lưu lại data của block vừa undo vào BoardCtrl để việc check win hoặc 
            LevelManager.Instance.BoardCtrl.BoardCells.Add(cellData);

            // xóa block justSpawn
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
        else
        {
            int indexx = LevelManager.Instance.cellPlayCtrl.BoardCellMatch_3.IndexOf(cellData);
            if (indexx == -1)
            {
                Debug.Log("not found gameobject");
                yield break;
            }
            GameObject block = LevelManager.Instance.cellPlayCtrl.BoardCellMatch_3[indexx].gameObject;
            block.SetActive(true);
            BoosterCtrl.Instance.BoosterAdd.BoosterAddPos.AddBoardCell(cellData, container);
            cellData.HasClick = true;
            cellData.BoardCellAnimation.SetActive();
            cellData.IsBoosterAdd = true;
            int index = LevelManager.Instance.cellPlayCtrl.BoardCells.IndexOf(cellData);
            if (index != -1)
            {
                // đặt IsContaining trước khi remove nếu CellPlays vẫn song song với BoardCells
                if (index < LevelManager.Instance.cellPlayCtrl.CellPlays.Count)
                    LevelManager.Instance.cellPlayCtrl.CellPlays[index].IsContaining = false;

                LevelManager.Instance.cellPlayCtrl.BoardCells.RemoveAt(index);
                if (LevelManager.Instance.cellPlayCtrl.CountCellType.ContainsKey(cellData.TypeItem))
                    LevelManager.Instance.cellPlayCtrl.CountCellType[cellData.TypeItem].Remove(cellData);
                yield return StartCoroutine(LevelManager.Instance.cellPlayCtrl.RearrangeCellsAfterRemove());
                AudioManager.Instance.PlayOneShot("BLJ_Boosters_Undo_01", 1f);
            }
        }
    }

    #endregion
    #endregion
}
