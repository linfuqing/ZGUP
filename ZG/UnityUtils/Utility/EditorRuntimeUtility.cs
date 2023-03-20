using System;
using UnityEngine;

namespace ZG
{
#if UNITY_EDITOR
    using UnityEditor;

    public static class EditorRuntimeUtility
    {
        public static void DelayModify<T>(T value, Action<T> callback) where T : Component
        {
            if (Application.isPlaying)
                return;

            string assetPath = null;
            bool isPrefab = PrefabUtility.GetPrefabAssetType(value) != UnityEditor.PrefabAssetType.NotAPrefab;
            if (isPrefab)
            {
                assetPath = AssetDatabase.GetAssetPath(value);
                value = (T)PrefabUtility.InstantiateAttachedAsset(value);
            }

            EditorApplication.CallbackFunction delayCall = null;
            delayCall = () =>
            {
                EditorApplication.delayCall -= delayCall;

                if (PrefabUtility.GetPrefabInstanceStatus(value) != UnityEditor.PrefabInstanceStatus.NotAPrefab)
                {
                    var target = PrefabUtility.GetCorrespondingObjectFromSourceAtPath(value, assetPath);

                    target = (T)PrefabUtility.InstantiatePrefab(target);

                    callback(target);

                    PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                    //UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, assetPath);

                    var root = target.transform.root.gameObject;

                    PrefabUtility.ApplyPrefabInstance(root, InteractionMode.AutomatedAction);

                    UnityEngine.Object.DestroyImmediate(root);
                }

                if (isPrefab)
                    UnityEngine.Object.DestroyImmediate(value.transform.root.gameObject);
            };

            EditorApplication.delayCall += delayCall;
        }
    }
#endif
}