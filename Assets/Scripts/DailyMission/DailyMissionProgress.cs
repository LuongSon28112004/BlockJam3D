using System;
using System.Collections.Generic;

[Serializable]
public class DailyMissionProgress
{
    public long lastResetUtcTicks;          // 0 = never seeded
    public List<MissionTaskProgress> tasks; // exactly 3 per day after seeding
}

[Serializable]
public class MissionTaskProgress
{
    public string missionId;
    public int currentCount;
    public bool claimed;
}
