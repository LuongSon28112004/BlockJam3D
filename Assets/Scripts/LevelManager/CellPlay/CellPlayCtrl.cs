using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;

public class CellPlayCtrl : MonoBehaviour
{
    private const int MAX_ROW = 7;

    // CellPlay Components
    [SerializeField] private List<BoardCell> boardCells;   // danh sách các ô hiện có (theo thứ tự trái -> phải)
    [SerializeField] private List<Container> cellPlays;    // danh sách container (vị trí)
    [SerializeField] private GameObject prefabCell;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform parentBoard;
    [SerializeField] private Dictionary<TypeItem, int> countCellType;

    //Undo
    [SerializeField] private bool isMatch3 = false;
    [SerializeField] private Queue<KeyValuePair<BoardCell, Container>> undoQueue;
    [SerializeField] private (BoardCell cell, Container container, List<Vector3> path) lastMove;
    [SerializeField] private Container containerLastMove;



    // luu lai cac trang thai phong chong click lien tuc
    private Queue<int> listPos;
    private Queue<BoardCell> listCell;
    public string prefabFolder = "Prefabs";

    private void Start()
    {
        if (LevelManager.Instance != null && LevelManager.Instance.BoardCtrl != null)
        {
            LevelManager.Instance.BoardCtrl.checkAndSavePosAction += CheckAndSavePos;
            LevelManager.Instance.BoardCtrl.MoveToCellPlay += MoveToCell;
        }

        boardCells = new List<BoardCell>();
        cellPlays = new List<Container>();
        listPos = new Queue<int>();
        listCell = new Queue<BoardCell>();
        lastMove = (new BoardCell(), new Container(), new List<Vector3>());
        undoQueue = new Queue<KeyValuePair<BoardCell, Container>>();
        InitCountCellType();
        GenerateCell();
    }

    private void InitCountCellType()
    {
        countCellType = new Dictionary<TypeItem, int>()
        {
            { TypeItem.BlueBase, 0 },
            { TypeItem.BrownBase, 0 },
            { TypeItem.GreenBase, 0 },
            { TypeItem.MagentaBase, 0 }
        };
    }

    private void OnDisable()
    {
        if (LevelManager.Instance != null && LevelManager.Instance.BoardCtrl != null)
        {
            LevelManager.Instance.BoardCtrl.checkAndSavePosAction -= CheckAndSavePos;
            LevelManager.Instance.BoardCtrl.MoveToCellPlay -= MoveToCell;
        }
    }

    private void GenerateCell()
    {
        if (prefabCell == null || spawnPoint == null)
        {
            Debug.LogError("prefabCell hoặc spawnPoint chưa cấu hình.");
            return;
        }

        const float offsetX = 1.25f;
        Vector3 basePos = spawnPoint.position;
        cellPlays.Clear();

        for (int i = 0; i < MAX_ROW; i++)
        {
            Vector3 worldPos = basePos + new Vector3(i * offsetX, 0f, 0f);
            GameObject go = Instantiate(prefabCell, worldPos, Quaternion.identity, spawnPoint);

            if (go.TryGetComponent(out Container container))
            {
                container.IsContaining = false;
                container.Pos = worldPos;
                cellPlays.Add(container);
            }
            else
            {
                Debug.LogWarning($"Prefab tại index {i} thiếu component Container.");
                cellPlays.Add(null);
            }
        }
    }

    #region Logic Check Match_3 and sort cellplay
    private IEnumerator CheckAndSavePos(BoardCell newCell)
    {
        if (boardCells.Count == MAX_ROW) yield break;

        int insertIndex = FindInsertIndex(newCell);
        if (insertIndex >= MAX_ROW)
        {
            Debug.LogWarning("Không thể thêm ô mới - đã đạt giới hạn tối đa!");
            yield break;
        }

        listPos.Enqueue(insertIndex);
        listCell.Enqueue(newCell);
        // Debug.Log("okok" + listPos.Count + insertIndex + newCell.TypeItem);

        if (insertIndex < boardCells.Count)
        {
            yield return StartCoroutine(ShiftCellsRight(insertIndex));
        }

        if (insertIndex == boardCells.Count)
        {
            boardCells.Add(newCell);
            newCell.HasClick = false;
            countCellType[newCell.TypeItem] += 1;
        }
        else
        {
            countCellType[newCell.TypeItem] += 1;
            boardCells[insertIndex] = newCell;
            newCell.HasClick = false;
        }

        cellPlays[insertIndex].IsContaining = true;
    }

    private int FindInsertIndex(BoardCell newCell)
    {
        int lastSameTypeIndex = -1;

        for (int i = 0; i < boardCells.Count; i++)
        {
            if (boardCells[i] != null && boardCells[i].TypeItem == newCell.TypeItem)
            {
                lastSameTypeIndex = i;
            }
        }

        return lastSameTypeIndex != -1 ? lastSameTypeIndex + 1 : boardCells.Count;
    }

    private IEnumerator ShiftCellsRight(int startIndex)
    {
        if (boardCells.Count >= MAX_ROW) yield break;

        boardCells.Add(null);
        for (int i = boardCells.Count - 1; i >= startIndex + 1; i--)
        {
            BoardCellMovement bc = boardCells[i - 1].BoardCellMovement;
            yield return StartCoroutine(bc.MovementToPos(cellPlays[i].Pos));
            cellPlays[i].IsContaining = true;
            cellPlays[i - 1].IsContaining = false;
            boardCells[i] = boardCells[i - 1];
        }
    }

    public bool Check3Item()
    {
        foreach (var i in countCellType)
        {
            if (i.Value >= 3)
            {
                isMatch3 = true;
                return true;

            }
        }
        isMatch3 = false;
        return false;
    }



    private IEnumerator MoveToCell(Container container, List<Vector3> path)
    {
        if (listPos.Count > 0)
        {
            int index = listPos.Dequeue();
            if (listCell.Count > 0)
            {
                BoardCell boardCell = listCell.Dequeue();
                if (boardCell == null) yield break;
                Debug.Log("okok move" + index + boardCell.TypeItem);

                boardCell.Pos = cellPlays[index].Pos;
                yield return StartCoroutine(LevelManager.Instance.BoardCtrl.MoveToPosAction(cellPlays[index].Pos));
                // lưu lại các dữ liệu phục vụ cho Undo
                boardCell.BoardCellAnimation.SetIdle();
                lastMove = (boardCell, container, path);
                containerLastMove = cellPlays[index];
                undoQueue.Clear();
                for (int i = 0; i < boardCells.Count; i++)
                {
                    if (boardCells[i].TypeItem == boardCell.TypeItem && boardCells[i] != boardCell)
                    {
                        undoQueue.Enqueue(new KeyValuePair<BoardCell, Container>(boardCells[i], cellPlays[i]));
                    }
                }

                // check match 3
                if (Check3Item())
                    yield return StartCoroutine(CheckMatch3());

                if (boardCells.Count == MAX_ROW)
                {
                    GameManager.Instance.LoseGame();
                    yield break;
                }
            }
        }
    }

    // Đổi tên và thay đổi kiểu trả về thành Tween
    // Đổi từ IEnumerator sang Tween
    private Tween JumpCellTween(BoardCell boardCell)
    {
        // Tạo một Sequence: Lên, sau đó Xuống về y ban đầu
        Vector3 startPos = boardCell.transform.position;
        return DOTween.Sequence()
            .Append(boardCell.transform.DOMoveY(startPos.y + 0.05f, 0.15f).SetEase(Ease.OutQuad));
        // .Append(boardCell.transform.DOMoveY(startPos.y, 0.15f).SetEase(Ease.InQuad));
    }

    private IEnumerator CheckMatch3()
    {
        List<TypeItem> matchedTypes = new List<TypeItem>();
        List<Tween> jumpTweens = new List<Tween>(); // Thay thế List<IEnumerator>

        // 1. Thu thập các loại cần ghép và Tweens nhảy (Giới hạn 3 ô)
        foreach (var x in countCellType)
        {
            if (x.Value >= 3)
            {
                matchedTypes.Add(x.Key);

                int count = 0;
                foreach (var cell in boardCells)
                {
                    if (cell != null && cell.TypeItem == x.Key)
                    {
                        jumpTweens.Add(JumpCellTween(cell));
                        count++;
                        if (count >= 3) break; // Giới hạn chỉ 3 ô cho hoạt ảnh nhảy
                    }
                }
            }
        }

        // 2. Chạy tất cả các hoạt ảnh nhảy cùng lúc và đợi chúng hoàn thành
        if (jumpTweens.Count > 0)
        {
            DG.Tweening.Sequence sequence = DOTween.Sequence();

            // Thêm Tween đầu tiên bằng Append()
            sequence.Append(jumpTweens[0]);

            // Thêm các Tween còn lại bằng Join() để chạy song song
            for (int i = 1; i < jumpTweens.Count; i++)
            {
                sequence.Join(jumpTweens[i]);
            }

            // Đợi toàn bộ Sequence chạy xong
            yield return sequence.WaitForCompletion();
        }

        // 3. Xử lý logic ghép 3
        foreach (var type in matchedTypes)
            yield return StartCoroutine(CombineCell(type));
    }

    private IEnumerator CombineCell(TypeItem type)
    {
        List<BoardCell> sameTypeCells = new List<BoardCell>();
        List<int> indexs = new List<int>();

        // Lấy 3 ô ĐẦU TIÊN cùng loại (Giới hạn 3 ô)
        for (int i = 0; i < boardCells.Count; i++)
        {
            if (boardCells[i] != null && boardCells[i].TypeItem == type)
            {
                sameTypeCells.Add(boardCells[i]);
                indexs.Add(i);

                if (sameTypeCells.Count >= 3) break;
            }
        }

        if (sameTypeCells.Count < 3) yield break;

        // Tính vị trí trung tâm để di chuyển đến
        // Vị trí trung tâm (midIndex) là chỉ mục của ô thứ hai trong nhóm 3 ô (theo thứ tự xuất hiện)
        int midIndex = indexs[1]; // Do indexs đã được thu thập theo thứ tự, index thứ 1 là ở giữa
        Vector3 targetPos = cellPlays[midIndex].Pos;

        List<Tween> moveTweens = new List<Tween>();
        foreach (var cell in sameTypeCells) // sameTypeCells đã giới hạn ở 3
        {
            cell.BoardCellAnimation.SetRaise();
            // Tạo Tween di chuyển
            moveTweens.Add(cell.transform.DOMove(targetPos , 0.15f));
        }

        // Chạy tất cả các hoạt ảnh di chuyển cùng lúc và đợi chúng hoàn thành
        DG.Tweening.Sequence sequence = DOTween.Sequence();

        // Thêm Tween đầu tiên bằng Append()
        sequence.Append(moveTweens[0]);

        // Thêm các Tween còn lại bằng Join() để chạy song song
        for (int i = 1; i < moveTweens.Count; i++)
        {
            sequence.Join(moveTweens[i]);
        }

        yield return new WaitForSeconds(1f);

        // Đợi toàn bộ Sequence chạy xong
        yield return sequence.WaitForCompletion();

        // Xóa 3 ô đã chọn
        yield return StartCoroutine(RemoveItem(sameTypeCells, indexs));
    }

    private IEnumerator RemoveItem(List<BoardCell> sameTypeCells, List<int> indexs)
    {
        foreach (var cell in sameTypeCells)
        {
            if (cell != null)
                Destroy(cell.gameObject);
        }

        indexs.Sort();
        indexs.Reverse();

        foreach (int idx in indexs)
        {
            if (idx >= 0 && idx < boardCells.Count)
            {
                boardCells.RemoveAt(idx);
                cellPlays[idx].IsContaining = false;
            }
        }

        foreach (var cell in sameTypeCells)
        {
            countCellType[cell.TypeItem] -= 1;
        }

        Debug.Log("Đã xóa các cell cùng loại và cập nhật danh sách.");
        yield return StartCoroutine(ShiftCellsLeft());
    }

    private IEnumerator ShiftCellsLeft()
    {
        int indexStartEmpty = -1;

        for (int i = 0; i < cellPlays.Count; i++)
        {
            if (!cellPlays[i].IsContaining)
            {
                indexStartEmpty = i;
                break;
            }
        }

        if (indexStartEmpty == -1) yield break;

        for (int i = indexStartEmpty; i < boardCells.Count; i++)
        {
            BoardCellMovement bc = boardCells[i].BoardCellMovement;
            cellPlays[indexStartEmpty].IsContaining = true;
            yield return StartCoroutine(bc.MovementToPos(cellPlays[indexStartEmpty].Pos));
            indexStartEmpty += 1;
        }

        for (int i = indexStartEmpty; i < cellPlays.Count; i++)
        {
            cellPlays[i].IsContaining = false;
        }

        yield return new WaitForSeconds(0.1f);
        LevelManager.Instance.BoardCtrl.UpdateBoardCell();
    }

    #endregion


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
        int index = boardCells.IndexOf(cell);
        if (index != -1)
        {
            boardCells.RemoveAt(index);
            countCellType[cell.TypeItem] -= 1;
            cellPlays[index].IsContaining = false;
        }

        // 2 Di chuyển ngược lại đường đi
        yield return StartCoroutine(MoveBackward(cell, path));

        // 3 Cập nhật trạng thái cuối cùng
        yield return StartCoroutine(ShiftCellsLeft());
        ResetCellAfterUndo(cell, container);
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

        cell.transform.localRotation *= Quaternion.Euler(0, 180, 0);
        yield return StartCoroutine(movement.MovementToPos(path[^1]));
        animation.SetRunning();

        path.RemoveAt(path.Count - 1);
        for (int i = path.Count - 1; i >= 0; i--)
        {
            yield return StartCoroutine(movement.MovementToPos(path[i]));
        }

        animation.SetIdle();
        cell.transform.localRotation *= Quaternion.Euler(0, 180, 0);
    }

    // Reset trạng thái sau khi Undo thường
    private void ResetCellAfterUndo(BoardCell cell, Container container)
    {
        cell.HasClick = true;
        cell.Container = container;
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
        newCell.BoardCellAnimation = block.GetComponentInChildren<BoardCellAnimation>();
        newCell.BoardCellMovement = block.GetComponentInChildren<BoardCellMovement>();
        newCell.BoardCellAnimation.SetActive();
        newCell.Barrel.SetActive(false);

        int posCell = cellPlays.IndexOf(container);
        yield return StartCoroutine(ShiftCellsRight(posCell));

        boardCells[posCell] = newCell;
        cellPlays[posCell].IsContaining = true;
        countCellType[newCell.TypeItem] += 1;
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
        recreatedCell.Container = container;
        recreatedCell.Pos = container.Pos;
        recreatedCell.BoardCellAnimation = block.GetComponentInChildren<BoardCellAnimation>();
        recreatedCell.BoardCellMovement = block.GetComponentInChildren<BoardCellMovement>();
        recreatedCell.BoardCellAnimation.SetActive();
        recreatedCell.Barrel.SetActive(false);
        recreatedCell.transform.localPosition = containerLastMove.Pos;

        int index = cellPlays.IndexOf(container);
        if (index != -1)
        {
            boardCells[index] = recreatedCell;
            cellPlays[index].IsContaining = true;
            countCellType[recreatedCell.TypeItem] += 1;
        }

        yield return StartCoroutine(MoveBackward(recreatedCell, path));

        recreatedCell.HasClick = true;
        container.IsContaining = true;
    }

    #endregion
}
