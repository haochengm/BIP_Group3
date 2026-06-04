using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Plays a sequence of intro/office videos with keyboard navigation.
/// Videos pause on the last frame when they finish; the frame stays visible until the player presses a key.
/// </summary>
///
/// =============================================================================
/// HOW TO SET UP IN THE INSPECTOR
/// =============================================================================
///
/// 1) Assign the VideoPlayer
///    - Create or select a GameObject with a VideoPlayer component (see scene setup below).
///    - Drag that VideoPlayer into the "Video Player" field on this script.
///
/// 2) Assign the video clips (order matters!)
///    - Set "Size" on the Video Clips array to 4.
///    - Drag clips from Assets/Art/ in this order:
///        Element 0 → Intro
///        Element 1 → Office_1
///        Element 2 → Office_2
///        Element 3 → Office_3
///    - Index 0 plays automatically when the scene starts.
///
/// 3) VideoPlayer recommended settings (Inspector on VideoPlayer component)
///    - Play On Awake: OFF  (this script starts playback in Start())
///    - Loop: OFF           (also forced in code)
///    - Render Mode: choose UI (Raw Image) or Camera Far Plane / Material override
///
/// =============================================================================
/// SCENE SETUP (if you are creating a new scene)
/// =============================================================================
///
/// A) UI-based video (good for fullscreen intro on Canvas):
///    1. File → New Scene → save as e.g. Assets/Scenes/IntroScene.unity
///    2. GameObject → UI → Canvas (Screen Space Overlay)
///    3. On Canvas: GameObject → UI → Raw Image (stretch to full screen)
///    4. Select Canvas (or a child) → Add Component → Video Player
///    5. Video Player: Render Mode = Render Texture OR Camera Near/Far Plane
///       - Easiest UI path: create Render Texture (Assets → Create → Render Texture),
///         assign it to Video Player "Target Texture", assign same texture to Raw Image "Texture"
///    6. Empty GameObject → Add Component → Cinematic Intro Controller
///    7. Wire Video Player + clips as described above
///    8. Add this scene to File → Build Settings → Scenes In Build if needed
///
/// B) Camera-based video:
///    1. Main Camera → Add Component → Video Player
///    2. Render Mode = Camera Far Plane, set Target Camera to Main Camera
///    3. Add CinematicIntroController on any GameObject and assign references
///
/// Controls: SPACE or RIGHT = next video | LEFT = previous video
/// =============================================================================
public class CinematicIntroController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The VideoPlayer in this scene (UI or Camera-based). Drag it here from the Hierarchy.")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Video sequence (play order)")]
    [Tooltip("Clips in play order: Intro, Office_1, Office_2, Office_3 from Assets/Art/")]
    [SerializeField] private VideoClip[] videoClips = new VideoClip[4];

    /// <summary>Index of the clip currently assigned to the VideoPlayer (0 = Intro).</summary>
    private int currentVideoIndex;

    /// <summary>True after a clip finishes and we are holding the last frame until input.</summary>
    private bool waitingForInputAfterEnd;

    private void Awake()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("CinematicIntroController: Assign a VideoPlayer in the Inspector.", this);
            enabled = false;
            return;
        }

        // This script owns playback timing — do not auto-play or loop in the Inspector.
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.loopPointReached += OnVideoFinished;
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoFinished;
    }

    private void Start()
    {
        if (videoClips == null || videoClips.Length == 0)
        {
            Debug.LogWarning("CinematicIntroController: No video clips assigned.", this);
            return;
        }

        // Requirement: first video (Intro) plays automatically when the scene starts.
        PlayVideoAtIndex(0);
    }

    private void Update()
    {
        if (videoPlayer == null || videoClips == null || videoClips.Length == 0)
            return;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.RightArrow))
            GoToNextVideo();

        if (Input.GetKeyDown(KeyCode.LeftArrow))
            GoToPreviousVideo();
    }

    /// <summary>Called by Unity when the current clip reaches its end (requires isLooping = false).</summary>
    private void OnVideoFinished(VideoPlayer source)
    {
        // Ignore if we switched clips and an old finish event fires.
        if (source != videoPlayer)
            return;

        HoldOnLastFrame();
        waitingForInputAfterEnd = true;
    }

    /// <summary>Keeps the final frame on screen (no fade, no clear).</summary>
    private void HoldOnLastFrame()
    {
        videoPlayer.Pause();

        if (videoPlayer.frameCount > 0)
            videoPlayer.frame = (long)videoPlayer.frameCount - 1;
        else if (videoPlayer.clip != null)
            videoPlayer.time = Mathf.Max(0f, (float)videoPlayer.clip.length - 0.05f);
    }

    private void GoToNextVideo()
    {
        int nextIndex = currentVideoIndex + 1;
        if (nextIndex >= videoClips.Length)
            return;

        PlayVideoAtIndex(nextIndex);
    }

    private void GoToPreviousVideo()
    {
        int previousIndex = currentVideoIndex - 1;
        if (previousIndex < 0)
            return;

        PlayVideoAtIndex(previousIndex);
    }

    /// <summary>
    /// Switches to a clip by index: always starts at time 0 and plays immediately.
    /// </summary>
    private void PlayVideoAtIndex(int index)
    {
        if (index < 0 || index >= videoClips.Length)
            return;

        if (videoClips[index] == null)
        {
            Debug.LogWarning($"CinematicIntroController: Video clip at index {index} is missing.", this);
            return;
        }

        waitingForInputAfterEnd = false;
        currentVideoIndex = index;

        videoPlayer.Stop();
        videoPlayer.clip = videoClips[index];
        videoPlayer.time = 0;
        videoPlayer.frame = 0;
        videoPlayer.isLooping = false;
        videoPlayer.Play();
    }

    /// <summary>Read-only access for other scripts or debug UI.</summary>
    public int CurrentVideoIndex => currentVideoIndex;

    public int VideoCount => videoClips != null ? videoClips.Length : 0;

    public bool IsWaitingForInput => waitingForInputAfterEnd;
}
