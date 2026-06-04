using UnityEngine;
using UnityEngine.UI;

public class PlayerGravityController : MonoBehaviour
{
    // 【新增】：单例模式，方便飞船脚本直接呼叫充能
    public static PlayerGravityController Instance { get; private set; } 

    [Header("引力点设置")]
    public GameObject gravityWellPrefab; 
    public float maxGravityTime = 15f;    

    [Header("UI 绑定设置")]
    public Slider energySlider;         // 电量滑动条 (绿条)
    // 注意：之前的 cooldownSlider (红条) 已经不需要了，你可以从代码和UI里删掉它

    [Header("状态监控 (只读)")]
    public float currentRemainingTime;   
    public static bool canUseGravity = false; 

    private GameObject activeGravityWell;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        activeGravityWell = Instantiate(gravityWellPrefab);
        activeGravityWell.SetActive(false);
        
        currentRemainingTime = maxGravityTime;
        canUseGravity = false;

        if (energySlider != null)
        {
            energySlider.maxValue = maxGravityTime; 
            energySlider.value = maxGravityTime;    
            energySlider.gameObject.SetActive(false); 
        }
    }

    private void Update()
    {
        if (!canUseGravity)
        {
            if (activeGravityWell.activeSelf) activeGravityWell.SetActive(false);
            return;
        }

        if (energySlider != null && !energySlider.gameObject.activeSelf)
        {
            energySlider.gameObject.SetActive(true);
        }

        // ==========================================
        // 纯粹的电池消耗逻辑 (没有自动CD了)
        // ==========================================
        if (Input.GetMouseButton(0))
        {
            if (currentRemainingTime > 0f)
            {
                ActivateGravityAtMouse();
                currentRemainingTime -= Time.deltaTime; 
                
                if (currentRemainingTime <= 0f)
                {
                    currentRemainingTime = 0f;
                    if (activeGravityWell.activeSelf) activeGravityWell.SetActive(false);
                    // 电量耗尽后，不再触发强制CD，只能等玩家去绕圈充能
                }
            }
        }
        else
        {
            if (activeGravityWell.activeSelf) activeGravityWell.SetActive(false);
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

    // ==========================================
    // 【核心新增】：暴露给外部的完美充能接口
    // ==========================================
    public void RefillEnergy()
    {
        currentRemainingTime = maxGravityTime;
        Debug.Log("【技巧达成】完美绕星一圈！引力电池已重新充满！");
    }
}