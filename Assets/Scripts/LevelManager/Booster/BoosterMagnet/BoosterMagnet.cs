using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public class BoosterMagnet : MonoBehaviour
{
    #region Magnet
    Dictionary<TypeItem, int> countDict = new Dictionary<TypeItem, int>();
    [SerializeField] private BoosterMagnetPos boosterMagnetPos;

    private void Start()
    {
        countDict = new Dictionary<TypeItem, int>();
    }
    public IEnumerator Magnet()
    {
        BoosterCtrl.Instance.IsBusy = true;
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
                BoosterCtrl.Instance.BoosterAdd.BoosterAddPos.RemoveBoardCell(boardCells[i]);
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
            ListChoices[i].SetActiveNeighBor();
            if (ListChoices[i].Barrel.activeSelf)
            {
                yield return StartCoroutine(ListChoices[i].PlayBarrelAnimation());
                AudioManager.Instance.PlayOneShot("BLJ_Game_Obstacles_Barrel_Break_01", 1f);
                ListChoices[i].Barrel.SetActive(false);
            }
            boardCellAnimation.SetActive();
            seqKnob.Join(bc.Knob());
        }

        // Lấy tất cả các BoardCell cùng loại ở cellplay bên dưới
        List<BoardCell> boardCellss = LevelManager.Instance.cellPlayCtrl.BoardCells;
        for (int i = 0; i < boardCellss.Count; i++)
        {
            if (boardCellss[i].TypeItem == type)
            {
                BoardCellMovement bc = boardCellss[i].BoardCellMovement;
                allBoardCells.Add(boardCellss[i]);
                boardCellss[i].IsMagnetBooster = true;
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
        LevelManager.Instance.boosterCtrl.BoosterUndo.ResetStackUndo();
        // tắt trạng thái bận
        BoosterCtrl.Instance.IsBusy = false;
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
