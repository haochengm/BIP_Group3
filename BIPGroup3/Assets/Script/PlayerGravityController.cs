using UnityEngine;
using UnityEngine.UI;
public class PlayerGravityController : MonoBehaviour
{
    [Header("引力点设置")]
    public GameObject gravityWellPrefab; // 引力点预制体
    public float maxGravityTime = 2f;    // 电池总电量（5秒）
    public float cooldownDuration = 4f;  // 榨干后的冷却时间（5秒）

    [Header("UI 绑定设置")]
    // 【新增】：拖入你场景中的 Slider 组件
    public Slider energySlider;         // 电量滑动条 (绿条)
    public Slider cooldownSlider;       // 冷却滑动条 (红条)

    [Header("状态监控 (只读)")]
    public float currentRemainingTime;   // 当前剩余电量
    public float currentCooldownTime;    // 当前CD倒计时
    public bool isOnCooldown = false;    // 是否正在充电/冷却中
    
    // 静态变量：控制当前是否允许使用引力（大炮发射后由大炮脚本开启）
    public static bool canUseGravity = false; 

    private GameObject activeGravityWell;

    private void Start()
    {
        // 游戏开始时实例化引力点并隐藏
        activeGravityWell = Instantiate(gravityWellPrefab);
        activeGravityWell.SetActive(false);
        
        // 初始状态：电量满，无CD
        currentRemainingTime = maxGravityTime;
        currentCooldownTime = 0f;
        isOnCooldown = false;
        canUseGravity = false;

        // 【新增 UI 初始化】
        if (energySlider != null)
        {
            energySlider.maxValue = maxGravityTime; // 设置滑块最大值
            energySlider.value = maxGravityTime;    // 初始满格
            energySlider.gameObject.SetActive(false); // 发射前先隐藏电量条
        }
        if (cooldownSlider != null)
        {
            cooldownSlider.maxValue = cooldownDuration;
            cooldownSlider.value = 0f;
            cooldownSlider.gameObject.SetActive(false); // 初始隐藏冷却条
        }
    }

    private void Update()
    {
        // 如果游戏还没发射，强制关闭引力点并不响应逻辑
        if (!canUseGravity)
        {
            if (activeGravityWell.activeSelf) activeGravityWell.SetActive(false);
            return;
        }

        // 【新增 UI 控制】：一旦发射成功，立刻显示电量条
        if (energySlider != null && !energySlider.gameObject.activeSelf)
        {
            energySlider.gameObject.SetActive(true);
        }
        // ==========================================
        // 状态一：电量彻底耗尽，正在进行 5 秒强制冷却
        // ==========================================
        if (isOnCooldown)
        {
            if (activeGravityWell.activeSelf) activeGravityWell.SetActive(false);

            currentCooldownTime -= Time.deltaTime;
            
            // 【核心修改】：让红条从“空”慢慢“充满”
            // 原理：总冷却时间(5) - 剩余冷却时间(比如4) = 已经过去的时间(1)
            // 这样 Slider 的值就会从 0 慢慢增加到 5，呈现充满效果
            if (cooldownSlider != null)
            {
                cooldownSlider.value = cooldownDuration - currentCooldownTime;
            }
            
            if (currentCooldownTime <= 0f)
            {
                isOnCooldown = false;
                currentCooldownTime = 0f;
                currentRemainingTime = maxGravityTime; 

                // 冷却结束，隐藏红条
                if (cooldownSlider != null) cooldownSlider.gameObject.SetActive(false);
                Debug.Log("【引力包】电池已重新充满！");
            }
            
            // 处于CD时，绿条保持空格状态
            if (energySlider != null) energySlider.value = 0f;
            return;
        }

        // ==========================================
        // 状态二：正常用电状态 (只要没进入CD，就可以自由支配)
        // ==========================================
        if (Input.GetMouseButton(0))
        {
            // 只有当按住鼠标，且电量还大于0时，才执行扣电和生成引力
            if (currentRemainingTime > 0f)
            {
                ActivateGravityAtMouse();
                currentRemainingTime -= Time.deltaTime; // 严格根据按住的时间扣电

                // 检查扣电后是不是刚好耗尽了
                if (currentRemainingTime <= 0f)
                {
                    currentRemainingTime = 0f;
                    // 【核心触发点】：只有在此时，才会触发冷却机制！
                    isOnCooldown = true;
                    currentCooldownTime = cooldownDuration;
                    if (activeGravityWell.activeSelf) activeGravityWell.SetActive(false);

                    if (cooldownSlider != null)
                    {
                        cooldownSlider.gameObject.SetActive(true);
                        cooldownSlider.value = 0f;
                    }
                    Debug.Log("Gravity drained! Entering cooldown...");
                }
            }
        }
        else
        {
            // 如果玩家没有按住鼠标，立刻关闭引力点
            // 【注意】：这里没有任何触发CD或恢复电量的代码，实现了“松开暂停且不回复”的机制
            if (activeGravityWell.activeSelf)
            {
                activeGravityWell.SetActive(false);
            }
        }
        if (energySlider != null)
        {
            energySlider.value = currentRemainingTime;
        }
    }

    private void ActivateGravityAtMouse()
    {
        activeGravityWell.SetActive(true);
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        activeGravityWell.transform.position = mouseWorldPos;
    }

    // 重置关卡时调用的备用方法
    public void ResetGravityTime()
    {
        currentRemainingTime = maxGravityTime;
        currentCooldownTime = 0f;
        isOnCooldown = false;
        canUseGravity = false;
        if (activeGravityWell != null) activeGravityWell.SetActive(false);
    }
}   