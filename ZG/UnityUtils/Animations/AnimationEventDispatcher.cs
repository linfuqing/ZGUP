using UnityEngine;
using UnityEngine.Events;

namespace ZG
{
    public class AnimationEventDispatcher : MonoBehaviour
    {
        [System.Serializable]
        public struct Event
        {
#if UNITY_EDITOR
            public string name;
#endif
            public UnityEvent value;

            public void Invoke()
            {
                if (value != null)
                    value.Invoke();
            }
        }

        public Event[] events;

        public void OnAnimationEvent(AnimationEvent animationEvent)
        {
            events[animationEvent.intParameter].value.Invoke();
        }
    }
}