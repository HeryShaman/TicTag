using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class VolumeSettings : MonoBehaviour, IPointerUpHandler
{
    [SerializeField] private Slider volumeSlider;

    [Header("Son de test")]
    [SerializeField] private AudioSource testAudioSource;

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        Init();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Init();
    }

    private void Init()
    {
        // -------- RECHERCHE SLIDER --------
        if (volumeSlider == null)
        {
            volumeSlider = GameObject.Find("Slider AUDIO")?.GetComponent<Slider>();
        }

        if (volumeSlider == null)
            return;

        // -------- LOAD SAVE --------
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);

        AudioListener.volume = savedVolume;

        // évite callbacks multiples
        volumeSlider.onValueChanged.RemoveListener(SetVolume);

        volumeSlider.value = savedVolume;

        volumeSlider.onValueChanged.AddListener(SetVolume);
    }

    public void SetVolume(float value)
    {
        AudioListener.volume = value;

        PlayerPrefs.SetFloat("MasterVolume", value);

        // optionnel mais conseillé
        PlayerPrefs.Save();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (testAudioSource != null)
        {
            testAudioSource.Play();
        }
    }
}