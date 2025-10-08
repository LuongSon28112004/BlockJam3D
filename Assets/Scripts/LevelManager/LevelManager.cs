using System;
using System.Collections.Generic;
using master;
using Unity.VisualScripting;
using UnityEngine;

public class LevelManager : SingletonDDOL<LevelManager>
{
    public int Level = 1;
    public List<LevelData> levelDatas;
    public BoardCtrl generateBoard;
    public CellPlayCtrl cellPlayCtrl;
    private void Start()
    {
        this.InitLevel(); // update last
    }

    private void InitLevel()
    {
        generateBoard.LoadLevel(levelDatas[Level - 1]);
    }
}
