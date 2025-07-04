using UnityEngine;

public class CollisionReporter : MonoBehaviour
{
    public bool collisionDetected = false;

    void OnCollisionEnter(Collision collision)
    {
        collisionDetected = true;
    }

    public void ResetCollision()
    {
        collisionDetected = false;
    }
}
