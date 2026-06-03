using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // 【重要】：引入 TextMeshPro 命名空间

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("关卡与背景设置")]
    public SpriteRenderer backgroundSprite; 

    [Header("UI 文本绑定 (TextMeshPro)")]
    public TextMeshProUGUI currentTimerText;  // 显示当前跑表时间
    public TextMeshProUGUI bestTimeText;      // 显示历史最佳时间

    // 计时相关的私有变量
    private float currentTime = 0f;
    private bool isTimerRunning = false;
    private string bestTimeKey;               // 用于动态生成本地保存的 Key

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 根据当前关卡的名称动态生成保存 Key，这样不同关卡的时间不会冲突
        bestTimeKey = "BestTime_" + SceneManager.GetActiveScene().name;
    }

    private void Start()
    {
        // 游戏开始时，读取并显示历史最佳纪录
        DisplayBestTime();
    }

    private void Update()
    {
        // 如果计时器正在运行，累加时间并更新UI
        if (isTimerRunning)
        {
            currentTime += Time.deltaTime;
            UpdateTimerUI(currentTimerText, currentTime);
        }
    }

    // ==========================================
    // 核心控制方法 (供外部脚本呼叫)
    // ==========================================

    // 由大炮脚本在发射成功时呼叫
    public void StartTimer()
    {
        currentTime = 0f;
        isTimerRunning = true;
        if (currentTimerText != null) currentTimerText.gameObject.SetActive(true);
        Debug.Log("【计时器】开始计时...");
    }

    // 失败处理
    public void GameOver()
    {
        isTimerRunning = false; // 失败了，停止计时
        Debug.Log("GameManager：游戏失败！");
        Invoke("RestartLevel", 1f);
    }
    public void GameOver1()
    {
        Debug.Log("GameManager：lost in space！");
        Invoke("RestartLevel", 1f);
    }
    // 胜利处理 (由你自己的终点触发脚本呼叫)
    
    public void LevelClear()
    {
        if (!isTimerRunning) return; // 防止重复触发
        
        isTimerRunning = false; // 停止计时
        Debug.Log($"【胜利】通关成功！最终用时：{currentTime:F2}秒");

        // 核心：检查并保存最佳纪录
        CheckAndSaveBestTime();

        // 可以在这里写呼出胜利结算UI的逻辑，这里我们先简单地延迟进入下一关或重启
        // Invoke("RestartLevel", 3f); 
    }

    // ==========================================
    // 数学格式化与数据存储逻辑
    // ==========================================

    // 将 float 秒数 格式化为 00:00.00 (分:秒.毫秒) 并赋给 UI
    private void UpdateTimerUI(TextMeshProUGUI textComponent, float timeToDisplay)
    {
        if (textComponent == null) return;

        int minutes = Mathf.FloorToInt(timeToDisplay / 60f);
        int seconds = Mathf.FloorToInt(timeToDisplay % 60f);
        int milliseconds = Mathf.FloorToInt((timeToDisplay * 100f) % 100f);

        // 格式化字符串：確保永远是两位数
        textComponent.text = string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
    }

    // 检查并保存最佳纪录
    private void CheckAndSaveBestTime()
    {
        // 读取历史时间，如果从来没通关过，默认给个极大值（float.MaxValue）
        float previousBest = PlayerPrefs.GetFloat(bestTimeKey, float.MaxValue);

        // 如果当前时间比历史最佳还要快（数值更小）
        if (currentTime < previousBest)
        {
            // 持久化保存到本地设备
            PlayerPrefs.SetFloat(bestTimeKey, currentTime);
            PlayerPrefs.Save(); // 强制刷新写入磁盘
            Debug.Log("【新纪录】恭喜打破历史最佳纪录！");
        }

        // 刷新最佳时间的显示
        DisplayBestTime();
    }

    // 读取并显示历史纪录
    private void DisplayBestTime()
    {
        if (bestTimeText == null) return;

        // 检查本地有没有这个关卡的保存纪录
        if (PlayerPrefs.HasKey(bestTimeKey))
        {
            float bestTime = PlayerPrefs.GetFloat(bestTimeKey);
            bestTimeText.gameObject.SetActive(true);
            
            // 格式化并显示
            int minutes = Mathf.FloorToInt(bestTime / 60f);
            int seconds = Mathf.FloorToInt(bestTime % 60f);
            int milliseconds = Mathf.FloorToInt((bestTime * 100f) % 100f);
            bestTimeText.text = "Best Time: " + string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
        }
        else
        {
            // 如果是新关卡，显示无纪录
            bestTimeText.text = "Best Time: --:--.--";
        }
    }

    private void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 【方便你测试用】：在编辑器里按 K 键可以清空本地所有保存的纪录
    [ContextMenu("Clear All Best Times")]
    public void ClearPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        Debug.Log("【测试】本地所有保存的通关纪录已被清空！");
    }
}
    

    