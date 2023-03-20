using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZG
{
    public class BlurManager : MonoBehaviour
    {
        public bool isGauss = false;

        [Range(0.0f, 0.3f), Tooltip("Standard Deviation (Gauss only)")]
        public float standardDeviation = 0.02f;

        [Range(0.0f, 0.5f), Tooltip("Blur Size")]
        public float blurSize = 0.02f;

        public
#if UNITY_EDITOR
            new 
#endif
        static RenderBlur renderer
        {
            get;

            internal set;
        }

        public static BlurManager instance
        {
            get;

            private set;
        }
        
        protected void OnEnable()
        {
            if (instance == null)
                instance = this;
        }

        protected void OnDisable()
        {
            if (instance == this)
                instance = null;
        }
    }
}