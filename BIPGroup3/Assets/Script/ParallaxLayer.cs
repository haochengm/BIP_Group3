using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [Header("视差系数 (0 = 不跟随, 1 = 完全跟随摄像机)")]
    [Range(0f, 1f)]
    public float parallaxFactor;

    private Transform cam;
    private Vector3 startPosition;

    void Start()
    {
        // 找到主摄像机
        cam = Camera.main.transform;
        // 记录背景层的初始位置
        startPosition = transform.position;
    }

    void LateUpdate()
    {
        if (cam == null) return;

        // 计算摄像机相对于初始位置移动了多远
        Vector2 distance = new Vector2(cam.position.x, cam.position.y) * parallaxFactor;

        // 将背景层移动相应的距离 (保持原有的 Z 轴不变)
        transform.position = new Vector3(startPosition.x + distance.x, startPosition.y + distance.y, transform.position.z);
    }
}