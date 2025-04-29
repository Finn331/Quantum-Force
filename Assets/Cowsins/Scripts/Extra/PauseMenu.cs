using System;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace cowsins
{
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject playerUI;
        [SerializeField] private Selectable firstSelectedItem;
        [SerializeField] private bool disablePlayerUIWhilePaused;
        [SerializeField] private CanvasGroup menu;
        [SerializeField] private float fadeSpeed;
        private Coroutine fadeCoroutine;

        public static PauseMenu Instance { get; private set; }

        /// <summary>
        /// Returns the Pause State of the game
        /// </summary>
        public static bool isPaused { get; private set; }

        [HideInInspector] public PlayerStats stats;

        public event Action OnPause;
        public event Action OnUnPause;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Initially, the game is not paused
            isPaused = false;
            menu.gameObject.SetActive(false);
            menu.alpha = 0;

            InputManager.onTogglePause += TogglePause;
        }

        private void OnDisable()
        {
            InputManager.onTogglePause -= TogglePause;
        }

        private IEnumerator HandlePause()
        {
            if (disablePlayerUIWhilePaused && !stats.IsDead)
                playerUI.SetActive(false);
            menu.gameObject.SetActive(true);
            while (menu.alpha < 1)
            {
                menu.alpha += Time.deltaTime * fadeSpeed;
                yield return null;
            }
            menu.alpha = 1;
        }

        private IEnumerator HandleUnpause()
        {
            playerUI.SetActive(true);
            while (menu.alpha > 0)
            {
                menu.alpha -= Time.deltaTime * fadeSpeed;
                yield return null;
            }
            menu.alpha = 0;
            menu.gameObject.SetActive(false);
        }

        public void TogglePause()
        {
            isPaused = !isPaused;

            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

            if (isPaused)
            {
                stats.LoseControl();
                fadeCoroutine = StartCoroutine(HandlePause());
                OnPause?.Invoke();
                EventSystem.current.SetSelectedGameObject(firstSelectedItem.gameObject);
            }
            else
                UnPause();
        }

        public void UnPause()
        {
            isPaused = false;
            stats.CheckIfCanGrantControl();
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(HandleUnpause());
            OnUnPause?.Invoke();
        }

        public void QuitGame()
        {
            Application.Quit();
        }

    }
}
