using UnityEngine;

public class ZombieSFX : MonoBehaviour
{
    // FOR LOD ONLY
    [SerializeField] ZombieAI zombieAI;

    public void PlayRageSFX()
    {
        zombieAI.PlayRageSFX();
    }

    public void DealDamage()
    {
        zombieAI.DealDamage();
    }
}
