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
    [SerializeField] private bool playOnStart = false;
    [SerializeField] private bool loopSingleSongIfOnlyOne = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private AudioSource audioSource;
    private Coroutine playbackRoutine;

    private int currentIndex = -1;
    private int lastIndex = -1;
    private bool isPlayingPlaylist;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    private void Start()
    {
        if (LabAudioController.Instance != null)
        {
            LabAudioController.Instance.RegisterMusicAudioSource(audioSource);
        }

        if (playOnStart)
        {
            Play();
        }
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
        isPlayingPlaylist = false;

        if (playbackRoutine != null)
        {
            StopCoroutine(playbackRoutine);
            playbackRoutine = null;
        }

        if (audioSource != null)
            audioSource.Stop();
    }

    public void Pause()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Pause();
    }

    public void Resume()
    {
        if (audioSource != null && audioSource.clip != null)
            audioSource.UnPause();
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
            audioSource.Play();

            if (showDebugLogs)
                Debug.Log($"ARJukebox: reproduciendo {nextClip.name}");

            while (isPlayingPlaylist && audioSource.isPlaying)
            {
                yield return null;
            }

            if (songs.Length == 1 && !loopSingleSongIfOnlyOne)
            {
                isPlayingPlaylist = false;
            }

            yield return null;
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

    private void OnDisable()
    {
        Stop();
    }
}