using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PrefabEditor : EditorWindow
{
    public const string NAME_SPACE_ASSET_ROOT = "ZGPrefabEditor";
    
    [MenuItem("Window/ZG/PrefabEditor")]
    public static void Show()
    {
        GetWindow<PrefabEditor>();
    }

    void OnGUI()
    {
        var prefabAssetGUID = EditorPrefs.GetString(NAME_SPACE_ASSET_ROOT);
        var prefabAssetPath = AssetDatabase.GUIDToAssetPath(prefabAssetGUID);
        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabAssetPath);
        EditorGUI.BeginChangeCheck();
        prefabAsset = EditorGUILayout.ObjectField("Prefab Asset Parent", prefabAsset, typeof(GameObject), false) as GameObject;
        if (EditorGUI.EndChangeCheck())
        {
            prefabAssetPath = prefabAsset == null ? null : AssetDatabase.GetAssetPath(prefabAsset);
            prefabAssetGUID = string.IsNullOrEmpty(prefabAssetPath) ? null : AssetDatabase.AssetPathToGUID(prefabAssetPath);
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
                    
                    instanceRoots[i] = (GameObject)PrefabUtility.InstantiatePrefab(instanceRoot);
                }

                var settings = new PrefabReplacingSettings
                {
                    logInfo = true,
                    objectMatchMode = ObjectMatchMode.ByHierarchy,
                    prefabOverridesOptions = PrefabOverridesOptions.KeepAllPossibleOverrides
                };
                PrefabUtility.ReplacePrefabAssetOfPrefabInstances(instanceRoots.ToArray(), prefabAsset, settings,
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
