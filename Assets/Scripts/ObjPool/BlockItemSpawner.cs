using UnityEngine;

public class BlockItemSpawner : BaseobjectPool
{
    private static BlockItemSpawner instance;

    public static BlockItemSpawner Instance
    {
        get => instance;
    }

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
        DontDestroyOnLoad(gameObject);
    }

    public Transform spawnCellItem(string prefabName, Vector3 spawnPos, Quaternion rotation)
    {
        Transform prefab = this.Spawn(prefabName, spawnPos, rotation);
        prefab.gameObject.SetActive(true);
        return prefab;
    }
}
