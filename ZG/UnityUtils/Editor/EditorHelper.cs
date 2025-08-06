using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO.Compression;

namespace ZG
{
    public static class EditorHelper
    {
        public static IEnumerable<Type> loadedTypes
        {
            get
            {
                Assembly assembly = Assembly.Load("UnityEditor");
                if (assembly != null)
                {
                    Type editorAssemblies = assembly.GetType("UnityEditor.EditorAssemblies");
                    if (editorAssemblies != null)
                    {
                        MethodInfo loadedTypes = editorAssemblies.GetMethod("get_loadedTypes", BindingFlags.NonPublic | BindingFlags.Static);
                        IEnumerable<Type> types = loadedTypes.Invoke(null, null) as IEnumerable<Type>;

                        return types;
                    }
                }

                return null;
            }
        }

        public static IEnumerable<SerializedProperty> GetSiblings(this SerializedProperty property, int level)
        {
            if (property == null || level < 1)
                yield break;

            SerializedObject serializedObject = property.serializedObject;
            if (serializedObject == null)
                yield break;

            string propertyPath = property.propertyPath;
            if (propertyPath == null)
                yield break;

            Match match = Regex.Match(propertyPath, @".Array\.data\[([0-9]+)\]", RegexOptions.RightToLeft);
            if (match == Match.Empty)
                yield break;

            int matchIndex = match.Index;
            SerializedProperty parent = serializedObject.FindProperty(propertyPath.Remove(matchIndex));
            int arraySize = parent == null ? 0 : parent.isArray ? parent.arraySize : 0;
            if (arraySize < 1)
                yield break;

            StringBuilder stringBuilder = new StringBuilder(propertyPath);
            Group group = match.Groups[1];
            int index = int.Parse(group.Value), startIndex = group.Index, count = group.Length, i;
            for (i = 0; i < arraySize; ++i)
            {
                if (i == index)
                    continue;

                stringBuilder = stringBuilder.Remove(startIndex, count);

                count = stringBuilder.Length;
                stringBuilder = stringBuilder.Insert(startIndex, i);
                count = stringBuilder.Length - count;

                yield return serializedObject.FindProperty(stringBuilder.ToString());
            }

            foreach (SerializedProperty temp in parent.GetSiblings(level - 1))
            {
                arraySize = temp == null ? 0 : temp.isArray ? temp.arraySize : 0;
                if (arraySize > 0)
                {
                    stringBuilder.Remove(0, matchIndex);
                    startIndex -= matchIndex;

                    propertyPath = temp.propertyPath;
                    stringBuilder = stringBuilder.Insert(0, propertyPath);
                    matchIndex = propertyPath == null ? 0 : propertyPath.Length;
                    startIndex += matchIndex;
                    for (i = 0; i < arraySize; ++i)
                    {
                        stringBuilder = stringBuilder.Remove(startIndex, count);

                        count = stringBuilder.Length;
                        stringBuilder = stringBuilder.Insert(startIndex, i);
                        count = stringBuilder.Length - count;

                        yield return serializedObject.FindProperty(stringBuilder.ToString());
                    }
                }
            }
        }

        public static SerializedProperty GetParent(this SerializedProperty property)
        {
            SerializedObject serializedObject = property == null ? null : property.serializedObject;
            if (serializedObject == null)
                return null;

            string path = GetParentPath(property.propertyPath);

            return serializedObject.FindProperty(path);
        }

        public static string GetParentPath(string propertyPath)
        {
            return Regex.Replace(propertyPath, @".((\w+\d*)|(Array\.data\[[\d]+\]))$", "");
        }

        public static string GetPropertyPath(string path)
        {
            return Regex.Replace(path, @".Array\.data(\[\d+\])", "$1");
        }

        public static void HelpBox(Rect position, GUIContent label, string message, MessageType type)
        {
            float width = position.width;
            position.width = EditorGUIUtility.labelWidth;
            EditorGUI.PrefixLabel(position, label);
            position.x += position.width;
            position.width = width - position.width;
            EditorGUI.HelpBox(position, message, MessageType.Error);
        }

        public static void CreateFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string directoryName = Path.GetDirectoryName(path);
            if (!AssetDatabase.IsValidFolder(directoryName))
                CreateFolder(directoryName);

            AssetDatabase.CreateFolder(directoryName, Path.GetFileName(path));
        }

        public static void CreateAsset(UnityEngine.Object asset)
        {
            if (asset == null)
                return;

            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (path == "")
                path = "Assets";
            else if (Path.GetExtension(path) != "")
                path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");

            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/" + asset.name + ".asset");

            AssetDatabase.CreateAsset(asset, assetPathAndName);

            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }

        public static T CreateAsset<T>(string assetName) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            if (asset != null)
            {
                asset.name = assetName;

                CreateAsset(asset);
            }

            return asset;
        }

        public static void UpdatePrefabs(string title, Predicate<GameObject> predicate)
        {
            string[] guids = AssetDatabase.FindAssets("t:prefab");
            string path;
            GameObject gameObject;
            int numGuids = guids == null ? 0 : guids.Length;
            for (int i = 0; i < numGuids; ++i)
            {
                if (EditorUtility.DisplayCancelableProgressBar(title, i.ToString() + "/" + numGuids, i * 1.0f / numGuids))
                    break;

                path = AssetDatabase.GUIDToAssetPath(guids[i]);
                gameObject = PrefabUtility.LoadPrefabContents(path);

                try
                {
                    if (predicate(gameObject))
                        PrefabUtility.SaveAsPrefabAsset(gameObject, path);
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                }

                PrefabUtility.UnloadPrefabContents(gameObject);
            }

            EditorUtility.ClearProgressBar();
        }

        public static int Visit(this IEnumerable<GameObject> gameObjects, Predicate<GameObject> predicate)
        {
            if (gameObjects == null)
                return -1;

            bool isPrefab;
            int count = 0;
            GameObject instance;
            foreach (GameObject gameObject in gameObjects)
            {
                ++count;

                if (gameObject == null)
                    continue;

                isPrefab = PrefabUtility.GetPrefabAssetType(gameObject) != PrefabAssetType.NotAPrefab;

                instance = isPrefab ? PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(gameObject)) : gameObject;
                try
                {
                    if (predicate(instance))
                    {
                        if (isPrefab)
                            PrefabUtility.UnloadPrefabContents(instance);

                        return count - 1;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                }

                if (isPrefab)
                    PrefabUtility.UnloadPrefabContents(instance);
            }

            return -1;
        }

        public static int Visit(this System.Collections.IEnumerable elements, Predicate<GameObject> predicate)
        {
            if (elements == null)
                return -1;

            bool isPrefab;
            int count = 0;
            UnityEngine.Object target;
            GameObject instance;
            Component component;
            foreach (object element in elements)
            {
                ++count;

                target = element as UnityEngine.Object;
                if (target == null)
                    continue;

                isPrefab = PrefabUtility.GetPrefabAssetType(target) != PrefabAssetType.NotAPrefab;
                if (isPrefab)
                    instance = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(target));
                else
                {
                    instance = target as GameObject;
                    if (instance == null)
                    {
                        component = target as Component;
                        instance = component == null ? null : component.gameObject;
                    }
                }

                if (instance == null)
                    continue;

                try
                {
                    if (predicate(instance))
                    {
                        if (isPrefab)
                            PrefabUtility.UnloadPrefabContents(instance);

                        return count - 1;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                }

                if (isPrefab)
                    PrefabUtility.UnloadPrefabContents(instance);
            }

            return -1;
        }

        public static int Visit(this System.Collections.IEnumerable elements, Predicate<UnityEngine.Object> predicate, Type type)
        {
            if (elements == null)
                return -1;

            bool isPrefab;
            int count = 0;
            Type temp;
            UnityEngine.Object target;
            GameObject instance;
            foreach (object element in elements)
            {
                ++count;

                target = element as UnityEngine.Object;
                if (target == null)
                    continue;

                isPrefab = PrefabUtility.GetPrefabAssetType(target) != PrefabAssetType.NotAPrefab;
                if (isPrefab)
                {
                    instance = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(target));

                    target = instance.GetComponent(type);
                }
                else
                {
                    instance = null;

                    temp = target.GetType();
                    if (temp == type || temp.IsSubclassOf(type))
                        target = target as Component;
                    else
                    {
                        target = target as Component;
                        if (target == null)
                        {
                            instance = target as GameObject;
                            target = instance == null ? null : instance.GetComponent(type);
                        }
                    }
                }

                if (target == null)
                    continue;

                try
                {
                    if (predicate(target))
                    {
                        if (isPrefab)
                            PrefabUtility.UnloadPrefabContents(instance);

                        return count - 1;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                }

                if (isPrefab)
                    PrefabUtility.UnloadPrefabContents(instance);
            }

            return -1;
        }


        [MenuItem("Assets/ZG/Replace Scene Selection with Prefab Asset")]
        public static void ReplaceSelectedWithPrefabInstance(MenuCommand command)
        {
            GameObject prefabAsset = null;
            var listOfInstanceRoots = new List<GameObject>();
            var listOfPlainGameObjects = new List<GameObject>();
            foreach (var go in Selection.gameObjects)
            {
                if (AssetDatabase.Contains(go))
                    prefabAsset = go;
                else if (PrefabUtility.IsOutermostPrefabInstanceRoot(go))
                    listOfInstanceRoots.Add(go);
                else if (!PrefabUtility.IsPartOfNonAssetPrefabInstance(go))
                    listOfPlainGameObjects.Add(go);
            }

            if (prefabAsset == null || (listOfInstanceRoots.Count == 0 && listOfPlainGameObjects.Count == 0))
            {
                var helptext =
                    "Please make a multiselection with at least one Prefab instance root or plain GameObject in the Scene and one Prefab Asset from the Project Browser. \n\nUse Ctrl/Cmd + Click.";
                EditorUtility.DisplayDialog("Replace Prefab Asset of Prefab instance",
                    (prefabAsset == null ? "Prefab Asset missing.\n\n" : "Prefab instance missing.\n\n") + helptext,
                    "OK");
                return;
            }

            if (listOfInstanceRoots.Count > 0)
            {
                var settings = new PrefabReplacingSettings
                {
                    logInfo = true,
                    objectMatchMode = ObjectMatchMode.ByHierarchy,
                    prefabOverridesOptions = PrefabOverridesOptions.ClearAllNonDefaultOverrides
                };
                PrefabUtility.ReplacePrefabAssetOfPrefabInstances(listOfInstanceRoots.ToArray(), prefabAsset, settings, InteractionMode.UserAction);
            }

            if (listOfPlainGameObjects.Count > 0)
            {
                var settings = new ConvertToPrefabInstanceSettings
                {
                    logInfo = true,
                    objectMatchMode = ObjectMatchMode.ByHierarchy,
                };
                PrefabUtility.ConvertToPrefabInstances(listOfPlainGameObjects.ToArray(), prefabAsset, settings, InteractionMode.UserAction);
            }
        }
        
        [MenuItem("GameObject/ZG/Print Dependencies")]
        public static void PrintDependencies(MenuCommand menuCommand)
        {
            var target = menuCommand?.context;
            if (target == null)
                return;

            int i, numTargets;
            UnityEngine.Object[] targets;
            var gameObject = target as GameObject;
            if (gameObject == null)
            {
                targets = new UnityEngine.Object[1];
                targets[0] = target;

                numTargets = 1;
            }
            else
            {
                var components = gameObject.GetComponents<Component>();
                numTargets = components.Length;
                targets = new UnityEngine.Object[numTargets];
                for (i = 0; i < numTargets; ++i)
                    targets[i] = components[i];
            }

            int j, k, numRootGameObjects, sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount;
            UnityEngine.SceneManagement.Scene scene;
            Component component;
            GameObject rootGameObject;
            GameObject[] rootGameObjects;
            IEnumerable<KeyValuePair<Component, KeyValuePair<object, FieldInfo>>> results;
            HashSet<object> temps = new HashSet<object>(new ReflectionHelper.EqualityComparer());
            for (i = 0; i < numTargets; ++i)
            {
                temps.Clear();

                target = targets[i];
                for (j = 0; j < sceneCount; ++j)
                {
                    scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(j);
                    rootGameObjects = scene.GetRootGameObjects();
                    numRootGameObjects = rootGameObjects == null ? 0 : rootGameObjects.Length;

                    for (k = 0; k < numRootGameObjects; ++k)
                    {
                        rootGameObject = rootGameObjects[k];
                        if (EditorUtility.DisplayCancelableProgressBar(
                            target.GetType().Name,
                            $"{scene.name}.{rootGameObject.name}",
                            (numRootGameObjects * sceneCount * i + numRootGameObjects * j + k) * 1.0f / (numTargets * sceneCount * numRootGameObjects)))
                            break;

                        results = target.GetDependencies(rootGameObject, true, temps);

                        if (results != null)
                        {
                            foreach (var result in results)
                            {
                                component = result.Key;
                                if (component == null)
                                    continue;

                                Debug.Log($"{target.GetType().Name} Depend On {result.Value.Key}, By {result.Value.Value}, Component Type: {component.GetType()}", component);
                            }
                        }
                    }

                    if (k < numRootGameObjects)
                        break;
                }

                if (j < sceneCount)
                    break;
            }

            EditorUtility.ClearProgressBar();
        }

        [MenuItem("Assets/ZG/GZip")]
        public static void GZip(MenuCommand command)
        {
            var target = command.context == null ? Selection.activeObject : command.context;
            if (target == null)
                return;

            var dataPath = Application.dataPath;
            var path = Path.Combine(dataPath.Remove(dataPath.Length - 7), AssetDatabase.GetAssetPath(target));
            using (var fileStreamToWrite = File.OpenWrite(path + ".gz"))
            {
                using (var fileStreamToRead = File.OpenRead(path))
                {
                    using (var gzipStream = new GZipStream(fileStreamToWrite, CompressionMode.Compress))
                        fileStreamToRead.CopyTo(gzipStream);
                }
            }

            AssetDatabase.Refresh();
        }
    }
}