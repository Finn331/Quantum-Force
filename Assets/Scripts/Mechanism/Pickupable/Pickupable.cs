using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Pickupable : MonoBehaviour
{
    [Header("Throwing Properties")]
    [Tooltip("Centang jika objek ini harus berputar saat dilempar (contoh: bola).")]
    public bool shouldSpin = false;

    [Tooltip("Kekuatan putaran saat dilempar. Hanya berfungsi jika 'Should Spin' dicentang.")]
    public float spinForce = 20f;
}