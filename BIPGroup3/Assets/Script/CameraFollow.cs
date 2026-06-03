using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("跟随目标")]
    public Transform target; // 要跟随的飞船

    [Header("跟随设置")]
    public float smoothTime = 0.3f;
    public Vector3 offset = new Vector3(0f, 0f, -20f);

    [Header("边界限制")]
    public SpriteRenderer backgroundSprite; // 把你的巨大背景图拖到这里

    private Vector3 velocity = Vector3.zero;
    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // 1. 计算摄像机原本想去的目标位置
        Vector3 targetPosition = target.position + offset;

        // 2. 如果配置了背景，则进行边界限制
        if (backgroundSprite != null)
        {
            // 获取背景图的物理边界
            Bounds bgBounds = backgroundSprite.bounds;

            // 计算摄像机视野的一半高度和一半宽度
            float camHalfHeight = cam.orthographicSize;
            float camHalfWidth = camHalfHeight * cam.aspect;

            // 计算摄像机中心点允许移动的最大和最小坐标
            // (背景边缘坐标 向内收缩 摄像机的一半视野)
            float minX = bgBounds.min.x + camHalfWidth;
            float maxX = bgBounds.max.x - camHalfWidth;
            float minY = bgBounds.min.y + camHalfHeight;
            float maxY = bgBounds.max.y - camHalfHeight;

            // 使用 Mathf.Clamp 强行把 X 和 Y 限制在这个安全范围内
            targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
        }

        // 3. 使用 SmoothDamp 平滑移动到限制后的安全位置
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }
}