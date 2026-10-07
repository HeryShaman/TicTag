using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class MIC_Result_Level_UI : MonoBehaviour
{
    [Header("UI")]
    public GameObject resultUI;

    public TextMeshProUGUI titleText;

    public Button restartButton;
    public Button nextButton;
    public Button menuButton;

    [Header("Timer Display")]
    public TextMeshProUGUI timeText;

    [Header("Music")]
    public AudioSource musicSource;
    public AudioClip victoryMusic;
    public AudioClip defeatMusic;

    [Header("Scenes")]
    public string nextSceneName;
    public string menuSceneName = "MIC_MENU";

    private void Awake()
    {
        if (resultUI != null)
            resultUI.SetActive(false);
    }

    // =====================================
    // AFFICHAGE
    // =====================================


    //private string FormatTime(float t)
    //{
    //    int minutes = Mathf.FloorToInt(t / 60f);
    //    int seconds = Mathf.FloorToInt(t % 60f);

    //    return string.Format("{0:00}:{1:00}", minutes, seconds);
    //}

    //public void ShowVictory(float currentTime, float maxTime)
    //{
    //    if (resultUI != null)
    //        resultUI.SetActive(true);

    //    titleText.text = "VICTOIRE";

    //    SetTimeDisplay(currentTime, maxTime);

    //    if (nextButton != null)
    //        nextButton.gameObject.SetActive(true);

    //    if (musicSource != null && victoryMusic != null)
    //    {
    //        musicSource.Stop();
    //        musicSource.clip = victoryMusic;
    //        musicSource.Play();
    //    }

    //    OpenUI();
    //}

    //public void ShowDefeat(float currentTime, float maxTime)
    //{
    //    if (resultUI != null)
    //        resultUI.SetActive(true);

    //    titleText.text = "ECHEC";

    //    SetTimeDisplay(currentTime, maxTime);

    //    if (nextButton != null)
    //        nextButton.gameObject.SetActive(false);

    //    if (musicSource != null && defeatMusic != null)
    //    {
    //        musicSource.Stop();
    //        musicSource.clip = defeatMusic;
    //        musicSource.Play();
    //    }

    //    OpenUI();
    //}

    private void OpenUI()
    {
        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }



    // =====================================
    // BOUTONS
    // =====================================

    public void RestartLevel()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }

    //public void NextLevel()
    //{
    //    Time.timeScale = 1f;

    //    SceneManager.LoadScene(nextSceneName);
    //}

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(menuSceneName);
    }

    //private void SetTimeDisplay(float currentTime, float maxTime)
    //{
    //    if (timeText == null)
    //        return;

    //    timeText.text =
    //        $"{FormatTime(currentTime)} / {FormatTime(maxTime)}";
    //}


}

