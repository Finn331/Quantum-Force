using UnityEngine;
using UnityEngine.Events;

public class WeightPuzzleManager : MonoBehaviour
{
    [Header("Puzzle Components")]
    [Tooltip("Masukkan semua tombol (WeightButton) yang menjadi bagian dari puzzle ini.")]
    public WeightButton[] buttons;

    [Header("Puzzle Event")]
    [Tooltip("Event yang akan dipicu ketika SEMUA tombol berhasil ditekan.")]
    public UnityEvent onPuzzleSolved;

    private bool isSolved = false;

    // Fungsi ini akan dipanggil oleh setiap tombol saat statusnya berubah
    public void CheckPuzzleState()
    {
        // Jika puzzle sudah terpecahkan, jangan cek lagi
        if (isSolved) return;

        // Cek apakah semua tombol sedang ditekan
        foreach (var button in buttons)
        {
            if (!button.IsPressed)
            {
                // Jika ada satu saja tombol yang tidak ditekan, puzzle belum selesai.
                return;
            }
        }

        // Jika loop selesai tanpa keluar, artinya semua tombol sudah ditekan
        SolvePuzzle();
    }

    private void SolvePuzzle()
    {
        isSolved = true;
        Debug.Log("PUZZLE TERPECAHKAN! Semua tombol aktif.");

        // Picu event utama
        onPuzzleSolved.Invoke();
    }
}