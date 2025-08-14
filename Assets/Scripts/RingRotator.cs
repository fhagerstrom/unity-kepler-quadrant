using UnityEngine;

public class RingRotator : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 30f;

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
    }
}
