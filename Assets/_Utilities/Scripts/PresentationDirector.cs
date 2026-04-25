using System.Collections;
using UnityEngine;
using TMPro;
using _Utilities.Scripts;
using _PowerUps.Scripts;
using _PowerUps.ScriptableObjects;

#if UNITY_EDITOR || DEVELOPMENT_BUILD

/// <summary>
/// Live presentation director tool for Corgi Battle Cars.
/// Attach to any GameObject in the gameplay scene.
///
/// CONTROLS:
///   TAB       → Toggle P1 focus (P1 big left 70%, others stacked small right 30%)
///   1         → Spawn Squirrel bone in front of P1 at a distance
///   2         → Give P1 Zoomies power-up directly
///   3         → Give P1 Poop Trail power-up directly
///   4         → Teleport all other players near P1
///   5         → Toggle slow motion
///   Shift+R   → Reset everything
/// </summary>
public class PresentationDirector : MonoBehaviour
{
    [Header("Power-Up ScriptableObjects")]
    [SerializeField] private PowerUpObject squirrelPowerUp;
    [SerializeField] private PowerUpObject zoomiesPowerUp;
    [SerializeField] private PowerUpObject poopPowerUp;

    [Header("Squirrel Bone World Spawn")]
    [Tooltip("The bone pickup prefab to spawn in the world for Squirrel")]
    [SerializeField] private GameObject squirrelPickupPrefab;
    [Tooltip("How far in front of P1 the bone spawns")]
    [SerializeField] private float squirrelSpawnDistance = 12f;

    [Header("Slow Motion")]
    [SerializeField] private float slowMotionScale = 0.35f;

    [Header("Teleport")]
    [Tooltip("Radius around P1 to scatter other players")]
    [SerializeField] private float teleportRadius = 4f;

    [Header("Cue Overlay (optional — for your own screen)")]
    [SerializeField] private Canvas          overlayCanvas;
    [SerializeField] private TextMeshProUGUI cueText;

    // ─── Viewport rects ───────────────────────────────────────
    // P1 = left 70%, P2/P3/P4 stacked on right 30%
    private static readonly Rect RectP1Big  = new Rect(0f,    0f,    0.7f, 1f);
    private static readonly Rect RectSide0  = new Rect(0.7f,  0.67f, 0.3f, 0.33f);
    private static readonly Rect RectSide1  = new Rect(0.7f,  0.34f, 0.3f, 0.33f);
    private static readonly Rect RectSide2  = new Rect(0.7f,  0f,    0.3f, 0.34f);

    // Normal 4-player split
    private static readonly Rect[] NormalRects = {
        new Rect(0f,   0.5f, 0.5f, 0.5f),
        new Rect(0.5f, 0.5f, 0.5f, 0.5f),
        new Rect(0f,   0f,   0.5f, 0.5f),
        new Rect(0.5f, 0f,   0.5f, 0.5f)
    };

    private Camera[]  playerCameras;
    private bool      isFocused = false;
    private bool      isSlowMo  = false;
    private Coroutine cueRoutine;

    // ═══════════════════════════════════════════════
    //  LIFECYCLE
    // ═══════════════════════════════════════════════

    private void Start()
    {
        playerCameras = CameraUtility.FindPlayerCameras();
        Debug.Log($"[PresentationDirector] Ready — {playerCameras.Length} cameras found.");
        if (overlayCanvas != null) overlayCanvas.gameObject.SetActive(false);
        ShowCue("🎬 DIRECTOR READY\nTAB=focus  1=Squirrel  2=Zoomies  3=Poop  4=Teleport  5=SlowMo  Shift+R=Reset", 4f);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))                                   ToggleFocus();
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.R)) ResetAll();
        if (Input.GetKeyDown(KeyCode.Alpha1))                                SpawnSquirrelBone();
        if (Input.GetKeyDown(KeyCode.Alpha2))                                GiveP1PowerUp(zoomiesPowerUp,  "ZOOMIES ⚡");
        if (Input.GetKeyDown(KeyCode.Alpha3))                                GiveP1PowerUp(poopPowerUp,     "POOP TRAIL 💩");
        if (Input.GetKeyDown(KeyCode.Alpha4))                                TeleportAllNearP1();
        if (Input.GetKeyDown(KeyCode.Alpha5))                                ToggleSlowMo();
    }

    private void LateUpdate()
    {
        // LateUpdate runs AFTER PlayerManager and SpawnManager touch cameras
        // so our viewport enforcement always wins
        if (isFocused) EnforceFocusLayout();
    }

    private void EnforceFocusLayout()
    {
        Camera p1Cam = GetCameraForTag("PlayerOne");
        if (p1Cam != null && p1Cam.rect != RectP1Big)
            p1Cam.rect = RectP1Big;

        string[] otherTags = { "PlayerTwo", "PlayerThree", "PlayerFour" };
        Rect[]   sideRects = { RectSide0, RectSide1, RectSide2 };
        for (int i = 0; i < otherTags.Length; i++)
        {
            Camera cam = GetCameraForTag(otherTags[i]);
            if (cam != null && cam.rect != sideRects[i])
                cam.rect = sideRects[i];
        }

        // Keep score text and death screens hidden every frame
        SetScoreTextVisible(false);
        SetDeathSpectateEnabled(false);
    }

    private void OnDisable()
    {
        Time.timeScale      = 1f;
        Time.fixedDeltaTime = 0.02f;
    }

    // ═══════════════════════════════════════════════
    //  TAB — FOCUS TOGGLE
    // ═══════════════════════════════════════════════

    private void ToggleFocus()
    {
        isFocused = !isFocused;

        if (isFocused)
        {
            // Find cameras by tag directly — don't rely on array order
            Camera p1Cam = GetCameraForTag("PlayerOne");
            if (p1Cam != null) p1Cam.rect = RectP1Big;

            // Stack the rest on the right
            string[] otherTags  = { "PlayerTwo", "PlayerThree", "PlayerFour" };
            Rect[]   sideRects  = { RectSide0, RectSide1, RectSide2 };
            int      sideIndex  = 0;

            foreach (string tag in otherTags)
            {
                Camera cam = GetCameraForTag(tag);
                if (cam != null && sideIndex < sideRects.Length)
                    cam.rect = sideRects[sideIndex++];
            }

            // Disable death spectate screens so they don't fight the layout
            SetDeathSpectateEnabled(false);
            SetPowerUpSpawning(false);
            SetScoreTextVisible(false);
            ShowCue("📺 P1 FOCUS MODE", 1.5f);
        }
        else
        {
            RestoreSplitScreen();
            SetDeathSpectateEnabled(true);
            SetPowerUpSpawning(true);
            SetScoreTextVisible(true);
            ShowCue("📺 NORMAL SPLIT SCREEN", 1.5f);
        }
    }

    private Camera GetCameraForTag(string tag)
    {
        GameObject go = GameObject.FindWithTag(tag);
        return go != null ? go.GetComponentInChildren<Camera>() : null;
    }

    private void SetPowerUpSpawning(bool enabled)
    {
        var spawner = FindFirstObjectByType<_PowerUps.Scripts.PowerUpSpawner>();
        if (spawner == null) return;

        if (enabled)
        {
            spawner.StartSpawning();
        }
        else
        {
            spawner.StopSpawning();
            spawner.ClearAllPowerUps();
        }
    }

    private void SetDeathSpectateEnabled(bool enabled)
    {
        var managers = FindObjectsByType<_UI.Scripts.DeathSpectateManager>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var m in managers)
            m.SetSuppressed(!enabled);
    }

    private void SetScoreTextVisible(bool visible)
    {
        // Find all PlayerUIManagers and toggle their score text
        var uiManagers = FindObjectsByType<_UI.Scripts.PlayerUIManager>(FindObjectsSortMode.None);
        foreach (var ui in uiManagers)
        {
            // ScoreText is private — find it via the canvas children
            var texts = ui.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            foreach (var t in texts)
                t.gameObject.SetActive(visible);
        }
    }

    private void RestoreSplitScreen()
    {
        isFocused = false;

        string[] tags = { "PlayerOne", "PlayerTwo", "PlayerThree", "PlayerFour" };
        for (int i = 0; i < tags.Length && i < NormalRects.Length; i++)
        {
            Camera cam = GetCameraForTag(tags[i]);
            if (cam != null) cam.rect = NormalRects[i];
        }

        // Refresh all player UI anchors so power up holder repositions correctly
        var uiManagers = FindObjectsByType<_UI.Scripts.PlayerUIManager>(FindObjectsSortMode.None);
        foreach (var ui in uiManagers)
            ui.RefreshAnchors();

        // Fix any DeathSpectateManagers that captured wrong originalViewportRect
        // while spawned during focus mode
        var spectateManagers = FindObjectsByType<_UI.Scripts.DeathSpectateManager>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var s in spectateManagers)
            s.RefreshOriginalViewportRect();
    }

    // ═══════════════════════════════════════════════
    //  1 — SPAWN SQUIRREL BONE IN WORLD
    // ═══════════════════════════════════════════════

    private void SpawnSquirrelBone()
    {
        GameObject p1 = GetP1();
        if (p1 == null) return;

        if (squirrelPickupPrefab == null)
        {
            // No prefab — just give P1 the power-up directly
            GiveP1PowerUp(squirrelPowerUp, "SQUIRREL 🐿️");
            return;
        }

        // Spawn bone ahead of P1 so it reveals naturally as they drive toward it
        Vector3 spawnPos = p1.transform.position + p1.transform.forward * squirrelSpawnDistance;

        // Snap to ground
        if (Physics.Raycast(spawnPos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
            spawnPos = hit.point + Vector3.up * 0.5f;

        Instantiate(squirrelPickupPrefab, spawnPos, Quaternion.identity);
        ShowCue($"🐿️ SQUIRREL bone spawned {squirrelSpawnDistance}u ahead of P1", 2f);
    }

    // ═══════════════════════════════════════════════
    //  2 & 3 — GIVE P1 A POWER-UP DIRECTLY
    // ═══════════════════════════════════════════════

    private void GiveP1PowerUp(PowerUpObject powerUp, string label)
    {
        if (powerUp == null)
        {
            ShowCue($"❌ {label} not assigned in Inspector!", 2f);
            return;
        }

        GameObject p1 = GetP1();
        if (p1 == null) return;

        PowerUpHandler handler = p1.GetComponent<PowerUpHandler>();
        if (handler == null)
        {
            ShowCue("❌ No PowerUpHandler on P1!", 2f);
            return;
        }

        bool success = handler.TryPickUpPowerUp(powerUp);
        ShowCue(success
            ? $"✅ {label} given to P1! Press use button!"
            : $"⚠️ P1 already has a power-up!", 2f);
    }

    // ═══════════════════════════════════════════════
    //  4 — TELEPORT ALL PLAYERS NEAR P1
    // ═══════════════════════════════════════════════

    private void TeleportAllNearP1()
    {
        GameObject p1 = GetP1();
        if (p1 == null) return;

        string[] tags   = { "PlayerTwo", "PlayerThree", "PlayerFour" };
        float[]  angles = { 90f, 180f, 270f };
        int      count  = 0;

        for (int i = 0; i < tags.Length; i++)
        {
            GameObject other = GameObject.FindWithTag(tags[i]);
            if (other == null) continue;

            float   rad    = angles[i] * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * teleportRadius;
            Vector3 target = p1.transform.position + offset;

            // Snap to ground
            if (Physics.Raycast(target + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f))
                target = hit.point + Vector3.up * 0.5f;

            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null) { rb.linearVelocity = Vector3.zero; rb.MovePosition(target); }
            else              other.transform.position = target;

            count++;
        }

        ShowCue($"🚗 Teleported {count} players near P1!", 2f);
    }

    // ═══════════════════════════════════════════════
    //  5 — SLOW MOTION
    // ═══════════════════════════════════════════════

    private void ToggleSlowMo()
    {
        isSlowMo = !isSlowMo;
        Time.timeScale      = isSlowMo ? slowMotionScale : 1f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        ShowCue(isSlowMo ? $"🐌 SLOW MO ({slowMotionScale}x)" : "⚡ NORMAL SPEED", 1.5f);
    }

    // ═══════════════════════════════════════════════
    //  RESET
    // ═══════════════════════════════════════════════

    private void ResetAll()
    {
        isSlowMo            = false;
        Time.timeScale      = 1f;
        Time.fixedDeltaTime = 0.02f;
        RestoreSplitScreen();
        ShowCue("🔄 RESET", 2f);
    }

    // ═══════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════

    private GameObject GetP1()
    {
        GameObject p1 = GameObject.FindWithTag("PlayerOne");
        if (p1 == null) ShowCue("❌ PlayerOne not found!", 2f);
        return p1;
    }

    private void ShowCue(string message, float duration)
    {
        Debug.Log($"[PresentationDirector] {message}");
        if (cueText == null) return;
        if (cueRoutine != null) StopCoroutine(cueRoutine);
        cueRoutine = StartCoroutine(CueRoutine(message, duration));
    }

    private IEnumerator CueRoutine(string message, float duration)
    {
        if (overlayCanvas != null) overlayCanvas.gameObject.SetActive(true);
        cueText.text  = message;
        cueText.color = Color.white;

        yield return new WaitForSecondsRealtime(duration - 0.3f);

        float t = 0f;
        while (t < 0.3f)
        {
            t            += Time.unscaledDeltaTime;
            cueText.color = Color.Lerp(Color.white, Color.clear, t / 0.3f);
            yield return null;
        }

        if (overlayCanvas != null) overlayCanvas.gameObject.SetActive(false);
    }
}

#endif