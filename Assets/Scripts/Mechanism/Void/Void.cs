using cowsins;
using UnityEngine;

public class Void : MonoBehaviour
{
    [Header("Damage Setting")]
    [SerializeField] float damageToPlayer;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStats playerStats = other.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.Damage(damageToPlayer, false);
            }
        }
    }
}
