using UnityEngine;

public class Seesaw : MonoBehaviour
{
    [Header("Seesaw Settings")]
    [Tooltip("Spawn falling object")]
    [SerializeField] GameObject fallingObjectPrefab;
    [Tooltip("The point where the falling object will spawn.")]
    [SerializeField] Transform fallingObjectSpawnPoint;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SpawnBall()
    {
        Instantiate(fallingObjectPrefab, fallingObjectSpawnPoint.position, fallingObjectSpawnPoint.rotation);
    }
}
