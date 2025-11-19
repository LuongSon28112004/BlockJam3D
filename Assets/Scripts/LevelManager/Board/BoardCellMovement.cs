using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;

public class BoardCellMovement : MonoBehaviour
{
    [Header("Timing Settings")]
    private float timerPerCellMatrixSecond = 0.08f;
    private float distancePerCell = 1.25f;

    private float totalCell;

    /// <summary>
    /// Di chuyển qua các ô trên ma trận theo danh sách vị trí (Coroutine).
    /// </summary>
    public IEnumerator MovementPath(List<Vector3> containers, Action<bool> complete)
    {
        var parent = transform.parent;
        var boardCell = parent.GetComponent<BoardCell>();
        boardCell.BoardCellAnimation.SetRunning();

        if (containers == null || containers.Count == 0)
        {
            Debug.LogWarning("containers rỗng, không thể di chuyển.");
            complete(false);
            yield break;
        }

        totalCell = containers.Count;

        for (int i = 1; i < containers.Count; i++)
        {
            if (parent == null)
            {
                Debug.LogWarning("Không có parent!");
                complete(false);
                yield break;
            }

            Vector3 current = parent.position;
            Vector3 next = containers[i];
            Vector3 dir = (next - current).normalized;

            // ----- XÁC ĐỊNH GÓC XOAY DỰA TRÊN HƯỚNG -----
            float targetYRotation = parent.localEulerAngles.y;

            // Ưu tiên di chuyển theo trục nào có độ thay đổi lớn hơn
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.z))
            {
                // Di chuyển theo trục X
                if (dir.x > 0)
                    targetYRotation = -90f;  // phải
                else
                    targetYRotation = 90f;   // trái
            }
            else
            {
                // Di chuyển theo trục Z
                if (dir.z > 0)
                    targetYRotation = 180f;  // lên
                else
                    targetYRotation = 0f;    // xuống
            }

            // ----- XOAY CHỈ 1 LẦN -----
            parent.DOLocalRotate(new Vector3(0, targetYRotation, 0), 0.15f)
                  .SetEase(Ease.InSine);

            // ----- DI CHUYỂN -----
            Tween moveTween = parent.DOMove(next, timerPerCellMatrixSecond)
                                    .SetEase(Ease.InSine);

            yield return moveTween.WaitForCompletion();
        }

        // Sau khi đi hết path → đi đến cellPlay
        yield return StartCoroutine(MovementToCellPlay(boardCell.Pos));

        boardCell.BoardCellAnimation.SetIdle();
        boardCell.IsInCellPlay = true;

        complete(true);
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
            .SetEase(Ease.InSine);

        yield return moveTween.WaitForCompletion();
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

        if (transform.parent.position.x > pos.x)
        {
            // Di chuyển sang trái → quay sang trái
            transform.parent.DOLocalRotate(new Vector3(0, 60, 0), 0.15f).SetEase(Ease.InSine);
        }
        else if (transform.parent.position.x < pos.x)
        {
            // Di chuyển sang phải → quay sang phải
            transform.parent.DOLocalRotate(new Vector3(0, -60, 0), 0.15f).SetEase(Ease.InSine);
        }
        transform.parent.GetComponent<BoardCell>().BoardCellAnimation.SetRunning();
        // float distanceMagnitude = Vector3.Distance(pos, transform.parent.position);
        // float timer = (distanceMagnitude / distancePerCell) * timerPerCellMatrixSecond;
        Tween moveTween = transform.parent.DOMove(pos, timerPerCellMatrixSecond)
            .SetEase(Ease.InSine);

        yield return moveTween.WaitForCompletion();
        yield return new WaitForSeconds(0.1f);
        transform.parent.DOLocalRotate(new Vector3(0, 0, 0), 0.15f).SetEase(Ease.InSine);
        transform.parent.GetComponent<BoardCell>().BoardCellAnimation.SetIdle();

        Debug.Log("Đã hoàn thành MovementToPos.");
    }

    /// <summary>
    /// ⭐ HÀM MỚI: Trả về Tween để có thể chạy song song (dùng trong ShiftCellsLeft/Right).
    /// </summary>
    public Tween MovementToPosTween(Vector3 pos, bool isRunning = true, float timerOffset = 0)
    {
        if (transform.parent == null)
        {
            Debug.LogError("Không có transform.parent, không thể tạo Tween.");
            return null;
        }

        float distanceMagnitude = Vector3.Distance(pos, transform.parent.position);
        float timer = (distanceMagnitude / distancePerCell) * timerPerCellMatrixSecond;

        if (isRunning)
        {
            if (transform.parent.position.x > pos.x)
            {
                // Di chuyển sang trái → quay sang trái
                transform.parent.DOLocalRotate(new Vector3(0, 60, 0), 0.15f).SetEase(Ease.InSine);
            }
            else if (transform.parent.position.x < pos.x)
            {
                // Di chuyển sang phải → quay sang phải
                transform.parent.DOLocalRotate(new Vector3(0, -60, 0), 0.15f).SetEase(Ease.InSine);
            }
        }

        if (isRunning)
            transform.parent.GetComponent<BoardCell>().BoardCellAnimation.SetRunning();

        // Di chuyển và xoay lại sau khi di chuyển xong
        Tween moveTween;
        if (isRunning)
            moveTween = transform.parent.DOMove(pos, timer).SetEase(Ease.InSine);
        else
            moveTween = transform.parent.DOMove(pos, timerOffset).SetEase(Ease.InSine);

        // Xoay lại hướng 0° sau khi di chuyển xong
        moveTween.OnComplete(() =>
        {
            transform.parent.DOLocalRotate(Vector3.zero, 0.15f).SetEase(Ease.OutSine);
        });

        return moveTween;
    }


    public IEnumerator MovementToPosOwner()
    {
        if (transform.parent.GetComponent<BoardCell>().NeedUpdatePosAfter) yield break;
        transform.parent.GetComponent<BoardCell>().NeedUpdatePosAfter = true;
        Vector3 pos = transform.parent.GetComponent<BoardCell>().Pos;
        if (transform.parent == null)
        {
            Debug.LogWarning("Không có transform.parent, không thể di chuyển.");
            yield break;
        }

        if (transform.parent.position.x > pos.x)
        {
            // Di chuyển sang trái → quay sang trái
            transform.parent.DOLocalRotate(new Vector3(0, 60, 0), 0.15f).SetEase(Ease.InSine);
        }
        else if (transform.parent.position.x < pos.x)
        {
            // Di chuyển sang phải → quay sang phải
            transform.parent.DOLocalRotate(new Vector3(0, -60, 0), 0.15f).SetEase(Ease.InSine);
        }
        transform.parent.GetComponent<BoardCell>().BoardCellAnimation.SetRunning();
        // float distanceMagnitude = Vector3.Distance(pos, transform.parent.position);
        // float timer = (distanceMagnitude / distancePerCell) * timerPerCellMatrixSecond;
        Tween moveTween = transform.parent.DOMove(pos, timerPerCellMatrixSecond)
            .SetEase(Ease.Linear);

        yield return moveTween.WaitForCompletion();
        yield return new WaitForSeconds(0.1f);
        transform.parent.DOLocalRotate(new Vector3(0, 0, 0), 0.15f).SetEase(Ease.InSine);
        transform.parent.GetComponent<BoardCell>().BoardCellAnimation.SetIdle();
        transform.parent.GetComponent<BoardCell>().NeedUpdatePosAfter = false;

        Debug.Log("Đã hoàn thành MovementToPos.");
    }

    public IEnumerator MovementToPosNormal(Vector3 pos)
    {
        if (transform.parent == null)
        {
            Debug.LogWarning("Không có transform.parent, không thể di chuyển.");
            yield break;
        }

        if (transform.parent.position.x > pos.x)
        {
            // Di chuyển sang trái → quay sang trái
            transform.parent.DOLocalRotate(new Vector3(0, 60, 0), 0.15f).SetEase(Ease.InSine);
        }
        else if (transform.parent.position.x < pos.x)
        {
            // Di chuyển sang phải → quay sang phải
            transform.parent.DOLocalRotate(new Vector3(0, -60, 0), 0.15f).SetEase(Ease.InSine);
        }
        transform.parent.GetComponent<BoardCell>().BoardCellAnimation.SetRunning();
        // float distanceMagnitude = Vector3.Distance(pos, transform.parent.position);
        // float timer = (distanceMagnitude / distancePerCell) * timerPerCellMatrixSecond;
        Tween moveTween = transform.parent.DOMove(pos, timerPerCellMatrixSecond / 4)
            .SetEase(Ease.InSine);

        yield return moveTween.WaitForCompletion();
        // yield return new WaitForSeconds(0.1f);
        transform.parent.DOLocalRotate(new Vector3(0, 0, 0), 0.15f).SetEase(Ease.InSine);
        transform.parent.GetComponent<BoardCell>().BoardCellAnimation.SetIdle();

        Debug.Log("Đã hoàn thành MovementToPos.");
    }

    public Tween Knob()
    {
        if (transform.parent == null)
        {
            Debug.LogWarning("Không có transform.parent, không thể di chuyển.");
            return null;
        }

        return transform.parent.DOMoveY(2f, 0.14f).SetEase(Ease.InSine);
    }
}
