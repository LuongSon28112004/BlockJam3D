using System.IO;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerData
{
    public int coin;
    public int level;
    public List<BoosterCounter> listBoosterCounters;
}

public static class SaveDataManager
{
    private static string saveFilePath = Path.Combine(Application.persistentDataPath, "UserData.json");

    // Lưu dữ liệu
    public static void Save()
    {
        PlayerData data = new PlayerData
        {
            coin = UserData.coin,
            level = UserData.level,
            listBoosterCounters = UserData.listBoosterCounters
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(saveFilePath, json);

        Debug.Log($"[SaveDataManager] Dữ liệu đã được lưu tại: {saveFilePath}");
    }

    // Tải dữ liệu
    public static void Load()
    {
        if (!File.Exists(saveFilePath))
        {
            Debug.LogWarning("[SaveDataManager] Không tìm thấy file UserData.json, tạo dữ liệu mặc định...");

            // Tạo dữ liệu mặc định ban đầu
            UserData.listBoosterCounters = new List<BoosterCounter>
            {
                new BoosterCounter { name = "Undo", count = 2 },
                new BoosterCounter { name = "Add", count = 2 },
                new BoosterCounter { name = "Shuffle", count = 2 },
                new BoosterCounter { name = "Magnet", count = 2 }
            };

            Save(); // Tạo file mới
            return;
        }

        string json = File.ReadAllText(saveFilePath);
        PlayerData data = JsonUtility.FromJson<PlayerData>(json);

        UserData.coin = data.coin;
        UserData.level = data.level;

        // Kiểm tra list null
        if (data.listBoosterCounters != null)
            UserData.listBoosterCounters = data.listBoosterCounters;
        else
            UserData.listBoosterCounters = new List<BoosterCounter>();

        Debug.Log("[SaveDataManager] Dữ liệu đã được tải thành công!");
    }

    // Xóa file lưu
    public static void DeleteSave()
    {
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
            Debug.Log("[SaveDataManager] File UserData.json đã bị xóa.");
        }
    }
}
