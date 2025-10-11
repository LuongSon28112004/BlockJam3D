using System;
using System.Collections.Generic;
using master;
using UnityEngine;

public class LevelManager : SingletonDDOL<LevelManager>
{
    [Header("Setting LevelManager Components")]
    public int Level = 1;
    public List<LevelData> levelDatas;
    public BoardCtrl BoardCtrl;
    public CellPlayCtrl cellPlayCtrl;
    private void Start()
    {
        this.InitLevel(); // update last
        // if (Instance != null)
        // {
        //     Debug.Log("ok");
        // }
    }

    private void InitLevel()
    {
        BoardCtrl.LoadLevel(levelDatas[Level - 1]);
    }
}
