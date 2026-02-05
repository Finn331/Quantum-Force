using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LoadingScreenManager : MonoBehaviour
{
    [System.Serializable]
    public class MonsterInfo
    {
        public string monsterName;

        [Header("Description - Indonesian")]
        [TextArea(2, 4)] public string indonesianDescription;

        [Header("Description - English")]
        [TextArea(2, 4)] public string englishDescription;

        public Sprite sprite;
    }

    [Header("Scene Settings")]
    [SerializeField] private string gameplaySceneName = "GameplayScene";

    [Header("Loading UI")]
    [SerializeField] private GameObject loadingScreenRoot;
    [SerializeField] private Image monsterImage;
    [SerializeField] private GameObject monsterImages; // parent container for image
    [SerializeField] private TMP_Text monsterNameText;
    [SerializeField] private TMP_Text monsterDescriptionText;
    [SerializeField] private Slider progressBar;

    [Header("Fade Background")]
    [SerializeField] private CanvasGroup blackFadePanel;
    [SerializeField] private float fadeDuration = 1.2f;

    [Header("Monster Data")]
    [SerializeField] private List<MonsterInfo> monsters = new List<MonsterInfo>();

    [Header("Timing Settings")]
    [SerializeField] private float monsterDisplayDuration = 4f;

    [Header("Fallback / Default Text")]
    [SerializeField] private string defaultMonsterName = "Unknown Creature";
    [SerializeField, TextArea(2, 3)] private string defaultDescription = "Description not available.";

    [Header("Text Animation Settings")]
    [SerializeField] private float textAnimDuration = 0.35f;
    [SerializeField] private float textStartScale = 0.9f;

    private bool isLoading = false;

    // ================================
    // Called by Play button (OnClick)
    // ================================
    public void OnPlayButtonPressed()
    {
        if (isLoading) return;

        if (string.IsNullOrEmpty(gameplaySceneName))
        {
            Debug.LogError("LoadingScreenManager: gameplaySceneName is empty!");
            return;
        }

        isLoading = true;
        StartCoroutine(StartLoadingWithFade());
    }

    // =================================
    // Fade in screen and start loading
    // =================================
    private IEnumerator StartLoadingWithFade()
    {
        if (blackFadePanel != null)
        {
            blackFadePanel.gameObject.SetActive(true);
            blackFadePanel.alpha = 0f;

            LeanTween.alphaCanvas(blackFadePanel, 1f, fadeDuration);
            yield return new WaitForSeconds(fadeDuration);
        }

        if (loadingScreenRoot != null)
            loadingScreenRoot.SetActive(true);

        // Hide image initially
        if (monsterImage != null)
            monsterImage.enabled = false;
        if (monsterImages != null)
            monsterImages.SetActive(false);

        StartCoroutine(LoadGameRoutine());
    }

    // =================================
    // Async loading and random monster
    // =================================
    private IEnumerator LoadGameRoutine()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(gameplaySceneName);
        op.allowSceneActivation = false;

        // No monsters: simple loading
        if (monsters == null || monsters.Count == 0)
        {
            Debug.LogWarning("LoadingScreenManager: Monster list is empty.");

            op.allowSceneActivation = true;
            yield break;
        }

        // Shuffle monsters
        List<MonsterInfo> shuffledMonsters = new List<MonsterInfo>(monsters);
        for (int i = 0; i < shuffledMonsters.Count; i++)
        {
            int rand = Random.Range(i, shuffledMonsters.Count);
            MonsterInfo tmp = shuffledMonsters[i];
            shuffledMonsters[i] = shuffledMonsters[rand];
            shuffledMonsters[rand] = tmp;
        }

        // Show monsters one by one
        for (int i = 0; i < shuffledMonsters.Count; i++)
        {
            ShowMonster(shuffledMonsters[i]);

            float t = 0f;
            while (t < monsterDisplayDuration)
            {
                t += Time.deltaTime;
                UpdateProgressUI(op);
                yield return null;
            }

            // HIDE only if this is NOT the last monster
            if (i < shuffledMonsters.Count - 1)
            {
                if (monsterImage != null)
                    monsterImage.enabled = false;
                if (monsterImages != null)
                    monsterImages.SetActive(false);
            }
            // if last monster: keep image ON while entering gameplay scene
        }

        // All explanations done, allow scene activation
        op.allowSceneActivation = true;

        while (!op.isDone)
        {
            UpdateProgressUI(op);
            yield return null;
        }
    }

    // =================================
    // Get description based on language
    // =================================
    private string GetDescriptionByLanguage(MonsterInfo info)
    {
        GameLanguage lang = GameLanguage.Indonesian;

        if (LanguageManager.Instance != null)
            lang = LanguageManager.CurrentLanguage;

        if (lang == GameLanguage.Indonesian)
            return info.indonesianDescription;
        else
            return info.englishDescription;
    }

    // =================================
    // Display monster info with animation
    // =================================
    private void ShowMonster(MonsterInfo info)
    {
        // IMAGE (only active when showing monster)
        if (monsterImage != null)
        {
            if (info.sprite != null)
            {
                if (monsterImages != null)
                    monsterImages.SetActive(true);

                monsterImage.sprite = info.sprite;
                monsterImage.enabled = true;

                Color c = monsterImage.color;
                c.a = 0f;
                monsterImage.color = c;

                LeanTween.value(monsterImage.gameObject, 0f, 1f, 0.3f)
                    .setOnUpdate((float val) =>
                    {
                        if (monsterImage == null) return;
                        Color cc = monsterImage.color;
                        cc.a = val;
                        monsterImage.color = cc;
                    });
            }
            else
            {
                monsterImage.enabled = false;
                if (monsterImages != null)
                    monsterImages.SetActive(false);
            }
        }

        // NAME
        string nameToShow = string.IsNullOrWhiteSpace(info.monsterName)
            ? defaultMonsterName
            : info.monsterName;

        if (monsterNameText != null)
        {
            monsterNameText.text = nameToShow;
            AnimateText(monsterNameText);
        }

        // DESCRIPTION
        string desc = GetDescriptionByLanguage(info);
        if (string.IsNullOrWhiteSpace(desc))
            desc = defaultDescription;

        if (monsterDescriptionText != null)
        {
            monsterDescriptionText.text = desc;
            AnimateText(monsterDescriptionText);
        }
    }

    // =================================
    // Text animation with LeanTween
    // =================================
    private void AnimateText(TMP_Text text)
    {
        if (text == null) return;

        RectTransform rt = text.rectTransform;
        LeanTween.cancel(rt);

        rt.localScale = Vector3.one * textStartScale;

        Color c = text.color;
        c.a = 0f;
        text.color = c;

        LeanTween.scale(rt, Vector3.one, textAnimDuration).setEaseOutBack();
        LeanTween.value(rt.gameObject, 0f, 1f, textAnimDuration)
            .setOnUpdate((float val) =>
            {
                if (text == null) return;
                Color cc = text.color;
                cc.a = val;
                text.color = cc;
            });
    }

    // =================================
    // Update progress bar
    // =================================
    private void UpdateProgressUI(AsyncOperation op)
    {
        if (progressBar == null || op == null) return;

        float progress = Mathf.Clamp01(op.progress / 0.9f);
        progressBar.value = progress;
    }
}
