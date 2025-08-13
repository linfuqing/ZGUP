using UnityEngine;
using UnityEditorInternal;

namespace ZG
{
    public static class EditorTools
    {
        public static void FilterTo<T>(GameObject context, Transform root) where T : Component
        {
            var components = context == null ? null : context.GetComponentsInChildren<T>(true);
            if (components == null || components.Length < 1)
                return;

            GameObject temp;
            Transform source, destination;
            foreach(var component in components)
            {
                if (component == null)
                    continue;

                temp = new GameObject(component.name);
                temp.layer = component.gameObject.layer;
                destination = temp.transform;
                if(destination != null)
                {
                    destination.SetParent(root);

                    source = component.transform;
                    if(source != null)
                    {
                        destination.position = source.position;
                        destination.rotation = source.rotation;
                        destination.localScale = source.lossyScale;
                    }
                }
                
                if (ComponentUtility.CopyComponent(component))
                    ComponentUtility.PasteComponentAsNew(temp);
            }
        }
    }
}