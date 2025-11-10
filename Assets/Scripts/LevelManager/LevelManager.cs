using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using master;
using UnityEngine;

public class LevelManager : Singleton<LevelManager>
{
    [Header("Setting LevelManager Components")]
    public int Round = 0;
    public List<LevelData> levelDatas;
    public BoardCtrl BoardCtrl;
    public CellPlayCtrl cellPlayCtrl;
    public BoosterCtrl boosterCtrl;
    [Header("Action")]
    public Func<IEnumerator> NextRound;
    public bool isNextRound = false;

    public void Init()
    {
        StartCoroutine(LoadLevel());
        NextRound += NextRoundLevel;
    }

    private IEnumerator LoadLevel()
    {
        Round = 0;
        string levelGroupKey = "Level_" + GameManager.Instance.Level;
        List<LevelData> levelBoards = AddressableManager.Instance.GetLevelGroup(levelGroupKey);
        levelDatas = levelBoards;
        yield return BoardCtrl.LoadLevel(levelDatas[Round], false);
    }

    private IEnumerator NextRoundLevel()
    {
        if (isNextRound) yield break;
        isNextRound = true;
        // đưa các gameobject vào pool
        yield return new WaitForSeconds(1f);
        AudioManager.Instance.PlayOneShot("BLJ_League_LeaderBoard_Enter", 1f);
        // đưa các object vào pool để tái sử dụng
        AddPool();
        Round += 1;
        if (Round > 2) yield break;
        CustomeEventSystem.Instance.ChangeRound(Round);
        CustomeEventSystem.Instance.ActiveBooster(new List<int> { -1, -1, 1, 1 });
        isNextRound = false;
        yield return BoardCtrl.LoadLevel(levelDatas[Round]);
    }

    private void AddPool()
    {
        BlockItemSpawner.Instance.AddBlockInPool();
        WallItemSpawner.Instance.AddOtherInPool();
    }

}
