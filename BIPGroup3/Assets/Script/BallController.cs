using UnityEngine;

public class BallController : MonoBehaviour
{
    private Rigidbody2D rb;
    private Collider2D myCollider;
    private bool isDestroyed = false; 

    [Header("拖尾特效设置")]
    // 【核心修改】：从单个组件升级为组件数组，支持同时控制无限个拖尾
    public TrailRenderer[] trailRenderers; 

    // 用于记录火箭当前同时身处多少个引力场内
    private int activeGravityFieldsCount = 0;

    [Header("外观设置")]
    public float spriteAngleOffset = -90f; 

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>(); 

        // 【核心修改】：游戏开始时，遍历并确保关闭所有拖尾的发射
        SetAllTrailsEmitting(false);
    }

    public void Launch(Vector2 launchVelocity)
    {
        rb.linearVelocity = Vector2.zero; 
        rb.angularVelocity = 0f;
        rb.linearVelocity = launchVelocity; 

        // 【核心修改】：发射瞬间，清空并关闭所有拖尾，防止拉丝
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
    }

    private void Update()
    {
        CheckOutOfBounds();
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        

        if (other.GetComponent<PointEffector2D>() != null)
        {
            if (other.GetComponent<GravityWell>() != null || other.GetComponentInParent<GravityWell>() != null)
            {
                return; 
            }

            activeGravityFieldsCount++;
            UpdateTrailState();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<PointEffector2D>() != null)
        {
            if (other.GetComponent<GravityWell>() != null || other.GetComponentInParent<GravityWell>() != null)
            {
                return; 
            }

            activeGravityFieldsCount--;
            if (activeGravityFieldsCount < 0) activeGravityFieldsCount = 0;
            
            UpdateTrailState();
        }
    }

    private void UpdateTrailState()
    {
        // 根据计数决定开启或关闭所有拖尾
        SetAllTrailsEmitting(activeGravityFieldsCount > 0);
    }

    // 【新增辅助方法】：一键统一控制所有拖尾的开关
    private void SetAllTrailsEmitting(bool isEmitting)
    {
        if (trailRenderers == null) return;

        foreach (TrailRenderer trail in trailRenderers)
        {
            if (trail != null)
            {
                trail.emitting = isEmitting;
            }
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