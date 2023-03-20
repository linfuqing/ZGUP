using UnityEngine;
using UnityEditor;

namespace ZG
{
    public class MaterialClipDrawer : MaterialPropertyDrawer
    {
        private enum Type
        {
            None, 
            Normal, 
            Global
        }

        private readonly string __keyword;

        public MaterialClipDrawer()
        {

        }

        public MaterialClipDrawer(string keyword)
        {
            __keyword = keyword;
        }
        
        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            return prop.type == MaterialProperty.PropType.Vector && (Type)Mathf.RoundToInt(prop.vectorValue.w) == Type.Normal ? 
                EditorGUIUtility.singleLineHeight * 4.0f : base.GetPropertyHeight(prop, label, editor);
        }

        public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
        {
            bool isMixedValue = prop.hasMixedValue;
            Type type;
            switch (prop.type)
            {
                case MaterialProperty.PropType.Float:
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.showMixedValue = isMixedValue;
                    type = (Type)EditorGUI.EnumPopup(position, "Clip Type", (Type)Mathf.RoundToInt(prop.floatValue));
                    //if (EditorGUI.EndChangeCheck())
                    if(EditorGUI.EndChangeCheck() || !isMixedValue)
                    {
                        prop.floatValue = (int)type;

                        Material material;
                        Object[] targets = prop.targets;
                        int numTargets = targets.Length;
                        switch(type)
                        {
                            case Type.Normal:
                                for (int i = 0; i < numTargets; i++)
                                {
                                    material = (Material)targets[i];

                                    if (string.IsNullOrEmpty(__keyword))
                                        material.EnableKeyword(__keyword);

                                    material.DisableKeyword("CLIP_MIX");
                                    material.DisableKeyword("CLIP_GLOBAL");
                                    
                                    EditorUtility.SetDirty(material);
                                }
                                break;
                            case Type.Global:
                                for (int i = 0; i < numTargets; i++)
                                {
                                    material = (Material)targets[i];

                                    if (!string.IsNullOrEmpty(__keyword))
                                        material.EnableKeyword(__keyword);

                                    material.EnableKeyword("CLIP_GLOBAL");
                                    material.DisableKeyword("CLIP_MIX");

                                    EditorUtility.SetDirty(material);
                                }
                                break;
                            default:
                                for (int i = 0; i < numTargets; i++)
                                {
                                    material = (Material)targets[i];

                                    if (!string.IsNullOrEmpty(__keyword))
                                        material.DisableKeyword(__keyword);

                                    material.DisableKeyword("CLIP_MIX");
                                    material.DisableKeyword("CLIP_GLOBAL");

                                    EditorUtility.SetDirty(material);
                                }
                                break;
                        }
                    }
                    break;
                case MaterialProperty.PropType.Vector:
                    Vector4 value = prop.vectorValue;

                    float height = EditorGUIUtility.singleLineHeight;
                    position.height = height;

                    EditorGUI.BeginChangeCheck();
                    EditorGUI.showMixedValue = isMixedValue;
                    type = (Type)EditorGUI.EnumPopup(position, "Clip Type", (Type)Mathf.RoundToInt(value.w));
                    bool isDirty = EditorGUI.EndChangeCheck();
                    if (isDirty || !isMixedValue)
                    {
                        value.w = (int)type;

                        Material material;
                        Object[] targets = prop.targets;
                        int numTargets = targets.Length;
                        switch (type)
                        {
                            case Type.Normal:
                                for (int i = 0; i < numTargets; ++i)
                                {
                                    material = (Material)targets[i];

                                    if (string.IsNullOrEmpty(__keyword))
                                        material.EnableKeyword(__keyword);

                                    material.EnableKeyword("CLIP_MIX");
                                    material.DisableKeyword("CLIP_GLOBAL");

                                    EditorUtility.SetDirty(material);
                                }
                                break;
                            case Type.Global:
                                for (int i = 0; i < numTargets; ++i)
                                {
                                    material = (Material)targets[i];

                                    if (!string.IsNullOrEmpty(__keyword))
                                        material.EnableKeyword(__keyword);

                                    material.EnableKeyword("CLIP_GLOBAL");
                                    material.DisableKeyword("CLIP_MIX");

                                    EditorUtility.SetDirty(material);
                                }
                                break;
                            default:
                                for (int i = 0; i < numTargets; ++i)
                                {
                                    material = (Material)targets[i];

                                    if (string.IsNullOrEmpty(__keyword))
                                        material.DisableKeyword(__keyword);

                                    material.DisableKeyword("CLIP_MIX");
                                    material.DisableKeyword("CLIP_GLOBAL");
                                    
                                    EditorUtility.SetDirty(material);
                                }
                                break;
                        }
                    }

                    if (type == Type.Normal)
                    {
                        ++EditorGUI.indentLevel;

                        position.y += height;

                        EditorGUI.BeginChangeCheck();
                        float distance = EditorGUI.FloatField(position, "Distance", 1.0f / value.x);
                        if (EditorGUI.EndChangeCheck())
                        {
                            isDirty = true;

                            value.x = 1.0f / distance;
                        }

                        position.y += height;

                        EditorGUI.BeginChangeCheck();
                        float near = EditorGUI.FloatField(position, "Near", value.y * distance);
                        if (EditorGUI.EndChangeCheck())
                        {
                            isDirty = true;

                            value.y = near / distance;
                        }

                        position.y += height;

                        EditorGUI.BeginChangeCheck();
                        float far = EditorGUI.FloatField(position, "Far", value.z * distance);
                        if (EditorGUI.EndChangeCheck())
                        {
                            isDirty = true;

                            value.z = far / distance;
                        }

                        --EditorGUI.indentLevel;
                    }

                    if (isDirty)
                        prop.vectorValue = value;
                    break;
                default:
                    EditorGUI.HelpBox(position, "Clip Must be Vector Type", MessageType.Error);
                    break;
            }
        }
    }
}