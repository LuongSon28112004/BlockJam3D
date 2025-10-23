using System;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "BoosterData", menuName = "Game/BoosterData")]

public class BoosterData : ScriptableObject
{
    public string nameBoosster;
    public int price;
}
