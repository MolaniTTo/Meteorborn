using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource crossfadeSource;

    [Header("Playlist")]
    [SerializeField] private AudioClip[] allMusic;

    [Header("Settings")]
    [SerializeField] private float defaultVolume = 0.7f;
    [SerializeField] private float crossfadeTime = 2f;

    private int currentTrackIndex = 0;
    private Coroutine playlistCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (allMusic == null || allMusic.Length == 0)
        {
            Debug.LogWarning("No hay canciones asignadas en allMusic.");
            return;
        }

        musicSource.loop = false;
        crossfadeSource.loop = false;

        musicSource.volume = defaultVolume;
        crossfadeSource.volume = 0f;

        playlistCoroutine = StartCoroutine(PlaylistLoop());
    }

    private IEnumerator PlaylistLoop()
    {
        musicSource.clip = allMusic[currentTrackIndex];
        musicSource.Play();

        Debug.Log($"Reproduciendo: {musicSource.clip.name}");

        while (true)
        {
            float waitTime = Mathf.Max(
                musicSource.clip.length - crossfadeTime,
                0f
            );

            yield return new WaitForSeconds(waitTime);

            int nextTrack = (currentTrackIndex + 1) % allMusic.Length;

            yield return StartCoroutine(
                CrossfadeTo(allMusic[nextTrack])
            );

            currentTrackIndex = nextTrack;
        }
    }

    private IEnumerator CrossfadeTo(AudioClip nextClip)
    {
        crossfadeSource.clip = nextClip;
        crossfadeSource.volume = 0f;
        crossfadeSource.Play();

        float timer = 0f;

        while (timer < crossfadeTime)
        {
            timer += Time.deltaTime;
            float t = timer / crossfadeTime;

            musicSource.volume = Mathf.Lerp(defaultVolume, 0f, t);
            crossfadeSource.volume = Mathf.Lerp(0f, defaultVolume, t);

            yield return null;
        }

        // Detener el source anterior
        musicSource.Stop();
        musicSource.volume = 0f;

        // Ahora el crossfadeSource se convierte en el principal
        crossfadeSource.volume = defaultVolume;

        // Intercambiar las referencias para mantener la lógica
        AudioSource temp = musicSource;
        musicSource = crossfadeSource;
        crossfadeSource = temp;

        Debug.Log($"Reproduciendo: {musicSource.clip.name}");
    }

    public void PauseMusic()
    {
        musicSource.Pause();
        crossfadeSource.Pause();
    }

    public void ResumeMusic()
    {
        musicSource.UnPause();
        crossfadeSource.UnPause();
    }

    public void SetVolume(float volume)
    {
        defaultVolume = Mathf.Clamp01(volume);

        if (musicSource.isPlaying)
            musicSource.volume = defaultVolume;
    }

    public void SkipToNext()
    {
        if (allMusic.Length <= 1)
            return;

        if (playlistCoroutine != null)
            StopCoroutine(playlistCoroutine);

        StartCoroutine(SkipToNextRoutine());
    }

    private IEnumerator SkipToNextRoutine()
    {
        int nextTrack = (currentTrackIndex + 1) % allMusic.Length;

        yield return StartCoroutine(
            CrossfadeTo(allMusic[nextTrack])
        );

        currentTrackIndex = nextTrack;

        playlistCoroutine = StartCoroutine(PlaylistLoop());
    }

    public string GetCurrentSong()
    {
        if (musicSource.clip == null)
            return "Ninguna";

        return musicSource.clip.name;
    }
}