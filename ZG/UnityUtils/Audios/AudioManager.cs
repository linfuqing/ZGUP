using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZG
{
    public class AudioManager : MonoBehaviour
    {
        private static Dictionary<string, AudioComponent> __audios;

        [SerializeField]
        internal AudioComponent[] _audios;

        public static AudioComponent Find(string name)
        {
            return __audios.TryGetValue(name, out var audioSource) ? audioSource : null;
        }

        void OnEnable()
        {
            if(_audios == null || _audios.Length < 1)
                _audios = GetComponentsInChildren<AudioComponent>();

            if (_audios != null && _audios.Length > 0)
            {
                if (__audios == null)
                    __audios = new Dictionary<string, AudioComponent>();

                foreach(var audio in _audios)
                    __audios.Add(audio.name, audio);
            }
            else
                Debug.LogError($"Empty Audio Manager {name}", this);
        }

        void OnDisable()
        {
            if (_audios != null)
            {
                foreach (var audio in _audios)
                    __audios.Remove(audio.name);
            }
        }
    }
}