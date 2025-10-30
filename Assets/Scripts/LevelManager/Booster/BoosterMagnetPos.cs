using System.Collections.Generic;
using UnityEngine;

public class BoosterMagnetPos : MonoBehaviour
{
   [Header("Booster Add Pos Components")]
    [SerializeField] private List<Vector3> listPosBoosterMagnet;
    [SerializeField] private List<Transform> listTransformBoosterMagnet;

    public List<Vector3> ListPosBoosterMagnet { get => listPosBoosterMagnet; set => listPosBoosterMagnet = value; }

    public void Start()
    {
        listPosBoosterMagnet = new List<Vector3>();
        InitPosBoosterAdd();
    }

    private void InitPosBoosterAdd()
    {
        for(int i = 0; i < listTransformBoosterMagnet.Count; i++)
        {
            listPosBoosterMagnet.Add(listTransformBoosterMagnet[i].position);
        }
    }
}
