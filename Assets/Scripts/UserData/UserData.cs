using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BoosterCounter
{
    public string name;
    public int count;
}

public static class UserData
{
    public const int MAX_HEARTS = 5;
    public const int REGEN_SECONDS = 600;

    public static int coin = 99999;
    public static int level = 1;
    public static List<BoosterCounter> listBoosterCounters = new List<BoosterCounter>();

    public static int hearts = MAX_HEARTS;
    public static long nextHeartUnixTicks = 0;

    public static string language = "en";

    // Daily Mission progress (seeded lần đầu bởi DailyMissionManager).
    public static DailyMissionProgress dailyMissionProgress;
}
