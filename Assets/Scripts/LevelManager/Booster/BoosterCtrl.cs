using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
public class BoosterCtrl : MonoBehaviour
{
    [Header("Booster Component")]
    [SerializeField] private Transform parentBoard;
    [Header("Undo Booster")]
    [SerializeField] private Stack<bool> isMatch3s;
    [SerializeField] private Stack<Queue<KeyValuePair<BoardCell, Container>>> undoQueue;
    [SerializeField] private Stack<(BoardCell cell, Container container, List<Vector3> path)> lastMove;
    [Header("Add Booster")]
    [SerializeField] private BoosterAddPos boosterAddPos;
    [SerializeField] private BoosterMagnetPos boosterMagnetPos;

    public Stack<Queue<KeyValuePair<BoardCell, Container>>> UndoQueue { get => undoQueue; set => undoQueue = value; }
    public Stack<(BoardCell cell, Container container, List<Vector3> path)> LastMove { get => lastMove; set => lastMove = value; }
    public BoosterAddPos BoosterAddPos { get => boosterAddPos; set => boosterAddPos = value; }
    public Stack<bool> IsMatch3s { get => isMatch3s; set => isMatch3s = value; }

    [Header("Busy")]
    public bool IsBusy = false;

    void Start()
    {
        lastMove = new Stack<(BoardCell cell, Container container, List<Vector3> path)>();
        undoQueue = new Stack<Queue<KeyValuePair<BoardCell, Container>>>();
        countDict = new Dictionary<TypeItem, int>();
        isMatch3s = new Stack<bool>();
    }

    #region Undo

    public void ResetStackUndo()
    {
        lastMove.Clear();
        undoQueue.Clear();
        isMatch3s.Clear();
    }
    public IEnumerator Undo()
    {
        bool isMatch3 = isMatch3s.Pop();
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
        int isUndo = LevelManager.Instance.boosterCtrl.IsMatch3s.Count == 0 ? -1 : 1;
        CustomeEventSystem.Instance.ActiveBooster(new List<int> { isUndo, (LevelManager.Instance.cellPlayCtrl.BoardCells.Count == 0 ? -1 : 1), 1, 1 });
        LevelManager.Instance.BoardCtrl.itemClickCtrl.isStart = false;
        yield return new WaitForSeconds(1f);
        IsBusy = false;
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
        boosterAddPos.AddBoardCell(cell, container);
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
            AudioManager.Instance.PlayOneShot("BLJ_Boosters_Undo_01", 1f);
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

            //Audio sound
            AudioManager.Instance.PlayOneShot("BLJ_Boosters_Undo_01", 1f);

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

        //Audio sound
        AudioManager.Instance.PlayOneShot("BLJ_Boosters_Undo_01", 1f);

        // 3 Reset lại trạng thái
        // isMatch3 = false;
        //undoQueue.Clear();
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
        animation.SetRunning();
        // đi đến điểm cuối cùng trước
        yield return StartCoroutine(movement.MovementToPosNormal(path[^1]));

        // xóa phần tử cuối (đã đến)
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
            boosterAddPos.AddBoardCell(cellData, container);
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
            // StartCoroutine(MoveToCellAdd(cellData, container, path));
        }
    }

    #endregion
    #endregion

    #region Add
    public IEnumerator Add()
    {
        // Play sound
        if (LevelManager.Instance.cellPlayCtrl.BoardCells.Count == 0) yield break;
        IsBusy = true;
        AudioManager.Instance.PlayOneShot("BLJ_Boosters_Continue_01", 1f);
        int pos = 0;
        for (int i = 0; i < boosterAddPos.Containers.Count; i++)
        {
            if (!boosterAddPos.Containers[i].IsContaining)
            {
                pos = i;
                break;
            }
        }
        Sequence sc = DOTween.Sequence();
        for (int i = LevelManager.Instance.cellPlayCtrl.BoardCells.Count - 1; i >= Mathf.Max(0, LevelManager.Instance.cellPlayCtrl.BoardCells.Count - 3); i--)
        {
            BoardCellMovement boardCellMovement = LevelManager.Instance.cellPlayCtrl.BoardCells[i].BoardCellMovement;
            BoardCellAnimation boardCellAnimation = LevelManager.Instance.cellPlayCtrl.BoardCells[i].BoardCellAnimation;
            LevelManager.Instance.cellPlayCtrl.CountCellType[LevelManager.Instance.cellPlayCtrl.BoardCells[i].TypeItem].Remove(LevelManager.Instance.cellPlayCtrl.BoardCells[i]);
            if (boardCellMovement == null) yield break;
            if (boardCellAnimation == null) yield break;
            boardCellAnimation.SetRunning();
            boosterAddPos.Containers[pos].IsContaining = true;
            boosterAddPos.BoardCells.Add(LevelManager.Instance.cellPlayCtrl.BoardCells[i]);
            sc.Join(boardCellMovement.MovementToPosTween(boosterAddPos.ListPosBoosterAdd[pos]));
            LevelManager.Instance.cellPlayCtrl.BoardCells[i].IsInCellPlay = false;
            boardCellAnimation.SetIdle();
            // config Boardcell
            LevelManager.Instance.cellPlayCtrl.CellPlays[i].IsContaining = false;
            LevelManager.Instance.cellPlayCtrl.BoardCells[i].HasClick = true;
            LevelManager.Instance.cellPlayCtrl.BoardCells[i].IsBoosterAdd = true;
            // khi đi xuống dưới Add Booster thì gán lại bằng contaner bên dưới và đồng thời chỉnh lại Pos của BoardCell.
            LevelManager.Instance.cellPlayCtrl.BoardCells[i].Container = boosterAddPos.Containers[pos];
            LevelManager.Instance.cellPlayCtrl.BoardCells[i].Pos = boosterAddPos.Containers[pos].Pos;
            pos++;
        }

        var boardCells = LevelManager.Instance.cellPlayCtrl.BoardCells;

        int removeCount = 3;
        int startIndex = Mathf.Max(0, boardCells.Count - removeCount);

        boardCells.RemoveRange(startIndex, boardCells.Count - startIndex);
        //reset không cho Undo nưa
        CustomeEventSystem.Instance.ActiveBooster(new List<int> { -1, (LevelManager.Instance.cellPlayCtrl.BoardCells.Count == 0 ? -1 : 1), 1, 1 });
        LevelManager.Instance.BoardCtrl.itemClickCtrl.isStart = false;
        LevelManager.Instance.boosterCtrl.ResetStackUndo();
        yield return sc.WaitForCompletion();
        IsBusy = false;
    }
    #endregion

    #region Shuffle
    public IEnumerator Shuffle(List<GameObject> leaderBoards)
    {
        if (leaderBoards == null || leaderBoards.Count == 0)
            yield break;

        IsBusy = true;
        CustomeEventSystem.Instance.ActiveBooster(new List<int> { -1, -1, 1, -1 });

        int n = leaderBoards.Count;
        List<Vector3> startPositions = leaderBoards.Select(lb => lb?.transform.position ?? Vector3.zero).ToList();

        //Sequence scKnob = DOTween.Sequence();
        List<int> selectedIndices = new List<int>();
        for (int i = 0; i < n; i++)
        {
            if (int.TryParse(leaderBoards[i].name, out int val) && val >= 1 && val <= 7)
            {
                selectedIndices.Add(i);
                //scKnob.Join(leaderBoards[i].transform.DOMoveY(2f, 1.5f));
            }
        }

        if (selectedIndices.Count < 2)
        {
            IsBusy = false;
            CustomeEventSystem.Instance.ActiveBooster(new List<int> { -1, -1, 1, 1 });
            yield break;
        }

        //yield return scKnob.WaitForCompletion();

        System.Random rand = new System.Random();
        for (int i = selectedIndices.Count - 1; i > 0; i--)
        {
            int j = rand.Next(0, i + 1);
            (selectedIndices[i], selectedIndices[j]) = (selectedIndices[j], selectedIndices[i]);
        }
        List<Tween> tweens = new List<Tween>();
        for (int i = 0; i < selectedIndices.Count - 1; i += 2)
        {
            int a = selectedIndices[i];
            int b = selectedIndices[i + 1];
            tweens.Add(leaderBoards[a].transform.DOMove(startPositions[b], 0.5f));
            tweens.Add(leaderBoards[b].transform.DOMove(startPositions[a], 0.5f));
            (leaderBoards[a], leaderBoards[b]) = (leaderBoards[b], leaderBoards[a]);
        }
        if (selectedIndices.Count % 2 != 0)
        {
            int lastIndex = selectedIndices[selectedIndices.Count - 1];
            tweens.Add(leaderBoards[lastIndex].transform.DOMove(startPositions[lastIndex], 0.5f));
        }

        yield return DOTween.Sequence().AppendInterval(0.5f).WaitForCompletion();

        LevelManager.Instance.BoardCtrl.RebuildGridFromBoardAlls();

        //reset không cho Undo nưa
        CustomeEventSystem.Instance.ActiveBooster(new List<int> { -1, (LevelManager.Instance.cellPlayCtrl.BoardCells.Count == 0 ? -1 : 1), 1, 1 });
        LevelManager.Instance.BoardCtrl.itemClickCtrl.isStart = false;
        LevelManager.Instance.boosterCtrl.ResetStackUndo();

        yield return new WaitForSeconds(1f);
        var cellCount = LevelManager.Instance.cellPlayCtrl.BoardCells.Count;
        CustomeEventSystem.Instance.ActiveBooster(new List<int> { -1, (cellCount == 0 ? -1 : 1), 1, 1 });
        LevelManager.Instance.BoardCtrl.itemClickCtrl.isStart = false;

        IsBusy = false;
    }

    #endregion

    #region Magnet
    Dictionary<TypeItem, int> countDict = new Dictionary<TypeItem, int>();
    public IEnumerator Magnet()
    {
        IsBusy = true;
        CustomeEventSystem.Instance.ActiveBooster(new List<int> { -1, -1, -1, 1 });
        // yield return new WaitForSeconds(0.15f);
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

        if (type == null) yield break;

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
                //xoa data nếu nó đang năm ở bên dưới add
                boosterAddPos.RemoveBoardCell(boardCells[i]);
                allBoardCells.Add(boardCells[i]);
            }
            if (count == maxMagnet) break;
        }

        // Nếu trên board không đủ quân thì phải sinh ra 1 quân từ các máy Pipe random
        while (count < maxMagnet)
        {
            List<GridSpotSpawn> gridSpotSpawns = LevelManager.Instance.BoardCtrl.gridSpotSpawns;
            BoardCell boardCell = null;
            GameObject obj = AddressableManager.Instance.GetPrefab(Enum.GetName(typeof(TypeItem), type));
            for (int i = 0; i < gridSpotSpawns.Count; i++)
            {
                if (gridSpotSpawns[i].CurrentPointSpawn > 0)
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
            // tránh vòng lặp vô tận nếu không spawn được
            if (gridSpotSpawns.All(g => g.CurrentPointSpawn <= 0)) break;
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
            if (ListChoices[i].Barrel.activeSelf)
            {
                yield return StartCoroutine(ListChoices[i].PlayBarrelAnimation());
                AudioManager.Instance.PlayOneShot("BLJ_Game_Obstacles_Barrel_Break_01", 1f);
                ListChoices[i].Barrel.SetActive(false);
            }
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
        yield return new WaitForSeconds(0.35f);

        // Giai đoạn Move
        Sequence seqMove = DOTween.Sequence();
        count = 1;

        for (int i = 0; i < ListChoices.Count; i++)
        {
            if (count > 3) break;
            BoardCellMovement bc = ListChoices[i].BoardCellMovement;
            seqMove.Join(bc.MovementToPosTween(boosterMagnetPos.ListPosBoosterMagnet[3 - count], false, 0.25f));
            count++;
        }

        for (int i = 0; i < boardCellss.Count; i++)
        {
            if (boardCellss[i].TypeItem == type)
            {
                BoardCellMovement bc = boardCellss[i].BoardCellMovement;
                seqMove.Join(bc.MovementToPosTween(boosterMagnetPos.ListPosBoosterMagnet[3 - count], false, 0.25f));
                count++;
            }
        }

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
        //reset không cho Undo nữa
        CustomeEventSystem.Instance.ActiveBooster(new List<int> { -1, (LevelManager.Instance.cellPlayCtrl.BoardCells.Count == 0 ? -1 : 1), 1, 1 });
        LevelManager.Instance.BoardCtrl.itemClickCtrl.isStart = false;
        LevelManager.Instance.boosterCtrl.ResetStackUndo();
        // tắt trạng thái bận
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

        // Tạo dict cục bộ để đếm (không cộng dồn từ lần trước)
        var localCountDict = new Dictionary<TypeItem, int>();

        foreach (var cell in boardCells)
        {
            if (cell == null) continue;
            TypeItem type = cell.TypeItem;
            if (localCountDict.ContainsKey(type))
                localCountDict[type]++;
            else
                localCountDict[type] = 1;
        }

        // cập nhật countDict trường lớp để dùng sau (nếu cần)
        countDict = new Dictionary<TypeItem, int>(localCountDict);

        // Nếu không có ô nào => trả về mặc định
        if (localCountDict.Count == 0)
            return default;

        TypeItem bestType = default;
        int maxCount = -1;
        int earliestOrderIndex = int.MaxValue;

        // Duyệt theo thứ tự orderTypeInCellPlay để ưu tiên loại nào có thứ tự sớm hơn
        for (int i = 0; i < orderTypeInCellPlay.Count; i++)
        {
            var type = orderTypeInCellPlay[i];
            if (!localCountDict.TryGetValue(type, out int count))
                continue; // loại này không còn trên bàn

            if (count > maxCount)
            {
                maxCount = count;
                earliestOrderIndex = i;
                bestType = type;
            }
            else if (count == maxCount && i < earliestOrderIndex)
            {
                earliestOrderIndex = i;
                bestType = type;
            }
        }

        return bestType;
    }

    #endregion
}
