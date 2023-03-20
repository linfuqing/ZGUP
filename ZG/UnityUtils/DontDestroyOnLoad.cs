using UnityEngine;

namespace ZG
{
    public class DontDestroyOnLoad : MonoBehaviour
    {

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}