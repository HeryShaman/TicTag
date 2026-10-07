using UnityEngine;

public class MusicManager : MonoBehaviour
{
    ///  /// Gestionnaire audio persistant. /// Menu principal + Player Selection : menuMusic. /// Gameplay : gameplayMusic. /// Pause : la musique gameplay est atténuée sans être arrêtée. ///  public class MusicManager : MonoBehaviour { public static MusicManager Instance { get; private set; }
    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip gameplayMusic;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)]
    private float normalVolume = 1f;

    [SerializeField, Range(0f, 1f)]
    private float pauseVolume = 0.25f;

    [SerializeField, Min(0.01f)]
    private float fadeSpeed = 5f;

    private float targetVolume;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();

        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();

        musicSource.loop = true;
        musicSource.playOnAwake = false;

        targetVolume = normalVolume;
        musicSource.volume = normalVolume;
    }

    private void Update()
    {
        musicSource.volume =
            Mathf.MoveTowards(
                musicSource.volume,
                targetVolume,
                fadeSpeed * Time.unscaledDeltaTime);
    }

    public void PlayMenuMusic()
    {
        PlayClip(menuMusic);
        SetPaused(false);
    }

    public void PlayGameplayMusic()
    {
        PlayClip(gameplayMusic);
        SetPaused(false);
    }

    public void SetPaused(bool paused)
    {
        targetVolume =
            paused
                ? normalVolume * pauseVolume
                : normalVolume;
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning(
                "MusicManager : AudioClip non assigné.",
                this);
            return;
        }

        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        musicSource.clip = clip;
        musicSource.volume = 0f;
        musicSource.Play();

        targetVolume = normalVolume;
    }
}  
