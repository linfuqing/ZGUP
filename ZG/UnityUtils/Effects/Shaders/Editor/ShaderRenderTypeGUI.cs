using UnityEngine;
using UnityEditor;

namespace ZG
{
    public class ShaderRenderTypeGUI : ShaderGUI
    {
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            Material material = materialEditor.target as Material;
            EditorGUI.BeginChangeCheck();
            string tag = EditorGUILayout.DelayedTextField("RenderType", material.GetTag("RenderType", false));
            if (EditorGUI.EndChangeCheck())
            {
                material.SetOverrideTag("RenderType", tag);

                EditorUtility.SetDirty(material);
            }

            base.OnGUI(materialEditor, properties);
        }
    }
}