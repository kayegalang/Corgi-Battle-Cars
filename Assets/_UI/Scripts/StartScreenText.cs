using UnityEngine;
using TMPro;

namespace _UI.Scripts
{
    public class StartScreenText : MonoBehaviour
    {
        [SerializeField] private float blinkSpeed;
        
        private TextMeshProUGUI startText;
        
        private void Start()
        {
            startText = GetComponent<TextMeshProUGUI>();
        }

        private void Update()
        {
            BlinkText();
        }

        private void BlinkText()
        {
            if (startText == null) return;
            
            float alpha = Mathf.Abs(Mathf.Sin(Time.time * blinkSpeed));
            
            Color currentColor = startText.color;
            currentColor.a = alpha;
            startText.color = currentColor;
        }
    }
}