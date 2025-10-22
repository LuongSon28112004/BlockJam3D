using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;

public class BoardCellMovement : MonoBehaviour
{
    [Header("Timing Settings")]
    private float timerPerCellMatrixSecond = 0.15f;
    private float distancePerCell = 1.25f;

    private float totalCell;

    /// <summary>
    /// Di chuyển qua các ô trên ma trận theo danh sách vị trí (Coroutine).
    /// </summary>
    public IEnumerator MovementMatrix(List<Vector3> containers)
    {
        if (containers == null || containers.Count == 0)
        {
            Debug.LogWarning("containers rỗng, không thể di chuyển.");
            yield break;
        }

        totalCell = containers.Count;

        for (int i = 1; i < containers.Count; i++)
        {
            if (transform.parent == null)
            {
                Debug.LogWarning("Không có transform.parent, không thể di chuyển.");
                yield break;
            }


            //rotate 
            if (transform.parent.position.x > containers[i].x)
            {
                // Di chuyển sang trái → quay sang trái
                transform.parent.localRotation = Quaternion.Euler(0, 90, 0);
            }
            else if (transform.parent.position.x < containers[i].x)
            {
                // Di chuyển sang phải → quay sang phải
                transform.parent.localRotation = Quaternion.Euler(0, -90, 0);
            }
            else
            {
                // Không thay đổi trục x → quay về hướng mặc định
                transform.parent.localRotation = Quaternion.Euler(0, 0, 0);
            }

            Vector3 nextPos = containers[i];

            // Tạo tween di chuyển
            Tween moveTween = transform.parent.DOMove(nextPos, timerPerCellMatrixSecond)
                .SetEase(Ease.InOutSine);

            // Chờ tween hoàn thành
            yield return moveTween.WaitForCompletion();
        }

        // Debug.Log("Đã hoàn thành di chuyển trên ma trận.");
    }

    /// <summary>
    /// Di chuyển từ ma trận xuống vị trí CellPlay với thời gian tính theo khoảng cách (Coroutine).
    /// </summary>
    public IEnumerator MovementToCellPlay(Vector3 pos)
    {
        if (transform.parent == null)
        {
            Debug.LogWarning("Không có transform.parent, không thể di chuyển.");
            yield break;
        }

        float distanceMagnitude = Vector3.Distance(pos, transform.parent.position);
        float timer = (distanceMagnitude / distancePerCell) * timerPerCellMatrixSecond;

        Tween moveTween = transform.parent.DOMove(pos, timer)
            .SetEase(Ease.InOutSine);

        yield return moveTween.WaitForCompletion();

        // Debug.Log("Đã di chuyển xuống CellPlay.");
    }

    /// <summary>
    /// Di chuyển đến vị trí cụ thể (ví dụ cho reposition hoặc chỉnh vị trí nhanh).
    /// </summary>
    public IEnumerator MovementToPos(Vector3 pos)
    {
        if (transform.parent == null)
        {
            Debug.LogWarning("Không có transform.parent, không thể di chuyển.");
            yield break;
        }

        float distanceMagnitude = Vector3.Distance(pos, transform.parent.position);
        float timer = (distanceMagnitude / distancePerCell) * timerPerCellMatrixSecond;
        Tween moveTween = transform.parent.DOMove(pos, timer)
            .SetEase(Ease.InOutSine);

        yield return moveTween.WaitForCompletion();

        Debug.Log("Đã hoàn thành MovementToPos.");
    }

    /// <summary>
    /// ⭐ HÀM MỚI: Trả về Tween để có thể chạy song song (dùng trong ShiftCellsLeft/Right).
    /// </summary>
    public Tween MovementToPosTween(Vector3 pos)
    {
        if (transform.parent == null)
        {
            Debug.LogError("Không có transform.parent, không thể tạo Tween.");
            return null;
        }

        float distanceMagnitude = Vector3.Distance(pos, transform.parent.position);
        float timer = (distanceMagnitude / distancePerCell) * timerPerCellMatrixSecond;

        // Tạo và trả về Tween. Tốc độ tương đương với hàm Coroutine cũ.
        return transform.parent.DOMove(pos, timer)
            .SetEase(Ease.InOutSine);
    }
}
