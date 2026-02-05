# White Box Testing - Flowgraphs

Dokumen ini merepresentasikan **Control Flow Graph (CFG)** untuk beberapa fungsi utama yang diuji dalam White Box Testing.

## 1. MenuManager: ContinueGame()
Fungsi ini menangani logika memuat permainan yang tersimpan.

**Logika:**
1.  Set flag `resetOnStart` ke `false`.
2.  Ambil nama scene yang tersimpan.
3.  **Decision Node:** Apakah nama scene kosong?
    *   **Yes:** Gunakan default scene ("Gameplay").
    *   **No:** Gunakan scene yang tersimpan.
4.  Load Scene.

```mermaid
flowchart TD
    A([Start]) --> B[Set resetOnStart = false]
    B --> C[Get Saved Scene]
    C --> D{Is SavedScene Empty?}
    D -- Yes --> E[Set SavedScene = Default]
    D -- No --> F[Keep SavedScene]
    E --> G[Load Scene]
    F --> G
    G --> H([End])
```

## 2. MenuManager: CheckSavedGame()
Fungsi ini menentukan apakah tombol "Continue" harus aktif atau tidak.

**Logika:**
1.  **Decision Node:** Apakah `continueButton` tidak null?
    *   **No:** Selesai.
    *   **Yes:** Lanjut.
2.  Ambil nama scene tersimpan.
3.  Cek apakah scene valid ATAU file save ada.
4.  Set properti `interactable` pada tombol sesuai hasil cek.

```mermaid
flowchart TD
    A([Start]) --> B{continueButton != null?}
    B -- No --> Z([End])
    B -- Yes --> C[Get Saved Scene]
    C --> D[Check hasSave condition]
    D --> E[Set continueButton.interactable]
    E --> Z
```

## 3. SaveManager: SaveGame()
Fungsi ini menyimpan data permainan ke file binary.

**Logika:**
1.  Inisialisasi `BinaryFormatter` & `FileStream`.
2.  Buat objek data (`PlayerData_Storage`).
3.  **Decision Node:** Apakah `playerPosition` ada nilainya?
    *   **Yes:** Simpan posisi pemain.
    *   **No:** Lewati.
4.  **Decision Node:** Apakah `weaponController` ada (save inventory)?
    *   **Yes:** Loop slot inventory -> Simpan state senjata.
    *   **No:** Lewati.
5.  Serialize data ke file.
6.  **Exception Handling:** Jika error, log error.

```mermaid
flowchart TD
    A([Start]) --> B[Init BinaryFormatter & FileStream]
    B --> C[Create PlayerData Object]
    C --> D{Has Player Position?}
    D -- Yes --> E[Save Position Data]
    D -- No --> F
    E --> F{Has WeaponController?}
    F -- Yes --> G[Loop Inventory & Save Weapon States]
    F -- No --> H
    G --> H[Serialize Data to File]
    H --> I([End])
    
    B -.-> X[Catch Exception]
    C -.-> X
    H -.-> X
    X --> Y[Log Error] --> I
```

## 4. LevelManager: FinishLevel()
Fungsi yang dipanggil saat pemain menyelesaikan level.

**Logika:**
1.  **Decision Node:** Apakah `finishOpened` sudah true?
    *   **Yes:** Return (cegah spam).
    *   **No:** Set `finishOpened` = true.
2.  Unlock kursor & disable player control.
3.  Save Game.
4.  Update teks UI (Timer, Score).
5.  Mainkan animasi panel (LeanTween).

```mermaid
flowchart TD
    A([Start]) --> B{finishOpened == true?}
    B -- Yes --> Z([End])
    B -- No --> C[Set finishOpened = true]
    C --> D[Unlock Cursor & Disable Controls]
    D --> E[Save Game]
    E --> F[Update UI Texts using Data]
    F --> G[Play Open Animation]
    G --> Z
```
