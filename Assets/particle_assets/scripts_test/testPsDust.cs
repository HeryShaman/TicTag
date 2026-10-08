using UnityEngine;

public class testPsDust : MonoBehaviour
{
    public ParticleSystem puff;

    void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        transform.position += new Vector3(x, y, 0) * 5f * Time.deltaTime;

        var em = puff.emission;
        em.enabled = x != 0 || y != 0;
    }
}
