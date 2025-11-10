using Unity.VisualScripting;
using UnityEngine;

public class WallItemSpawner : BaseobjectPool
{
    private static WallItemSpawner instance;

    public static WallItemSpawner Instance { get => instance; }

    protected override void Awake()
    {
        base.Awake();
        if (instance == null)
            instance = this;
    }

    public static string Wall = "Wall";

    public Transform SpawnOtherItem(string prefabName, Vector3 spawnPos, Quaternion rotation)
    {
        Transform prefab = this.Spawn(prefabName, spawnPos, rotation);
        prefab.gameObject.SetActive(true);
        return prefab;
    }

    public void AddOtherInPool()
    {
        foreach (var cell in LevelManager.Instance.BoardCtrl.boardAlls)
        {
            if (cell.name == "Wall")
            {
                Despawn(cell.transform);
            }
        }
    }
}
