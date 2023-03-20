using System;
using UnityEngine;

[CreateAssetMenu(menuName = "ZG/Audio Database")]
public class AudioDatabase : ScriptableObject
{
    [Serializable]
    public struct Item
    {
#if UNITY_EDITOR
        public string name;
#endif

        public AudioClip[] clips;
    }

    public Item[] items;
}
