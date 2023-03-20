using UnityEngine;

namespace ZG
{
    public class AudioController : MonoBehaviour
    {
        public void Play(int index)
        {
            AudioManager.Find(name).Play(index);
        }
    }
}