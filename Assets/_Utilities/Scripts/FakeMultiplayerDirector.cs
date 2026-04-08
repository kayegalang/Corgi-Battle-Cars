using System.Collections;
using System.Collections.Generic;
using _Bot.Scripts;
using _Cars.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Gameplay.Scripts
{
    /// <summary>
    /// Sets up a fake 4-player split-screen using bots.
    /// Uses the actual player UI Canvas prefab so the UI matches the real game exactly.
    /// Also shows the real GameTimer canvas as a full-screen overlay.
    ///
    /// SETUP:
    ///   1. Open your player prefab, find the Canvas child with PlayerUIManager on it
    ///      → drag to Project window → name it "PlayerUICanvas"
    ///   2. Open Park Map, find the GameTimer Canvas in the Hierarchy
    ///      → drag to Project window → name it "GameTimerCanvas"
    ///   3. Assign both prefabs in the Inspector below
    ///   4. Set humanPlayerCount = 0 on TrailerSceneBootstrapper
    ///   5. Disable the CinematicFreeCam camera component before recording
    ///   6. Hit Play!
    /// </summary>
    public class FakeMultiplayerDirector : MonoBehaviour
    {
        [Header("UI Prefabs")]
        [Tooltip("Canvas prefab extracted from your player prefab (has PlayerUIManager on it)")]
        [SerializeField] private Canvas playerUICanvasPrefab;

        [Tooltip("GameTimer canvas prefab extracted from Park Map")]
        [SerializeField] private Canvas gameTimerCanvasPrefab;

        [Header("Reticle")]
        [SerializeField] private Sprite reticleSprite;
        [SerializeField] private float  reticleSize  = 40f;
        [SerializeField] private Color  reticleColor = new Color(1f, 1f, 1f, 0.8f);

        [Header("Countdown")]
        [SerializeField] private TMP_FontAsset countdownFont;
        [SerializeField] private float         countdownFontSize = 120f;
        [SerializeField] private Color         color3  = new Color(0.9f, 0.2f, 0.2f);
        [SerializeField] private Color         color2  = new Color(0.9f, 0.6f, 0.1f);
        [SerializeField] private Color         color1  = new Color(0.9f, 0.9f, 0.1f);
        [SerializeField] private Color         colorGo = new Color(0.2f, 0.9f, 0.2f);

        [Header("Timer")]
        [Tooltip("Match duration in seconds — default 300 = 5 minutes")]
        [SerializeField] private float matchDuration     = 300f;
        [SerializeField] private Color timerNormalColor  = Color.white;
        [SerializeField] private Color timerWarningColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private float timerWarningTime  = 30f;

        [Header("Camera Settings")]
        [SerializeField] private int cameraPriority = 10;

        // Split-screen viewport rects — matches real 4-player multiplayer layout
        private static readonly Rect[] ViewportRects = new Rect[]
        {
            new Rect(0f,   0.5f, 0.5f, 0.5f), // Top-left
            new Rect(0.5f, 0.5f, 0.5f, 0.5f), // Top-right
            new Rect(0f,   0f,   0.5f, 0.5f), // Bottom-left
            new Rect(0.5f, 0f,   0.5f, 0.5f), // Bottom-right
        };

        private readonly List<Camera>          botCameras     = new List<Camera>();
        private readonly List<Canvas>          uiCanvases     = new List<Canvas>();
        private readonly List<RectTransform>   reticles       = new List<RectTransform>();
        private readonly List<TextMeshProUGUI> countdownTexts = new List<TextMeshProUGUI>();
        private readonly List<TextMeshProUGUI> timerTexts     = new List<TextMeshProUGUI>();
        private readonly List<BotAI>           bots           = new List<BotAI>();
        private readonly int[]                 scores         = new int[4];

        private float matchTimer   = 0f;
        private bool  matchRunning = false;

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(0.5f);

            FindBots();

            if (bots.Count < 4)
                Debug.LogWarning($"[FakeMultiplayerDirector] Only found {bots.Count}/4 bots. Make sure humanPlayerCount = 0.");

            SetupCameras();
            SetupPlayerUICanvases();
            SetupOverlayCanvases();
            SetupGameTimerCanvas();
            SubscribeToDeathEvents();

            matchTimer = matchDuration;

            // Start timer running immediately so it shows during countdown too
            matchRunning = true;

            SetBotsActive(false);
            yield return StartCoroutine(PlayCountdownInAllQuadrants());
            SetBotsActive(true);

            Debug.Log("[FakeMultiplayerDirector] GO! Match started.");
        }

        private void Update()
        {
            UpdateReticles();

            if (matchRunning)
            {
                UpdateMatchTimer();
                UpdateScoreDisplays();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromDeathEvents();
        }

        // ═══════════════════════════════════════════════
        //  BOT DISCOVERY
        // ═══════════════════════════════════════════════

        private void FindBots()
        {
            bots.Clear();

            string[] botTags = { "BotOne", "BotTwo", "BotThree", "BotFour" };
            foreach (string tag in botTags)
            {
                GameObject go = GameObject.FindWithTag(tag);
                if (go != null)
                {
                    BotAI ai = go.GetComponent<BotAI>();
                    if (ai != null) bots.Add(ai);
                }
            }

            Debug.Log($"[FakeMultiplayerDirector] Found {bots.Count} bots.");
        }

        // ═══════════════════════════════════════════════
        //  CAMERA SETUP
        // ═══════════════════════════════════════════════

        private void SetupCameras()
        {
            botCameras.Clear();

            for (int i = 0; i < bots.Count && i < 4; i++)
            {
                GameObject camGO  = new GameObject($"BotCamera_{i + 1}");
                camGO.transform.SetParent(transform);

                Camera cam        = camGO.AddComponent<Camera>();
                cam.rect          = ViewportRects[i];
                cam.fieldOfView   = 60f;
                cam.nearClipPlane = 0.3f;
                cam.farClipPlane  = 1000f;
                cam.depth         = cameraPriority + i;
                cam.clearFlags    = CameraClearFlags.Skybox;

                BotFollowCamera follow = camGO.AddComponent<BotFollowCamera>();
                follow.target          = bots[i].transform;

                botCameras.Add(cam);
            }
        }

        // ═══════════════════════════════════════════════
        //  PLAYER UI CANVAS — real prefab, one per bot
        // ═══════════════════════════════════════════════

        private void SetupPlayerUICanvases()
        {
            uiCanvases.Clear();

            if (playerUICanvasPrefab == null)
            {
                Debug.LogWarning("[FakeMultiplayerDirector] No Player UI Canvas Prefab assigned — skipping per-player UI.");
                return;
            }

            for (int i = 0; i < botCameras.Count; i++)
            {
                Camera cam           = botCameras[i];

                Canvas canvas        = Instantiate(playerUICanvasPrefab, transform);
                canvas.renderMode    = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera   = cam;
                canvas.planeDistance = 1f;
                canvas.sortingOrder  = i;

                uiCanvases.Add(canvas);

                _UI.Scripts.PlayerUIManager uiManager = canvas.GetComponent<_UI.Scripts.PlayerUIManager>();
                if (uiManager != null)
                {
                    uiManager.EnableGameplay();
                    uiManager.UpdateScore(0);
                }

                Debug.Log($"[FakeMultiplayerDirector] Set up real UI canvas for bot {i + 1}");
            }
        }

        // ═══════════════════════════════════════════════
        //  GAME TIMER CANVAS — full screen overlay
        // ═══════════════════════════════════════════════

        private void SetupGameTimerCanvas()
        {
            if (gameTimerCanvasPrefab == null)
            {
                Debug.LogWarning("[FakeMultiplayerDirector] No GameTimer canvas prefab assigned — skipping timer overlay.");
                return;
            }

            Canvas timerCanvas       = Instantiate(gameTimerCanvasPrefab, transform);
            timerCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            timerCanvas.sortingOrder = 99;

            // Search by name first (MatchTimerManager uses "GameTimerText" by name)
            // then fall back to GetComponentInChildren
            Transform timerChild     = FindDeepChild(timerCanvas.transform, "GameTimerText");
            TextMeshProUGUI timerTMP = timerChild != null
                ? timerChild.GetComponent<TextMeshProUGUI>()
                : timerCanvas.GetComponentInChildren<TextMeshProUGUI>();

            if (timerTMP != null)
            {
                timerTexts.Add(timerTMP);

                // Show the initial time immediately so it's visible during countdown
                int minutes    = Mathf.FloorToInt(matchDuration / 60f);
                int seconds    = Mathf.FloorToInt(matchDuration % 60f);
                timerTMP.text  = $"{minutes}:{seconds:D2}";
                timerTMP.color = timerNormalColor;

                Debug.Log("[FakeMultiplayerDirector] Game timer canvas set up!");
            }
            else
            {
                Debug.LogWarning("[FakeMultiplayerDirector] GameTimer canvas has no TextMeshProUGUI — timer won't display.");
            }
        }

        // ═══════════════════════════════════════════════
        //  OVERLAY CANVASES — reticle + countdown on top
        // ═══════════════════════════════════════════════

        private void SetupOverlayCanvases()
        {
            reticles.Clear();
            countdownTexts.Clear();

            for (int i = 0; i < botCameras.Count; i++)
            {
                Camera cam = botCameras[i];

                GameObject overlayGO       = new GameObject($"OverlayCanvas_{i + 1}");
                overlayGO.transform.SetParent(transform);

                Canvas overlay             = overlayGO.AddComponent<Canvas>();
                overlay.renderMode         = RenderMode.ScreenSpaceCamera;
                overlay.worldCamera        = cam;
                overlay.planeDistance      = 0.5f;
                overlay.sortingOrder       = 10 + i;

                CanvasScaler scaler        = overlayGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(960f, 540f);

                overlayGO.AddComponent<GraphicRaycaster>();

                reticles.Add(CreateReticle(overlayGO));
                countdownTexts.Add(CreateCountdownText(overlayGO));
            }
        }

        private RectTransform CreateReticle(GameObject parent)
        {
            GameObject go     = new GameObject("Reticle");
            go.transform.SetParent(parent.transform, false);

            Image img         = go.AddComponent<Image>();
            img.sprite        = reticleSprite;
            img.color         = reticleColor;
            img.raycastTarget = false;

            RectTransform rt  = go.GetComponent<RectTransform>();
            rt.sizeDelta      = new Vector2(reticleSize, reticleSize);
            rt.anchorMin      = new Vector2(0.5f, 0.5f);
            rt.anchorMax      = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;

            return rt;
        }

        private TextMeshProUGUI CreateCountdownText(GameObject parent)
        {
            GameObject go       = new GameObject("CountdownText");
            go.transform.SetParent(parent.transform, false);

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text            = "";
            tmp.fontSize        = countdownFontSize;
            tmp.fontStyle       = FontStyles.Bold;
            tmp.alignment       = TextAlignmentOptions.Center;
            tmp.color           = Color.clear;

            if (countdownFont != null) tmp.font = countdownFont;

            RectTransform rt    = go.GetComponent<RectTransform>();
            rt.anchorMin        = Vector2.zero;
            rt.anchorMax        = Vector2.one;
            rt.offsetMin        = Vector2.zero;
            rt.offsetMax        = Vector2.zero;

            return tmp;
        }

        // ═══════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════

        /// <summary>
        /// Recursively searches for a child transform by name.
        /// </summary>
        private Transform FindDeepChild(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName) return child;
                Transform result = FindDeepChild(child, childName);
                if (result != null) return result;
            }
            return null;
        }

        // ═══════════════════════════════════════════════
        //  RETICLE UPDATE
        // ═══════════════════════════════════════════════

        private void UpdateReticles()
        {
            for (int i = 0; i < bots.Count && i < reticles.Count; i++)
            {
                BotAI bot = bots[i];
                if (bot == null) continue;

                Vector3 aimDir = bot.GetAimDirection();
                if (aimDir == Vector3.zero) continue;

                Camera cam = botCameras[i];

                Vector3 worldAimPoint = bot.transform.position + aimDir * 20f;
                Vector3 screenPoint   = cam.WorldToScreenPoint(worldAimPoint);

                Canvas        overlayCanvas = reticles[i].GetComponentInParent<Canvas>();
                RectTransform canvasRT      = overlayCanvas.GetComponent<RectTransform>();
                Vector2       localPos;

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRT,
                    new Vector2(screenPoint.x, screenPoint.y),
                    cam,
                    out localPos);

                reticles[i].localPosition = localPos;
            }
        }

        // ═══════════════════════════════════════════════
        //  TIMER UPDATE
        // ═══════════════════════════════════════════════

        private void UpdateMatchTimer()
        {
            matchTimer -= Time.deltaTime;
            matchTimer  = Mathf.Max(matchTimer, 0f);

            int    minutes = Mathf.FloorToInt(matchTimer / 60f);
            int    seconds = Mathf.FloorToInt(matchTimer % 60f);
            string timeStr = $"{minutes}:{seconds:D2}";
            Color  color   = matchTimer <= timerWarningTime ? timerWarningColor : timerNormalColor;

            foreach (var tmp in timerTexts)
            {
                if (tmp == null) continue;
                tmp.text  = timeStr;
                tmp.color = color;
            }
        }

        // ═══════════════════════════════════════════════
        //  SCORE UPDATE
        // ═══════════════════════════════════════════════

        private void UpdateScoreDisplays()
        {
            for (int i = 0; i < uiCanvases.Count; i++)
            {
                Canvas canvas = uiCanvases[i];
                if (canvas == null) continue;

                _UI.Scripts.PlayerUIManager uiManager = canvas.GetComponent<_UI.Scripts.PlayerUIManager>();
                if (uiManager != null)
                    uiManager.UpdateScore(scores[i]);
            }
        }

        // ═══════════════════════════════════════════════
        //  DEATH EVENTS
        // ═══════════════════════════════════════════════

        private void SubscribeToDeathEvents()
        {
            CarHealth[] allHealth = FindObjectsByType<CarHealth>(FindObjectsSortMode.None);
            foreach (CarHealth health in allHealth)
                health.OnDeath += OnBotDied;
        }

        private void UnsubscribeFromDeathEvents()
        {
            CarHealth[] allHealth = FindObjectsByType<CarHealth>(FindObjectsSortMode.None);
            foreach (CarHealth health in allHealth)
                health.OnDeath -= OnBotDied;
        }

        private void OnBotDied(GameObject victim, GameObject killer)
        {
            if (killer == null) return;

            for (int i = 0; i < bots.Count; i++)
            {
                if (bots[i] != null && bots[i].gameObject == killer)
                {
                    scores[i]++;
                    Debug.Log($"[FakeMultiplayerDirector] P{i + 1} scored! Total: {scores[i]}");
                    return;
                }
            }
        }

        // ═══════════════════════════════════════════════
        //  COUNTDOWN
        // ═══════════════════════════════════════════════

        private IEnumerator PlayCountdownInAllQuadrants()
        {
            yield return StartCoroutine(ShowCountdownStep("3",   color3));
            yield return StartCoroutine(ShowCountdownStep("2",   color2));
            yield return StartCoroutine(ShowCountdownStep("1",   color1));
            yield return StartCoroutine(ShowCountdownStep("GO!", colorGo));

            foreach (var tmp in countdownTexts)
                tmp.color = Color.clear;
        }

        private IEnumerator ShowCountdownStep(string text, Color color)
        {
            foreach (var tmp in countdownTexts)
            {
                tmp.text  = text;
                tmp.color = color;
                tmp.transform.localScale = Vector3.one * 2f;
            }

            float elapsed = 0f;
            while (elapsed < 0.15f)
            {
                elapsed += Time.unscaledDeltaTime;
                float scale = Mathf.Lerp(2f, 1f, elapsed / 0.15f);
                foreach (var tmp in countdownTexts)
                    tmp.transform.localScale = Vector3.one * scale;
                yield return null;
            }

            foreach (var tmp in countdownTexts)
                tmp.transform.localScale = Vector3.one;

            yield return new WaitForSecondsRealtime(0.5f);

            elapsed = 0f;
            while (elapsed < 0.2f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t     = elapsed / 0.2f;
                Color faded = new Color(color.r, color.g, color.b, 1f - t);
                foreach (var tmp in countdownTexts)
                    tmp.color = faded;
                yield return null;
            }

            foreach (var tmp in countdownTexts)
                tmp.color = Color.clear;

            yield return new WaitForSecondsRealtime(0.1f);
        }

        // ═══════════════════════════════════════════════
        //  BOT ACTIVATION
        // ═══════════════════════════════════════════════

        private void SetBotsActive(bool active)
        {
            foreach (BotAI bot in bots)
            {
                if (bot == null) continue;
                bot.enabled = active;

                BotController controller = bot.GetComponent<BotController>();
                if (controller != null) controller.enabled = active;
            }
        }
    }
}