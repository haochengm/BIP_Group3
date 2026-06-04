using UnityEngine;
using System.Collections.Generic; // 【重要】：引入列表系统

public class BallController : MonoBehaviour
{
    private Rigidbody2D rb;
    private Collider2D myCollider;
    private bool isDestroyed = false; 

    [Header("外观与特效设置")]
    public float spriteAngleOffset = -90f; 
    public TrailRenderer[] trailRenderers; 
    public ParticleSystem rechargeFX; 

    private int activeGravityFieldsCount = 0;

    // ==========================================
    // 【核心数据结构】：为每个星球定制的独立“角度账本”
    // ==========================================
    private class OrbitTracker
    {
        public Transform planet;         // 目标星球
        public float previousAngle;     // 上一帧飞船相对该星球的角度
        public float accumulatedAngle;  // 已经相对该星球累加转过了多少度

        public OrbitTracker(Transform planetTransform, Vector3 rocketPos)
        {
            planet = planetTransform;
            // 初始化这一瞬间的相对角度
            Vector2 dir = rocketPos - planet.position;
            previousAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            accumulatedAngle = 0f;
        }

        // 每帧更新角度，转满一圈时通过 Action 回调通知火箭
        public void UpdateOrbit(Vector3 rocketPos, System.Action onCompleteCircle)
        {
            if (planet == null) return;

            // 计算当前帧的相对角度
            Vector2 dir = rocketPos - planet.position;
            float currentAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            // 计算与上一帧的角度差（自动处理180到-180的跨越）
            float delta = Mathf.DeltaAngle(previousAngle, currentAngle);
            
            accumulatedAngle += delta;
            previousAngle = currentAngle;

            // 检查该星球的独立进度是否达到一圈
            if (Mathf.Abs(accumulatedAngle) >= 360f)
            {
                accumulatedAngle -= Mathf.Sign(accumulatedAngle) * 360f; // 扣除一圈，允许继续套圈
                onCompleteCircle?.Invoke(); // 触发充能回调
            }
        }
    }

    // 【核心容器】：当前正在同时追踪的所有星球账本列表
    private List<OrbitTracker> activeOrbitTrackers = new List<OrbitTracker>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>(); 
        SetAllTrailsEmitting(false);
    }

    public void Launch(Vector2 launchVelocity)
    {
        rb.linearVelocity = Vector2.zero; 
        rb.angularVelocity = 0f;
        rb.linearVelocity = launchVelocity; 

        if (trailRenderers != null)
        {
            foreach (TrailRenderer trail in trailRenderers)
            {
                if (trail != null)
                {
                    trail.Clear();          
                    trail.emitting = false; 
                }
            }
        }
        activeGravityFieldsCount = 0;

        // 发射时务必清空所有未完成的轨道记录
        activeOrbitTrackers.Clear();
    }

    private void Update()
    {
        CheckOutOfBounds();

        // 【核心修改】：每帧遍历列表，独立更新所有当前身处引力场内的星球进度
        TrackAllActiveOrbits();
    }

    private void FixedUpdate()
    {
        if (PlayerGravityController.canUseGravity)
        {
            RotateTowardsVelocityPhysics();
        }
    }

    private void RotateTowardsVelocityPhysics()
    {
        Vector2 velocity = rb.linearVelocity;
        if (velocity.sqrMagnitude > 0.05f)
        {
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            rb.MoveRotation(angle + spriteAngleOffset);
        }
    }

    // 同时追踪所有身处引力圈的星球
    private void TrackAllActiveOrbits()
    {
        // 倒序遍历列表，这样在运行中如果有星球被销毁或移除，不会引发索引报错
        for (int i = activeOrbitTrackers.Count - 1; i >= 0; i--)
        {
            var tracker = activeOrbitTrackers[i];
            
            // 安全防错：如果星球本身被隐藏或销毁了，移出列表
            if (tracker.planet == null || !tracker.planet.gameObject.activeInHierarchy)
            {
                activeOrbitTrackers.RemoveAt(i);
                continue;
            }

            // 更新这个星球的角度，并传入充能成功后要执行的“烟花代码”
            tracker.UpdateOrbit(transform.position, () => 
            {
                // 1. 电池充能
                if (PlayerGravityController.Instance != null)
                {
                    PlayerGravityController.Instance.RefillEnergy();
                }

                // 2. 烟花爆发
                if (rechargeFX != null)
                {
                    rechargeFX.Stop();
                    rechargeFX.Play();
                }
            });
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PointEffector2D>() != null)
        {
            if (other.GetComponent<GravityWell>() != null || other.GetComponentInParent<GravityWell>() != null) return; 

            activeGravityFieldsCount++;
            UpdateTrailState();

            // 【核心修复】：进入引力圈时，只要列表中没有这个星球，就为它单独新建一个账本
            if (!IsAlreadyTracking(other.transform))
            {
                activeOrbitTrackers.Add(new OrbitTracker(other.transform, transform.position));
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<PointEffector2D>() != null)
        {
            if (other.GetComponent<GravityWell>() != null || other.GetComponentInParent<GravityWell>() != null) return; 

            activeGravityFieldsCount--;
            if (activeGravityFieldsCount < 0) activeGravityFieldsCount = 0;
            
            UpdateTrailState();

            // 【核心修复】：离开某个引力圈时，精准地将对应的星球账本销毁，其余重叠星球的账本不受影响！
            RemoveOrbitTracker(other.transform);
        }
    }

    // 辅助检查：是否已经在追踪某个星球
    private bool IsAlreadyTracking(Transform planetTransform)
    {
        foreach (var tracker in activeOrbitTrackers)
        {
            if (tracker.planet == planetTransform) return true;
        }
        return false;
    }

    // 辅助移除：删除指定星球的账本
    private void RemoveOrbitTracker(Transform planetTransform)
    {
        for (int i = activeOrbitTrackers.Count - 1; i >= 0; i--)
        {
            if (activeOrbitTrackers[i].planet == planetTransform)
            {
                activeOrbitTrackers.RemoveAt(i);
            }
        }
    }

    private void UpdateTrailState()
    {
        SetAllTrailsEmitting(activeGravityFieldsCount > 0);
    }

    private void SetAllTrailsEmitting(bool isEmitting)
    {
        if (trailRenderers == null) return;
        foreach (TrailRenderer trail in trailRenderers)
        {
            if (trail != null) trail.emitting = isEmitting;
        }
    }

    private void CheckOutOfBounds()
    {
        if (isDestroyed) return;
        if (GameManager.Instance == null || GameManager.Instance.backgroundSprite == null) return;

        Bounds bgBounds = GameManager.Instance.backgroundSprite.bounds;
        Bounds shipBounds = myCollider.bounds;

        if (shipBounds.max.x < bgBounds.min.x || 
            shipBounds.min.x > bgBounds.max.x || 
            shipBounds.max.y < bgBounds.min.y || 
            shipBounds.min.y > bgBounds.max.y)
        {
            TriggerOutOfBoundsFailure();
        }
    }

    private void TriggerOutOfBoundsFailure()
    {
        isDestroyed = true;
        rb.linearVelocity = Vector2.zero;
        gameObject.SetActive(false); 
        GameManager.Instance.GameOver();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Hazard"))
        {
            TriggerOutOfBoundsFailure();
        }
    }
}