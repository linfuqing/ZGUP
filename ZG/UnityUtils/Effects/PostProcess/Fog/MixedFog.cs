using System;
using UnityEngine;

#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;

namespace ZG
{
    [Serializable]
    [PostProcess(typeof(MixedFogRenderer), PostProcessEvent.BeforeTransparent, "ZG/MixedFog")]
    public sealed class MixedFog : PostProcessEffectSettings
    {
        /// <summary>
        /// Should the fog affect the skybox?
        /// </summary>
        [Tooltip("Mark true for the fog to ignore the skybox")]
        public BoolParameter isExcludeSkybox = new BoolParameter() { value = true };

        public FloatParameter alphaStart = new FloatParameter() { value = 200 };

        public FloatParameter alphaEnd = new FloatParameter() { value = 500 };

        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            return enabled
                && RenderSettings.fog
                && !RuntimeUtilities.scriptableRenderPipelineActive
                && context.resources.shaders.deferredFog
                && context.resources.shaders.deferredFog.isSupported
                && context.camera.actualRenderingPath == RenderingPath.DeferredShading;  // In forward fog is already done at shader level
        }
    }

    public class MixedFogRenderer : PostProcessEffectRenderer<MixedFog>
    {
        public static readonly int FogColor = Shader.PropertyToID("_FogColor");
        public static readonly int FogParams = Shader.PropertyToID("_FogParams");

        private PostProcessLayer __layer = null;
        private Shader __shader;

        public override DepthTextureMode GetCameraFlags()
        {
            return DepthTextureMode.Depth;
        }

        public override void Render(PostProcessRenderContext context)
        {
            if (__layer == null)
                __layer = context.camera.GetComponent<PostProcessLayer>();

            __layer.fog.enabled = false;

            if (__shader == null)
                __shader = Shader.Find("ZG/MixedFogPostProcess");

            var sheet = context.propertySheets.Get(__shader);
            sheet.ClearKeywords();
            //sheet.EnableKeyword("FOG_LINEAR");

            var fogColor = RuntimeUtilities.isLinearColorSpace ? RenderSettings.fogColor.linear : RenderSettings.fogColor;
            var properties = sheet.properties;
            properties.SetVector(FogColor, fogColor);
            properties.SetVector(FogParams, new Vector3(RenderSettings.fogDensity, RenderSettings.fogStartDistance, RenderSettings.fogEndDistance));
            properties.SetFloat("_AlphaStart", settings.alphaStart);
            properties.SetFloat("_AlphaEnd", settings.alphaEnd);

            var cmd = context.command;
            cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, settings.isExcludeSkybox ? 1 : 0);
        }
    }
}
#endif