using UnityEngine;

public class BallController : MonoBehaviour
{
    
    private Rigidbody2D rb;
    private Collider2D myCollider;
    private bool isDestroyed = false;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>();
    }


    public void Launch(Vector2 launchVelocity)
    {
        // Clean physics state before applying new velocity
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;


        // stable application of launch velocity
        rb.linearVelocity = launchVelocity;
    }
    private void Update()
    {
        CheckOutOfBounds();
    }

    private void CheckOutOfBounds()
    {
        if (isDestroyed) return;

        // 确保大管家和背景图都存在
        if (GameManager.Instance == null || GameManager.Instance.backgroundSprite == null) return;

        // 1. 获取背景的边界
        Bounds bgBounds = GameManager.Instance.backgroundSprite.bounds;
        // 2. 获取飞船自身的精确边界（算上了飞船的大小/半径）
        Bounds shipBounds = myCollider.bounds;

        // 3. 核心数学判定：完全出界的条件
        // 飞船的右边缘(max.x) 小于 背景的左边缘(min.x) -> 从左边完全飞出
        // 飞船的左边缘(min.x) 大于 背景的右边缘(max.x) -> 从右边完全飞出
        // 飞船的上边缘(max.y) 小于 背景的下边缘(min.y) -> 从下边完全飞出
        // 飞船的下边缘(min.y) 大于 背景的上边缘(max.y) -> 从上边完全飞出
        if (shipBounds.max.x < bgBounds.min.x ||
            shipBounds.min.x > bgBounds.max.x ||
            shipBounds.max.y < bgBounds.min.y ||
            shipBounds.min.y > bgBounds.max.y)
        {
            Debug.Log("BallController：飞船完全出界了，触发失败！");
            TriggerOutOfBoundsFailure();
        }


    }
    
    private void TriggerOutOfBoundsFailure()
    {
        isDestroyed = true;
        rb.linearVelocity = Vector2.zero;
        gameObject.SetActive(false); // 隐藏飞船
        
        // 呼叫大管家
        GameManager.Instance.GameOver1();
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Hazard"))
        {
            rb.linearVelocity = Vector2.zero;
            GameManager.Instance.GameOver();
            gameObject.SetActive(false); // 先隐藏球，避免它继续碰撞

        }
        
        if (collision.gameObject.CompareTag("Destination"))
        {
            rb.linearVelocity = Vector2.zero;
            GameManager.Instance.GameWin();
            gameObject.SetActive(false); // 先隐藏球，避免它继续碰撞
        }
    }
}