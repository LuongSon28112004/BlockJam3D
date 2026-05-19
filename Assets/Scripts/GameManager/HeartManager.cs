using System;
using master;
using UnityEngine;

public class HeartManager : SingletonAutoCreate<HeartManager>
{
    public event Action OnHeartsChanged;

    public int Hearts => UserData.hearts;
    public int MaxHearts => UserData.MAX_HEARTS;
    public bool IsFull => Hearts >= MaxHearts;

    public int SecondsUntilNextHeart
    {
        get
        {
            if (IsFull) return 0;
            long now = DateTime.UtcNow.Ticks;
            if (UserData.nextHeartUnixTicks <= 0 || UserData.nextHeartUnixTicks <= now) return 0;
            long deltaTicks = UserData.nextHeartUnixTicks - now;
            return Mathf.Max(0, (int)(deltaTicks / TimeSpan.TicksPerSecond));
        }
    }

    private void Start()
    {
        if (!IsFull && UserData.nextHeartUnixTicks <= 0)
        {
            UserData.nextHeartUnixTicks = DateTime.UtcNow.Ticks + TimeSpan.TicksPerSecond * UserData.REGEN_SECONDS;
        }
        CatchUpRegen();
    }

    private void Update()
    {
        if (IsFull) return;
        if (UserData.nextHeartUnixTicks <= 0) return;
        if (DateTime.UtcNow.Ticks < UserData.nextHeartUnixTicks) return;
        CatchUpRegen();
    }

    public bool TryConsume(int amount = 1)
    {
        if (amount <= 0) return true;
        if (UserData.hearts < amount) return false;

        bool wasFull = IsFull;
        UserData.hearts -= amount;

        if (wasFull)
        {
            UserData.nextHeartUnixTicks = DateTime.UtcNow.Ticks + TimeSpan.TicksPerSecond * UserData.REGEN_SECONDS;
        }

        Persist();
        return true;
    }

    public void Add(int amount, bool clampToMax = true)
    {
        if (amount <= 0) return;

        int newCount = UserData.hearts + amount;
        if (clampToMax) newCount = Mathf.Min(newCount, MaxHearts);
        UserData.hearts = newCount;

        if (IsFull) UserData.nextHeartUnixTicks = 0;

        Persist();
    }

    public void CatchUpRegen()
    {
        if (IsFull)
        {
            if (UserData.nextHeartUnixTicks != 0)
            {
                UserData.nextHeartUnixTicks = 0;
                Persist();
            }
            return;
        }

        if (UserData.nextHeartUnixTicks <= 0)
        {
            UserData.nextHeartUnixTicks = DateTime.UtcNow.Ticks + TimeSpan.TicksPerSecond * UserData.REGEN_SECONDS;
            Persist();
            return;
        }

        long now = DateTime.UtcNow.Ticks;
        long regenTicks = TimeSpan.TicksPerSecond * UserData.REGEN_SECONDS;
        int awarded = 0;

        while (!IsFull && now >= UserData.nextHeartUnixTicks)
        {
            UserData.hearts++;
            awarded++;
            UserData.nextHeartUnixTicks += regenTicks;
        }

        if (IsFull) UserData.nextHeartUnixTicks = 0;

        if (awarded > 0) Persist();
    }

    private void OnApplicationFocus(bool focus)
    {
        if (focus) CatchUpRegen();
        else SaveDataManager.Save();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) SaveDataManager.Save();
        else CatchUpRegen();
    }

    private void Persist()
    {
        SaveDataManager.Save();
        OnHeartsChanged?.Invoke();

        if (UserDataFirebaseManager.Instance != null)
        {
            UserDataFirebaseManager.Instance.UpdateHeart(UserData.hearts);
        }
    }
}
