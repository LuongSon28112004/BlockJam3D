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
    //public Action NextLevel;
    
    public void Init()
    {
         StartCoroutine(LoadLevel()); // update last
        // if (Instance != null)
        // {
        //     Debug.Log("ok");
        // }
        NextRound += NextRoundLevel;
    }

    private IEnumerator LoadLevel()
    {
        Round = 0;
        string levelGroupKey = "Level_" + GameManager.Instance.Level;
        List<LevelData> levelBoards = AddressableManager.Instance.GetLevelGroup(levelGroupKey);
        levelDatas = levelBoards;
        yield return BoardCtrl.LoadLevel(levelDatas[Round]);
    }

    // public void NextLevel()
    // {

    // }


    private IEnumerator NextRoundLevel()
    {
        Round += 1;
        if (Round > 2) yield break;
        yield return new WaitForSeconds(0.1f);
        CustomeEventSystem.Instance.ChangeRound(Round);
        yield return BoardCtrl.LoadLevel(levelDatas[Round]);
    }

}
