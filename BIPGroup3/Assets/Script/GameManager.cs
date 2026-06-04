using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // 引入 TextMeshPro 命名空间
using UnityEngine.UI;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("关卡与背景设置")]
    public SpriteRenderer backgroundSprite; 

    [Header("UI 文本绑定 (TextMeshPro)")]
    public TextMeshProUGUI currentTimerText;  // 显示当前跑表时间
    public TextMeshProUGUI bestTimeText;      // 显示历史最佳时间

    // 【新增】：用于显示通关结果的文本（你可以把这个放到通关面板UI里）
    public TextMeshProUGUI levelClearResultText;
    public GameObject levelClearPanel;         // 【新增】：通关结算UI面板物体
    public Image levelClearResultImage;          // 【新增】：通关结果的图片组件（可以放在通关面板里）
    public Sprite resultSprite1; // 【新增】：不同终点的通关结果图（可选）
    public Sprite resultSprite2;        
    public Sprite resultSprite3;
    public GameObject levelFailPanel;          // 【新增】：失败结算UI面板物体
    // 计时相关的私有变量
    private float currentTime = 0f;
    private bool isTimerRunning = false;
    private string bestTimeKey;               // 用于动态生成本地保存的 Key

    // 【新增】：用于记录玩家最后到达的是哪个终点 (通过Tag区分)
    private string lastReachedDestinationTag; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 根据当前关卡的名称动态生成保存 Key
        bestTimeKey = "BestTime_" + SceneManager.GetActiveScene().name;
    }

    private void Start()
    {
        // 游戏开始时，读取并显示历史最佳纪录
        DisplayBestTime();

        // 【新增 UI 初始化】：确保通关面板隐藏
        if (levelClearPanel != null) levelClearPanel.SetActive(false);
        if (levelFailPanel != null) levelFailPanel.SetActive(false);
    }

    private void Update()
    {
        if (isTimerRunning)
        {
            currentTime += Time.deltaTime;
            UpdateTimerUI(currentTimerText, currentTime);
        }
    }

    // ==========================================
    // 核心控制方法
    // ==========================================

    public void StartTimer()
    {
        currentTime = 0f;
        isTimerRunning = true;
        if (currentTimerText != null) currentTimerText.gameObject.SetActive(true);
        
        // 【新增状态重置】：开始计时前清空上一个终点记录
        lastReachedDestinationTag = null;
        if (levelClearPanel != null) levelClearPanel.SetActive(false);
        if (levelFailPanel != null) levelFailPanel.SetActive(false);

        Debug.Log("【计时器】开始计时...");
    }

    public void GameOver()
    {
        if (levelFailPanel != null) levelFailPanel.SetActive(true);
        isTimerRunning = false; // 失败了，停止计时
        Debug.Log("GameManager：游戏失败！");
        //Invoke("RestartLevel", 1f);
    }

    // 【新增/升级】：当飞船碰到任何一个目标星球时被呼叫
    public void OnDestinationReached(GameObject planet)
    {
        // 1. 【核心机制】：如果已经通关，不再处理（防止重复触发）
        if (!isTimerRunning) return;

        // 2. 【核心状态保存】：记录玩家最后到达的是哪个终点的 Tag
        lastReachedDestinationTag = planet.tag;
        Debug.Log($"【通关触发】玩家到达终点: {lastReachedDestinationTag}");

        // 3. 【核心通关呼叫】：只要到达一个终点，立刻呼叫通关逻辑
        LevelClear();
    }

    // 通关逻辑
    public void LevelClear()
    {
        // 如果计时器已经在运行，且还没有停止过（即防止重复触发）
        if (!isTimerRunning) return;
        
        isTimerRunning = false; // 【核心】：停止计时
        Debug.Log($"【胜利】通关成功！最终用时：{currentTime:F2}秒");

        // 核心：检查并保存最佳纪录
        CheckAndSaveBestTime();

        // 【核心新增】：根据最后终点，显示特定的通关结果
        ShowSpecificLevelClearResult();
    }

    // ==========================================
    // 数学格式化与数据存储逻辑
    // ==========================================

    private void UpdateTimerUI(TextMeshProUGUI textComponent, float timeToDisplay)
    {
        if (textComponent == null) return;
        int minutes = Mathf.FloorToInt(timeToDisplay / 60f);
        int seconds = Mathf.FloorToInt(timeToDisplay % 60f);
        int milliseconds = Mathf.FloorToInt((timeToDisplay * 100f) % 100f);
        textComponent.text = string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
    }

    private void CheckAndSaveBestTime()
    {
        float previousBest = PlayerPrefs.GetFloat(bestTimeKey, float.MaxValue);
        if (currentTime < previousBest)
        {
            PlayerPrefs.SetFloat(bestTimeKey, currentTime);
            PlayerPrefs.Save(); 
            Debug.Log("【新纪录】恭喜打破历史最佳纪录！");
        }
        DisplayBestTime();
    }

    private void DisplayBestTime()
    {
        if (bestTimeText == null) return;
        if (PlayerPrefs.HasKey(bestTimeKey))
        {
            float bestTime = PlayerPrefs.GetFloat(bestTimeKey);
            bestTimeText.gameObject.SetActive(true);
            int minutes = Mathf.FloorToInt(bestTime / 60f);
            int seconds = Mathf.FloorToInt(bestTime % 60f);
            int milliseconds = Mathf.FloorToInt((bestTime * 100f) % 100f);
            bestTimeText.text = "BestTime: " + string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
        }
        else
        {
            bestTimeText.text = "BestTime: --:--.--";
        }
    }

    // 【核心新增】：根据最后终点，在通关面板上显示不同的文本
    private void ShowSpecificLevelClearResult()
    {
        if (levelClearPanel == null) return;

        // 呼出通关 UI 面板
        levelClearPanel.SetActive(true);

        // 格式化当前时间
        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);
        int milliseconds = Mathf.FloorToInt((currentTime * 100f) % 100f);
        string clearTimeStr = string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);

        // 如果保留了文字组件，可以让它专门用来显示最终成绩
        if (levelClearResultText != null)
        {
            levelClearResultText.text = "最终通关用时: " + clearTimeStr;
        }

        // 【核心修改】：使用 Switch 语句根据 Tag 决定给 UI Image 塞哪张图片
        if (levelClearResultImage != null)
        {
            switch (lastReachedDestinationTag)
            {
                case "Destination1":
                    levelClearResultImage.sprite = resultSprite1;
                    break;
                case "Destination2":
                    levelClearResultImage.sprite = resultSprite2;
                    break;
                case "Destination3":
                    levelClearResultImage.sprite = resultSprite3;
                    break;
                default:
                    Debug.LogWarning("【未知通关状态】玩家到达了一个没有预设图片的终点。");
                    break;
            }
        }
    }

    private void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 【重试按钮呼叫】：供通关 UI 面板上的“重试按钮”呼叫
    public void ResetGame()
    {
        RestartLevel();
    }
}