using UnityEngine;
using UnityEditor;

namespace ZG
{
    [CustomEditor(typeof(AnimatorWrapper))]
    public class AnimatorWrapperEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var target = base.target as AnimatorWrapper;

            if (GUILayout.Button("RestoreValues"))
                target.RestoreValues();

            if (GUILayout.Button("PlaybackValues"))
                target.PlaybackValues();

            base.OnInspectorGUI();
        }
    }
}