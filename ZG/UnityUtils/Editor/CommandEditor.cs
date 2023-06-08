using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;

namespace ZG
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandEditorAttribute : Attribute
    {
        public string menu;
        public int priority;

        public CommandEditorAttribute(string menu, int priority)
        {
            this.menu = menu;
            this.priority = priority;
        }
    }

    public class CommandEditor : EditorWindow
    {
        private struct Method : IComparable<Method>
        {
            public int priority;
            public MethodInfo instance;

            public int CompareTo(Method other)
            {
                return priority.CompareTo(other.priority);
            }
        }

        private Dictionary<MethodInfo, (int, bool)> __methodInfos; 

        [MenuItem("Window/ZG/Command Editor")]
        public static void ShowWindow()
        {
            GetWindow<CommandEditor>();
        }

        void OnGUI()
        {
            if (__methodInfos == null)
            {
                __methodInfos = new Dictionary<MethodInfo, (int, bool)>();

                CommandEditorAttribute attribute;
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (Type t in asm.GetTypes())
                    {
                        foreach(var methodInfo in t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                        {
                            attribute = methodInfo.GetCustomAttribute<CommandEditorAttribute>();
                            if (attribute == null)
                                continue;

                            __methodInfos.Add(methodInfo, (attribute.priority, false));
                        }
                    }
                }
            }

            bool value;
            Method method;
            List<Method> methods = null;
            var methodInfos = __methodInfos.ToArray();
            foreach(var pair in methodInfos)
            {
                value = pair.Value.Item2;
                if (EditorGUILayout.Toggle(pair.Key.Name, value) != value)
                {
                    value = !value;
                    __methodInfos[pair.Key] = (pair.Value.Item1, value);
                }

                if(value)
                {
                    method.priority = pair.Value.Item1;
                    method.instance = pair.Key;

                    if (methods == null)
                        methods = new List<Method>();

                    methods.Add(method);
                }
            }

            if (methods != null)
            {
                if (GUILayout.Button("Execute"))
                {
                    foreach (var methodToInvoke in methods)
                        methodToInvoke.instance.Invoke(null, null);
                }
            }
        }

    }
}