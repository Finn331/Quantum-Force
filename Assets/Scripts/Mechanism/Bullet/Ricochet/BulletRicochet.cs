using cowsins;
using UnityEngine;

public class BulletRicochet : MonoBehaviour
{
    [Header("Ricochet Setting")]
    public int maxRicochets = 3;
    private int ricochetCount = 0;
    private Vector3 currentDirection;

    [Header("Scripts Reference")]
    public Bullet bullet;

    void Start()
    {
        currentDirection = transform.forward;
    }

    void Update()
    {
        transform.position += currentDirection * bullet.speed * Time.deltaTime;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (ricochetCount >= maxRicochets)
        {
            Destroy(gameObject);
            return;
        }

        ContactPoint contact = collision.contacts[0];
        Vector3 reflectDir = Vector3.Reflect(currentDirection, contact.normal);
        currentDirection = reflectDir.normalized;
        transform.forward = currentDirection; // update arah visual

        ricochetCount++;
    }
}
