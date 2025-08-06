using System.Collections.Generic;
using UnityEngine;

namespace ZG
{
    public enum TouchFeedbackType
    {
        Light,
        Medium,
        Heavy,
        Selection
    }

    public interface IUISelectable
    {

    }

    public static partial class UnityUtility
    {
        public readonly static Collider[] colliders = new Collider[256];

#if UNITY_IOS
        [System.Runtime.InteropServices.DllImport("__Internal")]
        public extern static void TouchFeedback(TouchFeedbackType type);
#else
        public static void TouchFeedback(TouchFeedbackType type)
        {
        }
#endif

        public static bool IsUISelected()
        {
            GameObject selectedGameObject = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
            return selectedGameObject != null && (selectedGameObject.transform is RectTransform || selectedGameObject.GetComponent<IUISelectable>() != null);
        }

        public static bool IsInputFieldFocused()
        {
            return System.Array.Find(UnityEngine.UI.Selectable.allSelectablesArray, x => x is UnityEngine.UI.InputField && ((UnityEngine.UI.InputField)x).isFocused) != null;
        }

        public static bool IsActiveIn(this GameObject gameObject, Transform root)
        {
            if (gameObject == null)
                return false;

            if (gameObject.transform == root)
                return true;

            if (!gameObject.activeSelf)
                return false;

            var parent = gameObject.transform;
            parent = parent == null ? null : parent.parent;
            return IsActiveIn(parent == null ? null : parent.gameObject, root);
        }

        public static void SetLayer(this GameObject gameObject, int layer)
        {
            if (gameObject == null)
                return;

            gameObject.layer = layer;

            Transform transform = gameObject.transform;
            if (transform != null)
            {
                foreach (Transform child in transform)
                    SetLayer(child == null ? null : child.gameObject, layer);
            }
        }

        public static Bounds GetBounds(this GameObject target, int layerMask)
        {
            Bounds bounds = default(Bounds);
            Collider[] colliders = target == null ? null : target.GetComponentsInChildren<Collider>();
            if (colliders != null)
            {
                bool isFirst = true;
                GameObject temp;
                foreach (Collider collider in colliders)
                {
                    temp = collider == null ? null : collider.gameObject;
                    if (temp == null || ((1 << temp.layer) & layerMask) == 0)
                        continue;

                    if (isFirst)
                    {
                        isFirst = false;

                        bounds = collider.bounds;
                    }
                    else
                        bounds.Encapsulate(collider.bounds);
                }
            }

            return bounds;
        }

        public static int IndexOfMinDistance(
            this IEnumerable<Collider> colliders,
            int layerMask,
            ref Vector3 position,
            ref float minDistance)
        {
            if (colliders == null)
                return -1;

            int index = -1, count = 0;
            float distance;
            Vector3 source = position, destination;
            GameObject gameObject;
            foreach (Collider collider in colliders)
            {
                ++count;

                gameObject = collider == null ? null : collider.gameObject;
                if (gameObject == null || (layerMask & (1 << gameObject.layer)) == 0)
                    continue;

                destination = collider.ClosestPoint(position);

                distance = (source - destination).sqrMagnitude;
                if (distance < minDistance)
                {
                    minDistance = distance;

                    position = destination;

                    index = count - 1;
                }
            }

            if (index != -1)
                minDistance = Mathf.Sqrt(minDistance);

            return index;
        }
    }
}