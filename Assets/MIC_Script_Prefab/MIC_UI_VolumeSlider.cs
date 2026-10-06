using UnityEngine;
using UnityEngine.UI;

public class MIC_UI_VolumeSlider : MonoBehaviour
{
    public Slider slider;

    private void ResolveReferences()
    {
        if (slider == null)
        {
            slider = GameObject.Find("Slider AUDIO")?.GetComponent<Slider>();
        }
    }

    public void Start()
    {
        //slider.value = MIC_AudioManager.Instance.masterVolume;
        slider.onValueChanged.AddListener(OnValueChanged); 
    }

    public void OnValueChanged(float value)
    {
        MIC_AudioManager.Instance.SetVolume(value);
    }

    private void OnEnable()
    {
        //SceneReferenceResolver.OnSceneReady += ResolveReferences;
    }

    private void OnDisable()
    {
        //SceneReferenceResolver.OnSceneReady -= ResolveReferences;
    }


}