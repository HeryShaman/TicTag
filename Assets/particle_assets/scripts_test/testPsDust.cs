using UnityEngine;

public class testPsDust : MonoBehaviour
{
    public ParticleSystem particleEffect;

    void Start()
    {
        // Get the ParticleSystem component if it's attached to the GameObject or a child
        if (particleEffect == null)
        {
            particleEffect = GetComponentInChildren<ParticleSystem>();
        }
    }

    public void TriggerEffect()
    {
        if (particleEffect != null)
        {
            particleEffect.Play();
        }
    }
    //public bool x;
    //public ParticleSystem dust;

    //void Update()
    //{
    //    if (x)
    //    {

    //        dust.Play();
    //    }

    //    else if (!x)
    //    {

    //        dust.Stop();

    //        //dust.Pause();
    //        //dust.Clear();
    //    }
    //}
}
