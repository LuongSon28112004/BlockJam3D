using System;
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
    [Header("Action")]
    public Func<Task> NextRound;
    //public Action NextLevel;
    
    public void Init()
    {
          _ = LoadLevel(); // update last
        // if (Instance != null)
        // {
        //     Debug.Log("ok");
        // }
        NextRound += NextRoundLevel;
    }

    private async Task LoadLevel()
    {
        Round = 0;
        string levelGroupKey = "Level_" + GameManager.Instance.Level;
        List<LevelData> levelBoards = AddressableManager.Instance.GetLevelGroup(levelGroupKey);
        levelDatas = levelBoards;
        await BoardCtrl.LoadLevel(levelDatas[Round]);
    }

    // public void NextLevel()
    // {

    // }


    private async Task NextRoundLevel()
    {
        Round += 1;
        await Task.Delay(100);
        await BoardCtrl.LoadLevel(levelDatas[Round]);
    }

}
