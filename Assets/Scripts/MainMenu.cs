using UnityEngine;
using UnityEngine.EventSystems;


public class MainMenu : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private GameObject firstSelected;   // bouton Jouer (navigation clavier / manette)

    [SerializeField] private MusicPlayer music;
    [SerializeField] private AudioClip menuMusic;
    [SerializeField, Min(0f)] private float musicFadeIn = 1.5f;
    [SerializeField, Min(0f)] private float musicFadeOut = 0.6f;

    private bool _loading;

    private void Start()
    {
        Time.timeScale = 1f;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(firstSelected);

        if (music != null && menuMusic != null)
            music.Play(menuMusic, musicFadeIn);
    }

    public void OnPlayClicked()
    {
        if (_loading || SceneTransition.IsBusy) return;
        _loading = true;

        if (music != null) music.Stop(musicFadeOut);

        SceneTransition.LoadScene(gameSceneName);
    }

    public void OnQuitClicked()
    {
        if (_loading) return;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}