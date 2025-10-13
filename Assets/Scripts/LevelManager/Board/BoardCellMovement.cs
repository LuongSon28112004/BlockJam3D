using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Threading.Tasks;
using System;

public class BoardCellMovement : MonoBehaviour
{
    [Header("Timing Settings")]
    [SerializeField] private float timerPerCellMatrixSecond = 0.1f;
    [SerializeField] private float distancePerCell = 1.25f;

    private float totalCell;

    /// <summary>
    /// Di chuyển qua các ô trên ma trận theo danh sách vị trí.
    /// </summary>
    public async Task MovementMatrix(List<Vector3> containers)
    {
        if (containers == null || containers.Count == 0)
        {
            Debug.LogWarning("containers rỗng, không thể di chuyển.");
            return;
        }

        totalCell = containers.Count;

        for (int i = 1; i < containers.Count; i++)
        {
            Vector3 nextPos = containers[i];

            if (transform.parent == null)
            {
                Debug.LogWarning("Không có transform.parent, không thể di chuyển.");
                return;
            }

            // ⚙️ Tăng thời gian gấp đôi để di chuyển chậm hơn
            Tween moveTween = transform.parent.DOMove(nextPos, timerPerCellMatrixSecond)
                .SetEase(Ease.InOutSine);

            await moveTween.AsyncWaitForCompletion();
        }

        Debug.Log("Đã hoàn thành di chuyển trên ma trận.");
    }

    /// <summary>
    /// Di chuyển từ ma trận xuống vị trí CellPlay với thời gian tính theo khoảng cách.
    /// </summary>
    public async Task MovementToCellPlay(Vector3 pos)
    {
        if (transform.parent == null)
        {
            Debug.LogWarning("Không có transform.parent, không thể di chuyển.");
            return;
        }

        float distanceMagnitude = Vector3.Distance(pos, transform.parent.position);
        float timer = (distanceMagnitude / distancePerCell) * timerPerCellMatrixSecond;

        // ⚙️ Tăng thời gian gấp đôi để di chuyển chậm hơn
        Tween moveTween = transform.parent.DOMove(pos, timer * 1.5f)
            .SetEase(Ease.OutCubic);

        await moveTween.AsyncWaitForCompletion();

        Debug.Log("Đã di chuyển xuống CellPlay.");
    }

    /// <summary>
    /// Di chuyển đến vị trí cụ thể (ví dụ cho reposition hoặc chỉnh vị trí nhanh).
    /// </summary>
    public async Task MovementToPos(Vector3 pos)
    {
        if (transform.parent == null)
        {
            Debug.LogWarning("Không có transform.parent, không thể di chuyển.");
            return;
        }

        // ⚙️ Tăng thời gian gấp đôi để di chuyển chậm hơn
        Tween moveTween = transform.parent.DOMove(pos, timerPerCellMatrixSecond)
            .SetEase(Ease.InOutSine);

        await moveTween.AsyncWaitForCompletion();

        Debug.Log("Đã hoàn thành MovementToPos.");
    }
}
