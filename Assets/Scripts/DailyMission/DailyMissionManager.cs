using System;
using System.Collections.Generic;
using System.Linq;
using master;
using UnityEngine;

/// <summary>
/// Quản lý Daily Mission: reset hằng ngày theo UTC midnight, subscribe các event tiến trình,
/// và cấp phần thưởng khi player Claim. Sống ở Init.unity như AddressableManager (DDOL).
/// </summary>
public class DailyMissionManager : SingletonDDOL<DailyMissionManager>
{
    public event Action OnMissionProgressChanged;

    // Pool tất cả các template mission load từ Resources/MissionData (xem CLAUDE.md: tạm dùng Resources,
    // có thể promote lên Addressables label "Mission" sau).
    private DailyMissionData[] missionPool;
    private Dictionary<string, DailyMissionData> missionLookup = new Dictionary<string, DailyMissionData>();

    // Theo dõi coin trước đó để tính positive delta (EarnCoins)
    private int lastSeenCoin;

    private bool subscribed = false;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        LoadPool();
        EnsureProgress();
        CheckAndResetIfNeeded();
        lastSeenCoin = UserData.coin;
        SubscribeEvents();
    }

    private void OnEnable()
    {
        // Có thể được gọi trước Start ngay sau Awake, nhưng pool có thể chưa load -> guard.
        if (missionPool != null) SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    #region --- Load & seed ---

    private void LoadPool()
    {
        missionPool = Resources.LoadAll<DailyMissionData>("MissionData");
        missionLookup.Clear();
        if (missionPool != null)
        {
            foreach (var m in missionPool)
            {
                if (m != null && !string.IsNullOrEmpty(m.missionId))
                {
                    missionLookup[m.missionId] = m;
                }
            }
        }
        if (missionPool == null || missionPool.Length == 0)
        {
            Debug.LogWarning("[DailyMissionManager] Không tìm thấy DailyMissionData nào tại Resources/MissionData.");
        }
    }

    private void EnsureProgress()
    {
        if (UserData.dailyMissionProgress == null)
        {
            UserData.dailyMissionProgress = new DailyMissionProgress
            {
                lastResetUtcTicks = 0,
                tasks = new List<MissionTaskProgress>()
            };
        }
        if (UserData.dailyMissionProgress.tasks == null)
        {
            UserData.dailyMissionProgress.tasks = new List<MissionTaskProgress>();
        }
    }

    public DailyMissionData GetMissionData(string missionId)
    {
        if (string.IsNullOrEmpty(missionId)) return null;
        missionLookup.TryGetValue(missionId, out var data);
        return data;
    }

    #endregion

    #region --- Daily reset ---

    public void CheckAndResetIfNeeded()
    {
        long midnightTicks = GetCurrentUtcMidnightTicks();
        var progress = UserData.dailyMissionProgress;

        bool needReset = progress.lastResetUtcTicks < midnightTicks
                         || progress.tasks == null
                         || progress.tasks.Count == 0;

        if (!needReset) return;

        progress.tasks = PickDailyTasks();
        progress.lastResetUtcTicks = midnightTicks;
        SaveDataManager.Save();
        OnMissionProgressChanged?.Invoke();
    }

    private List<MissionTaskProgress> PickDailyTasks()
    {
        var list = new List<MissionTaskProgress>();
        if (missionPool == null || missionPool.Length == 0) return list;

        int desired = Mathf.Min(3, missionPool.Length);
        var indices = Enumerable.Range(0, missionPool.Length).ToList();
        // Fisher-Yates shuffle
        var rand = new System.Random();
        for (int i = indices.Count - 1; i > 0; i--)
        {
            int j = rand.Next(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }
        for (int i = 0; i < desired; i++)
        {
            var data = missionPool[indices[i]];
            if (data == null) continue;
            list.Add(new MissionTaskProgress
            {
                missionId = data.missionId,
                currentCount = 0,
                claimed = false
            });
        }
        return list;
    }

    public static long GetCurrentUtcMidnightTicks()
    {
        DateTime now = DateTime.UtcNow;
        DateTime midnight = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
        return midnight.Ticks;
    }

    public TimeSpan TimeUntilNextReset()
    {
        DateTime now = DateTime.UtcNow;
        DateTime nextMidnight = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(1);
        var ts = nextMidnight - now;
        if (ts.Ticks < 0) ts = TimeSpan.Zero;
        return ts;
    }

    #endregion

    #region --- Event subscriptions ---

    private void SubscribeEvents()
    {
        if (subscribed) return;
        var es = CustomeEventSystem.Instance;
        if (es == null) return;
        es.ChangeLevelAction += OnLevelChanged;
        es.ChangeCoinAction += OnCoinChanged;
        es.CheckMatch_3_Action += OnMatch3;
        es.UseBoosterAction += OnBoosterUsed;
        subscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!subscribed) return;
        var es = CustomeEventSystem.Instance;
        if (es != null)
        {
            es.ChangeLevelAction -= OnLevelChanged;
            es.ChangeCoinAction -= OnCoinChanged;
            es.CheckMatch_3_Action -= OnMatch3;
            es.UseBoosterAction -= OnBoosterUsed;
        }
        subscribed = false;
    }

    private int lastSeenLevel = -1;

    private void OnLevelChanged(int newLevel)
    {
        if (lastSeenLevel < 0)
        {
            lastSeenLevel = newLevel;
            return;
        }
        if (newLevel > lastSeenLevel)
        {
            int delta = newLevel - lastSeenLevel;
            IncrementByType(MissionType.WinLevels, delta);
        }
        lastSeenLevel = newLevel;
    }

    private void OnCoinChanged(int newCoin)
    {
        int delta = newCoin - lastSeenCoin;
        if (delta > 0)
        {
            IncrementByType(MissionType.EarnCoins, delta);
        }
        lastSeenCoin = newCoin;
    }

    private void OnMatch3(TypeItem typeItem)
    {
        IncrementByType(MissionType.CreateMatch3, 1);
    }

    private void OnBoosterUsed(int boosterId)
    {
        IncrementByType(MissionType.UseBoosters, 1);
    }

    #endregion

    #region --- Progress updates ---

    private void IncrementByType(MissionType type, int amount)
    {
        if (amount <= 0) return;
        if (UserData.dailyMissionProgress == null || UserData.dailyMissionProgress.tasks == null) return;

        bool changed = false;
        foreach (var task in UserData.dailyMissionProgress.tasks)
        {
            var data = GetMissionData(task.missionId);
            if (data == null) continue;
            if (data.type != type) continue;
            if (task.claimed) continue;
            if (task.currentCount >= data.targetCount) continue;

            task.currentCount = Mathf.Min(task.currentCount + amount, data.targetCount);
            changed = true;
        }
        if (changed)
        {
            SaveDataManager.Save();
            OnMissionProgressChanged?.Invoke();
        }
    }

    #endregion

    #region --- Claim ---

    public bool ClaimReward(string missionId)
    {
        if (UserData.dailyMissionProgress == null || UserData.dailyMissionProgress.tasks == null) return false;
        var task = UserData.dailyMissionProgress.tasks.FirstOrDefault(t => t.missionId == missionId);
        if (task == null) return false;

        var data = GetMissionData(missionId);
        if (data == null) return false;
        // Dùng effective progress/claimed để mockStatus điều khiển được trạng thái claim.
        if (data.GetEffectiveClaimed(task.claimed)) return false;
        if (data.GetEffectiveProgress(task.currentCount) < data.targetCount) return false;

        // Cộng coin và phát event để ChangeCoin animation chạy
        UserData.coin += data.coinReward;
        // Sync lastSeenCoin trước khi fire để OnCoinChanged không tính coin thưởng vào EarnCoins
        lastSeenCoin = UserData.coin;
        CustomeEventSystem.Instance?.ChangeCoin(UserData.coin);

        task.claimed = true;
        SaveDataManager.Save();
        // Đẩy coin mới lên Firestore qua helper dùng chung.
        UserDataFirebaseManager.Instance?.PushCoinSnapshot();
        OnMissionProgressChanged?.Invoke();
        return true;
    }

    public bool HasClaimableMission
    {
        get
        {
            if (UserData.dailyMissionProgress == null || UserData.dailyMissionProgress.tasks == null) return false;
            foreach (var task in UserData.dailyMissionProgress.tasks)
            {
                var data = GetMissionData(task.missionId);
                if (data == null) continue;
                if (data.GetEffectiveClaimed(task.claimed)) continue;
                if (data.GetEffectiveProgress(task.currentCount) >= data.targetCount) return true;
            }
            return false;
        }
    }

    #endregion
}
