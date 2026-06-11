using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource; //aquest es el principal
    [SerializeField] private AudioSource crossfadeSource; //aquest es per fer crossfade (fade entre dos can�ons)

    [Header("Musica del joc")]
    [SerializeField] private AudioClip menuMusic; //la dels menus
    [SerializeField] private AudioClip startSequenceMusic; //la de la sequencia d'inici del joc
    [SerializeField] private AudioClip baseMusic; //la maint
    [SerializeField] private AudioClip enemyMusic; //la del boss

    [Header("Settings")]
    [SerializeField] private float defaultVolume = 0.7f; //volum per defecte
    [SerializeField] private float defaultFadeTime = 1f; //temps de fade per defecte

    private Dictionary<string, AudioClip> musicLibrary; //diccionari per accedir a les can�ons per nom

    private Coroutine currentFadeCoroutine; //coroutine actual de fade
    private string currentMusicKey = ""; //musica actual que s'est� reproduint

    void Awake()
    {
        if (Instance == null) //Singleton
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeMusicLibrary();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void InitializeMusicLibrary() //inicialitza el diccionari de musica
    {
        musicLibrary = new Dictionary<string, AudioClip>
        {
            { "Menu", menuMusic },
            { "StartSequence", startSequenceMusic },
            { "Base", baseMusic },
            { "Enemy", enemyMusic },
        };

        // Configurar AudioSources
        musicSource.loop = true;
        musicSource.volume = 0f;
        crossfadeSource.loop = true;
        crossfadeSource.volume = 0f;

        Debug.Log("AudioManager inicializado con " + musicLibrary.Count + " canciones");
    }


    //==================== Metodes publics ===================//

    public void PlayMusic(string musicKey, float fadeTime = -1f) //metode per reproduir musica amb crossfade
    {
        if (fadeTime < 0) fadeTime = defaultFadeTime;

        //Si esta sonant musica no fem res
        if (currentMusicKey == musicKey && musicSource.isPlaying)
        {
            Debug.Log($" {musicKey} ya est� sonando");
            return;
        }

        if (!musicLibrary.ContainsKey(musicKey)) //si no existeix la musica
        {
            Debug.LogError($" M�sica '{musicKey}' no encontrada en la biblioteca");
            return;
        }

        AudioClip newClip = musicLibrary[musicKey]; //agafem la musica del diccionari

        if (newClip == null) //si la musica no est� assignada
        {
            Debug.LogError($" AudioClip para '{musicKey}' no est� asignado en el Inspector");
            return;
        }

        //si hi ha una coroutine de fade en marxa, la parem
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
        }

        //inicia el crossfade
        currentFadeCoroutine = StartCoroutine(CrossfadeMusic(newClip, fadeTime));
        currentMusicKey = musicKey;

        Debug.Log($" Reproduciendo: {musicKey} (fade: {fadeTime}s)");
    }


    public void StopMusic(float fadeTime = -1f) //metode per aturar la musica amb fade out
    {
        if (fadeTime < 0) fadeTime = defaultFadeTime;

        if (currentFadeCoroutine != null) //si hi ha una coroutine de fade en marxa, la parem
        {
            StopCoroutine(currentFadeCoroutine);
        }

        currentFadeCoroutine = StartCoroutine(FadeOutMusic(fadeTime)); //inicia el fade out
        currentMusicKey = "";

        Debug.Log($"Deteniendo m�sica (fade: {fadeTime}s)");
    }


    public void PauseMusic() //metode per pausar la musica sense fade
    {
        musicSource.Pause();
        crossfadeSource.Pause();
        Debug.Log("M�sica pausada");
    }

    public void ResumeMusic() //metode per reanudar la musica sense fade
    {
        musicSource.UnPause();
        crossfadeSource.UnPause();
        Debug.Log("M�sica reanudada");
    }

    public void SetVolume(float volume) //metode per ajustar el volum
    {
        defaultVolume = Mathf.Clamp01(volume);
        musicSource.volume = defaultVolume;
        crossfadeSource.volume = 0f;
        Debug.Log($"Volumen ajustado: {defaultVolume}");
    }

    public string GetCurrentMusic() //metode per obtenir la musica actual
    {
        return currentMusicKey;
    }

    private IEnumerator CrossfadeMusic(AudioClip newClip, float fadeTime) //coroutine per fer crossfade entre can�ons
    {
        //si no hi ha musica sonant, fem un simple fade in
        if (!musicSource.isPlaying)
        {
            musicSource.clip = newClip;
            musicSource.Play();
            yield return StartCoroutine(FadeIn(musicSource, fadeTime));
            yield break;
        }

        //iniciar crossfade 
        crossfadeSource.clip = newClip;
        crossfadeSource.Play();

        float elapsed = 0f;
        float startVolume = musicSource.volume;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeTime;

            musicSource.volume = Mathf.Lerp(startVolume, 0f, t);
            crossfadeSource.volume = Mathf.Lerp(0f, defaultVolume, t);

            yield return null;
        }

        //finalitzar valors
        musicSource.volume = 0f;
        crossfadeSource.volume = defaultVolume;

        //Intercanviar les AudioSources
        musicSource.Stop();
        AudioSource temp = musicSource;
        musicSource = crossfadeSource;
        crossfadeSource = temp;
    }

    private IEnumerator FadeIn(AudioSource source, float fadeTime) //coroutine per fer fade in
    {
        float elapsed = 0f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(0f, defaultVolume, elapsed / fadeTime);
            yield return null;
        }

        source.volume = defaultVolume;
    }

    private IEnumerator FadeOutMusic(float fadeTime) //coroutine per fer fade out
    {
        float elapsed = 0f;
        float startVolume = musicSource.volume;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeTime);
            yield return null;
        }

        musicSource.volume = 0f;
        musicSource.Stop();
    }




}