using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

public class CellPlayCtrl : MonoBehaviour
{
    private const int MAX_ROW = 7;

    [SerializeField] private List<BoardCell> boardCells;   // bottom-row board cells (left -> right)
    [SerializeField] private List<Container> cellPlays;    // slot containers (left -> right)
    [SerializeField] private GameObject prefabCell;
    [SerializeField] private Transform spawnPoint;

    public string prefabFolder = "Prefabs";

    private void Start()
    {
        if (LevelManager.Instance != null && LevelManager.Instance.BoardCtrl != null)
        {
            LevelManager.Instance.BoardCtrl.ExcuteMoveAction += ExecuteMove;
            LevelManager.Instance.BoardCtrl.TryReserverSlotAction += TryReserveSlot;
        }

        boardCells = new List<BoardCell>();
        cellPlays = new List<Container>();
        GenerateCell();
    }

    private void OnDisable()
    {
        if (LevelManager.Instance != null && LevelManager.Instance.BoardCtrl != null)
        {
            LevelManager.Instance.BoardCtrl.ExcuteMoveAction -= ExecuteMove;
            LevelManager.Instance.BoardCtrl.TryReserverSlotAction += TryReserveSlot;
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

    // === HÀM 1: Chỉ kiểm tra và giữ chỗ (KHÔNG di chuyển) ===
    public int TryReserveSlot(BoardCell boardCell)
    {
        if (cellPlays == null || cellPlays.Count == 0)
        {
            Debug.LogWarning("Không có slot cellPlays.");
            return -1;
        }

        boardCells ??= new List<BoardCell>();

        if (boardCells.Count >= MAX_ROW)
        {
            Debug.Log("Hàng dưới đã đầy.");
            return -1;
        }

        int index = SortTheCell(boardCell);
        boardCells[index] = boardCell;
        if (index < 0 || index >= cellPlays.Count)
        {
            Debug.LogError($"Index chèn không hợp lệ: {index}");
            return -1;
        }

        if (cellPlays[index] != null)
        {
            cellPlays[index].IsContaining = true;
            Debug.Log($"🔒 Đã giữ chỗ container tại index {index}");
        }

        return index;
    }


    // === HÀM 2: Chỉ thực hiện di chuyển thật sự ===
    public void ExecuteMove(BoardCell boardCell, int index)
    {
        if (index < 0 || index >= cellPlays.Count)
        {
            Debug.LogError($"Index không hợp lệ khi ExecuteMove: {index}");
            return;
        }

        Vector3 pos = cellPlays[index].Pos;

        var moveFunc = LevelManager.Instance?.BoardCtrl?.MoveToCellPlayAction;
        if (moveFunc == null)
        {
            Debug.LogError("MoveToCellPlayAction null hoặc chưa được set.");
            return;
        }

        BoardCell result = moveFunc.Invoke(pos);
        if (result == null)
        {
            Debug.LogWarning("MoveToCellPlayAction trả về null (không di chuyển được).");
            cellPlays[index].IsContaining = false; // reset nếu không di chuyển được
            return;
        }

        if (index < boardCells.Count)
            boardCells[index] = result;
        else
            boardCells.Add(result);

        Debug.Log($"✅ Đã di chuyển BoardCell xuống vị trí index {index}, pos {pos}");
    }


    // === HÀM GỐC: chỉ là orchestrator (gọi 2 hàm trên) ===
    // public void CheckTheContainer(BoardCell boardCell)
    // {
    //     int index = TryReserveSlot(boardCell);
    //     if (index == -1) return;

    //     ExecuteMove(boardCell, index);
    //     CheckMatchAndClear();
    // }


    private int SortTheCell(BoardCell boardCell)
    {
        int lastMatch = -1;

        for (int i = 0; i < boardCells.Count; i++)
        {
            var bc = boardCells[i];
            if (bc == null) break;
            if (bc.TypeItem == boardCell.TypeItem)
                lastMatch = i;
        }

        int insertIndex = (lastMatch == -1) ? boardCells.Count : lastMatch + 1;
        insertIndex = Mathf.Clamp(insertIndex, 0, MAX_ROW - 1);

        ShiftRightFrom(insertIndex);
        return insertIndex;
    }

    private void ShiftRightFrom(int index)
    {
        if (boardCells.Count == 0 || index < 0 || index >= MAX_ROW)
            return;

        if (boardCells.Count >= MAX_ROW)
        {
            Debug.Log("Không thể shift vì đã đầy hàng.");
            return;
        }

        for (int j = boardCells.Count - 1; j >= index; j--)
        {
            var bc = boardCells[j];
            if (bc == null) continue;

            int target = j + 1;
            if (target < cellPlays.Count)
            {
                bc.BoardCellMovement?.MovementToPos(cellPlays[target].Pos);
                cellPlays[target].IsContaining = true;
                if (cellPlays[j] != null)
                    cellPlays[j].IsContaining = false;
            }
        }

        boardCells.Insert(index, null);
        if (boardCells.Count > MAX_ROW)
            boardCells.RemoveAt(boardCells.Count - 1);
    }


    // Kiểm tra và xử lý khi có 3 quân liên tiếp giống nhau
    private void CheckMatchAndClear()
    {
        if (boardCells == null || boardCells.Count < 3)
            return;

        int count = 1;
        int startIndex = 0;

        for (int i = 1; i < boardCells.Count; i++)
        {
            if (boardCells[i] != null && boardCells[i - 1] != null &&
                boardCells[i].TypeItem == boardCells[i - 1].TypeItem)
            {
                count++;

                // Nếu đến cuối danh sách mà vẫn đang chuỗi giống nhau
                if (i == boardCells.Count - 1 && count >= 3)
                {
                    startIndex = i - count + 1;
                    RemoveAndShiftCells(startIndex, count);
                }
            }
            else
            {
                if (count >= 3)
                {
                    startIndex = i - count;
                    RemoveAndShiftCells(startIndex, count);
                }
                count = 1;
            }
        }
    }

    private void RemoveAndShiftCells(int start, int count)
    {
        Debug.Log($"🔥 Phát hiện {count} quân liên tục giống nhau từ vị trí {start}, đang xóa...");

        for (int i = start; i < start + count && i < boardCells.Count; i++)
        {
            var bc = boardCells[i];
            if (bc != null)
            {
                // Hiệu ứng nổ (nếu có)
                //bc.Explode(); // hoặc bạn có thể gọi Tween fade-out
                Destroy(bc.gameObject, 0.15f);
            }

            // Reset container
            if (i < cellPlays.Count && cellPlays[i] != null)
                cellPlays[i].IsContaining = false;
        }

        // Xóa khỏi danh sách logic
        boardCells.RemoveRange(start, count);

        // Dịch sang trái cho khít
        ShiftLeftAndReorder();
    }


    private void ShiftLeftAndReorder()
    {
        for (int i = 0; i < boardCells.Count; i++)
        {
            var bc = boardCells[i];
            if (bc == null) continue;

            // Di chuyển về đúng vị trí container tương ứng
            Vector3 targetPos = cellPlays[i].Pos;
            bc.BoardCellMovement.MovementToPos(targetPos);

            // Cập nhật container
            cellPlays[i].IsContaining = true;
        }

        // Cập nhật lại các container còn trống
        for (int i = boardCells.Count; i < cellPlays.Count; i++)
        {
            cellPlays[i].IsContaining = false;
        }

        Debug.Log("✅ Đã dịch trái các ô còn lại sau khi xóa.");
    }


    public void CombineCell() { }
    public void Explode() { }
}
