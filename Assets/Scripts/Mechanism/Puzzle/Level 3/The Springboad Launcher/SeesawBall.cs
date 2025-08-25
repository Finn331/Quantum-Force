using UnityEngine;

public class SeesawBall : MonoBehaviour
{
    [Header("Seesaw Ball Timer")]
    [SerializeField] float ballTimer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TimeToDestroy();
    }
        
    void TimeToDestroy()
    {
        Destroy(gameObject, ballTimer);
    }   
}
