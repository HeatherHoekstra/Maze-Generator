using UnityEngine;

public class Rotator : MonoBehaviour
{
    [SerializeField] private float speed;

    private void Update()
    {
        //Rotate the object along the z-axis
        transform.Rotate(speed * Time.deltaTime * Vector3.forward);
    }
}