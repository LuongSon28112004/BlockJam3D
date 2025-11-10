using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEngine;

public class BlockItemSpawner : BaseobjectPool
{
    private static BlockItemSpawner instance;
    [SerializeField] private List<BoardCell> boardCellPools;

    public static BlockItemSpawner Instance
    {
        get => instance;
    }
    public List<BoardCell> BoardCellPools { get => boardCellPools; set => boardCellPools = value; }

    // BlueBase,
    // BrownBase,
    // GreenBase,
    // MagentaBase,
    // RedBase,
    // PurpleBase,
    // YellowBase,

    public static string Block_BlueBase = "BlueBase";
    public static string Block_BrownBase = "BrownBase";
    public static string Block_GreenBase = "GreenBase";
    public static string Block_MagentBase = "MagentaBase";
    public static string Block_RedBase = "RedBase";
    public static string Block_PurleBase = "PurpleBase";
    public static string Block_YellowBase = "YellowBase";

    protected override void Awake()
    {
        base.Awake();
        if (instance == null)
            instance = this;
        boardCellPools = new List<BoardCell>();

    }

    public Transform spawnCellItem(string prefabName, Vector3 spawnPos, Quaternion rotation)
    {
        Transform prefab = this.Spawn(prefabName, spawnPos, rotation);
        prefab.gameObject.SetActive(true);
        return prefab;
    }

    public override void Despawn(Transform obj)
    {
        base.Despawn(obj);
        if (obj.TryGetComponent<BoardCell>(out BoardCell boardCell))
        {
            // hiện tại vẫn chưa có cái nào dùng đến boardCellPools tôi lưu lại để sau này cần mở rộng hệ thống thì dùng
            boardCellPools.Add(boardCell);
        }
    }

    public void AddBlockInPool()
    {
        foreach (var cell in LevelManager.Instance.cellPlayCtrl.BoardCellMatch_3)
        {
            //reset về trạng thái mặc định và đưa vào pool
            cell.Reinitialize();
            cell.gameObject.name = Enum.GetName(typeof(TypeItem), cell.TypeItem);
            BlockItemSpawner.Instance.Despawn(cell.gameObject.transform);
        }
        LevelManager.Instance.cellPlayCtrl.BoardCellMatch_3.Clear();
    }
}
