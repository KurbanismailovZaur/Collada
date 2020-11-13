using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using static UnityEngine.Debug;
using System;
using UnityEngine.UI;
using UnityEngine.Events;

namespace UI.Exploring.FileSystem
{
	public class Bookmark : MonoBehaviour 
	{
        #region Class-events
        [Serializable]
        public class ClickedEvent : UnityEvent<Bookmark> { }
        #endregion

        [SerializeField]
        private Button _button;

        [SerializeField]
        private Environment.SpecialFolder _specialFolder;

        [Header("Events")]
        public ClickedEvent Clicked;

        public Button Button { get => _button; }

        public Environment.SpecialFolder SpecialFolder { get => _specialFolder; }

        #region Event handlers
        public void Button_OnClick() => Clicked.Invoke(this);
        #endregion
    }
}