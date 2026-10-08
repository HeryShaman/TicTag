using UnityEngine;
using UnityEngine.EventSystems;

public class UIButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler {
    public UIAudioSource uiAudioSource;

    public void OnPointerEnter(PointerEventData eventData) {
        print("hovered");
        uiAudioSource.PlayUIHover();
    }

    public void OnPointerClick(PointerEventData eventData) {
        print("clicked");
        uiAudioSource.PlayUIClick();
    }
}