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

    public static string Block_BlueBase = "1";
    public static string Block_BrownBase = "2";
    public static string Block_GreenBase = "3";
    public static string Block_MagentBase = "4";
    public static string Block_RedBase = "5";
    public static string Block_PurleBase = "6";
    public static string Block_YellowBase = "7";

    protected override void Awake()
    {
        base.Awake();
        if (instance == null)
            instance = this;
    }

    public Transform spawnCellItem(string prefabName, Vector3 spawnPos, Quaternion rotation)
    {
        Transform prefab = this.Spawn(prefabName, spawnPos, rotation);
        prefab.gameObject.SetActive(true);
        prefab.GetComponent<SpriteRenderer>().sortingOrder = -1;
        prefab.GetComponent<SpriteRenderer>().color = Color.white;
        prefab.localScale = new Vector3(150, 150, 150);
        return prefab;
    }
}
