using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZG
{
    public interface IAudioMain
    {
        bool Play(bool isForce, int styleIndex, float volumeScale, double time, AudioClip clip);
    }
    
    public interface IAudioWrapper
    {
        bool Play(
            bool isForce, 
            int styleIndex, 
            float volumeScale, 
            double time, 
            AudioClip clip, 
            IAudioMain main);
    }

    [CreateAssetMenu(menuName = "ZG/Audio Database")]
    public class AudioDatabase : ScriptableObject
    {
        [Serializable]
        public struct Item
        {
#if UNITY_EDITOR
            public string name;
#endif

            [Type(typeof(IAudioWrapper))]
            public string wrapperType;

            public AudioClip[] clips;
        }

        public Item[] items;
    }
}