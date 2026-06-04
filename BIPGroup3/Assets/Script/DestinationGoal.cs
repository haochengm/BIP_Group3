using UnityEngine;

// 挂载要求：必须和 2D Collider (Is Trigger) 在同一个物体上
public class DestinationGoal : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 最稳妥的判断：只要撞到我的是玩家的飞船
        if (collision.GetComponent<BallController>() != null)
        {
            // 通知大管家：到达了一个目标！
            GameManager.Instance.OnDestinationReached(gameObject);
        }
    }
}