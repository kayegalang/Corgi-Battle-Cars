using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Prototyping.Scripts
{
    public class SliderScrollFix : MonoBehaviour, 
        IBeginDragHandler, IEndDragHandler, IPointerDownHandler
    {
        private ScrollRect parentScrollRect;

        private void Awake()
        {
            parentScrollRect = GetComponentInParent<ScrollRect>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (parentScrollRect != null)
                parentScrollRect.enabled = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (parentScrollRect != null)
                parentScrollRect.enabled = false;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (parentScrollRect != null)
                parentScrollRect.enabled = true;
        }
    }
}