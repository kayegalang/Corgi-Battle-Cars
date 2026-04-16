using _PowerUps.ScriptableObjects;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace _PowerUps.Scripts
{
    /// <summary>
    /// Manages holding and using a single power-up
    /// Like Mario Kart's item box!
    /// </summary>
    public class PowerUpHolder : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject powerUpUI;
        [SerializeField] private TextMeshProUGUI powerUpNameText;
        
        [Header("Input")]
        private PlayerInput playerInput;
        private InputAction usePowerUpAction;
        
        private PowerUpObject heldPowerUp;
        private bool hasPowerUp = false;
        
        private void Awake()
        {
            InitializeInput();
            HideUI();
        }
        
        private void OnEnable()
        {
            if (usePowerUpAction != null)
            {
                usePowerUpAction.Enable();
                usePowerUpAction.performed += OnUsePowerUpPerformed;
            }
        }
        
        private void OnDisable()
        {
            if (usePowerUpAction != null)
            {
                usePowerUpAction.performed -= OnUsePowerUpPerformed;
                usePowerUpAction.Disable();
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
        
        /// <summary>
        /// Try to pick up a power-up
        /// Returns true if successfully picked up
        /// </summary>
        public bool TryPickUpPowerUp(PowerUpObject powerUp)
        {
            if (hasPowerUp)
            {
                Debug.Log($"[PowerUpHolder] {gameObject.name} already has a power-up! Can't pick up another.");
                return false;
            }
            
            heldPowerUp = powerUp;
            hasPowerUp = true;
            
            ShowUI();
            
            Debug.Log($"[PowerUpHolder] {gameObject.name} picked up: {powerUp.powerUpName}! Press button to use!");
            
            return true;
        }
        
        private void OnUsePowerUpPerformed(InputAction.CallbackContext ctx)
        {
            UsePowerUp();
        }
        
        public void UsePowerUp()
        {
            if (!hasPowerUp || heldPowerUp == null)
            {
                Debug.Log($"[PowerUpHolder] {gameObject.name} has no power-up to use!");
                return;
            }
            
            Debug.Log($"[PowerUpHolder] {gameObject.name} is using: {heldPowerUp.powerUpName}!");
            
            // Get the PowerUpHandler and activate the power-up
            PowerUpHandler handler = GetComponent<PowerUpHandler>();
            if (handler != null)
            {
                handler.ActivatePowerUp(heldPowerUp);
            }
            
            // Clear the held power-up
            ClearPowerUp();
        }
        
        private void ClearPowerUp()
        {
            heldPowerUp = null;
            hasPowerUp = false;
            HideUI();
            
            Debug.Log($"[PowerUpHolder] {gameObject.name} used their power-up!");
        }
        
        private void ShowUI()
        {
            if (powerUpUI != null)
            {
                powerUpUI.SetActive(true);
            }
            
            if (powerUpNameText != null && heldPowerUp != null)
            {
                powerUpNameText.text = heldPowerUp.powerUpName;
            }
        }
        
        private void HideUI()
        {
            if (powerUpUI != null)
            {
                powerUpUI.SetActive(false);
            }
        }
        
        public bool HasPowerUp()
        {
            return hasPowerUp;
        }
        
        public PowerUpObject GetHeldPowerUp()
        {
            return heldPowerUp;
        }
    }
}