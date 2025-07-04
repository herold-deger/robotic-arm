using UnityEngine;

public class ArmSpawner : MonoBehaviour
{
    public GameObject armPrefab; 
    public Transform baseSpawn;  

    public float spawnDelay = 60f; 

    private GameObject currentArmInstance;

    void Start()
    {
        Invoke(nameof(SpawnArm), spawnDelay);
    }

    void SpawnArm()
    {
        if (armPrefab == null || baseSpawn == null)
        {
            return;
        }

       

        
        currentArmInstance = Instantiate(
            armPrefab,
            baseSpawn.position,
            baseSpawn.rotation
        );
    }
}
