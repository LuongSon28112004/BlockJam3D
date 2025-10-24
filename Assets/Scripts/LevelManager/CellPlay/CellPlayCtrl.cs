using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
    [SerializeField] private Dictionary<TypeItem, List<BoardCell>> countCellType;


    public string prefabFolder = "Prefabs";

    Queue<int> posCellPlays;

    public List<BoardCell> BoardCells { get => boardCells; set => boardCells = value; }
    public List<Container> CellPlays { get => cellPlays; set => cellPlays = value; }
    public Dictionary<TypeItem, List<BoardCell>> CountCellType { get => countCellType; set => countCellType = value; }

    private void OnEnable()
    {
        CustomeEventSystem.Instance.CheckMatch_3_Action += Match_3;
    }

    private void OnDisable()
    {
        CustomeEventSystem.Instance.CheckMatch_3_Action -= Match_3;
    }

    private void Start()
    {
        

        boardCells = new List<BoardCell>();
        cellPlays = new List<Container>();
        posCellPlays = new Queue<int>();
        InitCountCellType();
        GenerateCell();
    }

    private void InitCountCellType()
    {
        countCellType = new Dictionary<TypeItem, List<BoardCell>>()
        {
            { TypeItem.BlueBase, new List<BoardCell>() },
            { TypeItem.BrownBase, new List<BoardCell>() },
            { TypeItem.GreenBase, new List<BoardCell>() },
            { TypeItem.MagentaBase, new List<BoardCell>() }
        };
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
            return;
        }
        if (boardCell == null)
        {
            Debug.Log("Board Cell is Null not add List BoardCells");
            return;
        }

        int insertIndex = FindInsertIndex(boardCell);

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
        posCellPlays.Enqueue(insertIndex);

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
            StartCoroutine(bc.MovementToPos(cellPlays[i].Pos));
            cellPlays[i].IsContaining = true;
            cellPlays[i - 1].IsContaining = false;
            boardCells[i] = boardCells[i - 1];
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
            GameManager.Instance.LoseGame();
        }
    }


    public void checkWin()
    {

        if(boardCells.Count == 0 && LevelManager.Instance.Round == 3)
        {
            GameManager.Instance.WinGame();
        }
    }
    
    public void Match_3()
    {
        StartCoroutine(Match3Process());
    }

    private IEnumerator Match3Process()
    {
        foreach (var kvp in countCellType)
        {
            TypeItem type = kvp.Key;
            List<BoardCell> list = kvp.Value;

            if (list.Count < 3)
                continue;
            // for (int i = 0; i <= list.Count - 3; i++)
            // {
                BoardCell c1 = list[0];
                BoardCell c2 = list[1];
                BoardCell c3 = list[2];

                if (c1 == null || c2 == null || c3 == null)
                    continue;
                RemoveCellData(new List<BoardCell> { c1, c2, c3 }, type);
                StartCoroutine(RearrangeCellsAfterRemove());

                StartCoroutine(SetAnimMerge(new List<BoardCell> { c1, c2, c3 }));
            //}
        }
        yield break;
    }

    private IEnumerator SetAnimMerge(List<BoardCell> boardCells)
    {
        // Chạy animation raise + merge
        boardCells[0].BoardCellAnimation.SetRaise();
        boardCells[1].BoardCellAnimation.SetRaise();
        boardCells[2].BoardCellAnimation.SetRaise();
        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(MergeToCenter(boardCells[0], boardCells[1], boardCells[2]));
        boardCells[0].BoardCellAnimation.SetPop();
        boardCells[1].BoardCellAnimation.SetPop();
        boardCells[2].BoardCellAnimation.SetPop();
        yield return new WaitForSeconds(0.1f);

        // Xóa gameobject sau khi animation xong
        Destroy(boardCells[0].gameObject);
        Destroy(boardCells[1].gameObject);
        Destroy(boardCells[2].gameObject);
    }

    private IEnumerator MergeToCenter(BoardCell c1, BoardCell c2, BoardCell c3)
    {
        Vector3 centerPos = c2.transform.position;
        float mergeTime = 0.3f;

        // Chụm vào giữa
        var t1 = c1.transform.DOMove(centerPos, mergeTime).SetEase(Ease.InOutQuad);
        var t2 = c2.transform.DOMove(centerPos, mergeTime).SetEase(Ease.InOutQuad);
        var t3 = c3.transform.DOMove(centerPos, mergeTime).SetEase(Ease.InOutQuad);

        yield return new WaitForSeconds(mergeTime);

        // Hiệu ứng scale nhẹ khi tụ lại
        DG.Tweening.Sequence seq = DOTween.Sequence();
        seq.Join(c1.transform.DOScale(1.2f, 0.15f));
        seq.Join(c2.transform.DOScale(1.2f, 0.15f));
        seq.Join(c3.transform.DOScale(1.2f, 0.15f));
        seq.AppendInterval(0.1f);
        seq.Join(c1.transform.DOScale(1f, 0.15f));
        seq.Join(c2.transform.DOScale(1f, 0.15f));
        seq.Join(c3.transform.DOScale(1f, 0.15f));

        yield return seq.WaitForCompletion();
        //yield return new WaitForSeconds(0.4f);
        if (LevelManager.Instance.BoardCtrl.BoardCells.Count == 0)
        {
            StartCoroutine(LevelManager.Instance.NextRound.Invoke());
        }
        checkWin();
    }



    private void RemoveCellData(List<BoardCell> cells, TypeItem type)
    {
        foreach (var cell in cells)
        {
            if (cell == null) continue;

            // Xóa khỏi danh sách logic
            boardCells.Remove(cell);
            LevelManager.Instance.BoardCtrl.UpdateBoardCell(cell);
            countCellType[type].Remove(cell);

            // Đánh dấu container rỗng
            if (cell.Container != null)
                cell.Container.IsContaining = false;
        }
    }


    public IEnumerator RearrangeCellsAfterRemove()
    {
        // Tạo một Sequence
        DG.Tweening.Sequence sc = DOTween.Sequence();

        for (int i = 0; i < boardCells.Count; i++)
        {
            var cell = boardCells[i];
            if (cell == null) continue;

            Vector3 targetPos = cellPlays[i].Pos;
            cell.Container = cellPlays[i];
            cellPlays[i].IsContaining = true;

            // Gộp tất cả tween để chạy song song
            sc.Join(cell.BoardCellMovement.MovementToPosTween(targetPos));
        }

        // Bắt đầu chạy sequence và đợi hoàn thành
        sc.Play();
        yield return sc.WaitForCompletion();
        for (int i = 0; i < boardCells.Count; i++)
        {
            var cell = boardCells[i];
            if (cell == null) continue;

            // Dùng DOTween để xoay mượt mà
            cell.transform.DORotate(Vector3.zero, 0.25f)
                .SetEase(Ease.OutBack);
        }
    }


    public bool HasMatch3()
    {
        foreach (var kvp in countCellType)
        {
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