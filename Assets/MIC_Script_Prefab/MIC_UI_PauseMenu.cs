using UnityEngine;

public class MIC_UI_PauseMenu : MonoBehaviour
{
    public static MIC_UI_PauseMenu Instance;

    public GameObject panel;
    public GameObject gamemanager;

    private void Start()
    {
        //if (gamemanager == null)
        //{
        //    MIC_GameManager manager = FindFirstObjectByType<MIC_GameManager>();

        //    if (manager != null)
        //        gamemanager = manager.gameObject;
        //}
    }

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;


        }

        Instance = this;

        //DontDestroyOnLoad(gameObject); // PERSISTANT
    }

    private void OnEnable()
    {
        //panel.SetActive(false); // reset visuel
        //SceneReferenceResolver.OnSceneReady += ResolveReferences;
    }

    private void OnDisable()
    {
        //SceneReferenceResolver.OnSceneReady -= ResolveReferences;
    }

    public void SetActive(bool state)
    {
        panel.SetActive(state);
    }



    public void Resume()
    {
        //gamemanager.GetComponent<MIC_GameManager>().TogglePause();
    }

    private void ResolveReferences()
    {
        if (panel == null)
        {
            panel = GameObject.Find("Menu Pause");
        }
    }
}
