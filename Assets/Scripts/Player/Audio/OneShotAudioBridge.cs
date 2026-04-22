using UnityEngine;

[DisallowMultipleComponent]
public sealed class OneShotAudioBridge : MonoBehaviour
{
    #region Inspector
    [SerializeField] private OneShotAudio oneShotAudio;
    [SerializeField] private bool dontDestroyOnLoad = true;
    #endregion

    #region Cached
    private static OneShotAudioBridge instance;
    #endregion

    #region Unity
    private void Reset()
    {
        oneShotAudio = GetComponent<OneShotAudio>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        EnsureAudioComponent();

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);
    }
    #endregion

    #region Static API
    public static void PlayPickup() => EnsureInstance().oneShotAudio.Play(AudioCueType.Pickup);
    public static void PlayDrop() => EnsureInstance().oneShotAudio.Play(AudioCueType.Drop);
    public static void PlayHover() => EnsureInstance().oneShotAudio.Play(AudioCueType.Hover);
    public static void PlayInteract() => EnsureInstance().oneShotAudio.Play(AudioCueType.Interact);
    public static void PlayDenied() => EnsureInstance().oneShotAudio.Play(AudioCueType.Denied);
    public static void PlayConfirm() => EnsureInstance().oneShotAudio.Play(AudioCueType.Confirm);
    public static void PlayCancel() => EnsureInstance().oneShotAudio.Play(AudioCueType.Cancel);

    public static void Play(AudioCueType cue) => EnsureInstance().oneShotAudio.Play(cue);
    public static void Play(string cueId) => EnsureInstance().oneShotAudio.Play(cueId);
    public static void Play(OneShotId id) => EnsureInstance().oneShotAudio.Play(id);

    public static void PlayAtPosition(AudioCueType cue, Vector3 worldPosition) => EnsureInstance().oneShotAudio.PlayAtPosition(cue, worldPosition);
    public static void PlayAtPosition(string cueId, Vector3 worldPosition) => EnsureInstance().oneShotAudio.PlayAtPosition(cueId, worldPosition);
    #endregion

    #region Helpers
    private static OneShotAudioBridge EnsureInstance()
    {
        if (instance != null)
            return instance;

        instance = FindFirstObjectByType<OneShotAudioBridge>(FindObjectsInactive.Include);

        if (instance != null)
        {
            instance.EnsureAudioComponent();
            return instance;
        }

        GameObject bridgeObject = new GameObject("OneShotAudioBridge");
        instance = bridgeObject.AddComponent<OneShotAudioBridge>();
        instance.EnsureAudioComponent();
        return instance;
    }

    private void EnsureAudioComponent()
    {
        if (oneShotAudio == null)
            oneShotAudio = GetComponent<OneShotAudio>();

        if (oneShotAudio == null)
            oneShotAudio = gameObject.AddComponent<OneShotAudio>();
    }
    #endregion
}