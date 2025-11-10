using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class AdaptiveFOV : MonoBehaviour
{
    [Header("Cài đặt FOV")]
    [Tooltip("FOV cơ sở (Xem ghi chú về HFOV/VFOV)")]
    public float baseFOV = 30f; // Đã đổi tên từ baseHFOV để bớt nhầm lẫn

    [Tooltip("Resolution tham chiếu (Width x Height)")]
    public Vector2 baseResolution = new Vector2(1080, 1920);

    private Camera cam;

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = false; // Perspective
        AdjustFOV();
    }

    void Update()
    {
        // Xóa [ExecuteAlways] nếu bạn không muốn nó chạy liên tục trong Editor
        AdjustFOV();
    }

    void AdjustFOV()
    {
        if (cam == null) return;

        // --- SỬA ĐỔI BẮT ĐẦU TỪ ĐÂY ---

        // 1. Lấy vùng Safe Area
        Rect safeArea = Screen.safeArea;

        // 2. Dự phòng nếu safeArea chưa hợp lệ (ví dụ: trong Editor hoặc frame đầu)
        float safeWidth = (safeArea.width > 0) ? safeArea.width : Screen.width;
        float safeHeight = (safeArea.height > 0) ? safeArea.height : Screen.height;

        // 3. Tính toán Aspect Ratio dựa trên VÙNG AN TOÀN
        float currentAspect = safeWidth / safeHeight;

        // --- KẾT THÚC SỬA ĐỔI ---

        float baseAspect = baseResolution.x / baseResolution.y;

        // Chuyển base FOV sang rad
        float baseFovRad = baseFOV * Mathf.Deg2Rad;

        // Tính toán VFOV mới.
        // Logic này của bạn (Hor+) giữ cho Horizontal FOV không đổi
        // (dựa trên việc baseFOV=30 được coi là VFOV tại baseAspect)
        float vFovRad = 2f * Mathf.Atan(Mathf.Tan(baseFovRad / 2f) * (baseAspect / currentAspect));

        // Gán vertical FOV cho camera
        cam.fieldOfView = vFovRad * Mathf.Rad2Deg - 3.5f;
    }
}