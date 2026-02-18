using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _UI.Scripts
{
    public class UIFirstSelected : MonoBehaviour
    {
        [Header("First Selected Button")]
        [Tooltip("The button to auto-select when this menu opens")]
        [SerializeField] private Selectable firstSelected;

        private void OnEnable()
        {
            SelectFirstButton();
        }

        private void SelectFirstButton()
        {
            if (firstSelected == null)
            {
                Debug.LogWarning($"[UIFirstSelected] No first selected button assigned on {gameObject.name}");
                return;
            }

            EventSystem.current.SetSelectedGameObject(null);

            StartCoroutine(SelectNextFrame());
        }

        private System.Collections.IEnumerator SelectNextFrame()
        {
            yield return null;
            
            if (EventSystem.current != null && firstSelected != null)
            {
                EventSystem.current.SetSelectedGameObject(firstSelected.gameObject);
            }
        }
    }
}