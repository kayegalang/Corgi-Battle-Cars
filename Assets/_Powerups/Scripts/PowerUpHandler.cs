using System.Collections;
using _Bot.Scripts;
using _PowerUps.ScriptableObjects;
using _UI.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace _PowerUps.Scripts
{
    /// <summary>
    /// Sits on the player. Receives power-ups from PowerUpPickup and manages
    /// activating, ticking, and removing effects.
    /// Power-up UI sits on the existing PlayerUICanvas (Screen Space - Overlay)
    /// and is positioned exactly like the reticle — using player tag + count.
    /// </summary>
    public class PowerUpHandler : MonoBehaviour
    {
        [Header("Held Power-Up UI")]
        [SerializeField] private GameObject heldPowerUpUI;
        [SerializeField] private Image      heldPowerUpIcon;  // assign the Image component that shows the icon
        [SerializeField] private RectTransform heldPowerUpRect;

        [Header("Bot Settings")]
        [SerializeField] private float botAutoUseDelay = 1.5f;

        private PlayerInput playerInput;
        private InputAction usePowerUpAction;

        // Held power-up state
        private PowerUpObject heldPowerUp;
        private bool          hasPowerUp = false;

        // Active power-up state
        private PowerUpObject activePowerUp;
        private float         remainingDuration;
        private bool          isPowerUpActive = false;

        // Super Bark state
        private int              barkCharges  = 0;
        private SuperBarkPowerUp superBarkData;

        // Squirrel state
        private bool            hasThrowable = false;
        private SquirrelPowerUp squirrelData;

        // Pause
        private PauseController pauseController;

        public bool IsBot { get; private set; }

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Awake()
        {
            IsBot = GetComponent<BotAI>() != null;
            InitializeInput();
            HideHeldPowerUpUI();
        }

        private void Start()
        {
            pauseController = FindFirstObjectByType<PauseController>();
            if (pauseController != null)
            {
                pauseController.onPaused.AddListener(OnPaused);
                pauseController.onUnpaused.AddListener(OnUnpaused);
            }

            // Wait one frame for all players to spawn before positioning
            StartCoroutine(PositionDelayed());
        }

        private IEnumerator PositionDelayed()
        {
            yield return null;
            PositionPowerUpUI();
        }

        private void OnEnable()
        {
            if (usePowerUpAction != null)
            {
                usePowerUpAction.Enable();
                usePowerUpAction.performed += OnUsePowerUpPressed;
            }
        }

        private void OnDisable()
        {
            if (usePowerUpAction != null)
            {
                usePowerUpAction.performed -= OnUsePowerUpPressed;
                usePowerUpAction.Disable();
            }
        }

        private void OnDestroy()
        {
            if (pauseController != null)
            {
                pauseController.onPaused.RemoveListener(OnPaused);
                pauseController.onUnpaused.RemoveListener(OnUnpaused);
            }
        }

        private void InitializeInput()
        {
            playerInput = GetComponent<PlayerInput>();

            if (playerInput != null)
            {
                var actions = playerInput.actions;
                usePowerUpAction = actions.FindAction("UsePowerUp", true);
            }
        }

        private void Update()
        {
            if (!isPowerUpActive) return;

            TickActivePowerUp();
            activePowerUp?.OnUpdate(gameObject);
        }

        // ═══════════════════════════════════════════════
        //  POSITIONING — same approach as reticle
        // ═══════════════════════════════════════════════

        private void PositionPowerUpUI()
        {
            if (heldPowerUpRect == null) return;

            // Use playerCamera.rect exactly like PlayerUIManager does for score text
            Camera playerCam = GetComponentInChildren<Camera>();
            if (playerCam == null) return;

            Rect viewportRect = playerCam.rect;

            // Anchor to top-left of this player's viewport slice
            Vector2 anchorPosition = new Vector2(viewportRect.xMin, viewportRect.yMax);
            heldPowerUpRect.anchorMin = anchorPosition;
            heldPowerUpRect.anchorMax = anchorPosition;

            // Small offset from the corner so it's not right on the edge
            heldPowerUpRect.anchoredPosition = new Vector2(20f, -20f);

            Debug.Log($"[PowerUpHandler] {gameObject.name} anchored at {anchorPosition}");
        }

        // ═══════════════════════════════════════════════
        //  PAUSE HANDLING
        // ═══════════════════════════════════════════════

        private void OnPaused()
        {
            HideHeldPowerUpUI();
        }

        private void OnUnpaused()
        {
            if (hasPowerUp && heldPowerUp != null)
                ShowHeldPowerUpUI();
        }

        // ═══════════════════════════════════════════════
        //  HOLD AND USE
        // ═══════════════════════════════════════════════

        public bool TryPickUpPowerUp(PowerUpObject powerUp)
        {
            if (hasPowerUp)
            {
                Debug.Log($"[PowerUpHandler] {gameObject.name} already has a power-up! Can't pick up another.");
                return false;
            }

            heldPowerUp = powerUp;
            hasPowerUp  = true;

            ShowHeldPowerUpUI();

            Debug.Log($"[PowerUpHandler] {gameObject.name} picked up: {powerUp.powerUpName}! Press button to use!");

            if (IsBot)
                UseHeldPowerUp();

            return true;
        }

        private void OnUsePowerUpPressed(InputAction.CallbackContext ctx)
        {
            UseHeldPowerUp();
        }

        public void UseHeldPowerUp()
        {
            if (!hasPowerUp || heldPowerUp == null)
            {
                Debug.Log($"[PowerUpHandler] {gameObject.name} has no power-up to use!");
                return;
            }

            Debug.Log($"[PowerUpHandler] {gameObject.name} is using: {heldPowerUp.powerUpName}!");

            ActivatePowerUp(heldPowerUp);
            ClearHeldPowerUp();
        }

        private void ClearHeldPowerUp()
        {
            heldPowerUp = null;
            hasPowerUp  = false;
            HideHeldPowerUpUI();
        }

        public bool          HasHeldPowerUp() => hasPowerUp;
        public PowerUpObject GetHeldPowerUp() => heldPowerUp;

        // ═══════════════════════════════════════════════
        //  ACTIVATION
        // ═══════════════════════════════════════════════

        public void ActivatePowerUp(PowerUpObject powerUp)
        {
            if (isPowerUpActive)
                RemoveActivePowerUp();

            activePowerUp     = powerUp;
            remainingDuration = powerUp.duration;
            isPowerUpActive   = true;

            powerUp.Apply(gameObject);

            if (powerUp.duration <= 0f)
            {
                RemoveActivePowerUp();
                return;
            }

            if (IsBot)
                StartCoroutine(BotAutoUse());

            Debug.Log($"[PowerUpHandler] {gameObject.name} activated {powerUp.powerUpName}!");
        }

        private IEnumerator BotAutoUse()
        {
            yield return new WaitForSeconds(botAutoUseDelay);

            if (barkCharges > 0)
            {
                while (barkCharges > 0)
                {
                    UseBark();
                    yield return new WaitForSeconds(0.8f);
                }
                yield break;
            }

            if (hasThrowable && squirrelData != null)
                ThrowSquirrelAtNearestTarget();
        }

        private void ThrowSquirrelAtNearestTarget()
        {
            float      closestDist   = Mathf.Infinity;
            GameObject closestTarget = null;

            foreach (PowerUpHandler handler in FindObjectsByType<PowerUpHandler>(FindObjectsSortMode.None))
            {
                if (handler.gameObject == gameObject) continue;
                if (handler.IsBot) continue;

                float dist = Vector3.Distance(transform.position, handler.transform.position);
                if (dist < closestDist)
                {
                    closestDist   = dist;
                    closestTarget = handler.gameObject;
                }
            }

            Vector3 throwDir = closestTarget != null
                ? (closestTarget.transform.position - transform.position).normalized
                : transform.forward;

            squirrelData.ThrowSquirrel(gameObject, throwDir);
            hasThrowable = false;
            RemoveActivePowerUp();
        }

        private void TickActivePowerUp()
        {
            remainingDuration -= Time.deltaTime;
            if (remainingDuration <= 0f)
                RemoveActivePowerUp();
        }

        private void RemoveActivePowerUp()
        {
            if (activePowerUp == null) return;

            activePowerUp.Remove(gameObject);
            Debug.Log($"[PowerUpHandler] {gameObject.name}'s {activePowerUp.powerUpName} expired!");

            activePowerUp     = null;
            isPowerUpActive   = false;
            remainingDuration = 0f;
        }

        // ═══════════════════════════════════════════════
        //  END GAME
        // ═══════════════════════════════════════════════

        public void OnGameEnd()
        {
            isPowerUpActive = false;
            hasPowerUp      = false;
            heldPowerUp     = null;
            activePowerUp   = null;

            HideHeldPowerUpUI();
        }

        // ═══════════════════════════════════════════════
        //  UI — HELD POWER-UP
        // ═══════════════════════════════════════════════

        private void ShowHeldPowerUpUI()
        {
            if (heldPowerUpUI != null)
                heldPowerUpUI.SetActive(true);

            // Show the icon sprite from the PowerUpObject
            if (heldPowerUpIcon != null && heldPowerUp != null)
            {
                heldPowerUpIcon.sprite  = heldPowerUp.icon;
                heldPowerUpIcon.enabled = heldPowerUp.icon != null;
            }
        }

        private void HideHeldPowerUpUI()
        {
            if (heldPowerUpUI != null)
                heldPowerUpUI.SetActive(false);
        }

        // ═══════════════════════════════════════════════
        //  SUPER BARK
        // ═══════════════════════════════════════════════

        public void SetBarkCharges(int charges)             => barkCharges  = charges;
        public void SetSuperBarkData(SuperBarkPowerUp data) => superBarkData = data;
        public int  GetBarkCharges()                        => barkCharges;

        public void UseBark()
        {
            if (barkCharges <= 0 || superBarkData == null) return;

            superBarkData.ExecuteBark(gameObject);
            barkCharges--;

            Debug.Log($"[PowerUpHandler] {gameObject.name} barked! {barkCharges} charges left");

            if (barkCharges <= 0)
                RemoveActivePowerUp();
        }

        // ═══════════════════════════════════════════════
        //  SQUIRREL
        // ═══════════════════════════════════════════════

        public void SetSquirrelData(SquirrelPowerUp data) => squirrelData = data;
        public void SetHasThrowable(bool value)           => hasThrowable = value;
        public bool HasThrowable()                        => hasThrowable;

        public void ThrowSquirrel()
        {
            if (!hasThrowable || squirrelData == null) return;

            squirrelData.ThrowSquirrel(gameObject, transform.forward);
            hasThrowable = false;
            RemoveActivePowerUp();

            Debug.Log($"[PowerUpHandler] {gameObject.name} threw the squirrel!");
        }

        // ═══════════════════════════════════════════════
        //  GETTERS
        // ═══════════════════════════════════════════════

        public bool          IsPowerUpActive()      => isPowerUpActive;
        public PowerUpObject GetActivePowerUp()     => activePowerUp;
        public float         GetRemainingDuration() => remainingDuration;
    }
}