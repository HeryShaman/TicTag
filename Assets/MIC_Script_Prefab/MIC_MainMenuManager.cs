using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MIC_MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject levelSelectPanel;
    public GameObject optionsPanel;
    public GameObject Button_Option;
    public GameObject Button_Selection;
    public GameObject Button_Quit;



    [Header("First Level")]
    public string firstLevelName = "GameScene_1";

    private void Start()
    {
        OpenMainPanel();

        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // =========================
    // PANELS
    // =========================


    public void OpenMainPanel()
    {

        mainPanel.SetActive(true);
        levelSelectPanel.SetActive(false);
        optionsPanel.SetActive(false);

    }

    public void OpenLevelSelect()
    {
        mainPanel.SetActive(false);
        levelSelectPanel.SetActive(true);
        optionsPanel.SetActive(false);
    }

    public void OpenOptions()
    {
        mainPanel.SetActive(false);
        levelSelectPanel.SetActive(false);
        optionsPanel.SetActive(true);

    }

    // =========================
    // GAME
    // =========================

    public void PlayGame()
    {
        SceneManager.LoadScene(firstLevelName);
    }

    public void LoadLevel(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    //IEnumerator WaitTimeOption()
    //{
    //    yield return new WaitForSecondsRealtime(1);


    //    mainPanel.SetActive(false);
    //    levelSelectPanel.SetActive(false);
    //    optionsPanel.SetActive(true);
    //    Button_Option.transform.localScale = new Vector3(2, 2, 2);
    //}

    //IEnumerator WaitTimeSelect()
    //{
    //    yield return new WaitForSecondsRealtime(1);

    //    mainPanel.SetActive(false);
    //    levelSelectPanel.SetActive(true);
    //    optionsPanel.SetActive(false);
    //    Button_Selection.transform.localScale = new Vector3(2, 2, 2);
    //}


    //IEnumerator WaitTimeMainMenu()
    //{
    //    yield return new WaitForSecondsRealtime(1);

    //    mainPanel.SetActive(true);
    //    levelSelectPanel.SetActive(false);
    //    optionsPanel.SetActive(false);
    //    Button_Option.transform.localScale = new Vector3(2, 2, 2);
    //    Button_Selection.transform.localScale = new Vector3(2, 2, 2);
    //    Button_Quit.transform.localScale = new Vector3(2, 2, 2);
    //}


}