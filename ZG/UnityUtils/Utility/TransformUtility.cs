using UnityEngine;

namespace ZG
{
    public static class TransformUtility
    {
        public static Matrix4x4 GetMatrixOf(this Transform transform, Transform root)
        {
            if (transform == root)
                return Matrix4x4.identity;

            Matrix4x4 matrix = Matrix4x4.TRS(transform.localPosition, transform.localRotation, transform.localScale);
            Transform parent = transform.parent;
            if (parent == null || parent == root)
                return matrix;

            return GetMatrixOf(parent, root) * matrix;
        }

        public static Quaternion GetRotationOf(this Transform transform, Transform root)
        {
            if (transform == root)
                return Quaternion.identity;

            Quaternion rotation = transform.localRotation;
            Transform parent = transform.parent;
            if (parent == null || parent == root)
                return rotation;

            return GetRotationOf(parent, root) * rotation;
        }

        public static Vector3 GetPositionOf(this Transform transform, Transform root)
        {
            if (transform == root)
                return Vector3.zero;

            Vector3 position = transform.localPosition;
            Transform parent = transform.parent;
            if (parent == null)
                return position;
            
            return GetMatrixOf(parent, root).MultiplyPoint3x4(position);
        }

        public static Plane InverseTransform(this Transform transform, Plane plane)
        {
            return new Plane(
                transform.InverseTransformDirection(plane.normal),
                transform.InverseTransformPoint(plane.normal * -plane.distance));
        }

        public static bool ContainsInParent(this Transform transform, Transform target)
        {
            if (transform == target)
                return true;

            if (transform == null)
                return false;

            return ContainsInParent(transform.parent, target);
        }

        public static T GetComponentInParentGreedy<T>(this Transform transform)
        {
            if (transform == null)
                return default;

            T component = GetComponentInParentGreedy<T>(transform.parent);
            bool result = component is UnityEngine.Object ? (component as UnityEngine.Object) == null : component == null;

            return result ? transform.GetComponent<T>() : component;
        }

        public static T GetComponentInParent<T>(this Transform transform, bool isIncludeInactive)
        {
            if (transform == null)
                return default;

            if (isIncludeInactive)
            {
                T component = transform.GetComponent<T>();
                bool isNull;
                if (component is UnityEngine.Object)
                    isNull = (component as UnityEngine.Object) == null;
                else
                    isNull = component == null;

                return isNull ? GetComponentInParent<T>(transform.parent, true) : component;
            }

            return transform.GetComponentInParent<T>();
        }

        public static void GetComponentsInChildren<T>(this Transform transform, bool isIncludeInactive, System.Action<T> handler, params System.Type[] maskTypes)
        {
            if (!isIncludeInactive && !transform.gameObject.activeSelf)
                return;

            T component = transform.GetComponent<T>();
            if(component is UnityEngine.Object ? (component as UnityEngine.Object) != null : component != null)
                handler(component);

            bool isMask;
            foreach (Transform child in transform)
            {
                isMask = false;
                foreach (var maskType in maskTypes)
                {
                    if (child.GetComponent(maskType) != null)
                    {
                        isMask = true;

                        break;
                    }
                }

                if (isMask)
                    continue;

                GetComponentsInChildren(child, isIncludeInactive, handler, maskTypes);
            }
        }

        public static void GetComponentsInChildren<T, U>(this T instance, System.Action<U> handler, bool isIncludeInactive = false) where T : Component
        {
            GetComponentsInChildren(instance.transform, isIncludeInactive, handler, typeof(T));
        }

        public static int GetSiblingIndex(this Transform transform, bool isIncludeInactive)
        {
            if (transform == null)
                return -1;

            if (isIncludeInactive)
                return transform.GetSiblingIndex();

            Transform parent = transform.parent;
            if (parent == null)
                return -1;

            int index = 0;
            GameObject gameObject;
            foreach (Transform child in parent)
            {
                if (child == transform)
                    break;

                gameObject = child == null ? null : child.gameObject;
                if (gameObject != null && gameObject.activeSelf)
                    ++index;
            }

            return index;
        }

        public static int GetLeafCount(this Transform transform)
        {
            if (transform == null)
                return 0;

            if (transform.childCount < 1)
                return 1;

            int count = 0;
            foreach (Transform child in transform)
                count += GetLeafCount(child);

            return count;
        }

        public static int GetNodeCount(this Transform transform)
        {
            if (transform == null)
                return 0;
            
            int count = 1;
            foreach (Transform child in transform)
                count += GetNodeCount(child);

            return count;
        }

        public static bool FindNode(this Transform root, Transform transform, out int index)
        {
            index = 0;
            if (root == transform)
                return true;

            ++index;

            bool result;
            int temp;
            foreach(Transform child in root)
            {
                result = FindNode(child, transform, out temp);
                index += temp;
                if (result)
                    return true;
            }

            return false;
        }

        public static int GetChildIndex(this Transform root, Transform child)
        {
            Transform parent;
            while (child != null)
            {
                parent = child.parent;
                if (parent == root)
                    break;

                child = parent;
            }

            if (child == null)
                return -1;

            return child.GetSiblingIndex();
        }

        public static int GetLeafIndex(this Transform root, Transform child)
        {
            if (root == null || root == child || child == null)
                return -1;

            int index = 0, siblingIndex, i;
            Transform parent;
            while (true)
            {
                parent = child.parent;
                if (parent == null)
                    return -1;

                siblingIndex = child.GetSiblingIndex();
                for (i = 0; i < siblingIndex; ++i)
                    index += GetLeafCount(parent.GetChild(i));

                if (parent == root)
                    break;

                child = parent;
            }

            return index;
        }

        public static Transform GetLeaf(this Transform transform, bool isIncludeInactive, ref int index)
        {
            if (transform == null || index < 0)
                return null;

            if (!isIncludeInactive && !transform.gameObject.activeSelf)
                return null;

            if (transform.childCount < 1)
            {
                if (index < 1)
                    return transform;

                --index;

                return null;
            }

            Transform result;
            foreach (Transform child in transform)
            {
                result = GetLeaf(child, isIncludeInactive, ref index);
                if (result != null)
                    return result;
            }

            return null;
        }

        public static Transform GetLeaf(this Transform transform, bool isIncludeInactive, int index)
        {
            return GetLeaf(transform, isIncludeInactive, ref index);
        }

        public static string GetPath(this Transform transform, Transform root)
        {
            System.Text.StringBuilder stringBuilder = null;
            while (transform != null)
            {
                if (transform == root)
                    break;

                if (stringBuilder == null)
                    stringBuilder = new System.Text.StringBuilder(transform.name);
                else
                {
                    stringBuilder.Insert(0, '/');
                    stringBuilder.Insert(0, transform.name);
                }

                transform = transform.parent;
            }

            return stringBuilder == null ? null : stringBuilder.ToString();
        }

        public static void Visit(this Transform transform, System.Predicate<Transform> predicate)
        {
            foreach(Transform child in transform)
            {
                if (!predicate(child))
                    continue;

                Visit(child, predicate);
            }
        }
    }
}