using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using static UnityEngine.Debug;
using UnityEditor;

namespace UI.Exploring.FileSystem.Editor
{
    [CustomEditor(typeof(ScrollRect))]
    public class ScrollRectEditor : UnityEditor.UI.ScrollRectEditor
	{
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            SerializedProperty pointerUpedProperty = serializedObject.FindProperty("Clicked");
            EditorGUILayout.PropertyField(pointerUpedProperty);
            
            if (GUI.changed)
                serializedObject.ApplyModifiedProperties();
        }
    }
}