using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public class BoosterShuffle : MonoBehaviour
{
    #region Shuffle
    public IEnumerator Shuffle(List<GameObject> leaderBoards)
    {
        if (leaderBoards == null || leaderBoards.Count == 0)
            yield break;

        BoosterCtrl.Instance.IsBusy = true;
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
            BoosterCtrl.Instance.IsBusy = false;
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
        LevelManager.Instance.boosterCtrl.BoosterUndo.ResetStackUndo();

        yield return new WaitForSeconds(1f);
        var cellCount = LevelManager.Instance.cellPlayCtrl.BoardCells.Count;
        CustomeEventSystem.Instance.ActiveBooster(new List<int> { -1, (cellCount == 0 ? -1 : 1), 1, 1 });
        LevelManager.Instance.BoardCtrl.itemClickCtrl.isStart = false;

        BoosterCtrl.Instance.IsBusy = false;
    }

    #endregion
}
