using UnityEngine;

namespace ZG
{
    public class RenderBlur : MonoBehaviour
    {
        public enum Sample
        {
            Low,
            Medium,
            High
        }
        
        [Tooltip("Sample amount")]
        public Sample sample = Sample.Low;
        
        private Material __postprocessMaterial;

        protected void OnEnable()
        {
            if(BlurManager.renderer == null)
                BlurManager.renderer = this;
        }

        protected void OnDisable()
        {
            if (BlurManager.renderer == this)
                BlurManager.renderer = null;
        }

        //method which is automatically called by unity after the camera is done rendering
        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (BlurManager.renderer != this)
            {
                Graphics.Blit(source, destination);

                return;
            }

            var manager = BlurManager.instance;
            if (manager == null || !manager.isActiveAndEnabled)
            {
                Graphics.Blit(source, destination);

                return;
            }

            if (__postprocessMaterial == null)
                __postprocessMaterial = new Material(Shader.Find("ZG/Blur"));

            __postprocessMaterial.SetFloat("_BlurSize", manager.blurSize);

            switch (sample)
            {
                case Sample.Low:
                    __postprocessMaterial.EnableKeyword("_SAMPLES_LOW");
                    __postprocessMaterial.DisableKeyword("_SAMPLES_MEDIUM");
                    __postprocessMaterial.DisableKeyword("_SAMPLES_HIGH");
                    break;
                case Sample.Medium:
                    __postprocessMaterial.DisableKeyword("_SAMPLES_LOW");
                    __postprocessMaterial.EnableKeyword("_SAMPLES_MEDIUM");
                    __postprocessMaterial.DisableKeyword("_SAMPLES_HIGH");
                    break;
                case Sample.High:
                    __postprocessMaterial.DisableKeyword("_SAMPLES_LOW");
                    __postprocessMaterial.DisableKeyword("_SAMPLES_MEDIUM");
                    __postprocessMaterial.EnableKeyword("_SAMPLES_HIGH");
                    break;
            }

            if (manager.isGauss)
            {
                __postprocessMaterial.EnableKeyword("GAUSS");

                __postprocessMaterial.SetFloat("_StandardDeviation", manager.standardDeviation);
            }
            else
                __postprocessMaterial.DisableKeyword("GAUSS");

            //draws the pixels from the source texture to the destination texture
            var temporaryTexture = RenderTexture.GetTemporary(source.width, source.height);
            Graphics.Blit(source, temporaryTexture, __postprocessMaterial, 0);
            Graphics.Blit(temporaryTexture, destination, __postprocessMaterial, 1);
            RenderTexture.ReleaseTemporary(temporaryTexture);
        }
    }
}