using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using static UnityEngine.Debug;
using UnityEngine.EventSystems;
using System;
using UnityEngine.Events;

namespace UI.Exploring.FileSystem
{
	public class ScrollRect : UnityEngine.UI.ScrollRect, IPointerClickHandler
	{
        public UnityEvent Clicked;

        public override void OnBeginDrag(PointerEventData eventData) { }
        public override void OnDrag(PointerEventData eventData) { }
        public override void OnEndDrag(PointerEventData eventData) { }

        public void OnPointerClick(PointerEventData eventData) { Clicked.Invoke(); print("DESELECTED"); }
    }
}