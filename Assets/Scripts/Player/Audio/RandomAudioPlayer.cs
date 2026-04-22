using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class RandomAudioPlayer : MonoBehaviour
{
    #region Inspector

    [Header("Audio Clips")]
    [SerializeField] private AudioClip[] clips;

    [Header("Playback")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool loop = false;

    [Header("Delay (if looping)")]
    [SerializeField] private float minDelay = 1f;
    [SerializeField] private float maxDelay = 3f;

    #endregion

    #region Variables

    private AudioSource source;

    #endregion

    #region Unity

    void Awake()
    {
        source = GetComponent<AudioSource>();
    }

    void Start()
    {
        if (playOnStart)
        {
            PlayRandom();
        }
    }

    #endregion

    #region Public API

    public void PlayRandom()
    {
        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning("No audio clips assigned.", this);
            return;
        }

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        source.clip = clip;
        source.Play();

        if (loop)
        {
            Invoke(nameof(PlayRandom), Random.Range(minDelay, maxDelay));
        }
    }

    #endregion
}