using UnityEngine;

public class MinimapFollow : MonoBehaviour
{
    [Header("跟随目标")]
    public Camera mainCamera;               // 跟着主相机移动
    public SpriteRenderer backgroundSprite; // 巨大的背景图，用于限制边界

    private Camera miniCam;

    void Start()
    {
        miniCam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (mainCamera == null || backgroundSprite == null) return;

        // 1. 获取主相机的位置
        Vector3 targetPos = mainCamera.transform.position;
        targetPos.z = transform.position.z; // 保持自己的Z轴深度不变

        // 2. 获取背景的物理边界
        Bounds bgBounds = backgroundSprite.bounds;

        // 3. 计算小地图相机自身的视野一半宽和高
        float camHalfHeight = miniCam.orthographicSize;
        float camHalfWidth = camHalfHeight * miniCam.aspect;

        // 4. 算出现在小地图相机的安全活动范围
        float minX = bgBounds.min.x + camHalfWidth;
        float maxX = bgBounds.max.x - camHalfWidth;
        float minY = bgBounds.min.y + camHalfHeight;
        float maxY = bgBounds.max.y - camHalfHeight;

        // 5. 强行限制不出界
        targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
        targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);

        // 6. 应用最终位置
        transform.position = targetPos;
    }
}