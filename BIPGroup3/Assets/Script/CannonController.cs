using UnityEngine;

public class CannonController : MonoBehaviour
{
    [Header("references")]
    public GameObject ballPrefab;       // 炮弹的预制体
    public Transform launchPoint;       // 发射点位置

    [Header("launch settings")]
    public float fixedForce = 15f;      // 固定的发射速度/力度

    private bool isDragging = false;    // 标记是否正在拖拽炮口
    private bool hasFired = false;      // 标记是否已经发射过了
    private void Update()
    {
        if (hasFired) return; // 如果已经发射过了，就不再处理拖拽
        HandleDragAiming();
        
        // 【可选】为了方便你在编辑器里测试，保留了键盘空格键发射，正式版可以用UI按钮
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Fire();
        }
    }

    // 核心逻辑：只有鼠标点击并拖拽时，大炮才会旋转
    private void HandleDragAiming()
    {
        // 1. 当玩家按下鼠标左键
        if (Input.GetMouseButtonDown(0))
        {
            // 检查鼠标是否点击在大炮附近（这里通过射线检测是否有碰撞体，或者简单地只要点击就允许拖拽）
            // 为了最好的拖拽手感，我们检查鼠标点击位置与大炮中心的距离
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0f;

            float distance = Vector2.Distance(mouseWorldPos, transform.position);
            
            // 假设大炮控制范围是 3 个单位以内（你可以根据大炮大小调整这个数值）
            if (distance < 3f)
            {
                isDragging = true;
            }
        }

        // 2. 当玩家放开鼠标左键，停止拖拽
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        // 3. 如果正在拖拽中，更新大炮角度
        if (isDragging)
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0f;

            // 计算大炮指向鼠标的方向
            Vector2 lookDirection = (Vector2)mouseWorldPos - (Vector2)transform.position;
            
            // 计算旋转角度
            float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;

            // 旋转大炮 (假设你的炮管图片默认朝向右侧 X 轴)
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    // 公共发射方法：可以被 Unity 的 UI Button 直接绑定调用
    public void Fire()
    {
        if (ballPrefab == null || launchPoint == null || hasFired) return;

        GameObject spawnedBall = Instantiate(ballPrefab, launchPoint.position, Quaternion.identity);
        BallController ballController = spawnedBall.GetComponent<BallController>();

        if (ballController != null)
        {
            Vector2 launchVelocity = launchPoint.right * fixedForce;
            ballController.Launch(launchVelocity);
            
            PlayerGravityController.canUseGravity = true;
            hasFired = true; 

            CameraFollow camFollow = Camera.main.GetComponent<CameraFollow>();
            if (camFollow != null) camFollow.target = spawnedBall.transform;

            // 【新增这一行】：通知大管家，飞船上天了，立刻掐表计时！
            GameManager.Instance.StartTimer(); 
        }
    }
}