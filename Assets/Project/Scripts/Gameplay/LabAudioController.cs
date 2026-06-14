using System.Collections;
using UnityEngine;

public class LabAudioController : MonoBehaviour
{
    public static LabAudioController Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource instructionAudioSource;
    [SerializeField] private AudioSource musicAudioSource;
    [SerializeField] private AudioSource[] sfxAudioSources;

    [Header("Normal Volumes")]
    [Range(0f, 1f)]
    [SerializeField] private float instructionNormalVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float musicNormalVolume = 0.35f;

    [Range(0f, 1f)]
    [SerializeField] private float sfxNormalVolume = 1f;

    [Header("Muted Volumes")]
    [Range(0f, 1f)]
    [SerializeField] private float instructionMutedVolume = 0f;

    [Range(0f, 1f)]
    [SerializeField] private float musicMutedVolume = 0f;

    [Range(0f, 1f)]
    [SerializeField] private float sfxMutedVolume = 0f;

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.35f;

    [Header("State")]
    [SerializeField] private bool muted;

    private Coroutine fadeRoutine;

    public bool IsMuted => muted;

    private void Awake()
    {
        Instance = this;
        ApplyVolumesImmediate();
    }

    public void ToggleMuteAll()
    {
        SetMuted(!muted);
    }

    public void SetMuted(bool value)
    {
        muted = value;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeVolumesRoutine());
    }

    public void RegisterInstructionAudioSource(AudioSource source)
    {
        instructionAudioSource = source;
        ApplyVolumeImmediate(source, muted ? instructionMutedVolume : instructionNormalVolume);
    }

    public void RegisterMusicAudioSource(AudioSource source)
    {
        musicAudioSource = source;
        ApplyVolumeImmediate(source, muted ? musicMutedVolume : musicNormalVolume);
    }

    public float GetInstructionTargetVolume()
    {
        return muted ? instructionMutedVolume : instructionNormalVolume;
    }

    public float GetMusicTargetVolume()
    {
        return muted ? musicMutedVolume : musicNormalVolume;
    }

    public float GetSfxTargetVolume()
    {
        return muted ? sfxMutedVolume : sfxNormalVolume;
    }

    private IEnumerator FadeVolumesRoutine()
    {
        float startInstruction = instructionAudioSource != null ? instructionAudioSource.volume : 0f;
        float startMusic = musicAudioSource != null ? musicAudioSource.volume : 0f;

        float[] startSfxVolumes = null;

        if (sfxAudioSources != null)
        {
            startSfxVolumes = new float[sfxAudioSources.Length];

            for (int i = 0; i < sfxAudioSources.Length; i++)
            {
                startSfxVolumes[i] = sfxAudioSources[i] != null
                    ? sfxAudioSources[i].volume
                    : 0f;
            }
        }

        float targetInstruction = GetInstructionTargetVolume();
        float targetMusic = GetMusicTargetVolume();
        float targetSfx = GetSfxTargetVolume();

        if (fadeDuration <= 0f)
        {
            ApplyVolumesImmediate();
            fadeRoutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            if (instructionAudioSource != null)
                instructionAudioSource.volume = Mathf.Lerp(startInstruction, targetInstruction, t);

            if (musicAudioSource != null)
                musicAudioSource.volume = Mathf.Lerp(startMusic, targetMusic, t);

            if (sfxAudioSources != null && startSfxVolumes != null)
            {
                for (int i = 0; i < sfxAudioSources.Length; i++)
                {
                    if (sfxAudioSources[i] != null)
                    {
                        sfxAudioSources[i].volume = Mathf.Lerp(
                            startSfxVolumes[i],
                            targetSfx,
                            t
                        );
                    }
                }
            }

            yield return null;
        }

        ApplyVolumesImmediate();
        fadeRoutine = null;
    }

    private void ApplyVolumesImmediate()
    {
        ApplyVolumeImmediate(instructionAudioSource, GetInstructionTargetVolume());
        ApplyVolumeImmediate(musicAudioSource, GetMusicTargetVolume());

        if (sfxAudioSources != null)
        {
            foreach (AudioSource source in sfxAudioSources)
            {
                ApplyVolumeImmediate(source, GetSfxTargetVolume());
            }
        }
    }

    private void ApplyVolumeImmediate(AudioSource source, float volume)
    {
        if (source != null)
            source.volume = volume;
    }
}