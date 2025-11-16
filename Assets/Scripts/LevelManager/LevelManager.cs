using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using master;
using NUnit.Framework;
using UnityEngine;

public class LevelManager : Singleton<LevelManager>
{
    [Header("Setting LevelManager Components")]
    public BoardCtrl BoardCtrl;
    public CellPlayCtrl cellPlayCtrl;
    public BoosterCtrl boosterCtrl;
    public TutorialCtrl TutorialCtrl;


    public List<LevelData> levelDatas;
    public int Round = 0;
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
        //khóa không cho dùng Booster khi chưa Load song round
        BoosterCtrl.Instance.IsBusy = true;
        //Load round level
        yield return BoardCtrl.LoadLevel(levelDatas[Round], false);
        AddTutorial();
        //Mở Khóa để sử dụng được Booster
        yield return new WaitForSeconds(0.3f);
        BoosterCtrl.Instance.IsBusy = false;
    }

    public void AddTutorial()
    {
        if (GameManager.Instance.Level == 1)
        {

            if (!UIManager.Instance.HasPopupShowing())
            {
                UIManager.Instance.ShowPopup<PopupTutorial>(null);
            }
            if (Round == 0)
            {
                TutorialCtrl.ShowText(TutorialType.Click);
                TutorialCtrl.TutorialClick();
            }
            else
            {
                TutorialCtrl.ShowText(TutorialType.Order);
                TutorialCtrl.ShowOrHideTextMatch_3(false);
            }
        }
        else if (GameManager.Instance.Level == 2)
        {
            if (Round == 1)
            {
                if (!UIManager.Instance.HasPopupShowing())
                {
                    UIManager.Instance.ShowPopup<PopupTutorial>(null);
                }
                TutorialCtrl.ShowOrHideTextMatch_3(false);
                TutorialCtrl.ShowText(TutorialType.Pipe);
            }
        }
    }

    private IEnumerator NextRoundLevel()
    {
        if (isNextRound) yield break;
        isNextRound = true;

        yield return new WaitForSeconds(1.5f);
        AudioManager.Instance.PlayOneShot("BLJ_League_LeaderBoard_Enter", 1f);

        // đưa các object vào pool để tái sử dụng
        AddPool();

        // Chuyển Round mới
        Round += 1;
        if (Round > 2) yield break;
        CustomeEventSystem.Instance.ChangeRound(Round);
        CustomeEventSystem.Instance.ActiveBooster(new List<int> { -1, -1, 1, 1 });
        isNextRound = false;
        //khóa không cho dùng Booster khi chưa Load song round
        BoosterCtrl.Instance.IsBusy = true;
        //reset booster Undo
        boosterCtrl.BoosterUndo.ResetStackUndo();
        //Load round level
        yield return BoardCtrl.LoadLevel(levelDatas[Round]);

        // check xem có tutorial ở Round này hay không
        AddTutorial();
        //Mở Khóa để sử dụng được Booster
        yield return new WaitForSeconds(0.3f);
        BoosterCtrl.Instance.IsBusy = false;
    }

    private void AddPool()
    {
        BlockItemSpawner.Instance.AddBlockInPool();
        WallItemSpawner.Instance.AddOtherInPool();
    }

}
