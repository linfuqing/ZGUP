using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;

namespace ZG
{
    [Serializable]
    [PostProcess(typeof(BlurOutlineRenderer), PostProcessEvent.AfterStack, "ZG/BlurOutline")]
    public sealed class BlurOutline : PostProcessEffectSettings
    {
        public IntParameter blurIterCount = new IntParameter { value = 1 };
        public IntParameter downSample = new IntParameter { value = 1 };
        public FloatParameter strength = new FloatParameter { value = 1.0f };
        public FloatParameter blurScaleX = new FloatParameter { value = 1.0f };
        public FloatParameter blurScaleY = new FloatParameter { value = 1.0f };

        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            return enabled.value;
        }
    }

    public class BlurOutlineRenderer : PostProcessEffectRenderer<BlurOutline>
    {
        private bool __isDirty;
        private RenderTexture __solidSilhouette;
        private RenderTexture __blurSilhouette;
        private RenderTexture __blurTemp;
        private Material __silhouetteMaterial;
        
        public override void Render(PostProcessRenderContext context)
        {
            CommandBuffer command = context?.command;
            if (command == null)
                return;

            RenderBlurOutline renderBlurOutline = RenderBlurOutline.instance;
            if (renderBlurOutline == null)
                return;

            renderBlurOutline.enabled = false;

            IEnumerable<KeyValuePair<int, RenderBlurOutline.ISilhouette>> silhouettes = renderBlurOutline.silhouettes;
            if (silhouettes == null || renderBlurOutline.silhouetteCount < 1)
            {
                command.BlitFullscreenTriangle(context.source, context.destination);

                return;
            }

            PropertySheetFactory propertySheets = context.propertySheets;
            if (propertySheets == null)
                return;

            BlurOutline settings = base.settings;
            if (settings == null)
                return;

            //__silhouetteMaterial.SetColor("_SolidColor", outlineColor);
            if(__solidSilhouette == null)
                __solidSilhouette = context.GetScreenSpaceTemporaryRT();

            command.SetRenderTarget(__solidSilhouette);

            command.ClearRenderTarget(true, true, Color.clear);

            if (__silhouetteMaterial == null)
                __silhouetteMaterial = new Material(Shader.Find("ZG/SolidColor"));

            RenderBlurOutline.ISilhouette silhouette;
            foreach (KeyValuePair<int, RenderBlurOutline.ISilhouette> pair in silhouettes)
            {
                silhouette = pair.Value;
                command.SetGlobalColor("_SolidColor", silhouette.color);
                silhouette.Draw(command, __silhouetteMaterial);
            }

            int width = context.width >> settings.downSample, height = context.height >> settings.downSample;
            if(__blurSilhouette == null)
                __blurSilhouette = context.GetScreenSpaceTemporaryRT(0, RenderTextureFormat.Default, RenderTextureReadWrite.Default, width, height);

            PropertySheet sheet = propertySheets.Get(Shader.Find("ZG/BlurOutlinePostProcess"));

            command.BlitFullscreenTriangle(__solidSilhouette, __blurSilhouette, sheet, 0);

            MaterialPropertyBlock materialPropertyBlock = sheet?.properties;
            if (materialPropertyBlock != null)
            {
                if(__blurTemp == null)
                    __blurTemp = context.GetScreenSpaceTemporaryRT(0, RenderTextureFormat.Default, RenderTextureReadWrite.Default, width, height);

                for (int i = 0; i < settings.blurIterCount; ++i)
                {
                    materialPropertyBlock.SetFloat("_BlurOffsetX", 0.0f);
                    materialPropertyBlock.SetFloat("_BlurOffsetY", settings.blurScaleY);

                    command.BlitFullscreenTriangle(__blurSilhouette, __blurTemp, sheet, 1);

                    materialPropertyBlock.SetFloat("_BlurOffsetX", settings.blurScaleX);
                    materialPropertyBlock.SetFloat("_BlurOffsetY", 0.0f);

                    command.BlitFullscreenTriangle(__blurTemp, __blurSilhouette, sheet, 1);
                }

                materialPropertyBlock.SetTexture("_BlurTex", __blurSilhouette);
                command.BlitFullscreenTriangle(__solidSilhouette, __blurTemp, sheet, 2);

                materialPropertyBlock.SetFloat("_Strength", settings.strength);
                materialPropertyBlock.SetTexture("_BlurTex", __blurTemp);
                command.BlitFullscreenTriangle(context.source, context.destination, sheet, 3);
            }
        }

        public override void Release()
        {
            if (__solidSilhouette != null)
            {
                RenderTexture.ReleaseTemporary(__solidSilhouette);

                __solidSilhouette = null;
            }

            if (__blurSilhouette != null)
            {
                RenderTexture.ReleaseTemporary(__blurSilhouette);

                __blurSilhouette = null;
            }

            if(__blurTemp != null)
            {
                RenderTexture.ReleaseTemporary(__blurTemp);

                __blurTemp = null;
            }

            base.Release();
        }
    }
}
#endif