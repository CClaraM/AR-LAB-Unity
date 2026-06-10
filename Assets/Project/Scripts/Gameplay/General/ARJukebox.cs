using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ARJukebox : MonoBehaviour
{
    [Header("Playlist")]
    [SerializeField] private AudioClip[] songs;
    [SerializeField] private bool playRandom = true;
    [SerializeField] private bool avoidImmediateRepeat = true;

    [Header("Playback")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.35f;

    [SerializeField] private bool playOnStart = false;
    [SerializeField] private bool loopSingleSongIfOnlyOne = true;

    [Header("Fade")]
    [SerializeField] private bool useFadeIn = true;
    [SerializeField] private float fadeInDuration = 1.5f;

    [SerializeField] private bool useFadeOut = true;
    [SerializeField] private float fadeOutDuration = 0.75f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private AudioSource audioSource;
    private Coroutine playbackRoutine;
    private Coroutine fadeRoutine;

    private int currentIndex = -1;
    private int lastIndex = -1;
    private bool isPlayingPlaylist;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f; // música 2D
        audioSource.volume = 0f;
    }

    private void Start()
    {
        if (playOnStart)
            Play();
    }

    public void Play()
    {
        if (songs == null || songs.Length == 0)
        {
            Debug.LogWarning("ARJukebox: no hay canciones asignadas.");
            return;
        }

        if (isPlayingPlaylist)
            return;

        isPlayingPlaylist = true;

        if (playbackRoutine != null)
            StopCoroutine(playbackRoutine);

        playbackRoutine = StartCoroutine(PlaylistRoutine());
    }

    public void Stop()
    {
        if (!isPlayingPlaylist && !audioSource.isPlaying)
            return;

        isPlayingPlaylist = false;

        if (playbackRoutine != null)
        {
            StopCoroutine(playbackRoutine);
            playbackRoutine = null;
        }

        if (useFadeOut)
        {
            StartFade(0f, fadeOutDuration, stopAfterFade: true);
        }
        else
        {
            audioSource.Stop();
            audioSource.volume = 0f;
        }
    }

    public void Pause()
    {
        if (!audioSource.isPlaying)
            return;

        audioSource.Pause();
    }

    public void Resume()
    {
        if (audioSource.clip == null)
            return;

        audioSource.UnPause();
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);

        if (audioSource != null && audioSource.isPlaying)
            audioSource.volume = volume;
    }

    private IEnumerator PlaylistRoutine()
    {
        while (isPlayingPlaylist)
        {
            AudioClip nextClip = GetNextClip();

            if (nextClip == null)
            {
                yield return null;
                continue;
            }

            audioSource.clip = nextClip;
            audioSource.volume = useFadeIn ? 0f : volume;
            audioSource.Play();

            if (showDebugLogs)
                Debug.Log($"ARJukebox: reproduciendo {nextClip.name}");

            if (useFadeIn)
                StartFade(volume, fadeInDuration, stopAfterFade: false);
            else
                audioSource.volume = volume;

            while (isPlayingPlaylist && audioSource.isPlaying)
            {
                yield return null;
            }

            if (songs.Length == 1 && loopSingleSongIfOnlyOne)
            {
                yield return null;
            }
        }

        playbackRoutine = null;
    }

    private AudioClip GetNextClip()
    {
        if (songs == null || songs.Length == 0)
            return null;

        if (songs.Length == 1)
        {
            currentIndex = 0;
            lastIndex = 0;
            return songs[0];
        }

        int nextIndex;

        if (playRandom)
        {
            nextIndex = Random.Range(0, songs.Length);

            if (avoidImmediateRepeat)
            {
                int safety = 0;

                while (nextIndex == lastIndex && safety < 10)
                {
                    nextIndex = Random.Range(0, songs.Length);
                    safety++;
                }
            }
        }
        else
        {
            nextIndex = currentIndex + 1;

            if (nextIndex >= songs.Length)
                nextIndex = 0;
        }

        currentIndex = nextIndex;
        lastIndex = nextIndex;

        return songs[currentIndex];
    }

    private void StartFade(float targetVolume, float duration, bool stopAfterFade)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeRoutine(targetVolume, duration, stopAfterFade));
    }

    private IEnumerator FadeRoutine(float targetVolume, float duration, bool stopAfterFade)
    {
        float startVolume = audioSource.volume;

        if (duration <= 0f)
        {
            audioSource.volume = targetVolume;

            if (stopAfterFade)
                audioSource.Stop();

            fadeRoutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, t);

            yield return null;
        }

        audioSource.volume = targetVolume;

        if (stopAfterFade)
            audioSource.Stop();

        fadeRoutine = null;
    }

    private void OnDisable()
    {
        if (audioSource != null)
            audioSource.Stop();

        isPlayingPlaylist = false;
    }
}