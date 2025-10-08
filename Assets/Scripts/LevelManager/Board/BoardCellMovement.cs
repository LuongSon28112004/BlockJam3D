using System.Collections.Generic;
using UnityEngine;
using DG;
using DG.Tweening;
using System.Threading.Tasks;

public class BoardCellMovement : MonoBehaviour
{
    public async void Movement(List<Vector3> containers)
    {
        for (int i = 1; i < containers.Count; i++)
        {
            transform.parent.DOMove(containers[i],0.1f);
            await Task.Delay(100);
        }
    }
}
