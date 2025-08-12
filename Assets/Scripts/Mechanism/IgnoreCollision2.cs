using UnityEngine;

// Pastikan objek ini memiliki Collider dan Rigidbody
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class IgnoreCollision2 : MonoBehaviour
{
    [Header("Pengaturan Tabrakan")]
    [Tooltip("Tag dari objek yang tabrakannya ingin diabaikan.")]
    public string tagToIgnore = "Board";

    private Collider myCollider;

    private void Awake()
    {
        // Dapatkan collider dari objek ini sendiri
        myCollider = GetComponent<Collider>();
    }

    // Fungsi ini terpanggil saat pertama kali terjadi tabrakan fisik
    private void OnCollisionEnter(Collision collision)
    {
        // Cek apakah objek yang ditabrak memiliki tag yang ingin diabaikan
        if (collision.gameObject.CompareTag(tagToIgnore))
        {
            // Jika ya, perintahkan mesin fisika untuk mengabaikan tabrakan
            // antara collider kita dan collider objek tersebut
            Physics.IgnoreCollision(myCollider, collision.collider, true);
            Debug.Log("Mengabaikan tabrakan dengan: " + collision.gameObject.name);
        }
    }
}