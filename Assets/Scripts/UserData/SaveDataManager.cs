using System.IO;
using master;
using UnityEngine;

[System.Serializable]
public class PlayerData
{
    public int coin;
    public int level;
}

public static class SaveDataManager 
{
    // Tên File
    private static string saveFilePath = Path.Combine(Application.persistentDataPath, "UserData.json");

    // Lưu dữ liệu người chơi
    public static void Save()
    {
        PlayerData data = new PlayerData
        {
            coin = UserData.coin,
            level = UserData.level
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log($"[SaveDataManager] Dữ liệu đã được lưu tại: {saveFilePath}");
    }

    // Tải dữ liệu người chơi
    public static void Load()
    {
        if (!File.Exists(saveFilePath))
        {
            Debug.LogWarning("[SaveDataManager] Không tìm thấy file UserData.json, tạo dữ liệu mặc định...");
            Save(); // tạo file mặc định
            return;
        }

        string json = File.ReadAllText(saveFilePath);
        PlayerData data = JsonUtility.FromJson<PlayerData>(json);

        UserData.coin = data.coin;
        UserData.level = data.level;

        Debug.Log("[SaveDataManager] Dữ liệu đã được tải thành công!");
    }

    // Xóa file lưu (nếu cần reset)
    public static void DeleteSave()
    {
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
            Debug.Log("[SaveDataManager] File UserData.json đã bị xóa.");
        }
    }
}
