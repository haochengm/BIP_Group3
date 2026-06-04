using UnityEngine;

public class EllipticalMovement : MonoBehaviour
{
    [Header("轨道基本设置")]
    [Tooltip("移动速度。正数顺时针，负数逆时针，值越大转得越快")]
    public float speed = 1f;

    [Tooltip("椭圆的横向半径 (X轴长轴)")]
    public float xAxisRadius = 5f;

    [Tooltip("椭圆的纵向半径 (Y轴短轴)")]
    public float yAxisRadius = 3f;

    [Header("个性化微调")]
    [Tooltip("初始位置偏移（弧度制）。如果你有多个星球，改动这个值（比如0, 1.5, 3.14）可以让它们错开位置，不会同步运动。")]
    public float phaseOffset = 0f;

    private Vector2 centerPosition;
    private Rigidbody2D rb;

    private void Start()
    {
        // 1. 游戏开始时，将当前位置记录为椭圆的中心点
        centerPosition = transform.position;
        
        // 2. 尝试获取刚体组件
        rb = GetComponent<Rigidbody2D>();

        // 💡 避坑提示：如果星球带有 Rigidbody2D 物理组件，必须将其设为 Kinematic（运动学）模式
        // 这样物理引擎才能完美预测它的移动，保证飞船在引力圈内不会产生高频抖动
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    private void Update()
    {
        // 模式 A：如果星球身上没有 Rigidbody2D，直接在 Update 里修改 transform
        if (rb == null)
        {
            float timeFactor = Time.time * speed + phaseOffset;
            Vector3 nextPos = CalculateEllipsePosition(timeFactor);
            transform.position = new Vector3(nextPos.x, nextPos.y, transform.position.z);
        }
    }

    private void FixedUpdate()
    {
        // 模式 B：如果星球身上有 Rigidbody2D，在 FixedUpdate 里用物理方法移动，体验极佳
        if (rb != null)
            {
            float timeFactor = Time.fixedTime * speed + phaseOffset;
            Vector2 nextPos = CalculateEllipsePosition(timeFactor);
            rb.MovePosition(nextPos);
        }
    }

    // 数学核心：根据正弦和余弦函数公式计算椭圆坐标
    private Vector2 CalculateEllipsePosition(float time)
    {
        float x = centerPosition.x + Mathf.Cos(time) * xAxisRadius;
        float y = centerPosition.y + Mathf.Sin(time) * yAxisRadius;
        return new Vector2(x, y);
    }
}