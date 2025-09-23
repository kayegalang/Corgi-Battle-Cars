using UnityEngine;
using TMPro;

namespace UI.Scripts
{
    public class StartScreenText : MonoBehaviour
    {
        private TextMeshProUGUI startText;
        [SerializeField] private float blinkSpeed;
        void Start()
        {
            startText = GetComponent<TextMeshProUGUI>();
        }

        void Update()
        {
            float alpha = Mathf.Abs(Mathf.Sin(Time.time * blinkSpeed));
                startText.color = new Color(
                startText.color.r,
                startText.color.g,
                startText.color.b,
                alpha
            );
        }
        
        
        
    }
}

