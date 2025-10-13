using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using master;
using UnityEngine;

public class LevelManager : Singleton<LevelManager>
{
    [Header("Setting LevelManager Components")]
    public int Level = 1;
    public int Round = 0;
    public List<LevelData> levelDatas;
    public BoardCtrl BoardCtrl;
    public CellPlayCtrl cellPlayCtrl;
    [Header("Action")]
    public Func<Task> NextRound;
    public Action LoseGame;
    public Action WinGame;
    public Canvas canvas;
    [Header("list popup")]
    [SerializeField] private GameObject popupLose;
    [SerializeField] private GameObject popupWin;
     //public Action NextLevel;

    private void Start()
    {
        _ = LoadLevel(); // update last
        // if (Instance != null)
        // {
        //     Debug.Log("ok");
        // }
        NextRound += NextRoundLevel;
        LoseGame += ShowPopupLose;
        WinGame += ShowPopupWin;
    }

    private async Task LoadLevel()
    {
        Round = 0;

        List<LevelData> levelBoards = AddressableManager.Instance.GetLevelGroup("Level_1");
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

    private void ShowPopupLose()
    {
        GameObject popuplose = Instantiate(popupLose, canvas.transform);
        popuplose.transform.localScale = Vector3.zero;
        popuplose.transform.DOScale(Vector3.one, 0.1f);
    }
    
    private void ShowPopupWin()
    {
        GameObject popupwin = Instantiate(popupWin, canvas.transform);
        popupwin.transform.localScale = Vector3.zero;
        popupwin.transform.DOScale(Vector3.one, 0.1f);
    }
}
