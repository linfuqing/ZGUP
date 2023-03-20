using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ZG
{
    public class GraphicRaycasterEx : GraphicRaycaster
    {
        private Dictionary<int, int> __counts;

        private static HashSet<GraphicRaycasterEx> __instances;

        public static bool IsHit(int pointerId)
        {
            if (__instances == null)
                return false;

            int count;
            foreach (GraphicRaycasterEx instance in __instances)
            {
                if (instance != null && instance.__counts != null && instance.__counts.TryGetValue(pointerId, out count) && count > 0)
                    return true;
            }

            return false;
        }

        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            int count = resultAppendList == null ? 0 : resultAppendList.Count;
            base.Raycast(eventData, resultAppendList);
            count = (resultAppendList == null ? 0 : resultAppendList.Count) - count;

            if (__counts == null)
                __counts = new Dictionary<int, int>();

            __counts[eventData == null ? 0 : eventData.pointerId] = count;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (__instances == null)
                __instances = new HashSet<GraphicRaycasterEx>();

            __instances.Add(this);
        }

        protected override void OnDisable()
        {
            if (__instances != null)
                __instances.Remove(this);

            base.OnDisable();
        }
    }
}