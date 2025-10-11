using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Threading.Tasks;
using System;

public class BoardCellMovement : MonoBehaviour
{
    private float timerPerCellMatrixSecond = 0.05f;
    private int timerPerCellMatrixMiliSecond = 50;
    private float totalCell;
    private float DistancePerCell = 1.25f;
    public async Task MovementMatrix(List<Vector3> containers)
    {
        totalCell = containers.Count;
        if (containers == null || containers.Count == 0)
        {
            Debug.LogWarning("containers rỗng, không thể di chuyển.");
            return;
        }

        for (int i = 1; i < containers.Count; i++)
        {
            Vector3 nextPos = containers[i];
            transform.parent.DOMove(nextPos, timerPerCellMatrixSecond);
            await Task.Delay(timerPerCellMatrixMiliSecond);
        }

        Debug.Log("Đã hoàn thành di chuyển trên ma trận.");
    }

    public void MovementToCellPlay(Vector3 pos)
    {
        if (transform.parent == null) return;

        float distanceMagnitude = Vector3.Magnitude(pos - transform.parent.position);
        float timer = distanceMagnitude / DistancePerCell;
        timer *= timerPerCellMatrixSecond;

        transform.parent.DOMove(pos, timer);
        Debug.Log("Đã di chuyển xuống CellPlay.");
    }

    public void MovementToPos(Vector3 pos)
    {
        if (transform.parent == null) return;

        transform.parent.DOMove(pos, timerPerCellMatrixSecond);
    }
}
