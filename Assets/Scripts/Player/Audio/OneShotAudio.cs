using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum OneShotId
{
    Jump = 0,
    Land = 1,
}

public enum AudioCueType
{
    Pickup = 0,
    Drop = 1,
    Hover = 2,
    Interact = 3,
    Denied = 4,
    Confirm = 5,
    Cancel = 6,
    Jump = 100,
    Land = 101,
}

[DisallowMultipleComponent]
public sealed class OneShotAudio : MonoBehaviour
{
    [Serializable]
    private struct OneShotEntry
    {
        #region Inspector
        [FormerlySerializedAs("id")]
        public AudioCueType cue;
        public string cueId;
        public AudioClip clip;

        [Range(0f, 1f)] public float volume;
        [Range(0.5f, 2f)] public float pitchMin;
        [Range(0.5f, 2f)] public float pitchMax;

        public bool skipLeadingSilence;
        #endregion

        #region Cached (Serialized for Builds)
        [SerializeField, HideInInspector] private float cachedOffsetSeconds;
        [SerializeField, HideInInspector] private int cachedClipInstanceId;
        [SerializeField, HideInInspector] private bool cachedWasSkipEnabled;

        public float CachedOffsetSeconds => cachedOffsetSeconds;
        public string NormalizedCueId => string.IsNullOrWhiteSpace(cueId) ? string.Empty : cueId.Trim();

#if UNITY_EDITOR
        public bool NeedsRebake()
        {
            int id = clip != null ? clip.GetInstanceID() : 0;

            if (cachedClipInstanceId != id)
                return true;

            if (cachedWasSkipEnabled != skipLeadingSilence)
                return true;

            if (!skipLeadingSilence && cachedOffsetSeconds != 0f)
                return true;

            return false;
        }

        public void MarkBaked(float offsetSeconds)
        {
            cachedOffsetSeconds = Mathf.Max(0f, offsetSeconds);
            cachedClipInstanceId = clip != null ? clip.GetInstanceID() : 0;
            cachedWasSkipEnabled = skipLeadingSilence;
        }
#endif
        #endregion
    }

    #region Inspector
    [Header("One-shot Library")]
    [SerializeField] private List<OneShotEntry> entries = new();

    [Header("Playback Pool")]
    [SerializeField, Tooltip("How many one-shots can overlap.")]
    private int poolSize = 8;
    
    [SerializeField, Tooltip("Default spatial blend for regular Play calls. 0 = 2D, 1 = 3D")]
    [Range(0f, 1f)]
    private float defaultSpatialBlend = 0f;

    [Header("Diagnostics")]
    [SerializeField] private bool logMissingCues = true;

    [Header("Silence Detection")]
    [SerializeField, Tooltip("Anything below this absolute amplitude counts as silence.")]
    private float silenceThreshold = 0.001f;

    [SerializeField, Tooltip("How many samples per chunk to scan while detecting leading silence.")]
    private int scanChunkSize = 1024;
    #endregion

    #region Cached
    private readonly Dictionary<AudioCueType, int> cueIndexMap = new();
    private readonly Dictionary<string, int> cueIdIndexMap = new(StringComparer.OrdinalIgnoreCase);
    private AudioSource[] pool;
    private int poolIndex;
    #endregion

    #region Unity
    private void Awake()
    {
        RebuildIndexMap();
        BuildPoolIfNeeded();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Auto-bake offsets in the Editor so you never forget.
        // This runs when you change values in the Inspector.
        AutoBakeOffsetsInEditor();
        RebuildIndexMap();
    }
#endif
    #endregion

    #region Public API
    public void Play(OneShotId id)
    {
        Play(ToCueType(id));
    }

    public void Play(AudioCueType cue)
    {
        if (!TryGetEntryIndex(cue, out int entryIndex))
        {
            LogMissing(cue.ToString());
            return;
        }

        PlayFromEntry(entryIndex, defaultSpatialBlend, transform.position);
    }

    public void Play(string cueId)
    {
        if (!TryGetEntryIndex(cueId, out int entryIndex))
        {
            LogMissing(cueId);
            return;
        }

        PlayFromEntry(entryIndex, defaultSpatialBlend, transform.position);
    }

    public void PlayAtPosition(AudioCueType cue, Vector3 worldPosition)
    {
        if (!TryGetEntryIndex(cue, out int entryIndex))
        {
            LogMissing(cue.ToString());
            return;
            
        }

        PlayFromEntry(entryIndex, 1f, worldPosition);
    }

    public void PlayAtPosition(string cueId, Vector3 worldPosition)
    {
        if (!TryGetEntryIndex(cueId, out int entryIndex))
        {
            LogMissing(cueId);
            return;
        }

        PlayFromEntry(entryIndex, 1f, worldPosition);
    }
    #endregion

    #region Playback
    private void PlayFromEntry(int entryIndex, float spatialBlend, Vector3 worldPosition)
    {
        if (pool == null || pool.Length == 0)
            BuildPoolIfNeeded();

        if (entryIndex < 0 || entryIndex >= entries.Count)
            return;

        OneShotEntry e = entries[entryIndex];

        if (e.clip == null)
            return;

        AudioSource s = GetNextSource();
        s.transform.position = worldPosition;
        s.spatialBlend = Mathf.Clamp01(spatialBlend);

        s.volume = Mathf.Clamp01(e.volume);

        float pitch = UnityEngine.Random.Range(
            Mathf.Min(e.pitchMin, e.pitchMax),
            Mathf.Max(e.pitchMin, e.pitchMax)
        );

        s.pitch = pitch;
        s.clip = e.clip;

        float offset = e.skipLeadingSilence ? e.CachedOffsetSeconds : 0f;

        offset = Mathf.Clamp(offset, 0f, GetMaxStartTimeSafe(e.clip));

        s.time = offset;
        s.Play();
    }
    #endregion

    #region Pool
    private void BuildPoolIfNeeded()
    {
        poolSize = Mathf.Clamp(poolSize, 1, 16);

        // If we already have a pool with correct size, keep it.
        if (pool != null && pool.Length == poolSize)
            return;

        // Destroy old sources if any (only if they were created by us)
        if (pool != null)
        {
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i] != null)
                    Destroy(pool[i]);
            }
        }

        pool = new AudioSource[poolSize];

        for (int i = 0; i < poolSize; i++)
        {
            AudioSource s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = false;

            // Your project is 3D but plays like a sidescroller.
            // Keep it 3D so it follows the player in space.
            s.dopplerLevel = 0f;
            s.spatialBlend = defaultSpatialBlend;
            
            pool[i] = s;
        }

        poolIndex = 0;
    }

    private AudioSource GetNextSource()
    {
        if (pool == null || pool.Length == 0)
            BuildPoolIfNeeded();

        AudioSource s = pool[poolIndex];
        poolIndex = (poolIndex + 1) % pool.Length;

        if (s.isPlaying)
            s.Stop();

        return s;
    }
    #endregion

    #region Lookup
    private bool TryGetEntryIndex(AudioCueType cue, out int entryIndex)
    {
        return cueIndexMap.TryGetValue(cue, out entryIndex);
    }

    private bool TryGetEntryIndex(string cueId, out int entryIndex)
    {
        entryIndex = -1;

        if (string.IsNullOrWhiteSpace(cueId))
            return false;

        return cueIdIndexMap.TryGetValue(cueId.Trim(), out entryIndex);
    }
    
    private void RebuildIndexMap()
    {
        cueIndexMap.Clear();
        cueIdIndexMap.Clear();

        for (int i = 0; i < entries.Count; i++)
        {
            OneShotEntry entry = entries[i];
            cueIndexMap[entry.cue] = i;

            string normalizedCueId = entry.NormalizedCueId;
            if (!string.IsNullOrEmpty(normalizedCueId))
                cueIdIndexMap[normalizedCueId] = i;
        }
    }

    private static AudioCueType ToCueType(OneShotId id)
    {
        return id switch
        {
            OneShotId.Jump => AudioCueType.Jump,
            OneShotId.Land => AudioCueType.Land,
            _ => AudioCueType.Interact,
        };
    }

    private void LogMissing(string cueLabel)
    {
        if (!logMissingCues)
            return;

        Debug.LogWarning($"[OneShotAudio] Missing cue '{cueLabel}'.", this);
    }
    #endregion

    #region Offset Helpers
    private float GetMaxStartTimeSafe(AudioClip clip)
    {
        // Prevent setting time >= clip length which can throw warnings or behave oddly.
        float len = clip.length;
        return Mathf.Max(0f, len - (1f / Mathf.Max(1, clip.frequency)));
    }
    #endregion

#if UNITY_EDITOR
    #region Editor Auto-Bake
    private void AutoBakeOffsetsInEditor()
    {
        bool changed = false;

        for (int i = 0; i < entries.Count; i++)
        {
            OneShotEntry e = entries[i];

            if (!e.NeedsRebake())
                continue;

            float offset = 0f;

            if (e.skipLeadingSilence && e.clip != null)
                offset = FindFirstNonSilentTime(e.clip);

            e.MarkBaked(offset);
            entries[i] = e;
            changed = true;
        }

        if (changed)
            UnityEditor.EditorUtility.SetDirty(this);
    }
    #endregion
#endif

    #region Silence Scan
    private float FindFirstNonSilentTime(AudioClip clip)
    {
        // Works for PCM clips that allow GetData. If a clip can't be read, we fall back to 0.
        if (clip == null)
            return 0f;

        int channels = clip.channels;
        int samples = clip.samples;

        int chunk = Mathf.Max(64, scanChunkSize);
        float[] buffer = new float[chunk * channels];

        int pos = 0;

        while (pos < samples)
        {
            int read = Mathf.Min(chunk, samples - pos);

            try
            {
                clip.GetData(buffer, pos);
            }
            catch
            {
                return 0f;
            }

            int count = read * channels;

            for (int i = 0; i < count; i++)
            {
                if (Mathf.Abs(buffer[i]) > silenceThreshold)
                    return (float)pos / clip.frequency;
            }

            pos += read;
        }

        return 0f;
    }
    #endregion
}