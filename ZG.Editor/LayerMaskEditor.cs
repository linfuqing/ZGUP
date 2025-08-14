using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace ZG
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class LayerMaskAttribute : Attribute
    {

    }

    public class LayerMaskEditor : EditorWindow
    {
        private class Value
        {
            public readonly object Target;
            public readonly MemberInfo Member;
            public readonly Value Parent;

            public Value(object target, MemberInfo member, Value parent)
            {
                Target = target;
                Member = member;
                Parent = parent;
            }

            public virtual object value
            {
                get
                {
                    if(Member is FieldInfo field)
                        return field.GetValue(Target);
                    
                    return ((PropertyInfo)Member).GetValue(Target, null);
                }

                set
                {
                    if(Member is FieldInfo field)
                        field.SetValue(Target, value);
                    else
                        ((PropertyInfo)Member).SetValue(Target, value);

                    if (Target is Component target)
                        PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                    else if (Parent != null)
                        Parent.value = Target;
                }
            }

            public void OnGUI(Rect rect, bool active, bool focused)
            {
                EditorGUI.LabelField(rect, Target.ToString());
            }
        }

        private class LayerMaskValue : Value
        {
            public readonly MemberInfo MemberExclude;

            public LayerMaskValue(
                object target, 
                MemberInfo memberInclude, 
                MemberInfo memberExclude, 
                Value parent) : base(
                target, memberInclude, parent)
            {
                MemberExclude = memberExclude;
            }

            public override object value
            {
                get => base.value;

                set
                {
                    if(MemberExclude is FieldInfo field)
                        field.SetValue(Target, value);
                    else
                        ((PropertyInfo)MemberExclude).SetValue(Target, value);

                    base.value = value;
                }
            }

            public static void Create(int layerMask, object target, MemberInfo member, Value parent, Action<Value> results)
            {
                Value value;
                if (member == null)
                    value = null;
                else
                {
                    value = new Value(target, member, parent);
                    target = value.value;
                    if (member != null && typeof(LayerMask) == member.DeclaringType &&
                        (layerMask & (LayerMask)target) != 0)
                    {
                        results(value);

                        return;
                    }
                }

                PropertyInfo[] properties;
                FieldInfo[] fields;
                var type = target?.GetType();
                while (type != null)
                {
                    fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    foreach (var field in fields)
                        Create(layerMask, target, field, value, results);
                    
                    properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    foreach (var property in properties)
                        Create(layerMask, target, property, value, results);
                    
                    type = type.BaseType;
                }
            }
        }

        private LayerMask __layerMask;
        private ReorderableList __reorderableList;

        [MenuItem("Window/ZG/LayerMask Editor")]
        public static void Show()
        {
            GetWindow<LayerMaskEditor>();
        }

        public static LayerMask LayerMaskFieldLayout(LayerMask layers, GUIContent label,
            params GUILayoutOption[] options)
        {
            var method = typeof(EditorGUILayout).GetMethod(
                "LayerMaskField",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new Type[]
                {
                    typeof(LayerMask),
                    typeof(GUIContent),
                    typeof(GUILayoutOption[])
                },
                null);
            return (LayerMask)method.Invoke(null, new object[]
            {
                layers,
                label,
                options
            });
        }

        void OnGUI()
        {
            __layerMask = LayerMaskFieldLayout(__layerMask, new GUIContent("LayerMask"));
            if (GUILayout.Button("Refresh"))
            {
                var values = new List<Value>();
                EditorHelper.UpdatePrefabs("Refresh LayerMask..", x =>
                {
                    Type type;
                    PropertyInfo[] properties;
                    FieldInfo[] fields;
                    var components = x.GetComponentsInChildren<Component>(true);
                    foreach (var component in components)
                        LayerMaskValue.Create(__layerMask, component, null, null, values.Add);

                    return false;
                });
                
                __reorderableList = new ReorderableList(
                    values,
                    typeof(Value),
                    true,
                    false,
                    false,
                    false);
                __reorderableList.drawElementCallback += (rect, index, active, focused) =>
                {
                    values[index].OnGUI(rect, active, focused);
                };
            }

            if (__reorderableList != null)
                __reorderableList.DoLayoutList();
        }
    }
}