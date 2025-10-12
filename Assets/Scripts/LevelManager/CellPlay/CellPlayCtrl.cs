using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class CellPlayCtrl : MonoBehaviour
{
    private const int MAX_ROW = 7;

    [SerializeField] private List<BoardCell> boardCells;   // danh sách các ô hiện có (theo thứ tự trái -> phải)
    [SerializeField] private List<Container> cellPlays;    // danh sách container (vị trí)
    [SerializeField] private GameObject prefabCell;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Dictionary<TypeItem, int> countCellType;

    Queue<int> listPos;

    public string prefabFolder = "Prefabs";

    private void Start()
    {
        if (LevelManager.Instance != null && LevelManager.Instance.BoardCtrl != null)
        {
            LevelManager.Instance.BoardCtrl.checkAndSavePosAction += checkAndSavePos;
            LevelManager.Instance.BoardCtrl.MoveToCellPlay += MoveToCell;
        }

        boardCells = new List<BoardCell>();
        cellPlays = new List<Container>();
        listPos = new Queue<int>();
        this.initCountCellType();
        GenerateCell();
    }

    private void initCountCellType()
    {
        countCellType = new Dictionary<TypeItem, int>();
        countCellType.Add(TypeItem.BlueBase, 0);
        countCellType.Add(TypeItem.BrownBase, 0);
        countCellType.Add(TypeItem.GreenBase, 0);

    }

    private void OnDisable()
    {
        if (LevelManager.Instance != null && LevelManager.Instance.BoardCtrl != null)
        {
            LevelManager.Instance.BoardCtrl.checkAndSavePosAction -= checkAndSavePos;
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

    public void checkAndSavePos(BoardCell newCell)
    {
        if (boardCells.Count == MAX_ROW) return;
        // Xác định vị trí cần chèn
        int insertIndex = FindInsertIndex(newCell);

        // Kiểm tra nếu vượt quá giới hạn
        if (insertIndex >= MAX_ROW)
        {
            Debug.LogWarning("Không thể thêm ô mới - đã đạt giới hạn tối đa!");
            return;
        }

        // Lưu vị trí chèn vào queue
        listPos.Enqueue(insertIndex);

        // Dịch các ô phía sau sang phải (nếu cần)
        if (insertIndex < boardCells.Count)
        {
            ShiftCellsRight(insertIndex);
        }

        // Thêm ô mới vào đúng vị trí
        if (insertIndex == boardCells.Count)
        {
            boardCells.Add(newCell);
            //tắt click
            newCell.HasClick = false;
            countCellType[newCell.TypeItem] += 1;
        }
        else
        {
            countCellType[newCell.TypeItem] += 1;
            // lưu vị trí boardCell cần trèn là ô insertIndex luôn 
            boardCells[insertIndex] = newCell;
            // tắt click
            newCell.HasClick = false;
        }

        // Cập nhật container
        cellPlays[insertIndex].IsContaining = true;
    }

    /// <summary>
    /// Tìm vị trí chèn phù hợp cho ô mới.
    /// </summary>
    private int FindInsertIndex(BoardCell newCell)
    {
        int lastSameTypeIndex = -1;

        // Tìm vị trí cuối cùng của ô cùng loại
        for (int i = 0; i < boardCells.Count; i++)
        {
            if (boardCells[i] != null && boardCells[i].TypeItem == newCell.TypeItem)
            {
                lastSameTypeIndex = i;
            }
        }

        // Nếu có ô cùng loại -> chèn sau ô cùng loại cuối cùng
        if (lastSameTypeIndex != -1)
            return lastSameTypeIndex + 1;

        // Nếu không có ô cùng loại -> chèn vào cuối danh sách
        return boardCells.Count;
    }

    /// <summary>
    /// Dịch các ô sau vị trí chèn sang phải 1 slot.
    /// </summary>
    private void ShiftCellsRight(int startIndex)
    {
        if (boardCells.Count >= MAX_ROW) return;
        boardCells.Add(null);
        for (int i = boardCells.Count - 1; i >= startIndex + 1; i--)
        {
            BoardCellMovement bc = boardCells[i - 1].BoardCellMovement;
            _ = bc.MovementToPos(cellPlays[i].Pos);
            cellPlays[i].IsContaining = true;
            cellPlays[i - 1].IsContaining = false;
            boardCells[i] = boardCells[i - 1];
        }
    }
    
     public bool check_3Item()
    {
        foreach (var i in countCellType)
        {
            if (i.Value >= 3) return true;
        }
        return false;
    }

    public async Task MoveToCell()
    {
        if (listPos.Count > 0)
        {
            int index = listPos.Dequeue();

            // Thực hiện di chuyển đến vị trí
            await LevelManager.Instance.BoardCtrl.MoveToPosAction.Invoke(cellPlays[index].Pos);

            // Kiểm tra match-3 sau khi di chuyển xong
            if (check_3Item())
            {
                await checkMatch_3(); // chờ animation hoàn tất
            }
        }
    }
    
    /// <summary>
    /// Animation nhảy lên cho cell.
    /// </summary>
    public async Task JumpCell(BoardCell boardCell)
    {
        // Nhảy lên rồi dừng ở vị trí mới (không quay lại)
        await boardCell.transform.DOMoveY(boardCell.transform.position.y + 3f, 0.15f)
            .SetEase(Ease.OutQuad) // hiệu ứng nhảy mượt
            .AsyncWaitForCompletion();
    }


    /// <summary>
    /// Kiểm tra và xử lý các ô trùng loại >= 3.
    /// </summary>
    public async Task checkMatch_3()
    {
        List<Task> jumpTasks = new List<Task>();
        List<TypeItem> matchedTypes = new List<TypeItem>();

        // Tìm loại vật phẩm có >= 3 ô
        foreach (var x in countCellType)
        {
            if (x.Value >= 3)
            {
                matchedTypes.Add(x.Key);

                for (int i = 0; i < boardCells.Count; i++)
                {
                    if (boardCells[i] != null && boardCells[i].TypeItem == x.Key)
                    {
                        jumpTasks.Add(JumpCell(boardCells[i]));
                    }
                }
            }
        }

        // Đợi tất cả nhảy xong
        await Task.WhenAll(jumpTasks);

        // // Gộp các ô đã match
        // await Task.Delay(100);
        foreach (var type in matchedTypes)
        {
            await CombineCell(type);
        }
    }

    // merge và xóa các ô 
    public async Task CombineCell(TypeItem type)
    {
        // Lấy danh sách 3 cell cùng loại
        List<BoardCell> sameTypeCells = new List<BoardCell>();
        List<int> indexs = new List<int>();
        for (int i = 0; i < boardCells.Count; i++)
        {
            if (boardCells[i] != null && boardCells[i].TypeItem == type)
            {
                sameTypeCells.Add(boardCells[i]);
                indexs.Add(i);
            }
        }

        if (sameTypeCells.Count < 3) return;

        // Xác định vị trí trung tâm (ô thứ 2)
        int firstIndex = boardCells.IndexOf(sameTypeCells[0]);
        int lastIndex = boardCells.IndexOf(sameTypeCells[2]);
        int midIndex = (firstIndex + lastIndex) / 2;
        Vector3 targetPos = cellPlays[midIndex].Pos;

        // Gộp 3 cell về giữa (animation)
        List<Task> moveTasks = new List<Task>();
        foreach (var cell in sameTypeCells)
        {
            moveTasks.Add(cell.transform.DOMove(targetPos, 0.25f).AsyncWaitForCompletion());
        }
        await Task.WhenAll(moveTasks);

        await RemoveItem(sameTypeCells,indexs);

    }

    public async Task RemoveItem(List<BoardCell> sameTypeCells, List<int> indexs)
    {
        // Xóa animation hoặc object thực tế
        foreach (var cell in sameTypeCells)
        {
            if (cell != null)
            {
                Destroy(cell.gameObject);
            }
        }

        // Xóa khỏi danh sách boardCells theo thứ tự giảm dần
        indexs.Sort();
        indexs.Reverse(); // từ lớn đến nhỏ

        foreach (int idx in indexs)
        {
            if (idx >= 0 && idx < boardCells.Count)
            {
                boardCells.RemoveAt(idx);
                cellPlays[idx].IsContaining = false;
            }
        }

        // Cập nhật lại biến đếm
        foreach (var cell in sameTypeCells)
        {
            countCellType[cell.TypeItem] -= 1;
        }

        Debug.Log("Đã xóa các cell cùng loại và cập nhật danh sách.");
        await ShiftCellsLeft();
    }
    
     private async Task ShiftCellsLeft()
    {
        int indexStartEmpty = 0;
        for (int i = 0; i < cellPlays.Count; i++)
        {
            if (!cellPlays[i].IsContaining)
            {
                indexStartEmpty = i;
                break;
            }

        }

        for (int i = indexStartEmpty; i < boardCells.Count; i++)
        {
            BoardCellMovement bc = boardCells[i].BoardCellMovement;
            cellPlays[indexStartEmpty].IsContaining = true;
            await bc.MovementToPos(cellPlays[indexStartEmpty].Pos);
            indexStartEmpty += 1;
        }

        for (int i = indexStartEmpty; i < cellPlays.Count; i++)
        {
            cellPlays[i].IsContaining = false;
        }
        //update lại các ô trong BoarlCellCtrl
        await Task.Delay(100);
        LevelManager.Instance.BoardCtrl.UpdateBoardCell();
    }
}