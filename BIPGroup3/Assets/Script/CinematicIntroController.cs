using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

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

    private VideoPlayer playerA;
    private VideoPlayer playerB;
    private VideoPlayer activePlayer;

    private int currentVideoIndex;
    private bool waitingForInputAfterEnd;
    private bool isTransitioning;

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

        // FIX: If the background player already has this clip loaded and prepared from a previous swap,
        // bypass Unity's broken Prepare() pipeline and jump straight to playing it.
        if (nextPlayer.clip == videoClips[index] && nextPlayer.isPrepared)
        {
            nextPlayer.time = 0;
            nextPlayer.frame = 0;
            StartCoroutine(TransitionPlayers(nextPlayer));
            return;
        }

        // Otherwise, perform a fresh prepare for a clip it hasn't seen yet
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
        // Start playing the new video in the background/buffer
        newPlayer.Play();

        // Wait a frame to let the engine physically render the first frame of the new clip
        yield return new WaitForEndOfFrame();

        // Safe visual swap for camera render modes
        if (newPlayer.renderMode == VideoRenderMode.CameraFarPlane || newPlayer.renderMode == VideoRenderMode.CameraNearPlane)
        {
            newPlayer.targetCameraAlpha = 1f;
            activePlayer.targetCameraAlpha = 0f;
        }

        // Retire the old player to a paused state
        activePlayer.Pause();

        activePlayer = newPlayer;
        isTransitioning = false;
    }

    /// <summary>Shows the fullscreen final-frame PNG overlay. Call when the cinematic should end on a still image.</summary>
    public void ShowFinalPNG()
    {
        if (finalFrameUI == null)
            return;

        ApplyFullscreenLayout(finalFrameUI);
        finalFrameUI.SetActive(true);
    }

    private void InitializeFinalFrameUI()
    {
        if (finalFrameUI == null)
            return;

        ApplyFullscreenLayout(finalFrameUI);
        finalFrameUI.SetActive(false);
    }

    private void HideFinalFrameUI()
    {
        if (finalFrameUI == null)
            return;

        finalFrameUI.SetActive(false);
    }

    private void ApplyFullscreenLayout(GameObject uiRoot)
    {
        RectTransform rect = uiRoot.GetComponent<RectTransform>();
        if (rect == null)
        {
            Debug.LogWarning("CinematicIntroController: finalFrameUI has no RectTransform.", uiRoot);
            return;
        }

        if (uiRoot.GetComponentInParent<Canvas>() == null)
            Debug.LogWarning("CinematicIntroController: finalFrameUI should be a child of a Canvas.", uiRoot);

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
        if (image != null)
            image.preserveAspect = false;
    }

    public int CurrentVideoIndex => currentVideoIndex;
    public int VideoCount => videoClips != null ? videoClips.Length : 0;
    public bool IsWaitingForInput => waitingForInputAfterEnd;
}
