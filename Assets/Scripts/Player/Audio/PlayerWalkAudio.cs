using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class PlayerWalkAudio : MonoBehaviour
{
    [Serializable]
    private struct SurfaceLoopEntry
    {
        public Movement.SurfaceType surface;
        public AudioClip loopClip;

        [Range(0f, 1f)] public float volume;
        [Range(0.5f, 2f)] public float pitchMin;
        [Range(0.5f, 2f)] public float pitchMax;
    }

    #region Inspector
    [SerializeField] private Movement movement;

    [Header("Default")]
    [SerializeField, Tooltip("Used if no matching surface entry exists.")]
    private AudioClip defaultLoop;

    [SerializeField, Range(0f, 1f)]
    private float defaultVolume = 1f;

    [SerializeField, Range(0.5f, 2f)]
    private float defaultPitchMin = 0.98f;

    [SerializeField, Range(0.5f, 2f)]
    private float defaultPitchMax = 1.02f;

    [Header("Surface Loops")]
    [SerializeField]
    private List<SurfaceLoopEntry> surfaceLoops = new();

    [Header("Gating")]
    [SerializeField, Tooltip("Normalized speed threshold before footsteps start playing.")]
    private float minSpeedToPlay = 0.15f;
    #endregion

    #region Cached
    private AudioSource source;
    private readonly Dictionary<Movement.SurfaceType, SurfaceLoopEntry> loopMap = new();

    private Movement.SurfaceType lastSurface = Movement.SurfaceType.Wood;
    private AudioClip lastClip;
    #endregion

    #region Unity
    private void Reset()
    {
        movement = GetComponentInParent<Movement>();
    }

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;

        if (movement == null)
            movement = GetComponentInParent<Movement>();

        RebuildMap();
    }

    private void OnValidate()
    {
        RebuildMap();
    }

    private void Update()
    {
        if (movement == null || source == null)
            return;

        bool shouldPlay =
            movement.IsGrounded &&
            movement.CurrentState == Movement.MovementState.Walking &&
            movement.HorizontalSpeedNormalized >= minSpeedToPlay;

        if (!shouldPlay)
        {
            StopLoop();
            return;
        }

        Movement.SurfaceType surface = movement.CurrentSurface;

        SurfaceLoopEntry entry;
        AudioClip clip = defaultLoop;

        float volume = defaultVolume;
        float pitchMin = defaultPitchMin;
        float pitchMax = defaultPitchMax;

        if (loopMap.TryGetValue(surface, out entry) && entry.loopClip != null)
        {
            clip = entry.loopClip;
            volume = entry.volume;
            pitchMin = entry.pitchMin;
            pitchMax = entry.pitchMax;
        }

        // Swap clip if surface changed or clip changed
        if (surface != lastSurface || clip != lastClip || source.clip == null)
        {
            lastSurface = surface;
            lastClip = clip;

            ApplyClipSettings(clip, volume, pitchMin, pitchMax);

            if (clip != null)
                source.Play();

            return;
        }

        if (!source.isPlaying && source.clip != null)
            source.Play();
    }
    #endregion

    #region Helpers
    private void RebuildMap()
    {
        loopMap.Clear();

        for (int i = 0; i < surfaceLoops.Count; i++)
        {
            SurfaceLoopEntry e = surfaceLoops[i];

            // Last one wins if duplicates exist
            loopMap[e.surface] = e;
        }
    }

    private void ApplyClipSettings(AudioClip clip, float volume, float pitchMin, float pitchMax)
    {
        source.Stop();

        source.clip = clip;
        source.volume = Mathf.Clamp01(volume);

        float min = Mathf.Min(pitchMin, pitchMax);
        float max = Mathf.Max(pitchMin, pitchMax);

        source.pitch = UnityEngine.Random.Range(min, max);
    }

    private void StopLoop()
    {
        if (source.isPlaying)
            source.Stop();
    }
    #endregion
}