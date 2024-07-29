using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ZG
{
    public class AudioMain : MonoBehaviour, IAudioMain
    {
        public System.Action<AudioSource> onInit;

        [Tooltip("如果当前没有空闲组件则忽略播放。")]
        public bool isFixed;
        public int level;
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("styles")]
        internal AudioSource[] _styles = null;
        private Dictionary<int, List<AudioSource>> __audioSources;

        public bool Play(bool isForce, int styleIndex, float volumeScale, double time, AudioClip clip)
        {
            AudioSource style = _styles[styleIndex];
            List<AudioSource> audioSources;

            if (__audioSources == null)
                __audioSources = new Dictionary<int, List<AudioSource>>();

            if (!__audioSources.TryGetValue(styleIndex, out audioSources) || audioSources == null)
            {
                audioSources = new List<AudioSource>();

                __audioSources[styleIndex] = audioSources;

                if (style == null)
                    GetComponents(audioSources);
            }

            AudioSource audioSource = null;
            if (__audioSources != null)
            {
                foreach (AudioSource temp in audioSources)
                {
                    if (temp == null)
                        continue;

                    if (temp.isPlaying)
                    {
                        if (!isForce && temp.clip == clip)
                            return false;

                        continue;
                    }

                    audioSource = temp;

                    break;
                }
            }

            if (audioSource == null)
            {
                if (isFixed)
                    return false;

                if (style == null)
                {
                    GameObject gameObject = base.gameObject;
                    if (gameObject != null)
                        audioSource = gameObject.AddComponent<AudioSource>();
                }
                else
                {
                    Transform transform = style.transform;

                    audioSource = Instantiate(style, transform == null ? null : transform.parent);

                    GameObject gameObject = audioSource == null ? null : audioSource.gameObject;
                    if (gameObject != null)
                        gameObject.SetActive(true);
                }

                if (audioSource != null)
                {
                    if (onInit != null)
                        onInit(audioSource);

                    audioSources.Add(audioSource);
                }
            }

            if (audioSource == null)
                return false;

            audioSource.loop = false;

            if (time > double.Epsilon)
            {
                audioSource.clip = clip;

                audioSource.PlayScheduled(time);
            }
            else
                audioSource.PlayOneShot(clip, volumeScale);

            return true;
        }

        public bool Play(bool isForce, int styleIndex, int itemIndex, float volumeScale, double time, AudioDatabase database)
        {
            if(database == null)
                Debug.LogError("Error Database", this);
            
            ref var item = ref database.items[itemIndex];

            int level = this.level < 0 ?
                Random.Range(0, item.clips.Length) :
#if DEBUG
                this.level;
#else
                Mathf.Min(this.level, item.clips.Length);
#endif

#if DEBUG
            if (item.clips == null || item.clips.Length <= level)
                Debug.LogError($"{name} Missing Clip Level {level}", this);
#endif

            var wrapperType = System.Type.GetType(item.wrapperType);
            if(wrapperType == null)
                return Play(isForce, styleIndex, volumeScale, time, item.clips[level]);

            var wrapper = System.Activator.CreateInstance(wrapperType) as IAudioWrapper;
            if(wrapper == null)
                return Play(isForce, styleIndex, volumeScale, time, item.clips[level]);
            
            return wrapper.Play(isForce, styleIndex, volumeScale, time, item.clips[level], this);
        }

        public bool Play(bool isForce, int styleIndex, int itemIndex, double time, AudioDatabase database)
        {
            return Play(isForce, styleIndex, itemIndex, 1.0f, time, database);
        }

        public bool Play(bool isForce, int styleIndex, int itemIndex, float volumeScale, AudioDatabase database)
        {
            return Play(isForce, styleIndex, itemIndex, volumeScale, 0.0, database);
        }

        public bool Play(bool isForce, int styleIndex, int itemIndex, AudioDatabase database)
        {
            return Play(isForce, styleIndex, itemIndex, 1.0f, 0.0, database);
        }
    }
}
