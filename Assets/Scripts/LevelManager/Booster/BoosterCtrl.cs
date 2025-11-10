using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
public class BoosterCtrl : MonoBehaviour
{
    private static BoosterCtrl instance;
    public static BoosterCtrl Instance { get => instance; set => instance = value; }
    [SerializeField] private Transform parentBoard;
    [Header("Booster Component")]
    [SerializeField] BoosterAdd boosterAdd;
    [SerializeField] BoosterMagnet boosterMagnet;
    [SerializeField] BoosterShuffle boosterShuffle;
    [SerializeField] BoosterUndo boosterUndo;




    [Header("Busy")]
    public bool IsBusy = false;

    public BoosterAdd BoosterAdd { get => boosterAdd; set => boosterAdd = value; }
    public BoosterMagnet BoosterMagnet { get => boosterMagnet; set => boosterMagnet = value; }
    public BoosterShuffle BoosterShuffle { get => boosterShuffle; set => boosterShuffle = value; }
    public BoosterUndo BoosterUndo { get => boosterUndo; set => boosterUndo = value; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }
}
