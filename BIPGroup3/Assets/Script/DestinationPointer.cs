using UnityEngine;
using UnityEngine.UI;

public class DestinationPointer : MonoBehaviour
{
    [Header("核心引用")]
    public Camera mainCamera;
    public RectTransform canvasRect;  
    
    [Header("目标设置")]
    // 【新增】：在 Inspector 里配置这个箭头要找哪个 Tag 的星球！
    public string targetTag = "Destination1"; 
    public float edgePadding = 50f;   

    private Transform destination;    
    private RectTransform pointerRect;
    private Image pointerImage;

    void Start()
    {
        pointerRect = GetComponent<RectTransform>();
        pointerImage = GetComponent<Image>();
        
        // 根据填写的 Tag 去寻找对应的星球
        GameObject destObj = GameObject.FindGameObjectWithTag(targetTag);
        if (destObj != null)
        {
            destination = destObj.transform;
        }
        else
        {
            Debug.LogWarning($"未找到Tag为 {targetTag} 的物体！请检查拼写。");
            pointerImage.enabled = false;
        }
    }

    void LateUpdate()
    {
        // 【新增防御逻辑】：如果星球已经被吃掉（隐藏或销毁），或者主相机没了，直接隐藏箭头
        if (mainCamera == null || destination == null || !destination.gameObject.activeInHierarchy)
        {
            pointerImage.enabled = false;
            return;
        }

        Vector3 targetWorldPos = destination.position;
        Vector3 screenPos = mainCamera.WorldToScreenPoint(targetWorldPos);

        bool isOffScreen = screenPos.x <= 0 || screenPos.x >= Screen.width || 
                           screenPos.y <= 0 || screenPos.y >= Screen.height;

        if (isOffScreen)
        {
            pointerImage.enabled = true;

            Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
            screenPos -= screenCenter;

            float angle = Mathf.Atan2(screenPos.y, screenPos.x);
            float slope = Mathf.Tan(angle);

            float x = screenPos.x;
            float y = screenPos.y;

            float maxX = (Screen.width / 2f) - edgePadding;
            float maxY = (Screen.height / 2f) - edgePadding;

            if (screenPos.x > 0)
            {
                x = maxX;
                y = slope * x;
            }
            else
            {
                x = -maxX;
                y = slope * x;
            }

            if (y > maxY)
            {
                y = maxY;
                x = y / slope;
            }
            else if (y < -maxY)
            {
                y = -maxY;
                x = y / slope;
            }

            screenPos = new Vector3(x, y, 0f) + screenCenter;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, null, out Vector2 localPoint);
            pointerRect.anchoredPosition = localPoint;

            float rotAngle = angle * Mathf.Rad2Deg;
            pointerRect.rotation = Quaternion.Euler(0, 0, rotAngle);
        }
        else
        {
            pointerImage.enabled = false;
        }
    }
}