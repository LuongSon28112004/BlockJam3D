using UnityEngine;

public enum MissionType
{
    WinLevels,
    UseBoosters,
    EarnCoins,
    CreateMatch3
}

public enum MissionMockStatus
{
    None,         // Dùng tiến trình thật của player.
    NotStarted,   // 0/N, chưa claim.
    Completed,    // N/N, chưa claim (Claim button bật).
    Claimed       // N/N, đã claim (Claimed overlay hiện).
}

[CreateAssetMenu(fileName = "DailyMission", menuName = "Game/DailyMissionData")]
public class DailyMissionData : ScriptableObject
{
    public string missionId;        // unique key, used to look up progress after save/load
    public MissionType type;
    public int targetCount;
    public int coinReward;
    public string titleKey;         // localization key, formatted with {0} = targetCount
    public Sprite icon;             // optional

    [Header("Editor mock (debug)")]
    [Tooltip("Override progress khi test trong Editor. None = dùng tiến trình thật; Completed = ép hoàn thành; NotStarted = ép 0/N.")]
    public MissionMockStatus mockStatus = MissionMockStatus.None;

    public int GetEffectiveProgress(int rawProgress)
    {
        switch (mockStatus)
        {
            case MissionMockStatus.Completed: return targetCount;
            case MissionMockStatus.Claimed:   return targetCount;
            case MissionMockStatus.NotStarted: return 0;
            default: return rawProgress;
        }
    }

    public bool GetEffectiveClaimed(bool rawClaimed)
    {
        switch (mockStatus)
        {
            case MissionMockStatus.Claimed:    return true;   // luôn claimed (Claimed overlay)
            case MissionMockStatus.NotStarted: return false;  // luôn chưa làm
            case MissionMockStatus.Completed:  return false;  // ép "đã đủ progress, chưa nhận thưởng" — Claim button enable
            // None: dùng cờ claim thật từ save.
            default: return rawClaimed;
        }
    }
}
