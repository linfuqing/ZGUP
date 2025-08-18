using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ZG
{
    public class PrefabEditor : EditorWindow
    {
        public const string NAME_SPACE_ASSET_ROOT = "ZGPrefabEditor";

        [MenuItem("Window/ZG/Prefab Editor")]
        public static void ShowWindow()
        {
            GetWindow<PrefabEditor>();
        }

        void OnGUI()
        {
            var prefabAssetGUID = EditorPrefs.GetString(NAME_SPACE_ASSET_ROOT);
            var prefabAssetPath = AssetDatabase.GUIDToAssetPath(prefabAssetGUID);
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabAssetPath);
            EditorGUI.BeginChangeCheck();
            prefabAsset =
                EditorGUILayout.ObjectField("Prefab Asset Parent", prefabAsset, typeof(GameObject),
                    false) as GameObject;
            if (EditorGUI.EndChangeCheck())
            {
                prefabAssetPath = prefabAsset == null ? null : AssetDatabase.GetAssetPath(prefabAsset);
                prefabAssetGUID = string.IsNullOrEmpty(prefabAssetPath)
                    ? null
                    : AssetDatabase.AssetPathToGUID(prefabAssetPath);
                if (string.IsNullOrEmpty(prefabAssetGUID))
                    EditorPrefs.DeleteKey(NAME_SPACE_ASSET_ROOT);
                else
                    EditorPrefs.SetString(NAME_SPACE_ASSET_ROOT, prefabAssetGUID);
            }

            if (prefabAsset != null)
            {
                var instanceRoots = new List<GameObject>();
                foreach (var go in Selection.gameObjects)
                {
                    if (AssetDatabase.Contains(go))
                        instanceRoots.Add(go);
                }

                int numInstanceRoots = instanceRoots.Count;
                if (numInstanceRoots > 0 && GUILayout.Button("Replace Prefab Asset Parent"))
                {
                    GameObject instanceRoot;
                    var paths = new string[numInstanceRoots];
                    for (int i = 0; i < numInstanceRoots; ++i)
                    {
                        instanceRoot = instanceRoots[i];
                        paths[i] = AssetDatabase.GetAssetPath(instanceRoot);

                        instanceRoot = (GameObject)PrefabUtility.InstantiatePrefab(instanceRoot);

                        PrefabUtility.UnpackPrefabInstance(instanceRoot, PrefabUnpackMode.Completely,
                            InteractionMode.AutomatedAction);

                        instanceRoots[i] = instanceRoot;
                    }

                    var settings = new ConvertToPrefabInstanceSettings()
                    {
                        logInfo = true,
                        objectMatchMode = ObjectMatchMode.ByHierarchy,
                        componentsNotMatchedBecomesOverride = true,
                        gameObjectsNotMatchedBecomesOverride = true,
                        recordPropertyOverridesOfMatches = true,
                        changeRootNameToAssetName = false
                    };
                    PrefabUtility.ConvertToPrefabInstances(instanceRoots.ToArray(), prefabAsset, settings,
                        InteractionMode.AutomatedAction);

                    for (int i = 0; i < numInstanceRoots; ++i)
                    {
                        instanceRoot = instanceRoots[i];

                        PrefabUtility.SaveAsPrefabAsset(instanceRoot, paths[i]);

                        DestroyImmediate(instanceRoot);
                    }
                }
            }
        }
    }
}