using System;
using System.Collections.Generic;
using UnityEngine;

public class BoosterAddPos : MonoBehaviour
{
    [Header("Booster Add Pos Components")]
    [SerializeField] private List<Vector3> listPosBoosterAdd;
    [SerializeField] private List<Transform> listTransformBoossterAdd;

    public List<Vector3> ListPosBoosterAdd { get => listPosBoosterAdd; set => listPosBoosterAdd = value; }


    public void Start()
    {
        listPosBoosterAdd = new List<Vector3>();
        InitPosBoosterAdd();
    }

    private void InitPosBoosterAdd()
    {
        for(int i = 0; i < listTransformBoossterAdd.Count; i++)
        {
            listPosBoosterAdd.Add(listTransformBoossterAdd[i].position);
        }
    }
}
