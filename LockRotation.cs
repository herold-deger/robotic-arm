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
        // Empêche la rotation même si elle est appliquée via grab
        transform.rotation = initialRotation;
        rb.angularVelocity = Vector3.zero;
    }
}
