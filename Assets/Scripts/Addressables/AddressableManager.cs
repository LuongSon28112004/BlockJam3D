using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using master;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableManager : SingletonDDOL<AddressableManager>
{
    private Dictionary<string, GameObject> _cachedPrefabs = new Dictionary<string, GameObject>();
    private Dictionary<string, List<LevelData>> _cachedLevelGroups = new Dictionary<string, List<LevelData>>();

    private bool _isPreloaded = false;

    private void Start()
    {
        _ = LoadAllAsset();
    }

    public async Task LoadAllAsset()
    {
        await PreloadAllPrefabsAsync();
        await PreloadAllLevelsAsync();
        _isPreloaded = true;
        Debug.Log("✅ All Addressable prefabs & levels loaded and grouped!");
    }

    // =========================
    // LOAD PREFABS (Block, Container, Wall)
    // =========================
    private async Task PreloadAllPrefabsAsync()
    {
        string[] labels = { "Wall", "Container", "Block" };

        foreach (string label in labels)
        {
            AsyncOperationHandle<IList<GameObject>> handle = Addressables.LoadAssetsAsync<GameObject>(
                label, null
            );
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                foreach (GameObject obj in handle.Result)
                {
                    string address = obj.name;
                    if (!_cachedPrefabs.ContainsKey(address))
                    {
                        _cachedPrefabs[address] = obj;
                        Debug.Log($"Loaded prefab: {address} (label: {label})");
                    }
                }
            }
            else
            {
                Debug.LogError($"❌ Failed to load prefabs with label: {label}");
            }
        }
    }

    // =========================
    // LOAD LEVEL DATA (ScriptableObjects) và gom nhóm
    // =========================
    private async Task PreloadAllLevelsAsync()
    {
        string label = "Level";
        AsyncOperationHandle<IList<LevelData>> handle = Addressables.LoadAssetsAsync<LevelData>(
            label, null
        );

        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            foreach (LevelData data in handle.Result)
            {
                // Lấy tên gốc của Level: ví dụ "Level_1(board_2)" → group = "Level_1"
                string baseName = GetBaseLevelName(data.name);

                if (!_cachedLevelGroups.ContainsKey(baseName))
                    _cachedLevelGroups[baseName] = new List<LevelData>();

                _cachedLevelGroups[baseName].Add(data);

                Debug.Log($"Loaded LevelData: {data.name} -> Group: {baseName}");
            }

            // Sắp xếp theo thứ tự bảng (board_1, board_2, …)
            foreach (var key in _cachedLevelGroups.Keys.ToList())
            {
                _cachedLevelGroups[key] = _cachedLevelGroups[key]
                    .OrderBy(d => ExtractBoardIndex(d.name))
                    .ToList();
            }
        }
        else
        {
            Debug.LogError("❌ Failed to load LevelData with label 'Level'");
        }
    }

    private string GetBaseLevelName(string fullName)
    {
        // Ví dụ: "Level_1(board_2)" → "Level_1"
        int idx = fullName.IndexOf('(');
        if (idx >= 0)
            return fullName.Substring(0, idx);
        return fullName;
    }

    private int ExtractBoardIndex(string fullName)
    {
        // Lấy số thứ tự trong ngoặc, ví dụ "Level_1(board_3)" → 3
        int start = fullName.IndexOf("(board_") + 7;
        int end = fullName.IndexOf(")", start);
        if (start >= 0 && end > start)
        {
            string numStr = fullName.Substring(start, end - start);
            if (int.TryParse(numStr, out int result))
                return result;
        }
        return 0;
    }

    // =========================
    // GET LEVEL GROUP
    // =========================
    public List<LevelData> GetLevelGroup(string levelName)
    {
        if (_cachedLevelGroups.TryGetValue(levelName, out List<LevelData> levels))
            return levels;

        Debug.LogError($"❌ No LevelData group found with name '{levelName}'!");
        return null;
    }

    public List<string> GetAllLevelNames()
    {
        return new List<string>(_cachedLevelGroups.Keys);
    }

    // =========================
    // PREFAB ACCESS
    // =========================
    public GameObject GetPrefab(string address)
    {
        if (!_isPreloaded)
            Debug.LogWarning("⚠ Addressables not preloaded yet! Make sure to wait until preload completes.");

        if (_cachedPrefabs.TryGetValue(address, out GameObject prefab))
            return prefab;

        Debug.LogError($"❌ Prefab with address '{address}' not found!");
        return null;
    }

    public async Task<GameObject> LoadPrefabAsync(string address)
    {
        if (_cachedPrefabs.ContainsKey(address))
            return _cachedPrefabs[address];

        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(address);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            _cachedPrefabs[address] = handle.Result;
            return handle.Result;
        }

        Debug.LogError($"❌ Failed to load Addressable prefab: {address}");
        return null;
    }

    public async Task<GameObject> InstantiatePrefabAsync(string address, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        GameObject prefab = GetPrefab(address);
        if (prefab == null)
            prefab = await LoadPrefabAsync(address);

        if (prefab == null)
            return null;

        return Instantiate(prefab, position, rotation, parent);
    }

    public void ReleaseAll()
    {
        foreach (var kvp in _cachedPrefabs)
            Addressables.Release(kvp.Value);

        foreach (var group in _cachedLevelGroups.Values)
            foreach (var level in group)
                Addressables.Release(level);

        _cachedPrefabs.Clear();
        _cachedLevelGroups.Clear();
    }
}
