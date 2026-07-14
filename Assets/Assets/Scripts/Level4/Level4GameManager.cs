using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace Level4
{
    public enum GamePhase
    {
        CollectCans,
        ReturnToStart
    }

    public static class Level4State
    {
        public static int canCount = 0;
        public static int totalCans = 6;
        public static bool hasAllCans = false;
        public static bool hasDocument = false;
    }

    public class Level4GameManager : MonoBehaviour
    {
        [Header("Phase")]
        public GamePhase initialPhase = GamePhase.CollectCans;

        [Header("UI")]
        public TextMeshProUGUI objectiveText;
        public TextMeshProUGUI canCounterText;
        public TextMeshProUGUI holdPromptText;
        public Image holdProgressFill;
        public GameObject losePanel;
        public GameObject winPanel;
        public Image fadeOverlay;
        public float fadeDuration = 1f;

        [Header("Player")]
        public float playerSpeedMultiplier = 3f;

        [Header("Audio")]
        public AudioClip loseClip;
        public AudioClip winClip;
        public AudioClip canPickupClip;
        public AudioClip docPickupClip;

        [Header("Restart")]
        public float loseDelay = 3f;

        private bool isGameOver = false;
        private bool isRestarting = false;
        private bool isWin = false;
        private GamePhase currentPhase;

        void Awake()
        {
            if (initialPhase == GamePhase.ReturnToStart)
            {
                currentPhase = GamePhase.ReturnToStart;
                isWin = false;
                isGameOver = false;
                isRestarting = false;
                Time.timeScale = 1f;
            }
            else
            {
                currentPhase = GamePhase.CollectCans;
                Level4State.canCount = 0;
                Level4State.hasAllCans = false;
                Level4State.hasDocument = false;
                isGameOver = false;
                isRestarting = false;
                Time.timeScale = 1f;
            }
        }

        void Start()
        {
            if (currentPhase == GamePhase.ReturnToStart)
            {
                var player = FindAnyObjectByType<PlayerController>();
                if (player != null)
                    player.moveSpeed *= playerSpeedMultiplier;
            }

            if (fadeOverlay != null)
            {
                Color c = fadeOverlay.color;
                c.a = 0f;
                fadeOverlay.color = c;
                fadeOverlay.gameObject.SetActive(false);
            }

            UpdateObjectiveUI();
        }

        void Update()
        {
            if (canCounterText != null && currentPhase == GamePhase.CollectCans)
            {
                TextMeshProUGUI txt = canCounterText;
                if (Level4State.hasAllCans)
                    txt.text = $"{Level4State.totalCans}/{Level4State.totalCans}";
                else
                    txt.text = $"{Level4State.canCount}/{Level4State.totalCans}";
            }

            if (holdProgressFill != null && holdPromptText != null)
            {
                HoldInteraction active = HoldInteraction.ActiveInteraction;
                if (active != null && active.IsHolding)
                {
                    holdProgressFill.gameObject.SetActive(true);
                    holdProgressFill.fillAmount = active.HoldProgress;
                    holdPromptText.gameObject.SetActive(true);
                    holdPromptText.text = active.CurrentPrompt;
                }
                else
                {
                    holdProgressFill.gameObject.SetActive(false);
                    holdPromptText.gameObject.SetActive(false);
                }
            }
        }

        public void CollectCan()
        {
            if (isGameOver) return;
            Level4State.canCount++;
            if (canPickupClip != null)
                AudioSource.PlayClipAtPoint(canPickupClip, Camera.main.transform.position, 1f);
            if (Level4State.canCount >= Level4State.totalCans)
            {
                Level4State.hasAllCans = true;
                UpdateObjectiveUI();
            }
        }

        public void CollectDocument()
        {
            if (isGameOver || isWin) return;
            isWin = true;
            Level4State.hasDocument = true;
            if (docPickupClip != null)
                AudioSource.PlayClipAtPoint(docPickupClip, Camera.main.transform.position, 1f);
            StartCoroutine(FadeAndLoadScene("level4_dialog"));
        }

        public void ReachStartPoint()
        {
            if (isGameOver || isWin) return;
            isWin = true;
            WinGame();
        }

        public void WinGame()
        {
            if (isGameOver) return;
            isGameOver = true;
            if (winClip != null)
                AudioSource.PlayClipAtPoint(winClip, Camera.main.transform.position, 1f);
            if (winPanel != null) winPanel.SetActive(true);
            if (objectiveText != null) objectiveText.gameObject.SetActive(false);
            if (canCounterText != null) canCounterText.gameObject.SetActive(false);
            Time.timeScale = 0f;
        }

        public void LoseGame()
        {
            if (isGameOver) return;
            isGameOver = true;
            if (loseClip != null)
                AudioSource.PlayClipAtPoint(loseClip, Camera.main.transform.position, 1f);
            if (losePanel != null) losePanel.SetActive(true);
            if (objectiveText != null) objectiveText.gameObject.SetActive(false);
            if (canCounterText != null) canCounterText.gameObject.SetActive(false);
            Time.timeScale = 0f;
            StartCoroutine(AutoRestartAfterDelay());
        }

        void UpdateObjectiveUI()
        {
            if (objectiveText == null) return;
            objectiveText.text = "Ambil dokumen";
        }

        IEnumerator AutoRestartAfterDelay()
        {
            yield return new WaitForSecondsRealtime(loseDelay);
            RestartLevel();
        }

        public void RestartLevel()
        {
            if (isRestarting) return;
            StartCoroutine(RestartWithFade());
        }

        IEnumerator RestartWithFade()
        {
            isRestarting = true;
            Time.timeScale = 1f;
            if (fadeOverlay != null)
            {
                fadeOverlay.gameObject.SetActive(true);
                Color c = fadeOverlay.color;
                c.a = 0f;
                fadeOverlay.color = c;
                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    c.a = Mathf.Clamp01(elapsed / fadeDuration);
                    fadeOverlay.color = c;
                    yield return null;
                }
                c.a = 1f;
                fadeOverlay.color = c;
            }
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        IEnumerator FadeAndLoadScene(string sceneName)
        {
            if (fadeOverlay != null)
            {
                fadeOverlay.gameObject.SetActive(true);
                Color c = fadeOverlay.color;
                c.a = 0f;
                fadeOverlay.color = c;
                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    c.a = Mathf.Clamp01(elapsed / fadeDuration);
                    fadeOverlay.color = c;
                    yield return null;
                }
                c.a = 1f;
                fadeOverlay.color = c;
            }
            yield return new WaitForSeconds(0.2f);
            SceneManager.LoadScene(sceneName);
        }
    }
}
