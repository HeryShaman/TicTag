using UnityEngine;
using UnityEngine.EventSystems;

public class MIC_Manette_UI : MonoBehaviour
{
    public GameObject firstSelected;

    private void OnEnable()
    {
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstSelected);
    }
}
