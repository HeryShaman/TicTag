using UnityEngine;
using UnityEngine.EventSystems;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject firstSelected;   // bouton "Reprendre" (navigation clavier / manette)

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void Show()
    {
        if (panel != null) panel.SetActive(true);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(firstSelected);
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void OnResumeClicked()
    {
        if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
    }

    public void OnRestartClicked()
    {
        if (GameManager.Instance != null) GameManager.Instance.RestartGame();
    }

    public void OnQuitToMenuClicked()
    {
        if (GameManager.Instance != null) GameManager.Instance.QuitToMenu();
    }
}