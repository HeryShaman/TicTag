using UnityEngine;

public class MIC_UI_ReturnMenuButton : MonoBehaviour
{
    public GameObject gamemanager;

    public void ReturnToMenu()
    {
        if (gamemanager == null)
        {
            MIC_GameManager manager = FindFirstObjectByType<MIC_GameManager>();

            if (manager != null)
                gamemanager = manager.gameObject;
        }

        if (gamemanager != null)
        {
            gamemanager.GetComponent<MIC_GameManager>().ReturnToMenu();
        }
        else
        {
            Debug.LogWarning("MIC_GameManager introuvable");
        }
    }
}
