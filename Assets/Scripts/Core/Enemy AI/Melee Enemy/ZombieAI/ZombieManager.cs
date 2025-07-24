using System.Collections.Generic;
using UnityEngine;

public class ZombieManager : MonoBehaviour
{
    public static ZombieManager Instance;

    [SerializeField] private List<ZombieAI> allZombies = new List<ZombieAI>();

    [Header("Alert Settings")]
    [SerializeField] private float alertRadius = 15f;
    [SerializeField] private int maxAlertCount = 4;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void RegisterZombie(ZombieAI zombie)
    {
        if (!allZombies.Contains(zombie))
            allZombies.Add(zombie);
    }

    public void UnregisterZombie(ZombieAI zombie)
    {
        if (allZombies.Contains(zombie))
            allZombies.Remove(zombie);
    }

    public void AlertNearbyZombies(Vector3 position)
    {
        int alerted = 0;
        foreach (var zombie in allZombies)
        {
            if (zombie == null || zombie.IsProvoked) continue;

            float distance = Vector3.Distance(zombie.transform.position, position);
            if (distance <= alertRadius)
            {
                zombie.ReceiveLocalAlert();
                alerted++;

                if (alerted >= maxAlertCount)
                    break;
            }
        }
    }

    // Debug Raycast
    private void OnDrawGizmosSelected()
    {
        // Mengatur warna untuk gizmo (visualisasi)
        Gizmos.color = Color.yellow;

        // Menggambar bola kawat di posisi objek dengan radius dari variabel alertRadius
        Gizmos.DrawWireSphere(transform.position, alertRadius);
    }
}