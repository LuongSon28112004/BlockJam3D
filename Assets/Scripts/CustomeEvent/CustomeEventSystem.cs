using System;
using System.Collections.Generic;
using master;
using UnityEngine;

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


    public Action<TypeItem> CheckMatch_3_Action;

    public void CheckMatch_3(TypeItem typeItem)
    {
        CheckMatch_3_Action?.Invoke(typeItem);
    }

    public Action<List<int>>ActiveBoosterAction;
    public void ActiveBooster(List<int> lists)
    {
        ActiveBoosterAction?.Invoke(lists);
    }

}
