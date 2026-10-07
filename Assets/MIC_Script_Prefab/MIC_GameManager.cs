using UnityEngine;
using UnityEngine.SceneManagement;

public class MIC_GameManager : MonoBehaviour
{

    private bool isPaused = false;
    public string menuSceneName = "MIC_MENU";
    public string Scene_Jeu = "JEU";
    public bool DEBUF_Fin = false;

    public GameObject MIC_UI_PauseMenu;
    public GameObject MIC_UI_FIN;


    private void Update()
    {
        if (Input.GetButtonDown("Menu Pause"))
        {
            TogglePause();
        }

        if (Input.GetButtonDown("Debug_Fin") && DEBUF_Fin == true)
        {
            Debug.Log("Je met la pause");
            ToggleFin();
        }
    }

    public void PlayGame()
    {
        Time.timeScale = 1f;
        //SceneManager.LoadScene("GameScene_1");
        SceneManager.LoadScene(Scene_Jeu);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        Time.timeScale = isPaused ? 0f : 1f;

        // GESTION SOURIS
        if (isPaused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (MIC_UI_PauseMenu != null)
            MIC_UI_PauseMenu.SetActive(isPaused);

        


    }


    public void ToggleFin()
    {


        MIC_UI_FIN.SetActive(true);
    }


    public void ReturnToMenu()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(menuSceneName);
    }



}