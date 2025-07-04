using UnityEngine;

public class LockRotation : MonoBehaviour
{
    private Rigidbody rb;
    private Quaternion initialRotation;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        initialRotation = transform.rotation;
    }

    void FixedUpdate()
    {
        // Emp�che la rotation m�me si elle est appliqu�e via grab
        transform.rotation = initialRotation;
        rb.angularVelocity = Vector3.zero;
    }
}
