using UnityEngine;

public class BallController : MonoBehaviour
{
    private Rigidbody2D rb;
    private Collider2D myCollider;
    private bool isDestroyed = false; 

    [Header("拖尾特效设置")]
    public TrailRenderer[] trailRenderers;
    private int activeGravityFieldsCount = 0;
    public ParticleSystem rechargeFX;

    [Header("外观设置")]
    public float spriteAngleOffset = -90f; 

    // ==========================================
    // 【新增】：绕星充能检测专属变量
    // ==========================================
    private Transform currentOrbitPlanet;    // 当前正在绕哪个星球飞行
    private float previousOrbitAngle;        // 上一帧飞船相对星球的角度
    private float accumulatedOrbitAngle;     // 已经累加旋转了多少度

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>();
        SetAllTrailsEmitting(false);
        if (rechargeFX != null) rechargeFX.Stop();
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
        currentOrbitPlanet = null; // 重置绕星数据
    }

    private void Update()
    {
        CheckOutOfBounds();

        // 【新增】：每帧检测是否在绕星
        TrackOrbitForRecharge();
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

    // ==========================================
    // 【核心新增】：轨道角度数学追踪
    // ==========================================
    private void TrackOrbitForRecharge()
    {
        // 只有被星球引力场捕获时才计算
        if (currentOrbitPlanet == null) return;

        // 1. 计算飞船当前相对于星球中心点的角度
        Vector2 dir = transform.position - currentOrbitPlanet.position;
        float currentAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // 2. 计算这一帧转过的角度差 (Mathf.DeltaAngle 非常聪明，能完美处理 180度 到 -180度 的跨越)
        float delta = Mathf.DeltaAngle(previousOrbitAngle, currentAngle);
        
        // 3. 将角度差累加
        accumulatedOrbitAngle += delta;
        previousOrbitAngle = currentAngle;

        // 4. 检查是否转满了一圈 (360度)
        if (Mathf.Abs(accumulatedOrbitAngle) >= 360f)
        {
            // 扣除这 360 度，如果玩家继续转圈，还能继续充能
            accumulatedOrbitAngle -= Mathf.Sign(accumulatedOrbitAngle) * 360f;

            // 呼叫大管家：充能！
            if (PlayerGravityController.Instance != null)
            {
                PlayerGravityController.Instance.RefillEnergy();
            }
            if (rechargeFX != null)
            {
                rechargeFX.Stop(); // 先停止可能正在播放的（安全重置）
                rechargeFX.Play(); // 嘭！烟花爆开
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PointEffector2D>() != null)
        {
            if (other.GetComponent<GravityWell>() != null || other.GetComponentInParent<GravityWell>() != null) return; 

            activeGravityFieldsCount++;
            UpdateTrailState();

            // 【新增】：当钻进星球引力圈时，开始记录轨道初始数据
            if (currentOrbitPlanet == null)
            {
                currentOrbitPlanet = other.transform;
                Vector2 dir = transform.position - currentOrbitPlanet.position;
                previousOrbitAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                accumulatedOrbitAngle = 0f; // 进度清零
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

            // 【新增】：当飞出当前星球引力圈时，清空绕圈数据，防止玩家作弊（比如转半圈飞走又飞回来凑一圈）
            if (currentOrbitPlanet == other.transform)
            {
                currentOrbitPlanet = null;
                accumulatedOrbitAngle = 0f;
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

    // 边界和死亡逻辑保持不变
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