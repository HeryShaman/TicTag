using UnityEngine;


public class SpinObject : MonoBehaviour
{
    [SerializeField] private Vector3 axis = Vector3.up;
    [SerializeField] private float degreesPerSecond = 40f;
    [SerializeField] private Space space = Space.Self;

    private void Update()
    {
        transform.Rotate(axis.normalized, degreesPerSecond * Time.deltaTime, space);
    }
}