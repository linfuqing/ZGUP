using UnityEngine;
using UnityEditor;

namespace ZG
{
    [CustomPropertyDrawer(typeof(RotationAttribute))]
    public class RotationDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.Vector3)
            {
                float singleLineHeight = EditorGUIUtility.singleLineHeight;
                position.height = singleLineHeight;

                bool isExpanded = EditorGUI.Foldout(position, property.isExpanded, property.displayName);
                property.isExpanded = isExpanded;
                if (isExpanded)
                {
                    ++EditorGUI.indentLevel;

                    position.y += singleLineHeight;

                    bool isEmpty = true, isDirty = false;

                    var type = ((RotationAttribute)attribute).type;

                    Vector3 value;

                    var transform = EditorGUI.ObjectField(position, "Transform", null, typeof(Transform), true) as Transform;
                    if (transform == null)
                    {
                        value = property.vector3Value;
                        switch (type)
                        {
                            case RotationType.Direction:
                                isEmpty = Vector3.zero == value;
                                value = isEmpty ? Vector3.zero : Quaternion.FromToRotation(Vector3.forward, value).eulerAngles;
                                break;
                        }
                    }
                    else
                    {
                        isDirty = true;

                        value = transform.localRotation.eulerAngles;
                    }

                    position.y += singleLineHeight;

                    value[0] = Mathf.Floor(value.x / 1E-06f) * 1E-06f;
                    value[1] = Mathf.Floor(value.y / 1E-06f) * 1E-06f;
                    value[2] = Mathf.Floor(value.z / 1E-06f) * 1E-06f;

                    EditorGUI.BeginChangeCheck();
                    var rotation = EditorGUI.Vector3Field(position, property.displayName, value);
                    isDirty |= EditorGUI.EndChangeCheck();

                    position.y += singleLineHeight;

                    if (isEmpty && !isDirty)
                        GUI.Box(position, "Empty");
                    else
                    {
                        if (GUI.Button(position, "Clear"))
                            property.vector3Value = Vector3.zero;
                        else if (isDirty)
                        {
                            switch (type)
                            {
                                case RotationType.Direction:

                                    property.vector3Value = Quaternion.Euler(rotation) * Vector3.forward;

                                    break;
                            }
                        }
                    }

                    --EditorGUI.indentLevel;
                }
            }
            else
                EditorGUI.HelpBox(position, "Need Vector3.", MessageType.Error);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return property.isExpanded ? EditorGUIUtility.singleLineHeight * 4.0f : EditorGUIUtility.singleLineHeight;
        }
    }
}