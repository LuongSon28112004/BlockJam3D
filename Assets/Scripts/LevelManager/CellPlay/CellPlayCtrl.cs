using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DG.Tweening;
using NUnit.Framework;
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
    [SerializeField] private Dictionary<TypeItem, List<BoardCell>> countCellType;

    private bool isCheckWin = false;
    public string prefabFolder = "Prefabs";

    Queue<int> posCellPlays;

    public List<BoardCell> BoardCells { get => boardCells; set => boardCells = value; }
    public List<Container> CellPlays { get => cellPlays; set => cellPlays = value; }
    public Dictionary<TypeItem, List<BoardCell>> CountCellType { get => countCellType; set => countCellType = value; }
    public List<TypeItem> orderPlayInCellPlay;

    private void OnEnable()
    {
        CustomeEventSystem.Instance.CheckMatch_3_Action += Match_3;
    }

    private void OnDisable()
    {
        CustomeEventSystem.Instance.CheckMatch_3_Action -= Match_3;
    }

    public void ResetCellPlay()
    {
        boardCells.Clear();
        for (int i = 0; i < cellPlays.Count; i++)
        {
            cellPlays[i].IsContaining = false;
        }
        InitCountCellType();
    }

    private void Start()
    {


        boardCells = new List<BoardCell>();
        cellPlays = new List<Container>();
        posCellPlays = new Queue<int>();
        orderPlayInCellPlay = new List<TypeItem>();
        InitCountCellType();
        GenerateCell();
    }
    
   private void InitCountCellType()
    {
        countCellType = new Dictionary<TypeItem, List<BoardCell>>();
        foreach (TypeItem t in Enum.GetValues(typeof(TypeItem)))
            countCellType[t] = new List<BoardCell>();
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

    public void CheckAndSaveBoardCell(BoardCell boardCell)
    {
        if (boardCells.Count >= MAX_ROW)
        {
            Debug.Log("Max CellPlay");
        }
        if (boardCell == null)
        {
            Debug.Log("Board Cell is Null not add List BoardCells");
        }

        int insertIndex = FindInsertIndex(boardCell);
        // Debug.Log("yes"+ insertIndex);

        //save data cell
        if (insertIndex < boardCells.Count)
        {
            StartCoroutine(ShiftCellsRight(insertIndex));
        }

        if (insertIndex == boardCells.Count)
        {
            boardCells.Add(boardCell);
            // delete component 
            boardCell.HasClick = false;
            // add count type boardcell
            countCellType[boardCell.TypeItem].Add(boardCell);
        }
        else
        {
            boardCells[insertIndex] = boardCell;
            //delete component
            boardCell.HasClick = false;
            // add count type boardcell
            countCellType[boardCell.TypeItem].Add(boardCell);
        }

        cellPlays[insertIndex].IsContaining = true;
        boardCell.Pos = cellPlays[insertIndex].Pos;
        posCellPlays.Enqueue(insertIndex);
        // lưu danh sách các quân vừa đánh mới nhất từ thấp tới cao
        if (!orderPlayInCellPlay.Contains(boardCell.TypeItem))
        {
            orderPlayInCellPlay.Add(boardCell.TypeItem);
        }
        StartCoroutine(ResetPosCellPlay());
        
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
            cellPlays[i].IsContaining = true;
            cellPlays[i - 1].IsContaining = false;
            boardCells[i] = boardCells[i - 1];
            boardCells[i].Pos = cellPlays[i].Pos;
            boardCells[i].Container = cellPlays[i];
            if (!boardCells[i].IsInCellPlay) continue;
            BoardCellMovement bc = boardCells[i - 1].BoardCellMovement;
            StartCoroutine(bc.MovementToPos(cellPlays[i].Pos));
        }
    }
    


    public Vector3 PosCell()
    {
        if (posCellPlays.Count == 0)
        {
            Debug.Log("Pos cell is null");
            return Vector3.zero;
        }
        return cellPlays[posCellPlays.Dequeue()].Pos;
    }

    public void checkLose()
    {
         if (boardCells.Count == MAX_ROW)
        {
            for(int i = 0; i < boardCells.Count; i++)
            {
                if (!boardCells[i].IsInCellPlay) return;
            }
            GameManager.Instance.LoseGame();
        }
    }


    public IEnumerator checkWin()
    {
        if (isCheckWin) yield break;
        isCheckWin = true;
        if(LevelManager.Instance.BoardCtrl.BoardCells.Count == 0 && LevelManager.Instance.Round >= 2)
        {
            yield return new WaitForSeconds(1f);
            UserData.level += 1;
            SaveDataManager.Save();
            GameManager.Instance.WinGame();
        }
        isCheckWin = false;
    }
    
    public void Match_3(TypeItem typeItem)
    {
        StartCoroutine(Match3Process(typeItem));
    }

    private IEnumerator Match3Process(TypeItem typeItem)
    {
        foreach (var kvp in countCellType)
        {
            TypeItem type = kvp.Key;
            if (type != typeItem) continue;
            List<BoardCell> list = kvp.Value;

            if (list.Count < 3)
                continue;

            // Lặp để xử lý nhóm 3 đầu tiên
            for (int i = 0; i <= list.Count - 3; i++)
            {
                BoardCell c1 = list[0];
                BoardCell c2 = list[1];
                BoardCell c3 = list[2];

                if (c1 == null || c2 == null || c3 == null)
                    continue;

                // Xóa logic trước khi anim
                RemoveCellData(new List<BoardCell> { c1, c2, c3 }, type);

                // Animation merge & pop
                StartCoroutine(SetAnimMerge(new List<BoardCell> { c1, c2, c3 }));

                // Sau khi xóa, sắp xếp lại cell
                yield return new WaitForSeconds(0.2f);
                StartCoroutine(RearrangeCellsAfterRemove());
                StartCoroutine(ResetPosCellPlay());
            }
        }

        yield break;
    }

    public IEnumerator ResetPosCellPlay()
    {
        yield return new WaitForSeconds(0.5f);
        for (int i = 0; i < boardCells.Count; i++)
        {
            if (boardCells[i].IsInCellPlay && boardCells[i].transform.position != boardCells[i].Pos)
            {
                Debug.Log("yesyes" + boardCells[i].TypeItem + " " + boardCells[i].transform.position + " " + boardCells[i].Pos);
                if (boardCells[i].transform.position != boardCells[i].Pos) StartCoroutine(boardCells[i].BoardCellMovement.MovementToPosOwner());
            }
        }
    }
    
    private IEnumerator WaitRotaion(List<BoardCell> cells)
    {
        // Kiểm tra null tránh lỗi
        for(int i = 0; i < cells.Count; i++)
        {
            yield return cells[i].transform.DOLocalRotate(new Vector3(0, 0, 0), 0.1f).SetEase(Ease.InSine);
        }
    }

    public IEnumerator SetAnimMerge(List<BoardCell> cells)
    {
        yield return WaitRotaion(cells);
        cells.RemoveAll(c => c == null || c.gameObject == null);
        if (cells.Count < 3) yield break;

        // Animation nâng lên
        foreach (var c in cells)
        {
            if (c == null || c.gameObject == null) continue;
            c.BoardCellAnimation.SetRaise();
        }
        cells.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
        yield return new WaitForSeconds(0.3f);
        // Gộp vào giữa
        yield return StartCoroutine(MergeToCenter(cells[0], cells[1], cells[2]));

        // Pop hiệu ứng
        foreach (var c in cells)
        {
            if (c == null || c.gameObject == null) continue;
            c.BoardCellAnimation.SetPop();
        }

        yield return new WaitForSeconds(0.1f);

        // Xoá object sau khi pop
        foreach (var c in cells)
        {
            if (c == null) continue;
            Destroy(c.gameObject);
        }
        Handheld.Vibrate();
    }

    private IEnumerator MergeToCenter(BoardCell c1, BoardCell c2, BoardCell c3)
    {
        if (c1 == null || c2 == null || c3 == null) yield break;

        Vector3 centerPos = c2.transform.position;
        float mergeTime = 0.2f;

        var t1 = c1.transform.DOMove(centerPos, mergeTime).SetEase(Ease.InOutQuad);
        var t2 = c2.transform.DOMove(centerPos, mergeTime).SetEase(Ease.InOutQuad);
        var t3 = c3.transform.DOMove(centerPos, mergeTime).SetEase(Ease.InOutQuad);

        yield return new WaitForSeconds(mergeTime);

        // Scale hiệu ứng
        DG.Tweening.Sequence seq = DOTween.Sequence();
        seq.Join(c1.transform.DOScale(1.2f, 0.15f));
        seq.Join(c2.transform.DOScale(1.2f, 0.15f));
        seq.Join(c3.transform.DOScale(1.2f, 0.15f));
        seq.AppendInterval(0.1f);
        seq.Join(c1.transform.DOScale(1f, 0.15f));
        seq.Join(c2.transform.DOScale(1f, 0.15f));
        seq.Join(c3.transform.DOScale(1f, 0.15f));

        yield return seq.WaitForCompletion();
        AudioManager.Instance.PlayOneShot("BLJ_Game_Merge_Default_01", 1f);


        // Kiểm tra win sau khi merge xong
        if (LevelManager.Instance.BoardCtrl.BoardCells.Count == 0)
        {
            StartCoroutine(LevelManager.Instance.NextRound.Invoke());
        }
        //check win
        StartCoroutine(checkWin());

    }




    public void RemoveCellData(List<BoardCell> cells, TypeItem type)
    {
        foreach (var cell in cells)
        {
            if (cell == null || cell.gameObject == null) continue;

            // Xoá khỏi danh sách logic
            boardCells.Remove(cell);

            if (countCellType.ContainsKey(type))
                countCellType[type].Remove(cell);

            // Dọn Container
            if (cell.Container != null)
                cell.Container.IsContaining = false;

            // Xoá khỏi board chính
            LevelManager.Instance.BoardCtrl.UpdateBoardCell(cell);
        }

        bool flag = false;
        foreach (var cell in boardCells)
        {
            if (cell == null || cell.gameObject == null) continue;
            if (cell.TypeItem == type)
            {
                flag = true;
                break;
            }
        }
        if(!flag)
        {
            if(orderPlayInCellPlay.Contains(type))
            {
                orderPlayInCellPlay.Remove(type);
            }
        }
    }


    public IEnumerator RearrangeCellsAfterRemove()
    {
        DG.Tweening.Sequence sc = DOTween.Sequence();

        for (int i = 0; i < boardCells.Count; i++)
        {
            var cell = boardCells[i];
            if (cell == null || cell.gameObject == null) continue;

            Vector3 targetPos = cellPlays[i].Pos;
            cell.Container = cellPlays[i];
            cell.Pos = targetPos;
            cellPlays[i].IsContaining = true;
            if (!boardCells[i].IsInCellPlay) continue;

            // Join tween nếu hợp lệ
            var moveTween = cell.BoardCellMovement?.MovementToPosTween(targetPos);
            if (moveTween != null)
                sc.Join(moveTween);
        }

        sc.Play();
            yield return sc.WaitForCompletion();
        // Khi tất cả đã di chuyển xong → SetIdle cho từng cell
        for (int i = 0; i < boardCells.Count; i++)
        {
            var cell = boardCells[i];
            if (cell == null || cell.gameObject == null) continue;

            cell.BoardCellAnimation.SetIdle();
        }

        // Sau khi sắp xếp xong, đảm bảo các cell chưa bị Destroy
        for (int i = 0; i < boardCells.Count; i++)
        {
            var cell = boardCells[i];
            if (cell == null || cell.gameObject == null) continue;

            // Xoay lại góc ban đầu
            cell.transform.DOLocalRotate(Vector3.zero, 0.25f).SetEase(Ease.OutBack);
        }
    }



    public bool HasMatch3(TypeItem typeItem)
    {
        foreach (var kvp in countCellType)
        {
            if (kvp.Key != typeItem) continue;
            var list = kvp.Value;

            // Bỏ qua nếu chưa đủ 3 cell
            if (list.Count < 3)
                continue;

            // Đếm số lượng cell đã xuống chỗ
            int countInPlay = 0;
            foreach (var cell in list)
            {
                if (cell != null && cell.IsInCellPlay)
                    countInPlay++;
            }

            // Nếu có ít nhất 3 cell cùng loại đã xuống, thì match3
            if (countInPlay >= 3)
                return true;
        }

        return false;
    }
}   