using UnityEngine;
using UnityEngine.Events;

public class WeightPuzzleManager : MonoBehaviour
{
    [Header("Puzzle Components")]
    [Tooltip("Semua tombol (WeightButton) bagian dari puzzle ini.")]
    public WeightButton[] buttons;

    [Tooltip("Cari otomatis tombol di anak-objek jika array kosong.")]
    public bool autoFindButtons;

    [Header("Puzzle Event")]
    [Tooltip("Event yang dipicu ketika SEMUA tombol berhasil ditekan.")]
    public UnityEvent onPuzzleSolved;

    private bool isSolved = false;

    private void Awake()
    {
        if ((buttons == null || buttons.Length == 0) && autoFindButtons)
            buttons = GetComponentsInChildren<WeightButton>(true);
    }

    // Dipanggil oleh tombol saat statusnya berubah
    public void CheckPuzzleState()
    {
        if (isSolved) return;
        if (buttons == null || buttons.Length == 0) return;

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null || !buttons[i].IsPressed)
                return; // ada yang belum pressed
        }

        SolvePuzzle();
    }

    private void SolvePuzzle()
    {
        isSolved = true;
        Debug.Log("[WeightPuzzleManager] PUZZLE TERPECAHKAN! Semua tombol aktif.");
        onPuzzleSolved?.Invoke();
    }
}
