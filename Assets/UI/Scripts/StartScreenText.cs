using UnityEngine;
using TMPro;

namespace UI.Scripts
{
    public class StartScreenText : MonoBehaviour
    {
        private TextMeshProUGUI _startText;
        [SerializeField] private float blinkSpeed;
        void Start()
        {
            _startText = GetComponent<TextMeshProUGUI>();
        }

        void Update()
        {
            float alpha = Mathf.Abs(Mathf.Sin(Time.time * blinkSpeed));
                _startText.color = new Color(
                _startText.color.r,
                _startText.color.g,
                _startText.color.b,
                alpha
            );
        }
        
        
        
    }
}

