using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class BoosterAdd : MonoBehaviour
{
    [Header("Add Booster")]
    [SerializeField] private BoosterAddPos boosterAddPos;

    public BoosterAddPos BoosterAddPos { get => boosterAddPos; set => boosterAddPos = value; }
    public IEnumerator Add()
    {
        // Play sound
        if (LevelManager.Instance.cellPlayCtrl.BoardCells.Count == 0) yield break;
        BoosterCtrl.Instance.IsBusy = true;
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
        LevelManager.Instance.boosterCtrl.BoosterUndo.ResetStackUndo();
        yield return sc.WaitForCompletion();
        BoosterCtrl.Instance.IsBusy = false;
    }
}
