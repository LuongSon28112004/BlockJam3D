using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using master;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomeEventSystem : SingletonDDOL<CustomeEventSystem>
{

    //Change Round
    public Action<int> ChangeRoundAction;
    public Action ResetStartAction;
    public void ChangeRound(int Round)
    {
        ChangeRoundAction?.Invoke(Round);
    }


    //Change Coin
    public Action<int> ChangeCoinAction;
    public void ChangeCoin(int Coin)
    {
        ChangeCoinAction?.Invoke(Coin);
    }

    //Change Level
    public Action<int> ChangeLevelAction;
    public void ChangeLevel(int Level)
    {
        ChangeLevelAction?.Invoke(Level);
    }

    //Show LevelLoading
    public Action ShowLoadingAction;
    public void ShowLoading()
    {
        ShowLoadingAction?.Invoke();
    }

    //Check Match_3
    public Action<TypeItem> CheckMatch_3_Action;

    public void CheckMatch_3(TypeItem typeItem)
    {
        CheckMatch_3_Action?.Invoke(typeItem);
    }


    //active or inactive các booster UI
    public Action<List<int>> ActiveBoosterAction;
    public void ActiveBooster(List<int> lists)
    {
        ActiveBoosterAction?.Invoke(lists);
    }

    // Tutorial
    public Action<TutorialMode, Vector3> TutorialPosAction;
    public void TutorialPos(TutorialMode tutorialMode, Vector3 pos)
    {
        TutorialPosAction?.Invoke(tutorialMode, pos);
    }

    public Action<bool> ShowTextMatch_3_Action;
    public void ShowTextMatch_3(bool isShow)
    {
        ShowTextMatch_3_Action?.Invoke(isShow);
    }

    public Action<TutorialType> ChangeTextTutorialAction;
    public void ChangeTextTutorial(TutorialType tutorialType)
    {
        ChangeTextTutorialAction?.Invoke(tutorialType);
    }

    // Fire khi player THỰC SỰ tiêu thụ 1 booster (khác với ActiveBoosterAction là sự kiện
    // active/inactive UI dựa trên thay đổi inventory list — dùng cái đó sẽ bị double-count).
    // boosterId: 0=Undo, 1=Add, 2=Shuffle, 3=Magnet.
    public Action<int> UseBoosterAction;
    public void UseBooster(int boosterId)
    {
        UseBoosterAction?.Invoke(boosterId);
    }

    // Fire khi PlayerID/PlayerName đổi (login Google thành công, đồng bộ cloud, hoặc logout).
    // UI hiển thị user info nên subscribe để refresh.
    public Action AccountChangedAction;
    public void AccountChanged()
    {
        AccountChangedAction?.Invoke();
    }

}
