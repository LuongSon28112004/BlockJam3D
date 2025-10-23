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
    [Header("Cell Play Components")]
    [SerializeField] private List<BoardCell> boardCells;   // danh sách các ô hiện có (theo thứ tự trái -> phải)
    [SerializeField] private List<Container> cellPlays;    // danh sách container (vị trí)
    [SerializeField] private GameObject prefabCell;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Dictionary<TypeItem, int> countCellType;


    // luu lai cac trang thai phong chong click lien tuc
    private Queue<int> listPos;
    private Queue<BoardCell> listCell;
    public string prefabFolder = "Prefabs";

    public List<BoardCell> BoardCells { get => boardCells; set => boardCells = value; }
    public List<Container> CellPlays { get => cellPlays; set => cellPlays = value; }
    public Dictionary<TypeItem, int> CountCellType { get => countCellType; set => countCellType = value; }

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
    private IEnumerator CheckAndSavePos(BoardCell newCell, Action<int> onComplete)
    {
        if (boardCells.Count == MAX_ROW)
        {
            onComplete?.Invoke(-1);
            yield break;
        }

        int insertIndex = FindInsertIndex(newCell);
        if (insertIndex >= MAX_ROW)
        {
            Debug.LogWarning("Không thể thêm ô mới - đã đạt giới hạn tối đa!");
            onComplete?.Invoke(-1);
            yield break;
        }

        if(!newCell.IsBoosterAdd)
        {
             listPos.Enqueue(insertIndex);
            listCell.Enqueue(newCell);
        }

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

        // Gọi callback khi hoàn tất
        onComplete?.Invoke(insertIndex);
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

    public IEnumerator ShiftCellsRight(int startIndex)
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
                LevelManager.Instance.boosterCtrl.IsMatch3 = true;
                return true;

            }
        }
        LevelManager.Instance.boosterCtrl.IsMatch3 = false;
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
                LevelManager.Instance.boosterCtrl.LastMove = (boardCell, container, path);
                LevelManager.Instance.boosterCtrl.ContainerLastMove = cellPlays[index];
                LevelManager.Instance.boosterCtrl.UndoQueue.Clear();
                for (int i = 0; i < boardCells.Count; i++)
                {
                    if (boardCells[i].TypeItem == boardCell.TypeItem && boardCells[i] != boardCell)
                    {
                        LevelManager.Instance.boosterCtrl.UndoQueue.Enqueue(new KeyValuePair<BoardCell, Container>(boardCells[i], cellPlays[i]));
                    }
                }


                yield return StartCoroutine(CheckMatch_3Item());
                StartCoroutine(CheckStateGame());

            }
        }
    }

    public IEnumerator CheckMatch_3Item()
    {
        // check match 3
        if (Check3Item())
            //StartCoroutine(CheckMatch3());
            yield return StartCoroutine(CheckMatch3());
    }

    public IEnumerator CheckStateGame()
    {
        if (boardCells.Count == MAX_ROW)
        {
            GameManager.Instance.LoseGame();
            yield break;
        }

        if(boardCells.Count == 0 && LevelManager.Instance.Round == 3)
        {
            GameManager.Instance.WinGame();
            yield break;
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

        yield return new WaitForSeconds(1.25f);

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

    public IEnumerator ShiftCellsLeft()
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

        List<Tween> moveTweens = new List<Tween>();

    for (int i = indexStartEmpty; i < boardCells.Count; i++)
    {
        var cell = boardCells[i];
        BoardCellMovement bc = cell.BoardCellMovement;
        BoardCellAnimation boardCellAnimation = cell.BoardCellAnimation;
        cellPlays[indexStartEmpty].IsContaining = true;

        // Lưu rotation ban đầu (local)
        Quaternion startLocalRot = cell.transform.localRotation;

        // Tính direction và rotation hướng tới pos (dùng world direction rồi chuyển sang local nếu cần)
        Vector3 targetPos = cellPlays[indexStartEmpty].Pos;
        Vector3 dir = (targetPos - cell.transform.position).normalized;
        Quaternion lookRotation = Quaternion.identity;
        if (dir != Vector3.zero)
        {
            lookRotation = Quaternion.LookRotation(dir); // world rotation hướng tới pos
            // Nếu bạn muốn xoay theo local (VD: parent khác orientation), thử chuyển sang localEuler:
            // Vector3 lookEuler = lookRotation.eulerAngles;
            // hoặc dùng DOLocalRotate nếu cần euler local
        }

        // Tạo tween di chuyển (giả sử MovementToPosTween trả Tween và bắt đầu khi tạo)
        Tween moveTween = bc.MovementToPosTween(targetPos);

        // Capture local variables để tránh closure issue
        var capturedCell = cell;
        var capturedAnim = boardCellAnimation;
        var capturedStartRot = startLocalRot;
        var capturedLookRot = lookRotation;

        // Khi tween bắt đầu: bật animation chạy + xoay về hướng di chuyển (tween xoay ngắn 0.15s)
        moveTween.OnStart(() =>
        {
            capturedAnim.SetRunning();
            // Tween xoay mượt về lookRotation (dùng DORotateQuaternion trên transform, hoặc DOLocalRotate nếu bạn dùng local)
            // Mình dùng DORotateQuaternion (world). Nếu muốn local: DOLocalRotateQuaternion(...)
            capturedCell.transform.DORotateQuaternion(capturedLookRot, 0.15f).SetUpdate(true);
        });

        // Khi tween hoàn tất: đặt lại animation idle + quay về rotation ban đầu (0.2s)
        moveTween.OnComplete(() =>
        {
            capturedAnim.SetIdle();
            capturedCell.transform.DORotateQuaternion(capturedStartRot, 0.2f).SetUpdate(true);
        });

        moveTweens.Add(moveTween);
        indexStartEmpty += 1;
        }

        // Tạo sequence và join tất cả tween (đảm bảo include tween 0)
        DG.Tweening.Sequence sequence = DOTween.Sequence();
        if (moveTweens.Count > 0)
        {
            sequence.Append(moveTweens[0]);
            for (int i = 1; i < moveTweens.Count; i++)
            {
                sequence.Join(moveTweens[i]);
            }
        }

        // Đợi toàn bộ Sequence chạy xong
        yield return sequence.WaitForCompletion();

        // Reset các cellPlays còn lại
        for (int i = indexStartEmpty; i < cellPlays.Count; i++)
        {
            cellPlays[i].IsContaining = false;
        }

        yield return new WaitForSeconds(0.1f);
        LevelManager.Instance.BoardCtrl.UpdateBoardCell();

    }

    #endregion


    
}