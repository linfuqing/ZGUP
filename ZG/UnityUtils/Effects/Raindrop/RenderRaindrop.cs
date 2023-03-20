using UnityEngine;

namespace ZG
{
    [System.Serializable]
    public struct RaindropData
    {
        public float rainAmountSmoothTime;

        public float timeSmoothTime;

        [Range(0.0f, 1.0f)]
        public float rainAmount;

        public float rainZoom;

        public float timeScale;

        public float maxBlur;
        public float minBlur;

        public static RaindropData defaultValue
        {
            get
            {
                RaindropData data;
                data.rainAmountSmoothTime = 5.0f;

                data.timeSmoothTime = 0.5f;

                data.rainAmount = 0.3F;

                data.rainZoom = 0.5f;

                data.timeScale = 1.0f;

                data.maxBlur = 5.0f;
                data.minBlur = 2.0f;

                return data;
            }
        }
    }

    public class RenderRaindrop : MonoBehaviour
    {
        public static int count
        {
            get;

            private set;
        }

        protected void OnEnable()
        {
            ++count;
        }

        protected void OnDisable()
        {
            --count;
        }
    }
}