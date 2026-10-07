using UnityEngine;

public class UIAudioSource : MonoBehaviour {
    public AudioSource audioSource;

    public AudioClip hoverSound;
    public AudioClip clickSound;

    public void PlayUIClick() {
        audioSource.PlayOneShot(clickSound);
    }
    public void PlayUIHover() {
        audioSource.PlayOneShot(hoverSound);
    }
}