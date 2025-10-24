using System;
using master;
using UnityEngine;

public class CustomeEventSystem : SingletonDDOL<CustomeEventSystem>
{

    //Change Round
    public Action<int> ChangeRoundAction;
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


    public Action CheckMatch_3_Action;

    public void CheckMatch_3()
    {
        CheckMatch_3_Action?.Invoke();
    }

}
