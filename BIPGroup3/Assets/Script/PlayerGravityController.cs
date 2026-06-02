using UnityEngine;
using UnityEngine.UI; // 如果你想做个UI条显示剩余时间，可以引入这个

public class PlayerGravityController : MonoBehaviour
{
    [Header("引力点设置")]
    public GameObject gravityWellPrefab; // 拖入刚才制作的引力点预制体
    public float maxGravityTime = 5f;    // 玩家总共可用的引力时间

    [Header("状态监控 (只读)")]
    public float currentRemainingTime;
    
    // 静态变量：控制当前是否允许使用引力（大炮发射后开启）
    public static bool canUseGravity = false; 

    private GameObject activeGravityWell;

    private void Start()
    {
        // 游戏开始时，实例化一个引力点，但默认隐藏它
        activeGravityWell = Instantiate(gravityWellPrefab);
        activeGravityWell.SetActive(false);
        
        currentRemainingTime = maxGravityTime;
        canUseGravity = false;
    }

    private void Update()
    {
        // 1. 如果还没发射，或者时间用光了，就强制关闭引力点并停止检测
        if (!canUseGravity || currentRemainingTime <= 0f)
        {
            if (activeGravityWell.activeSelf)
            {
                activeGravityWell.SetActive(false);
            }
            return;
        }

        // 2. 发射后：按住鼠标左键，生成引力点并消耗时间
        if (Input.GetMouseButton(0))
        {
            ActivateGravityAtMouse();
            currentRemainingTime -= Time.deltaTime; // 每一帧扣除流逝的时间
        }
        else
        {
            // 松开鼠标左键时，隐藏引力点
            if (activeGravityWell.activeSelf)
            {
                activeGravityWell.SetActive(false);
            }
        }
    }

    private void ActivateGravityAtMouse()
    {
        activeGravityWell.SetActive(true);
        
        // 获取鼠标在世界空间的位置
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f; // 保持在 2D 平面

        // 将引力点移动到鼠标位置
        activeGravityWell.transform.position = mouseWorldPos;
    }

    // 提供一个重置方法，当关卡重新开始或重新发射时调用
    public void ResetGravityTime()
    {
        currentRemainingTime = maxGravityTime;
        canUseGravity = false;
        if (activeGravityWell != null) activeGravityWell.SetActive(false);
    }
}