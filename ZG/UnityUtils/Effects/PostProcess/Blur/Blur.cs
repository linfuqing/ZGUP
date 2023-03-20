using System;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;

namespace ZG
{
    [Serializable]
    [PostProcess(typeof(BlurRenderer), PostProcessEvent.AfterStack, "ZG/Blur")]
    public sealed class Blur : PostProcessEffectSettings
    {
        public IntParameter sampleAmount = new IntParameter { value = 10 };
    }

    public class BlurRenderer : PostProcessEffectRenderer<Blur>
    {
        private RenderTexture __renderTexture = null;

        public override void Render(PostProcessRenderContext context)
        {
            CommandBuffer command = context.command;
            var manager = BlurManager.instance;
            if (manager == null || !manager.isActiveAndEnabled)
            {
                command.BlitFullscreenTriangle(context.source, context.destination);

                return;
            }

            var renderer = BlurManager.renderer;
            if (renderer != null)
                renderer.enabled = false;

            PropertySheet sheet = context.propertySheets.Get(Shader.Find("ZG/BlurPostProcess"));
            MaterialPropertyBlock materialPropertyBlock = sheet.properties;

            materialPropertyBlock.SetFloat("_Samples", settings.sampleAmount.value);
            materialPropertyBlock.SetFloat("_BlurSize", manager.blurSize);

            int pass0, pass1;
            if (manager.isGauss)
            {
                pass0 = 1;
                pass1 = 3;

                materialPropertyBlock.SetFloat("_StandardDeviation", manager.standardDeviation);
            }
            else
            {
                pass0 = 0;
                pass1 = 2;
            }

            if(__renderTexture == null)
                __renderTexture = context.GetScreenSpaceTemporaryRT();

            command.BlitFullscreenTriangle(context.source, __renderTexture, sheet, pass0);
            command.BlitFullscreenTriangle(__renderTexture, context.destination, sheet, pass1);
        }

        public override void Release()
        {
            if (__renderTexture != null)
            {
                RenderTexture.ReleaseTemporary(__renderTexture);

                __renderTexture = null;
            }

            base.Release();
        }
    }
}
#endif