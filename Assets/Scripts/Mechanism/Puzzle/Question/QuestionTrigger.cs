using cowsins;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Data class untuk menyimpan satu pertanyaan quiz dengan dukungan multi-bahasa
/// </summary>
[System.Serializable]
public class QuizQuestion
{
    [Header("Indonesian (Bahasa Indonesia)")]
    [TextArea(2, 4)]
    public string questionTextID;
    public string[] answerOptionsID = new string[4]; // A, B, C, D

    [Header("English")]
    [TextArea(2, 4)]
    public string questionTextEN;
    public string[] answerOptionsEN = new string[4]; // A, B, C, D

    [Header("Answer Key")]
    [Tooltip("Index jawaban benar dari data asli (0 = A, 1 = B, 2 = C, 3 = D)")]
    [Range(0, 3)]
    public int correctAnswerIndex;

    /// <summary>
    /// Mendapatkan teks soal sesuai bahasa aktif dari LanguageManager
    /// </summary>
    public string GetQuestionText()
    {
        if (LanguageManager.CurrentLanguage == GameLanguage.English)
            return !string.IsNullOrEmpty(questionTextEN) ? questionTextEN : questionTextID;
        return !string.IsNullOrEmpty(questionTextID) ? questionTextID : questionTextEN;
    }

    /// <summary>
    /// Mendapatkan opsi jawaban sesuai bahasa aktif dari LanguageManager
    /// </summary>
    public string[] GetAnswerOptions()
    {
        if (LanguageManager.CurrentLanguage == GameLanguage.English)
        {
            if (answerOptionsEN != null && answerOptionsEN.Length > 0 && !string.IsNullOrEmpty(answerOptionsEN[0]))
                return answerOptionsEN;
            return answerOptionsID;
        }

        if (answerOptionsID != null && answerOptionsID.Length > 0 && !string.IsNullOrEmpty(answerOptionsID[0]))
            return answerOptionsID;
        return answerOptionsEN;
    }
}

/// <summary>
/// Enum untuk memilih kategori soal Hukum Newton
/// </summary>
public enum NewtonLawType
{
    Newton1 = 1,
    Newton2 = 2,
    Newton3 = 3
}

public class QuestionTrigger : MonoBehaviour
{
    [Header("Question Source")]
    [Tooltip("Jika true, gunakan soal bawaan yang sudah di-hardcode. Jika false, gunakan soal dari array questions.")]
    [SerializeField] private bool useBuiltInQuestions = true;

    [Tooltip("Pilih Hukum Newton mana yang akan digunakan (hanya berlaku jika useBuiltInQuestions = true)")]
    [SerializeField] private NewtonLawType newtonLawType = NewtonLawType.Newton1;

    [Header("Custom Questions (Optional)")]
    [Tooltip("Daftar soal custom. Hanya digunakan jika useBuiltInQuestions = false.")]
    [SerializeField] private QuizQuestion[] questions;

    [Header("UI References")]
    [SerializeField] private GameObject questionPanel;
    [SerializeField] private AudioClip panelOpenSFX;
    [SerializeField] private GameObject rightOrWrongTextPanel;
    [SerializeField] private GameObject rightAnswerText;
    [SerializeField] private GameObject wrongAnswerText;

    [Header("Question Display UI")]
    [Tooltip("Text untuk menampilkan soal")]
    [SerializeField] private TMP_Text questionDisplayText;
    [Tooltip("Button-button jawaban (urutan UI: A, B, C, D)")]
    [SerializeField] private Button[] answerButtons;
    [Tooltip("Text pada setiap button jawaban (urutan UI: A, B, C, D)")]
    [SerializeField] private TMP_Text[] answerButtonTexts;

    [Header("Player Reference")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private GameObject crosshair;
    [SerializeField] LevelManager levelManager;

    [Header("Custom Events")]
    [Tooltip("Dipanggil ketika jawaban benar.")]
    [SerializeField] private UnityEvent onRightAnswer;
    [Tooltip("Dipanggil ketika jawaban salah.")]
    [SerializeField] private UnityEvent onWrongAnswer;

    // PlayerMovement Settings
    private float originalWalkSpeed = 5f;
    private float originalRunSpeed = 10f;
    private float originalAcceleration = 4500f;
    private float originalJumpForce = 10f;
    private float cameraSensitivity = 4f;

    private GameObject localGameobject;

    // --- QUIZ STATE ---
    private bool isAnswered = false;
    private QuizQuestion currentQuestion;
    private List<int> availableQuestionIndices = new List<int>();
    private QuizQuestion[] activeQuestions; // Soal yang aktif digunakan

    // --- MODIFIKASI: Variable untuk menyimpan index tombol yang benar saat ini ---
    private int currentCorrectButtonIndex;

    private void Start()
    {
        localGameobject = gameObject;
        LoadQuestions();
        InitializeQuestionPool();
        SetupAnswerButtonListeners();
    }

    /// <summary>
    /// Load soal berdasarkan pengaturan (built-in atau custom)
    /// </summary>
    private void LoadQuestions()
    {
        if (useBuiltInQuestions)
        {
            activeQuestions = GetBuiltInQuestions(newtonLawType);
            Debug.Log($"QuestionTrigger: Loaded {activeQuestions.Length} built-in questions for Newton Law {(int)newtonLawType}");
        }
        else
        {
            activeQuestions = questions;
            Debug.Log($"QuestionTrigger: Using {activeQuestions?.Length ?? 0} custom questions");
        }
    }

    /// <summary>
    /// Inisialisasi pool index soal yang tersedia
    /// </summary>
    private void InitializeQuestionPool()
    {
        availableQuestionIndices.Clear();
        if (activeQuestions == null || activeQuestions.Length == 0)
        {
            Debug.LogWarning("QuestionTrigger: Tidak ada soal yang tersedia!");
            return;
        }

        for (int i = 0; i < activeQuestions.Length; i++)
        {
            availableQuestionIndices.Add(i);
        }
    }

    /// <summary>
    /// Generate soal bawaan berdasarkan Hukum Newton yang dipilih
    /// </summary>
    private QuizQuestion[] GetBuiltInQuestions(NewtonLawType lawType)
    {
        switch (lawType)
        {
            case NewtonLawType.Newton1:
                return GetNewton1Questions();
            case NewtonLawType.Newton2:
                return GetNewton2Questions();
            case NewtonLawType.Newton3:
                return GetNewton3Questions();
            default:
                return GetNewton1Questions();
        }
    }

    #region Newton Law 1 Questions
    private QuizQuestion[] GetNewton1Questions()
    {
        return new QuizQuestion[]
        {
            // Soal 1
            new QuizQuestion
            {
                questionTextID = "Kamu sedang berada di kereta yang bergerak sangat cepat dalam garis lurus. Jika kamu melempar bola lurus ke atas, di mana bola itu akan jatuh?",
                answerOptionsID = new string[] {
                    "Di belakangmu, karena kereta bergerak maju.",
                    "Kembali tepat ke tanganmu.",
                    "Di depanmu, karena bola mendapat dorongan tambahan.",
                    "Di sampingmu."
                },
                questionTextEN = "You are on a train moving very fast in a straight line. If you throw a ball straight up, where will it land?",
                answerOptionsEN = new string[] {
                    "Behind you, because the train is moving forward.",
                    "Right back into your hands.",
                    "In front of you, because the ball gets an extra push.",
                    "To your side."
                },
                correctAnswerIndex = 2
            },
            // Soal 2
            new QuizQuestion
            {
                questionTextID = "Sir Isaac Newton menyadari bahwa Bulan tetap pada orbit lingkarannya dan tidak bergerak lurus karena adanya...",
                answerOptionsID = new string[] {
                    "Hambatan udara di ruang angkasa.",
                    "Gaya yang bekerja pada Bulan (gaya gravitasi).",
                    "Kecepatan Bulan yang selalu berubah-ubah.",
                    "Sifat alami benda langit yang selalu berputar."
                },
                questionTextEN = "Sir Isaac Newton realized that the Moon stays in its circular orbit and does not move in a straight line because of...",
                answerOptionsEN = new string[] {
                    "Air resistance in space.",
                    "A force acting on the Moon (gravitational force).",
                    "The Moon's constantly changing speed.",
                    "The natural tendency of celestial bodies to always rotate."
                },
                correctAnswerIndex = 1
            },
            // Soal 3
            new QuizQuestion
            {
                questionTextID = "Jika gaya tarik dari Bumi tiba-tiba menghilang, maka sesuai dengan Hukum I Newton, lintasan Bulan akan berubah dari lingkaran menjadi...",
                answerOptionsID = new string[] {
                    "Garis lurus.",
                    "Spiral menjauhi Bumi.",
                    "Diam tak bergerak.",
                    "Berbalik arah menuju Matahari."
                },
                questionTextEN = "If the gravitational pull from Earth suddenly disappeared, then according to Newton's First Law, the Moon's trajectory would change from a circle to...",
                answerOptionsEN = new string[] {
                    "A straight line.",
                    "A spiral moving away from Earth.",
                    "Stationary and motionless.",
                    "Reversing direction toward the Sun."
                },
                correctAnswerIndex = 0
            },
            // Soal 4
            new QuizQuestion
            {
                questionTextID = "Sifat benda yang cenderung mempertahankan keadaannya (diam atau bergerak lurus beraturan) disebut sebagai...",
                answerOptionsID = new string[] {
                    "Gaya tarik.",
                    "Berat benda.",
                    "Kelembaman (Inersia).",
                    "Percepatan."
                },
                questionTextEN = "The property of an object that tends to maintain its state (at rest or in uniform linear motion) is called...",
                answerOptionsEN = new string[] {
                    "Attractive force.",
                    "Weight of the object.",
                    "Inertia.",
                    "Acceleration."
                },
                correctAnswerIndex = 2
            },
            // Soal 5
            new QuizQuestion
            {
                questionTextID = "Mengapa sebuah benda yang dilepaskan dari ketinggian tertentu di atas permukaan Bumi tidak bergerak lurus ke samping, melainkan jatuh ke tanah?",
                answerOptionsID = new string[] {
                    "Karena benda tersebut ingin mempertahankan posisinya.",
                    "Karena benda tersebut tidak memiliki massa.",
                    "Karena adanya gaya tarik yang disebut gaya gravitasi.",
                    "Karena benda mengikuti rotasi Bumi ke arah bawah."
                },
                questionTextEN = "Why does an object released from a certain height above Earth's surface not move sideways but instead falls to the ground?",
                answerOptionsEN = new string[] {
                    "Because the object wants to maintain its position.",
                    "Because the object has no mass.",
                    "Because of an attractive force called gravity.",
                    "Because the object follows Earth's rotation downward."
                },
                correctAnswerIndex = 2
            },
            // Soal 6
            new QuizQuestion
            {
                questionTextID = "Menurut Hukum I Newton, sebuah benda akan tetap diam jika...",
                answerOptionsID = new string[] {
                    "Benda tersebut berada di ruang hampa.",
                    "Tidak ada gaya total (resultan gaya nol) yang bekerja padanya.",
                    "Benda tersebut memiliki massa yang sangat besar.",
                    "Benda tersebut berada sangat jauh dari permukaan Bumi."
                },
                questionTextEN = "According to Newton's First Law, an object will remain at rest if...",
                answerOptionsEN = new string[] {
                    "The object is in a vacuum.",
                    "There is no net force (zero resultant force) acting on it.",
                    "The object has a very large mass.",
                    "The object is very far from Earth's surface."
                },
                correctAnswerIndex = 1
            },
            // Soal 7
            new QuizQuestion
            {
                questionTextID = "Pengetahuan tentang Hukum I Newton sangat membantu untuk memahami mengapa planet tetap pada orbitnya, karena hukum ini menjelaskan...",
                answerOptionsID = new string[] {
                    "Bagaimana cara menghitung massa planet.",
                    "Bahwa diperlukan gaya untuk mengubah arah gerak benda.",
                    "Bahwa semua benda di alam semesta saling tarik-menarik.",
                    "Berapa nilai tetap gravitasi di luar angkasa."
                },
                questionTextEN = "Knowledge of Newton's First Law greatly helps us understand why planets remain in their orbits, because this law explains...",
                answerOptionsEN = new string[] {
                    "How to calculate the mass of a planet.",
                    "That a force is required to change the direction of an object's motion.",
                    "That all objects in the universe attract each other.",
                    "The value of the gravitational constant in space."
                },
                correctAnswerIndex = 1
            },
            // Soal 8
            new QuizQuestion
            {
                questionTextID = "Perhatikan pernyataan: \"Sebuah kapsul bergerak melalui pusat Bumi dan mengalami gaya yang sebanding dengan simpangannya\". Jika gaya tersebut nol tepat di pusat Bumi, maka kapsul akan...",
                answerOptionsID = new string[] {
                    "Langsung berhenti tepat di pusat Bumi.",
                    "Tetap bergerak melewati pusat Bumi karena memiliki kecenderungan gerak (inersia).",
                    "Berubah massanya menjadi nol.",
                    "Terlempar keluar dari atmosfer Bumi."
                },
                questionTextEN = "Consider the statement: \"A capsule moves through Earth's center and experiences a force proportional to its displacement.\" If this force is zero exactly at Earth's center, the capsule will...",
                answerOptionsEN = new string[] {
                    "Stop immediately at Earth's center.",
                    "Continue moving through Earth's center due to its tendency to maintain motion (inertia).",
                    "Have its mass become zero.",
                    "Be ejected out of Earth's atmosphere."
                },
                correctAnswerIndex = 1
            },
            // Soal 9
            new QuizQuestion
            {
                questionTextID = "Di dalam buku dijelaskan bahwa gaya medan (seperti gravitasi) timbul meskipun benda tidak bersentuhan secara fisik. Jika gaya medan ini tidak ada pada dua benda yang diam, maka kedua benda tersebut akan...",
                answerOptionsID = new string[] {
                    "Saling menjauh secara otomatis.",
                    "Bergerak melingkar satu sama lain.",
                    "Tetap diam pada posisinya masing-masing.",
                    "Melebur menjadi satu partikel."
                },
                questionTextEN = "The book explains that field forces (such as gravity) arise even when objects are not in physical contact. If these field forces did not exist between two stationary objects, both objects would...",
                answerOptionsEN = new string[] {
                    "Automatically move away from each other.",
                    "Orbit around each other.",
                    "Remain stationary in their respective positions.",
                    "Merge into a single particle."
                },
                correctAnswerIndex = 2
            },
            // Soal 10
            new QuizQuestion
            {
                questionTextID = "Hukum I Newton menjadi dasar pemikiran Isaac Newton saat ia mengamati apel jatuh, karena ia menyadari bahwa gerak apel yang berubah dari diam menjadi jatuh pasti disebabkan oleh...",
                answerOptionsID = new string[] {
                    "Sifat alami buah apel yang sudah matang.",
                    "Angin yang meniup pucuk pohon.",
                    "Adanya pengaruh gaya luar (gravitasi Bumi).",
                    "Perubahan massa jenis apel."
                },
                questionTextEN = "Newton's First Law became the foundation of Isaac Newton's thinking when he observed a falling apple, because he realized that the apple's motion changing from rest to falling must be caused by...",
                answerOptionsEN = new string[] {
                    "The natural property of a ripe apple.",
                    "Wind blowing through the treetop.",
                    "The influence of an external force (Earth's gravity).",
                    "A change in the apple's density."
                },
                correctAnswerIndex = 2
            }
        };
    }
    #endregion

    #region Newton Law 2 Questions
    private QuizQuestion[] GetNewton2Questions()
    {
        return new QuizQuestion[]
        {
            // Soal 1
            new QuizQuestion
            {
                questionTextID = "Percepatan gravitasi (g) dapat dipandang sebagai dua hal yang berbeda. Ketika kita meninjau benda yang sedang jatuh bebas, g dipandang sebagai...",
                answerOptionsID = new string[] {
                    "Sifat ruang di sekitar benda.",
                    "Kuat medan gravitasi.",
                    "Percepatan gerak benda.",
                    "Massa jenis benda tersebut."
                },
                questionTextEN = "Gravitational acceleration (g) can be viewed as two different things. When we consider an object in free fall, g is viewed as...",
                answerOptionsEN = new string[] {
                    "A property of the space around the object.",
                    "The gravitational field strength.",
                    "The object's acceleration.",
                    "The object's density."
                },
                correctAnswerIndex = 2
            },
            // Soal 2
            new QuizQuestion
            {
                questionTextID = "Jika Anda berada di Bumi dan ingin mengetahui gaya gravitasi yang bekerja pada benda bermassa yang diam, maka nilai 9,8 N/kg dipandang sebagai...",
                answerOptionsID = new string[] {
                    "Percepatan jatuh bebas.",
                    "Kuat medan gravitasi Bumi.",
                    "Jari-jari orbit satelit.",
                    "Tetapan umum gravitasi Newton."
                },
                questionTextEN = "If you are on Earth and want to know the gravitational force acting on a stationary object with mass, then the value of 9.8 N/kg is viewed as...",
                answerOptionsEN = new string[] {
                    "Free-fall acceleration.",
                    "Earth's gravitational field strength.",
                    "The radius of a satellite's orbit.",
                    "Newton's universal gravitational constant."
                },
                correctAnswerIndex = 1
            },
            // Soal 3
            new QuizQuestion
            {
                questionTextID = "Mengapa benda yang berat tidak jatuh lebih cepat daripada benda yang ringan di dekat permukaan Bumi?",
                answerOptionsID = new string[] {
                    "Karena benda berat memiliki inersia yang lebih kecil.",
                    "Karena gaya gravitasi yang bekerja sebanding dengan massa benda tersebut.",
                    "Karena hambatan udara hanya bekerja pada benda yang ringan.",
                    "Karena percepatan gravitasi di permukaan Bumi bernilai nol."
                },
                questionTextEN = "Why doesn't a heavy object fall faster than a light object near Earth's surface?",
                answerOptionsEN = new string[] {
                    "Because heavy objects have smaller inertia.",
                    "Because the gravitational force acting on it is proportional to the object's mass.",
                    "Because air resistance only acts on light objects.",
                    "Because gravitational acceleration at Earth's surface is zero."
                },
                correctAnswerIndex = 1
            },
            // Soal 4
            new QuizQuestion
            {
                questionTextID = "Mengapa percepatan gravitasi di kutub utara lebih besar daripada di khatulistiwa?",
                answerOptionsID = new string[] {
                    "Karena Bumi berbentuk bola sempurna.",
                    "Karena massa Bumi di kutub lebih besar daripada di khatulistiwa.",
                    "Karena jari-jari permukaan Bumi di kutub adalah yang terkecil.",
                    "Karena kutub utara berada lebih dekat dengan Matahari."
                },
                questionTextEN = "Why is gravitational acceleration at the North Pole greater than at the equator?",
                answerOptionsEN = new string[] {
                    "Because Earth is a perfect sphere.",
                    "Because Earth's mass at the poles is greater than at the equator.",
                    "Because Earth's surface radius at the poles is the smallest.",
                    "Because the North Pole is closer to the Sun."
                },
                correctAnswerIndex = 2
            },
            // Soal 5
            new QuizQuestion
            {
                questionTextID = "Berdasarkan Hukum II Newton, arah percepatan gravitasi atau kuat medan gravitasi yang dihasilkan oleh suatu benda selalu...",
                answerOptionsID = new string[] {
                    "Menjauhi pusat massa benda tersebut.",
                    "Tegak lurus dengan garis hubung kedua benda.",
                    "Menuju ke pusat massa benda sumbernya.",
                    "Mengikuti arah rotasi satelit yang mengorbit."
                },
                questionTextEN = "According to Newton's Second Law, the direction of gravitational acceleration or gravitational field strength produced by an object always...",
                answerOptionsEN = new string[] {
                    "Points away from the object's center of mass.",
                    "Is perpendicular to the line connecting both objects.",
                    "Points toward the center of mass of the source object.",
                    "Follows the direction of the orbiting satellite's rotation."
                },
                correctAnswerIndex = 2
            },
            // Soal 6
            new QuizQuestion
            {
                questionTextID = "Jika percepatan gravitasi pada permukaan planet X dihitung menggunakan Hukum II Newton, maka nilai percepatan tersebut akan berbanding terbalik dengan...",
                answerOptionsID = new string[] {
                    "Massa planet tersebut.",
                    "Kuadrat jarak dari pusat massa planet.",
                    "Tetapan gravitasi umum.",
                    "Massa benda uji yang diletakkan di sana."
                },
                questionTextEN = "If the gravitational acceleration on planet X's surface is calculated using Newton's Second Law, that acceleration value will be inversely proportional to...",
                answerOptionsEN = new string[] {
                    "The planet's mass.",
                    "The square of the distance from the planet's center of mass.",
                    "The universal gravitational constant.",
                    "The mass of the test object placed there."
                },
                correctAnswerIndex = 1
            },
            // Soal 7
            new QuizQuestion
            {
                questionTextID = "Apa yang terjadi pada gaya gravitasi saat sebuah kapsul bergerak mendekati pusat Bumi?",
                answerOptionsID = new string[] {
                    "Gaya gravitasi terus meningkat secara drastis.",
                    "Gaya gravitasi berkurang secara linear (garis lurus).",
                    "Gaya gravitasi menjadi tidak terhingga.",
                    "Gaya gravitasi berubah arah menjadi menjauhi Bumi."
                },
                questionTextEN = "What happens to the gravitational force when a capsule moves toward Earth's center?",
                answerOptionsEN = new string[] {
                    "The gravitational force continues to increase dramatically.",
                    "The gravitational force decreases linearly.",
                    "The gravitational force becomes infinite.",
                    "The gravitational force reverses direction away from Earth."
                },
                correctAnswerIndex = 1
            },
            // Soal 8
            new QuizQuestion
            {
                questionTextID = "Sebuah benda bermassa 1 kg memiliki berat 9,83 N di kutub dan 9,78 N di khatulistiwa. Perbedaan berat ini disebabkan oleh perbedaan...",
                answerOptionsID = new string[] {
                    "Kandungan atom dalam benda.",
                    "Nilai percepatan gravitasi di kedua tempat tersebut.",
                    "Waktu yang diperlukan untuk jatuh.",
                    "Jarak benda ke Bulan."
                },
                questionTextEN = "An object with a mass of 1 kg weighs 9.83 N at the pole and 9.78 N at the equator. This difference in weight is caused by the difference in...",
                answerOptionsEN = new string[] {
                    "The atomic content of the object.",
                    "The gravitational acceleration values at both locations.",
                    "The time required to fall.",
                    "The object's distance to the Moon."
                },
                correctAnswerIndex = 1
            },
            // Soal 9
            new QuizQuestion
            {
                questionTextID = "Gaya yang berperan sebagai gaya sentripetal agar satelit tetap mengorbit Bumi tanpa jatuh adalah...",
                answerOptionsID = new string[] {
                    "Gaya gesekan udara.",
                    "Gaya gravitasi Bumi.",
                    "Gaya magnetik kutub.",
                    "Gaya otot dari satelit."
                },
                questionTextEN = "The force that acts as the centripetal force to keep a satellite orbiting Earth without falling is...",
                answerOptionsEN = new string[] {
                    "Air friction force.",
                    "Earth's gravitational force.",
                    "Polar magnetic force.",
                    "Muscular force from the satellite."
                },
                correctAnswerIndex = 1
            },
            // Soal 10
            new QuizQuestion
            {
                questionTextID = "Kamu ingin mendorong dua kereta, satu kosong dan satu penuh, agar bergerak dengan percepatan yang sama. Kereta mana yang membutuhkan gaya yang lebih besar?",
                answerOptionsID = new string[] {
                    "Kereta kosong.",
                    "Keduanya membutuhkan gaya yang sama.",
                    "Kereta penuh.",
                    "Tidak perlu gaya, karena mereka memiliki roda."
                },
                questionTextEN = "You want to push two carts, one empty and one full, to make them move with the same acceleration. Which cart requires a greater force?",
                answerOptionsEN = new string[] {
                    "The empty cart.",
                    "Both require the same force.",
                    "The full cart.",
                    "No force is needed, because they have wheels."
                },
                correctAnswerIndex = 2
            }
        };
    }
    #endregion

    #region Newton Law 3 Questions
    private QuizQuestion[] GetNewton3Questions()
    {
        return new QuizQuestion[]
        {
            // Soal 1
            new QuizQuestion
            {
                questionTextID = "Sesuai dengan prinsip dasar gaya dalam buku, jika sebuah benda (seperti buah apel) mengalami gaya tarik yang menyebabkannya jatuh, maka gaya tersebut haruslah...",
                answerOptionsID = new string[] {
                    "Muncul secara alami dari dalam benda itu sendiri tanpa pengaruh luar.",
                    "Disebabkan oleh interaksi dengan benda lain (dalam hal ini Bumi).",
                    "Terjadi karena benda tersebut kehilangan beratnya di udara.",
                    "Berasal dari gerakan atom di dalam benda yang sedang diam."
                },
                questionTextEN = "According to the fundamental principle of force in the book, if an object (like an apple) experiences an attractive force that causes it to fall, that force must...",
                answerOptionsEN = new string[] {
                    "Arise naturally from within the object itself without external influence.",
                    "Be caused by interaction with another object (in this case, Earth).",
                    "Occur because the object loses its weight in the air.",
                    "Originate from the movement of atoms inside a stationary object."
                },
                correctAnswerIndex = 1
            },
            // Soal 2
            new QuizQuestion
            {
                questionTextID = "Dalam interaksi gravitasi antara Bumi dan Bulan, jika Bumi memberikan gaya tarik pada Bulan, maka pernyataan yang paling tepat menurut Hukum III Newton adalah...",
                answerOptionsID = new string[] {
                    "Bulan tidak memberikan gaya balik karena massanya jauh lebih kecil daripada Bumi.",
                    "Bulan memberikan gaya tarik pada Bumi yang besarnya sama dengan gaya tarik Bumi pada Bulan.",
                    "Bulan memberikan gaya tarik yang lebih kecil karena jaraknya yang sangat jauh dari Bumi.",
                    "Bumi menarik Bulan, sedangkan Bulan hanya menerima gaya tanpa bereaksi."
                },
                questionTextEN = "In the gravitational interaction between Earth and the Moon, if Earth exerts an attractive force on the Moon, then the most accurate statement according to Newton's Third Law is...",
                answerOptionsEN = new string[] {
                    "The Moon does not exert a return force because its mass is much smaller than Earth's.",
                    "The Moon exerts an attractive force on Earth that is equal in magnitude to Earth's pull on the Moon.",
                    "The Moon exerts a smaller attractive force because of its great distance from Earth.",
                    "Earth pulls the Moon, while the Moon only receives the force without reacting."
                },
                correctAnswerIndex = 1
            },
            // Soal 3
            new QuizQuestion
            {
                questionTextID = "Manakah pernyataan berikut yang benar mengenai arah gaya aksi dan reaksi dalam interaksi gravitasi antara dua benda langit?",
                answerOptionsID = new string[] {
                    "Kedua gaya bekerja ke arah yang sama menuju pusat alam semesta.",
                    "Gaya aksi dan gaya reaksi memiliki arah yang saling berlawanan satu sama lain.",
                    "Arah gaya selalu mengikuti arah rotasi planet yang paling besar massanya.",
                    "Gaya hanya bekerja satu arah dari benda bermassa besar ke benda bermassa kecil."
                },
                questionTextEN = "Which of the following statements is correct regarding the direction of action and reaction forces in gravitational interaction between two celestial bodies?",
                answerOptionsEN = new string[] {
                    "Both forces act in the same direction toward the center of the universe.",
                    "The action and reaction forces have opposite directions to each other.",
                    "The direction of force always follows the rotation of the planet with the largest mass.",
                    "Force only acts in one direction from the larger-mass object to the smaller-mass object."
                },
                correctAnswerIndex = 1
            },
            // Soal 4
            new QuizQuestion
            {
                questionTextID = "Berdasarkan konsep pasangan gaya aksi-reaksi, gaya gravitasi yang bekerja pada dua benda yang berbeda (seperti Bumi dan satelit) memiliki ciri-ciri...",
                answerOptionsID = new string[] {
                    "Besarnya berbeda dan bekerja pada satu benda yang sama.",
                    "Besarnya sama tetapi bekerja pada benda yang sama sehingga saling meniadakan.",
                    "Besarnya sama, bekerja pada dua benda yang berbeda, dan arahnya berlawanan.",
                    "Bekerja secara bergantian, di mana gaya aksi muncul terlebih dahulu baru kemudian gaya reaksi."
                },
                questionTextEN = "Based on the concept of action-reaction force pairs, gravitational forces acting on two different objects (like Earth and a satellite) have the following characteristics...",
                answerOptionsEN = new string[] {
                    "They have different magnitudes and act on the same object.",
                    "They have equal magnitudes but act on the same object, thus canceling each other.",
                    "They have equal magnitudes, act on two different objects, and have opposite directions.",
                    "They act alternately, where the action force appears first, then the reaction force follows."
                },
                correctAnswerIndex = 2
            },
            // Soal 5
            new QuizQuestion
            {
                questionTextID = "Newton menyimpulkan bahwa besar gaya gravitasi harus sebanding dengan massa kedua benda yang berinteraksi. Kesimpulan ini diambil berdasarkan sifat...",
                answerOptionsID = new string[] {
                    "Kelengkungan permukaan Bumi di kutub.",
                    "Perbedaan waktu jatuh antara benda ringan dan benda berat.",
                    "Simetri, di mana kedua benda saling mengerjakan gaya satu sama lain.",
                    "Inersia atau kelembaman benda yang sedang bergerak melingkar."
                },
                questionTextEN = "Newton concluded that the magnitude of gravitational force must be proportional to the masses of both interacting objects. This conclusion was based on the property of...",
                answerOptionsEN = new string[] {
                    "The curvature of Earth's surface at the poles.",
                    "The difference in falling time between light and heavy objects.",
                    "Symmetry, where both objects exert forces on each other.",
                    "Inertia of an object moving in a circular path."
                },
                correctAnswerIndex = 2
            },
            // Soal 6
            new QuizQuestion
            {
                questionTextID = "Garis kerja gaya gravitasi yang merupakan pasangan aksi-reaksi menurut Hukum III Newton selalu terletak pada...",
                answerOptionsID = new string[] {
                    "Garis yang sejajar dengan khatulistiwa Bumi.",
                    "Garis hubung yang menghubungkan pusat massa benda pertama dan pusat massa benda kedua.",
                    "Permukaan terluar dari benda yang memiliki massa paling kecil.",
                    "Lintasan orbit elips yang dilalui oleh planet-planet."
                },
                questionTextEN = "The line of action of gravitational forces that form an action-reaction pair according to Newton's Third Law always lies on...",
                answerOptionsEN = new string[] {
                    "A line parallel to Earth's equator.",
                    "The line connecting the center of mass of the first object and the center of mass of the second object.",
                    "The outer surface of the object with the smallest mass.",
                    "The elliptical orbital path traveled by planets."
                },
                correctAnswerIndex = 1
            },
            // Soal 7
            new QuizQuestion
            {
                questionTextID = "Ketika sebuah apel jatuh dari pohon menuju pusat Bumi, interaksi yang terjadi berdasarkan Hukum III Newton adalah...",
                answerOptionsID = new string[] {
                    "Bumi menarik apel ke bawah, dan apel diam saja menerima tarikan tersebut.",
                    "Apel ditarik oleh Bumi, dan pada saat yang sama apel juga menarik Bumi dengan gaya yang sama besar.",
                    "Bumi menarik apel lebih kuat karena Bumi memiliki gravitasi, sedangkan apel tidak.",
                    "Apel menarik Bumi hanya ketika apel tersebut sudah menyentuh permukaan tanah."
                },
                questionTextEN = "When an apple falls from a tree toward Earth's center, the interaction that occurs according to Newton's Third Law is...",
                answerOptionsEN = new string[] {
                    "Earth pulls the apple downward, and the apple just passively receives the pull.",
                    "The apple is pulled by Earth, and at the same time the apple also pulls Earth with an equal force.",
                    "Earth pulls the apple more strongly because Earth has gravity, while the apple does not.",
                    "The apple pulls Earth only when the apple has touched the ground."
                },
                correctAnswerIndex = 1
            },
            // Soal 8
            new QuizQuestion
            {
                questionTextID = "Gaya gravitasi juga bekerja pada benda-benda kecil di sekitar kita, seperti antara dua buah bola timbal. Alasan mengapa kita tidak melihat kedua bola tersebut saling mendekat secara spontan adalah...",
                answerOptionsID = new string[] {
                    "Karena Hukum III Newton tidak berlaku untuk benda bermassa kecil.",
                    "Karena gaya gravitasi di antara benda bermassa kecil nilainya sangat kecil sehingga sulit diamati.",
                    "Karena gaya tarik-menarik hanya terjadi jika salah satu benda adalah planet.",
                    "Karena gaya reaksi dari benda kecil selalu bernilai nol."
                },
                questionTextEN = "Gravitational force also acts on small objects around us, such as between two lead balls. The reason we don't see these two balls spontaneously moving toward each other is...",
                answerOptionsEN = new string[] {
                    "Because Newton's Third Law doesn't apply to small-mass objects.",
                    "Because the gravitational force between small-mass objects is so small that it's difficult to observe.",
                    "Because mutual attraction only occurs if one of the objects is a planet.",
                    "Because the reaction force from a small object is always zero."
                },
                correctAnswerIndex = 1
            },
            // Soal 9
            new QuizQuestion
            {
                questionTextID = "Jika sebuah satelit ditarik oleh gravitasi Bumi dengan gaya tertentu (sebagai aksi), maka reaksi yang diberikan oleh satelit adalah...",
                answerOptionsID = new string[] {
                    "Mempercepat geraknya agar bisa lepas dari tarikan Bumi.",
                    "Menarik Bumi dengan gaya yang besarnya identik namun arahnya menuju satelit.",
                    "Mengeluarkan energi untuk melawan gaya tarik dari pusat massa Bumi.",
                    "Mengurangi massanya sendiri agar gaya tarik Bumi menjadi lebih kecil."
                },
                questionTextEN = "If a satellite is pulled by Earth's gravity with a certain force (as action), then the reaction given by the satellite is...",
                answerOptionsEN = new string[] {
                    "Accelerating its motion to escape Earth's pull.",
                    "Pulling Earth with an identical force but directed toward the satellite.",
                    "Releasing energy to oppose the attractive force from Earth's center of mass.",
                    "Reducing its own mass so that Earth's gravitational pull becomes smaller."
                },
                correctAnswerIndex = 1
            },
            // Soal 10
            new QuizQuestion
            {
                questionTextID = "Mengapa roket bisa bergerak ke atas saat terbang ke luar angkasa?",
                answerOptionsID = new string[] {
                    "Karena roket mengeluarkan gas panas ke bawah (aksi), dan gas tersebut mendorong roket ke atas (reaksi).",
                    "Karena mesin roket membakar udara di bawahnya.",
                    "Karena roket lebih ringan daripada udara.",
                    "Karena sayap roket menangkap angin."
                },
                questionTextEN = "Why can a rocket move upwards when it flies into space?",
                answerOptionsEN = new string[] {
                    "Because the rocket expels hot gas downwards (action), and that gas pushes the rocket upwards (reaction).",
                    "Because the rocket's engine burns the air beneath it.",
                    "Because the rocket is lighter than air.",
                    "Because the rocket's wings catch the wind."
                },
                correctAnswerIndex = 0
            }
        };
    }
    #endregion

    /// <summary>
    /// Setup listener untuk setiap button jawaban
    /// </summary>
    private void SetupAnswerButtonListeners()
    {
        if (answerButtons == null) return;

        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] == null) continue;

            int answerIndex = i; // capture untuk closure (Ini adalah index tombol fisik: 0=KiriAtas, 1=KananAtas, dst)
            answerButtons[i].onClick.RemoveAllListeners();
            answerButtons[i].onClick.AddListener(() => OnAnswerSelected(answerIndex));
        }
    }

    /// <summary>
    /// Pilih soal secara random dari daftar soal (benar-benar acak, bisa muncul soal yang sama)
    /// </summary>
    private QuizQuestion GetRandomQuestion()
    {
        if (activeQuestions == null || activeQuestions.Length == 0)
        {
            Debug.LogWarning("QuestionTrigger: Tidak ada soal yang tersedia!");
            return null;
        }

        // Pure random - langsung pilih acak dari semua soal
        int randomIndex = Random.Range(0, activeQuestions.Length);
        return activeQuestions[randomIndex];
    }

    /// <summary>
    /// Tampilkan soal dan jawaban ke UI dengan Opsi DIACAK
    /// Menggunakan bahasa sesuai setting dari LanguageManager
    /// </summary>
    private void DisplayQuestion(QuizQuestion question)
    {
        if (question == null) return;

        // Set question text sesuai bahasa aktif
        if (questionDisplayText != null)
        {
            questionDisplayText.text = question.GetQuestionText();
        }

        // Ambil opsi jawaban sesuai bahasa aktif
        string[] currentAnswers = question.GetAnswerOptions();

        // --- MODIFIKASI MULAI: Logika Pengacakan Jawaban ---

        // 1. Buat daftar index asli: [0, 1, 2, 3]
        List<int> shuffledIndices = new List<int>();
        for (int i = 0; i < currentAnswers.Length; i++)
        {
            shuffledIndices.Add(i);
        }

        // 2. Acak daftar index tersebut (Fisher-Yates Shuffle sederhana)
        for (int i = 0; i < shuffledIndices.Count; i++)
        {
            int temp = shuffledIndices[i];
            int randomIndex = Random.Range(i, shuffledIndices.Count);
            shuffledIndices[i] = shuffledIndices[randomIndex];
            shuffledIndices[randomIndex] = temp;
        }

        // 3. Pasang text ke tombol berdasarkan index yang sudah diacak
        if (answerButtonTexts != null)
        {
            // Loop melalui Tombol UI (i adalah index tombol fisik A, B, C, D)
            for (int i = 0; i < answerButtonTexts.Length && i < shuffledIndices.Count; i++)
            {
                if (answerButtonTexts[i] != null)
                {
                    // Ambil index data asli dari list yang sudah diacak
                    int originalDataIndex = shuffledIndices[i];

                    // Tampilkan text jawaban sesuai index aslinya
                    char optionLetter = (char)('A' + i);
                    answerButtonTexts[i].text = $"{optionLetter}. {currentAnswers[originalDataIndex]}";

                    // PENTING: Cek apakah data ini adalah jawaban yang benar?
                    // Jika index data asli == index jawaban benar di database,
                    // maka tombol 'i' inilah yang sekarang menjadi tombol benar.
                    if (originalDataIndex == question.correctAnswerIndex)
                    {
                        currentCorrectButtonIndex = i;
                    }
                }

            }
        }
        // --- MODIFIKASI SELESAI ---
    }

    /// <summary>
    /// Dipanggil ketika player memilih jawaban
    /// </summary>
    private void OnAnswerSelected(int selectedIndex)
    {
        if (isAnswered || currentQuestion == null) return;

        // --- MODIFIKASI: Bandingkan dengan currentCorrectButtonIndex (posisi tombol), bukan data asli ---
        if (selectedIndex == currentCorrectButtonIndex)
        {
            RightAnswer();
        }
        else
        {
            WrongAnswer();
        }
    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        pauseMenu.enabled = true;
        playerMovement.walkSpeed = originalWalkSpeed;
        playerMovement.runSpeed = originalRunSpeed;
        playerMovement.acceleration = originalAcceleration;
        playerMovement.jumpForce = originalJumpForce;
        playerMovement.sensitivityX = cameraSensitivity;
        playerMovement.sensitivityY = cameraSensitivity;
        crosshair.SetActive(true);
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        pauseMenu.enabled = false;
        playerMovement.walkSpeed = 0f;
        playerMovement.runSpeed = 0f;
        playerMovement.acceleration = 0f;
        playerMovement.jumpForce = 0f;
        playerMovement.sensitivityX = 0f;
        playerMovement.sensitivityY = 0f;
        crosshair.SetActive(false);
    }

    public void OpenQuestion()
    {
        if (isAnswered) return;

        currentQuestion = GetRandomQuestion();
        if (currentQuestion == null) return;

        DisplayQuestion(currentQuestion);

        questionPanel.SetActive(true);
        UnlockCursor();
        LeanTween.scale(questionPanel, Vector3.one, 0.5f).setEase(LeanTweenType.easeOutBack);
    }

    public void CloseQuestion()
    {
        LeanTween.scale(questionPanel, Vector3.zero, 0.5f).setEase(LeanTweenType.easeInBack).setOnComplete(() =>
        {
            questionPanel.SetActive(false);
            LockCursor();
        });
    }

    public void RightAnswer()
    {
        if (isAnswered) return;
        isAnswered = true;

        rightOrWrongTextPanel.SetActive(true);
        rightAnswerText.SetActive(true);

        onRightAnswer?.Invoke();

        LeanTween.scale(rightAnswerText, new Vector3(5.11f, 5.11f, 5.11f), 1f).setEase(LeanTweenType.easeOutSine).setOnComplete(() =>
        {
            CloseQuestion();
            levelManager.RightScoreAdd();
            rightOrWrongTextPanel.SetActive(false);
            rightAnswerText.SetActive(false);
            rightAnswerText.transform.localScale = Vector3.zero;
        });
    }

    public void WrongAnswer()
    {
        if (isAnswered) return;
        isAnswered = true;

        rightOrWrongTextPanel.SetActive(true);
        wrongAnswerText.SetActive(true);

        onWrongAnswer?.Invoke();

        LeanTween.scale(wrongAnswerText, new Vector3(5.11f, 5.11f, 5.11f), 1f).setEase(LeanTweenType.easeOutSine).setOnComplete(() =>
        {
            CloseQuestion();
            levelManager.WrongScoreAdd();
            rightOrWrongTextPanel.SetActive(false);
            wrongAnswerText.SetActive(false);
            wrongAnswerText.transform.localScale = Vector3.zero;
        });
    }

    public void ResetQuiz()
    {
        isAnswered = false;
        currentQuestion = null;
        InitializeQuestionPool();
    }

    public int GetRemainingQuestionsCount()
    {
        return availableQuestionIndices.Count;
    }
}