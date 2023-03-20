using System;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;

namespace ZG
{
    [Serializable]
    [PostProcess(typeof(RaindropRenderer), PostProcessEvent.AfterStack, "ZG/Raindrop")]
    //[CreateAssetMenu(menuName = "ZG/PostProcessEffects/Raindrop")]
    public sealed class Raindrop : PostProcessEffectSettings
    {
        public BoolParameter isActive = new BoolParameter() { value = true };

        public FloatParameter rainAmountSmoothTime = new FloatParameter() { value = 5.0f };

        public FloatParameter timeSmoothTime = new FloatParameter() { value = 0.5f };

        [MinMax(0.0f, 1.0f)]
        public FloatParameter rainAmount = new FloatParameter() { value = 0.3F };

        public FloatParameter rainZoom = new FloatParameter() { value = 0.5f };

        public FloatParameter timeScale = new FloatParameter() { value = 1.0f };

        public FloatParameter maxBlur = new FloatParameter() { value = 5.0f };
        public FloatParameter minBlur = new FloatParameter() { value = 2.0f };

        public RaindropData data
        {
            get
            {
                RaindropData result;
                result.rainAmountSmoothTime = rainAmountSmoothTime;
                result.timeSmoothTime = timeSmoothTime;
                result.rainAmount = rainAmount;
                result.rainZoom = rainZoom;
                result.timeScale = timeScale;
                result.maxBlur = maxBlur;
                result.minBlur = minBlur;

                return result;
            }
        }
    }

    public class RaindropRenderer : PostProcessEffectRenderer<Raindrop>
    {
        private float __elpasedTime;
        private float __timeScale;
        private float __timeScaleVecloity;
        private float __rainAmount;
        private float __rainAmountVelocity;

        public override void Render(PostProcessRenderContext context)
        {
            var settings = base.settings;
            bool isActive = settings.isActive;
            if (!isActive && Mathf.Approximately(__timeScale, 0.0f))
            {
                context.command.BlitFullscreenTriangle(context.source, context.destination);

                return;
            }

            var data = settings.data;

            var sheet = context.propertySheets.Get(Shader.Find("ZG/RaindropPostProcess"));
            var properties = sheet.properties;

            properties.SetFloat("_ElpasedTime", __elpasedTime);
            properties.SetFloat("_RainZoom", data.rainZoom);
            __rainAmount = Mathf.SmoothDamp(__rainAmount, isActive ? data.rainAmount : 0.0f, ref __rainAmountVelocity, data.rainAmountSmoothTime);
            properties.SetFloat("_RainAmount", __rainAmount);
            properties.SetFloat("_MaxBlur", data.maxBlur);
            properties.SetFloat("_MinBlur", data.minBlur);

            context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);

            __timeScale = Mathf.SmoothDamp(__timeScale, isActive ? data.timeScale : 0.0f, ref __timeScaleVecloity, data.timeSmoothTime);
            __elpasedTime += Time.deltaTime * __timeScale;
        }
    }
}
#endif