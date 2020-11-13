using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using static UnityEngine.Debug;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace Control
{
    public class CameraViewport : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [SerializeField]
        private PhysicsRaycaster _physicsRaycaster;

        private bool _isDrag;

        public UnityEvent PointerEnter;

        public UnityEvent PointerExit;

        public UnityEvent PointerDrag;

        public UnityEvent EmptyClick;

        public void OnPointerEnter(PointerEventData eventData) => PointerEnter.Invoke();

        public void OnPointerExit(PointerEventData eventData) => PointerExit.Invoke();

        public void OnBeginDrag(PointerEventData eventData) => _isDrag = true;

        public void OnDrag(PointerEventData eventData) => PointerDrag.Invoke();

        public void OnEndDrag(PointerEventData eventData) => _isDrag = false;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isDrag) return;

            if (!ContinueHandleClick(eventData)) EmptyClick.Invoke();
        }

        private bool ContinueHandleClick(PointerEventData eventData)
        {
            List<RaycastResult> results = new List<RaycastResult>();
            _physicsRaycaster.Raycast(eventData, results);

            if (results.Count == 0) return false;

            ExecuteEvents.Execute<IPointerClickHandler>(results[0].gameObject, eventData, (handler, edata) => handler.OnPointerClick((PointerEventData)edata));

            return true;
        }
    }
}