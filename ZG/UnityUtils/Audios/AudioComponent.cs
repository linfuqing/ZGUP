using System;
using UnityEngine;

namespace ZG
{
    public class AudioComponent : MonoBehaviour
    {
        [Serializable]
        public struct Item
        {
            public bool isForce;
            public int styleIndex;
            [Index("database.items", pathLevel = 2, uniqueLevel = 1)]
            public int itemIndex;
            public float weight;
        }

        public AudioDatabase database;
        public Item[] items;

        private AudioMain __main;

        public AudioMain main
        {
            get
            {
                if (__main == null)
                {
                    __main = GetComponent<AudioMain>();
                    if (__main == null)
                    {
                        GameObject gameObject = base.gameObject;
                        if (gameObject != null)
                            __main = gameObject.AddComponent<AudioMain>();
                    }
                }

                return __main;
            }
        }

        public void TriggerAudio(AnimationEvent animationEvent)
        {
            if (animationEvent == null || animationEvent.isFiredByAnimator)
                return;

            Play(animationEvent.intParameter, animationEvent.animatorClipInfo.weight);
        }

        public void Trigger(AnimationEvent animationEvent)
        {
            if (animationEvent == null || animationEvent.isFiredByAnimator)
                return;

            Play(animationEvent.intParameter, animationEvent.animatorClipInfo.weight);
        }

        public void Play(int index, float weight)
        {
            if (index < 0)
                return;

            int numItems = items == null ? 0 : items.Length;
            if (numItems < index)
                return;

            ref readonly var item = ref items[index];
            if (item.weight > weight)
                return;

            AudioMain main = this.main;
            if (main == null)
                return;

            main.Play(item.isForce, item.styleIndex, item.itemIndex, database);
        }

        public void Play(int index) => Play(index, 1.0f);
    }
}