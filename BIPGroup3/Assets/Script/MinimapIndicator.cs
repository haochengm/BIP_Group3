using UnityEngine;

public class MinimapIndicator : MonoBehaviour
{
    [Header("相机引用")]
    public Camera mainCamera;         // 主相机
    public Camera minimapCamera;      // 【升级】：把小地图工具人相机拖到这里，用它算边界更精准！

    [Header("UI 引用")]
    public RectTransform minimapUI;   // 小地图 UI (Raw Image)

    private RectTransform myRect;

    void Start()
    {
        myRect = GetComponent<RectTransform>();
    }

    void LateUpdate()
    {
        if (mainCamera == null || minimapCamera == null || minimapUI == null) return;

        // 1. 计算主相机的实际视野大小 (世界单位)
        float mainCamHeight = mainCamera.orthographicSize * 2f;
        float mainCamWidth = mainCamHeight * mainCamera.aspect;

        // 2. 计算小地图相机的实际视野大小 (世界单位)
        float miniCamHeight = minimapCamera.orthographicSize * 2f;
        float miniCamWidth = miniCamHeight * minimapCamera.aspect;

        // 3. 计算主相机占小地图相机视野的百分比比例
        float widthRatio = mainCamWidth / miniCamWidth;
        float heightRatio = mainCamHeight / miniCamHeight;

        // 4. 根据比例，计算出高亮框在 UI 里的真实像素大小
        myRect.sizeDelta = new Vector2(minimapUI.rect.width * widthRatio, minimapUI.rect.height * heightRatio);

        // 5. 计算主相机在小地图相机视野中的相对百分比坐标 (0 到 1)
        float miniCamMinX = minimapCamera.transform.position.x - (miniCamWidth / 2f);
        float miniCamMinY = minimapCamera.transform.position.y - (miniCamHeight / 2f);

        float pctX = (mainCamera.transform.position.x - miniCamMinX) / miniCamWidth;
        float pctY = (mainCamera.transform.position.y - miniCamMinY) / miniCamHeight;

        // 6. 将百分比坐标映射到小地图 UI 的像素坐标系中
        float uiX = (pctX * minimapUI.rect.width) - (minimapUI.rect.width / 2f);
        float uiY = (pctY * minimapUI.rect.height) - (minimapUI.rect.height / 2f);

        // 7. 赋予高亮框，完美同步
        myRect.anchoredPosition = new Vector2(uiX, uiY);
    }
}