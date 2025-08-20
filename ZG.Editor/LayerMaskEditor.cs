using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[assembly: ZG.LayerMask(typeof(Collider), "includeLayers", "excludeLayers")]

namespace ZG
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class LayerMaskAttribute : Attribute
    {
        public readonly Type Type;
        public readonly string IncludeMemberName;
        public readonly string ExcludeMemberName;

        public LayerMaskAttribute(Type type, string includeMemberName, string excludeMemberName)
        {
            Type = type;
            IncludeMemberName = includeMemberName;
            ExcludeMemberName = excludeMemberName;
        }
    }

    public class LayerMaskEditor : EditorWindow
    {
        private class Value
        {
            public readonly object[] Indices;
            public readonly object Target;
            public readonly MemberInfo Member;
            public readonly Value Parent;

            public float guiHeight
            {
                get
                {
                    return EditorGUIUtility.singleLineHeight;
                }
            }

            public virtual object value
            {
                get
                {
                    if (Target is Array array)
                    {
                        int numIndices = Indices.Length;
                        var indices = new int[numIndices];
                        for(int i = 0; i < numIndices; ++i)
                            indices[i] = (int)Indices[i];

                        return array.GetValue(indices);
                    }

                    if (Member is FieldInfo field)
                        return field.GetValue(Target);

                    try
                    {
                        if (Member is PropertyInfo property)
                            return property.GetValue(Target, Indices);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                    
                    return null;
                }

                set
                {
                    if (Target is Array array)
                    {
                        int numIndices = Indices.Length;
                        var indices = new int[numIndices];
                        for(int i = 0; i < numIndices; ++i)
                            indices[i] = (int)Indices[i];

                        array.SetValue(value, indices);
                    }
                    else if(Member is FieldInfo field)
                        field.SetValue(Target, value);
                    else if (Member is PropertyInfo property)
                        property.SetValue(Target, value, Indices);

                    if (Target is Component component)
                    {
                        PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                        
                        EditorUtility.SetDirty(component);//.transform.root.gameObject);
                    }
                    else if(Target is UnityEngine.Object target)
                        EditorUtility.SetDirty(target);
                    else if (Parent != null)
                        Parent.value = Target;
                }
            }

            public Value root
            {
                get
                {
                    if (Parent == null)
                        return this;

                    return Parent.root;
                }
            }

            public Value(object[] indices, object target, MemberInfo member, Value parent)
            {
                Indices = indices;
                Target = target;
                Member = member;
                Parent = parent;
            }

            public void OnGUI(Rect rect, bool active, bool focused)
            {
                if (!__windowPosition.Contains(rect.position - __scrollPosition + __windowPosition.position))
                    return;
                
                float width = rect.width /= 4.0f;
                if (root.Target is UnityEngine.Object target)
                {
                    //if(focused)
                    //    Selection.activeObject = target;
                    string path = String.Empty;
                    if (target is Component component)
                    {
                        var transform = component.transform;
                        path = transform.GetPath(transform.root);
                    }
                    
                    EditorGUI.ObjectField(rect, path, target, target.GetType(), false);
                }
                
                rect.x += width;
                rect.width = width * 3.0f;

                _OnGUI(rect, active, focused);
            }

            public override string ToString()
            {
                if (Parent == null)
                    return Member.Name;

                StringBuilder stringBuilder = null;
                if (Indices != null)
                {
                    foreach (var index in Indices)
                    {
                        if(stringBuilder == null)
                            stringBuilder = new StringBuilder($"[{index}");
                        else
                            stringBuilder.Append($", {index}");
                    }

                    if(stringBuilder != null)
                        stringBuilder.Append(']');
                    
                    return $"{Parent}{stringBuilder}";
                }
                
                return $"{Parent}.{Member.Name}";
            }

            protected virtual void _OnGUI(Rect rect, bool active, bool focused)
            {
                EditorGUI.LabelField(rect, ToString());
            }
        }

        private class LayerMaskValue : Value
        {
            public readonly MemberInfo MemberExclude;

            public static readonly HashSet<LayerMaskValue> Toggles = new HashSet<LayerMaskValue>();

            public LayerMaskValue(
                object[] indices, 
                object target, 
                MemberInfo memberInclude, 
                MemberInfo memberExclude, 
                Value parent) : base(
                indices, 
                target, 
                memberInclude, 
                parent)
            {
                MemberExclude = memberExclude;
            }

            public override object value
            {
                get => base.value;

                set
                {
                    if (MemberExclude != null)
                    {
                        if (MemberExclude is FieldInfo field)
                            field.SetValue(Target, (LayerMask)~((LayerMask)value).value);
                        else
                            ((PropertyInfo)MemberExclude).SetValue(Target, (LayerMask)~((LayerMask)value).value);
                    }

                    base.value = value;
                }
            }

            protected override void _OnGUI(Rect rect, bool active, bool focused)
            {
                float width = rect.width / 10.0f;
                rect.width = width * 9.0f;
                //rect.x += (rect.width /= 3.0f) * 2.0f;
                EditorGUI.BeginChangeCheck();
                var layerMask = LayerMaskField(rect, (LayerMask)value, new GUIContent(ToString()));
                if (EditorGUI.EndChangeCheck())
                    value = layerMask;
                
                rect.x += rect.width;
                rect.width = width;
                EditorGUI.BeginChangeCheck();
                bool toggle = EditorGUI.Toggle(rect, Toggles.Contains(this));
                if (EditorGUI.EndChangeCheck())
                {
                    if (toggle)
                        Toggles.Add(this);
                    else
                        Toggles.Remove(this);
                }
            }

            public static HashSet<object> CreateTargets()
            {
                return new HashSet<object>();
            }

            public static void Create(
                int layerMaskInclude,
                int layerMaskExclude, 
                object target, 
                MemberInfo member, 
                Value parent, 
                Action<Value> results, 
                HashSet<object> targets)
            {
                if (member == null)
                    parent = null;
                else
                {
                    parent = new Value(null, target, member, parent);
                    target = parent.value;
                    if (target is LayerMask targetLayerMask)
                    {
                        if ((layerMaskInclude & targetLayerMask) != 0 &&
                            (layerMaskExclude & targetLayerMask) == 0)
                        {
                            MemberInfo memberExclude = null;
                            var type = parent.Target.GetType();
                            if (GetLayerMask(type, out _, out var excludeMemberName))
                                memberExclude = type.GetProperty(excludeMemberName,
                                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            
                            results(new LayerMaskValue(
                                parent.Indices, 
                                parent.Target, 
                                parent.Member, 
                                memberExclude, 
                                parent.Parent));
                        }

                        return;
                    }
                }

                __Create(layerMaskInclude, layerMaskExclude, target, parent, results, targets);
            }

            private static void __Create(
                int layerMaskInclude,
                int layerMaskExclude, 
                object target, 
                Value parent, 
                Action<Value> results, 
                HashSet<object> targets)
            {
                var type = target?.GetType();
                if (type == null || 
                    type.IsPrimitive || 
                    type == typeof(decimal) || 
                    type == typeof(string) || 
                    type == typeof(Pointer))
                    return;
                
                if (targets == null)
                    targets = CreateTargets();
                
                if (!targets.Add(target))
                    return;

                if (target is IList list)
                {
                    Value value;
                    int length = list.Count;
                    for (int i = 0; i < length; ++i)
                    {
                        value = new Value(new object[] {i}, target, type.GetProperty("Item", new[] { typeof(int) }), parent);
                        __Create(layerMaskInclude, layerMaskExclude, list[i], value, results, targets);
                    }

                    return;
                }

                //while (type != null)
                //{
                    var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    foreach (var field in fields)
                        Create(layerMaskInclude, layerMaskExclude, target, field, parent, results, targets);

                    if (GetLayerMask(type, out string memberName, out _))
                    {
                        var property = type.GetProperty(memberName,
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (property != null)
                        {
                            /*if (property.GetIndexParameters().Length > 0 || property.PropertyType != typeof(LayerMask))
                                continue;*/

                            Create(layerMaskInclude, layerMaskExclude, target, property, parent, results, targets);
                        }
                    }
                    //type = type.BaseType;
                //}
            }
        }

        private LayerMask __layerMaskInclude;
        private LayerMask __layerMaskExclude;
        private ReorderableList __reorderableList;

        [MenuItem("Window/ZG/LayerMask Editor")]
        public static void ShowWindow()
        {
            GetWindow<LayerMaskEditor>();
        }

        private static Dictionary<Type, (string, string)> __layerMaskMemberNames;

        public static bool GetLayerMask(Type type, out string includeMemberName, out string excludeMemberName)
        {
            if (__layerMaskMemberNames == null)
            {
                __layerMaskMemberNames = new Dictionary<Type, (string, string)>();
                foreach (var assembly in EditorHelper.loadedAssembliess)
                {
                    foreach (var attribute in assembly.GetCustomAttributes<LayerMaskAttribute>())
                        __layerMaskMemberNames[attribute.Type] = (attribute.IncludeMemberName, attribute.ExcludeMemberName);
                }
            }

            while (type != null)
            {
                if (__layerMaskMemberNames.TryGetValue(type, out var temp))
                {
                    includeMemberName = temp.Item1;
                    excludeMemberName = temp.Item2;

                    return true;
                }
                
                type = type.BaseType;
            }

            includeMemberName = null;
            excludeMemberName = null;
            
            return false;
        }

        public static LayerMask LayerMaskFieldLayout(LayerMask layers, GUIContent label, params GUILayoutOption[] options)
        {
            var method = typeof(EditorGUILayout).GetMethod(
                "LayerMaskField",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new []
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
        
        public static LayerMask LayerMaskField(in Rect position, LayerMask layers, GUIContent label)
        {
            var method = typeof(EditorGUI).GetMethod(
                "LayerMaskField",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new []
                {
                    typeof(Rect), 
                    typeof(LayerMask),
                    typeof(GUIContent)
                },
                null);
            return (LayerMask)method.Invoke(null, new object[]
            {
                position, 
                layers,
                label
            });
        }
        
        private static Rect __windowPosition;
        private static Vector2 __scrollPosition;
        private List<Component> __components;
        private LayerMask __layerMask;

        void OnGUI()
        {
            __windowPosition = position;
            
            __layerMaskInclude = LayerMaskFieldLayout(__layerMaskInclude, new GUIContent("Include"));
            __layerMaskExclude = LayerMaskFieldLayout(__layerMaskExclude, new GUIContent("Exclude"));
            if (GUILayout.Button("Refresh"))
            {
                if (__components != null)
                    __components.Clear();
                
                var targets = LayerMaskValue.CreateTargets();
                var values = new List<Value>();
                string[] guids = AssetDatabase.FindAssets("t:prefab");
                Component[] components;
                string path;
                GameObject gameObject;
                int numValues, numGuids = guids == null ? 0 : guids.Length;
                for (int i = 0; i < numGuids; ++i)
                {
                    if (EditorUtility.DisplayCancelableProgressBar(title, i.ToString() + "/" + numGuids, i * 1.0f / numGuids))
                        break;

                    path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                    components = gameObject.GetComponentsInChildren<Component>(true);
                    foreach (var component in components)
                    {
                        try
                        {
                            numValues = values.Count;
                            LayerMaskValue.Create(__layerMaskInclude, __layerMaskExclude, component, null, null, values.Add, targets);
                            if (values.Count > numValues)
                            {
                                if (__components == null)
                                    __components = new List<Component>();
                                
                                __components.Add(component);
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }

                EditorUtility.ClearProgressBar();
                
                __reorderableList = new ReorderableList(
                    values,
                    typeof(Value),
                    true,
                    false,
                    false,
                    false);
                __reorderableList.multiSelect = true;
                
                __reorderableList.onSelectCallback += x =>
                {
                    int index = 0;
                    var targets = new UnityEngine.Object[__reorderableList.selectedIndices.Count];
                    foreach (var selectedIndex in __reorderableList.selectedIndices)
                    {
                        if (values[selectedIndex].root.Target is UnityEngine.Object target)
                            targets[index++] = target;
                    }

                    Selection.objects = targets;
                };

                __reorderableList.drawElementCallback += (rect, index, active, focused) =>
                {
                    values[index].OnGUI(rect, active, focused);
                };

                __reorderableList.elementHeightCallback += index => values[index].guiHeight;
            }

            if (__reorderableList != null)
            {
                var rect = GUILayoutUtility.GetRect(0.0f, __reorderableList.headerHeight, GUILayout.ExpandWidth(true));

                rect.width /= 4.0f;

                if (GUI.Button(rect, "Select all"))
                {
                    LayerMaskValue.Toggles.Clear();
                    foreach (var value in __reorderableList.list)
                    {
                        if (value is LayerMaskValue layerMaskValue)
                            LayerMaskValue.Toggles.Add(layerMaskValue);
                    }
                }

                rect.x += rect.width;

                if (GUI.Button(rect, "Deselect all"))
                    LayerMaskValue.Toggles.Clear();

                rect.x += rect.width;

                if (GUI.Button(rect, "Apply To"))
                {
                    foreach (var toggle in LayerMaskValue.Toggles)
                        toggle.value = __layerMask;
                }

                rect.x += rect.width;

                __layerMask = LayerMaskField(rect, __layerMask, GUIContent.none);
            }

            __scrollPosition = EditorGUILayout.BeginScrollView(__scrollPosition);
            if (__reorderableList != null)
                __reorderableList.DoLayoutList();

            EditorGUILayout.EndScrollView();
        }
    }
}