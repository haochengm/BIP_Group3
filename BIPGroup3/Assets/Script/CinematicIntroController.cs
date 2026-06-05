using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement; // 新增：用于场景切换
using TMPro; // 新增：用于使用 TextMeshPro 文本

/// <summary>
/// Plays a sequence of intro/office videos with keyboard navigation.
/// Uses an active background double-buffer to eliminate flicker and fixes Unity's background cache navigation bugs.
/// </summary>
public class CinematicIntroController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The VideoPlayer in this scene. Drag it here from the Hierarchy.")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Final frame PNG overlay")]
    [Tooltip("Fullscreen UI Image (PNG). Hidden during videos; shown only via ShowFinalPNG().")]
    [SerializeField] private GameObject finalFrameUI;

    [Header("Video sequence (play order)")]
    [SerializeField] private VideoClip[] videoClips = new VideoClip[4];

    [Header("Countdown Settings")]
    [Tooltip("用于显示倒计时的 TextMeshPro 组件")]
    [SerializeField] private TextMeshProUGUI countdownText;
    [Tooltip("倒计时时间（秒），默认 60 秒 = 1 分钟")]
    [SerializeField] private float countdownDuration = 60f;
    [Tooltip("倒计时结束后要跳转的场景名称")]
    [SerializeField] private string nextSceneName = "Scene2";

    private VideoPlayer playerA;
    private VideoPlayer playerB;
    private VideoPlayer activePlayer;

    private int currentVideoIndex;
    private bool waitingForInputAfterEnd;
    private bool isTransitioning;
    private bool isCountingDown; // 新增：防止倒计时被多次触发

    private void Awake()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("CinematicIntroController: Assign a VideoPlayer.", this);
            enabled = false;
            return;
        }

        // Setup Player A (the original inspector player)
        playerA = videoPlayer;
        ConfigurePlayer(playerA);

        // Clone Player B dynamically for seamless background loading
        playerB = gameObject.AddComponent<VideoPlayer>();
        ConfigurePlayer(playerB);
        
        // Match dimensions, targets, and routing
        playerB.renderMode = playerA.renderMode;
        playerB.targetTexture = playerA.targetTexture;
        playerB.targetCamera = playerA.targetCamera;
        playerB.aspectRatio = playerA.aspectRatio;
        playerB.audioOutputMode = playerA.audioOutputMode;
        
        // Match audio routing
        for (ushort i = 0; i < playerA.controlledAudioTrackCount; i++)
            playerB.SetTargetAudioSource(i, playerA.GetTargetAudioSource(i));

        // If using Camera planes, dim the backup player so it doesn't block the screen initially
        if (playerB.renderMode == VideoRenderMode.CameraFarPlane || playerB.renderMode == VideoRenderMode.CameraNearPlane)
        {
            playerA.targetCameraAlpha = 1f;
            playerB.targetCameraAlpha = 0f;
        }

        activePlayer = playerA;

        InitializeFinalFrameUI();
    }

    private void Start()
    {
        HideFinalFrameUI();

        // 初始状态隐藏倒计时 UI
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }

        if (videoClips != null && videoClips.Length > 0)
            PlayVideoAtIndex(0);
    }

    private void ConfigurePlayer(VideoPlayer player)
    {
        player.playOnAwake = false;
        player.isLooping = false;
        player.waitForFirstFrame = true; 
        player.loopPointReached += OnVideoFinished;
    }

    private void OnDestroy()
    {
        if (playerA != null) playerA.loopPointReached -= OnVideoFinished;
        if (playerB != null) playerB.loopPointReached -= OnVideoFinished;
    }

    private void Update()
    {
        if (isTransitioning) return;

        // 如果已经在倒计时了，屏蔽跳过视频的按键输入
        if (isCountingDown) return; 

        if (Input.GetKeyDown(KeyCode.Space) || 
            Input.GetKeyDown(KeyCode.RightArrow) ||
            Input.GetMouseButtonDown(0))
        {
            GoToNextVideo();
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
            GoToPreviousVideo();
    }

    private void OnVideoFinished(VideoPlayer source)
    {
        if (source != activePlayer) return;
        
        source.Pause(); // Freeze frame safely
        waitingForInputAfterEnd = true;

        // 判断是否是最后一个视频
        if (currentVideoIndex == videoClips.Length - 1 && !isCountingDown)
        {
            StartCoroutine(StartCountdownRoutine());
        }
    }

    private void GoToNextVideo()
    {
        if (currentVideoIndex + 1 < videoClips.Length)
            PlayVideoAtIndex(currentVideoIndex + 1);
    }

    private void GoToPreviousVideo()
    {
        if (currentVideoIndex - 1 >= 0)
            PlayVideoAtIndex(currentVideoIndex - 1);
    }

    private void PlayVideoAtIndex(int index)
    {
        if (index < 0 || index >= videoClips.Length || videoClips[index] == null) return;

        HideFinalFrameUI();

        isTransitioning = true;
        waitingForInputAfterEnd = false;
        currentVideoIndex = index;

        // Pick the idle background player
        VideoPlayer nextPlayer = (activePlayer == playerA) ? playerB : playerA;

        if (nextPlayer.clip == videoClips[index] && nextPlayer.isPrepared)
        {
            nextPlayer.time = 0;
            nextPlayer.frame = 0;
            StartCoroutine(TransitionPlayers(nextPlayer));
            return;
        }

        nextPlayer.clip = videoClips[index];
        nextPlayer.time = 0;
        nextPlayer.frame = 0;
        
        nextPlayer.prepareCompleted += OnPrepareCompleted;
        nextPlayer.Prepare(); 
    }

    private void OnPrepareCompleted(VideoPlayer source)
    {
        source.prepareCompleted -= OnPrepareCompleted;
        StartCoroutine(TransitionPlayers(source));
    }

    private IEnumerator TransitionPlayers(VideoPlayer newPlayer)
    {
        newPlayer.Play();
        yield return new WaitForEndOfFrame();

        if (newPlayer.renderMode == VideoRenderMode.CameraFarPlane || newPlayer.renderMode == VideoRenderMode.CameraNearPlane)
        {
            newPlayer.targetCameraAlpha = 1f;
            activePlayer.targetCameraAlpha = 0f;
        }

        activePlayer.Pause();
        activePlayer = newPlayer;
        isTransitioning = false;
    }

    // --- 新增：倒计时与场景跳转协程 ---
    private IEnumerator StartCountdownRoutine()
    {
        isCountingDown = true;
        
        // 可选：在倒计时开始时，如果你想展示最后一张 PNG，可以解除下面的注释
        // ShowFinalPNG(); 

        // 显示倒计时文本
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
        }

        float currentTime = countdownDuration;

        while (currentTime > 0)
        {
            if (countdownText != null)
            {
                // 将秒数格式化为 MM:SS，例如 "01:00" 或 "00:59"
                int minutes = Mathf.FloorToInt(currentTime / 60);
                int seconds = Mathf.FloorToInt(currentTime % 60);
                countdownText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            }

            // 等待一秒钟（使用真实时间流逝）
            yield return new WaitForSeconds(1f);
            currentTime--;
        }

        // 倒计时到达 0，执行场景跳转
        if (countdownText != null) countdownText.text = "00:00";
        SceneManager.LoadScene(nextSceneName);
    }

    public void ShowFinalPNG()
    {
        if (finalFrameUI == null) return;
        ApplyFullscreenLayout(finalFrameUI);
        finalFrameUI.SetActive(true);
    }

    private void InitializeFinalFrameUI()
    {
        if (finalFrameUI == null) return;
        ApplyFullscreenLayout(finalFrameUI);
        finalFrameUI.SetActive(false);
    }

    private void HideFinalFrameUI()
    {
        if (finalFrameUI == null) return;
        finalFrameUI.SetActive(false);
    }

    private void ApplyFullscreenLayout(GameObject uiRoot)
    {
        RectTransform rect = uiRoot.GetComponent<RectTransform>();
        if (rect == null) return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;

        uiRoot.transform.SetAsLastSibling();
        Image image = uiRoot.GetComponent<Image>();
        if (image != null) image.preserveAspect = false;
    }

    public int CurrentVideoIndex => currentVideoIndex;
    public int VideoCount => videoClips != null ? videoClips.Length : 0;
    public bool IsWaitingForInput => waitingForInputAfterEnd;
}