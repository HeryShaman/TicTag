using UnityEngine;

public class MIC_AudioManager : MonoBehaviour
{
    public static MIC_AudioManager Instance;

    public float masterVolume = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetVolume(float value)
    {
        masterVolume = value;
        AudioListener.volume = value;
    }
}
