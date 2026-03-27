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
    /// </summary>
    public class PowerUpHandler : MonoBehaviour
    {
        [Header("Active Power-Up UI")]
        [SerializeField] private GameObject      powerUpUI;
        [SerializeField] private Image           powerUpIcon;
        [SerializeField] private TextMeshProUGUI powerUpNameText;
        [SerializeField] private Image           timerBar;

        [Header("Held Power-Up UI")]
        [SerializeField] private GameObject      heldPowerUpUI;
        [SerializeField] private TextMeshProUGUI heldPowerUpNameText;

        [Header("Bot Settings")]
        [SerializeField] private float botAutoUseDelay = 1.5f;

        [Header("Input")]
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
        private bool         hasThrowable = false;
        private SquirrelPowerUp squirrelData;

        // Pause
        private PauseController pauseController;

        public bool IsBot { get; private set; }

        // ──────────────────────────────────────────────
        //  LIFECYCLE
        // ──────────────────────────────────────────────

        private void Awake()
        {
            IsBot = GetComponent<BotAI>() != null;
            InitializeInput();
            HidePowerUpUI();
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
            UpdateTimerBar();
            activePowerUp?.OnUpdate(gameObject);
        }

        // ──────────────────────────────────────────────
        //  PAUSE HANDLING
        // ──────────────────────────────────────────────

        private void OnPaused()
        {
            HidePowerUpUI();
            HideHeldPowerUpUI();
        }

        private void OnUnpaused()
        {
            // Restore UI state based on what's currently active
            if (isPowerUpActive && activePowerUp != null)
                ShowPowerUpUI(activePowerUp);

            if (hasPowerUp && heldPowerUp != null)
                ShowHeldPowerUpUI();
        }

        // ──────────────────────────────────────────────
        //  HOLD AND USE
        // ──────────────────────────────────────────────

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
            Debug.Log($"[PowerUpHandler] {gameObject.name} used their held power-up!");
        }

        public bool          HasHeldPowerUp()  => hasPowerUp;
        public PowerUpObject GetHeldPowerUp()  => heldPowerUp;

        // ──────────────────────────────────────────────
        //  ACTIVATION
        // ──────────────────────────────────────────────

        public void ActivatePowerUp(PowerUpObject powerUp)
        {
            if (isPowerUpActive)
                RemoveActivePowerUp();

            activePowerUp     = powerUp;
            remainingDuration = powerUp.duration;
            isPowerUpActive   = true;

            powerUp.Apply(gameObject);
            ShowPowerUpUI(powerUp);

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

            PowerUpHandler[] allHandlers = FindObjectsByType<PowerUpHandler>(FindObjectsSortMode.None);
            foreach (PowerUpHandler handler in allHandlers)
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

            HidePowerUpUI();
        }

        // ──────────────────────────────────────────────
        //  END GAME
        // ──────────────────────────────────────────────

        public void OnGameEnd()
        {
            isPowerUpActive = false;
            hasPowerUp      = false;
            heldPowerUp     = null;
            activePowerUp   = null;

            HidePowerUpUI();
            HideHeldPowerUpUI();
        }

        // ──────────────────────────────────────────────
        //  UI — ACTIVE POWER-UP
        // ──────────────────────────────────────────────

        private void ShowPowerUpUI(PowerUpObject powerUp)
        {
            if (powerUpUI       != null) powerUpUI.SetActive(true);
            if (powerUpIcon     != null) powerUpIcon.sprite = powerUp.icon;
            if (powerUpNameText != null) powerUpNameText.text = powerUp.powerUpName;
            if (timerBar        != null) timerBar.fillAmount = 1f;
        }

        private void HidePowerUpUI()
        {
            if (powerUpUI != null) powerUpUI.SetActive(false);
        }

        private void UpdateTimerBar()
        {
            if (timerBar == null || activePowerUp == null) return;
            timerBar.fillAmount = remainingDuration / activePowerUp.duration;
        }

        // ──────────────────────────────────────────────
        //  UI — HELD POWER-UP
        // ──────────────────────────────────────────────

        private void ShowHeldPowerUpUI()
        {
            if (heldPowerUpUI != null)
                heldPowerUpUI.SetActive(true);

            if (heldPowerUpNameText != null && heldPowerUp != null)
                heldPowerUpNameText.text = heldPowerUp.powerUpName;
        }

        private void HideHeldPowerUpUI()
        {
            if (heldPowerUpUI != null)
                heldPowerUpUI.SetActive(false);
        }

        // ──────────────────────────────────────────────
        //  SUPER BARK
        // ──────────────────────────────────────────────

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

        // ──────────────────────────────────────────────
        //  SQUIRREL
        // ──────────────────────────────────────────────

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

        // ──────────────────────────────────────────────
        //  GETTERS
        // ──────────────────────────────────────────────

        public bool          IsPowerUpActive()      => isPowerUpActive;
        public PowerUpObject GetActivePowerUp()     => activePowerUp;
        public float         GetRemainingDuration() => remainingDuration;
    }
}