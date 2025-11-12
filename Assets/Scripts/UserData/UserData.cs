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
    public static int coin = 99999;
    public static int level = 1;
    public static List<BoosterCounter> listBoosterCounters = new List<BoosterCounter>();
}
