using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ZG
{
    [Serializable]
    public struct RenderBlurOutlineData
    {
        public int blurIterCount;
        public int downSample;
        public float strength;
        public Vector2 blurScale;
    }

    public interface IRenderBlurOutline
    {
        public static IRenderBlurOutline instance
        {
            get;

            set;
        }

        bool isVail { get; }

        bool needDepthSort { get; }

        RenderBlurOutlineData data
        {
            get;
        }

        void Draw(
            Material[] silhouetteMaterials, 
            CommandBuffer cmd,
            ref ScriptableRenderContext context,
            ref CullingResults cullingResults,
            ref DrawingSettings drawingSettings);
    }

    [RequireComponent(typeof(Camera))]
    //[ExecuteAlways]
    public class RenderBlurOutline : MonoBehaviour, IRenderBlurOutline
    {
        public enum SilhouetteType
        {
            Normal, 
            LinearBlendSkinning, 
            ComputeDeformation
        }

        public interface ISilhouette
        {
            Color color { get; }

            bool GetTypeAndOffset(out SilhouetteType silhouetteType, out int offset);

            void Draw(CommandBuffer buffer, Material material);
        }

        public interface IMeshRenderer
        {
            Matrix4x4 matrix { get; }

            bool GetTypeAndOffset(out SilhouetteType silhouetteType, out int offset);
        }

        public class MeshSilhouette : ISilhouette
        {
            private int __submeshIndex;
            private Mesh __mesh;

            private IMeshRenderer __renderer;

            public Color color
            {
                get;

                set;
            }

            public bool GetTypeAndOffset(out SilhouetteType silhouetteType, out int offset) => __renderer.GetTypeAndOffset(out silhouetteType, out offset);

            public MeshSilhouette(
                Color color, 
                int submeshIndex, 
                Mesh mesh,
                IMeshRenderer renderer)
            {
                this.color = color;

                __submeshIndex = submeshIndex;
                __mesh = mesh;
                __renderer = renderer;
            }

            public void Draw(CommandBuffer commandBuffer, Material material)
            {
                if (__mesh == null)
                    return;

                commandBuffer.DrawMesh(__mesh, __renderer.matrix, material, __submeshIndex);
            }

        }

        public class RendererSilhouette : ISilhouette
        {
            public Color color;
            public Renderer __renderer;

            public bool isVisible => __renderer != null && __renderer.isVisible;

            public SilhouetteType type => SilhouetteType.Normal;

            public int offset => 0;

            public RendererSilhouette(Color color, Renderer renderer)
            {
                this.color = color;

                __renderer = renderer;
            }

            public bool GetTypeAndOffset(out SilhouetteType silhouetteType, out int offset)
            {
                silhouetteType = SilhouetteType.Normal;

                offset = 0;

                return isVisible;
            }

            public void Draw(CommandBuffer commandBuffer, Material material)
            {
                if (!isVisible)
                    return;

                commandBuffer.DrawRenderer(__renderer, material);
            }

            Color ISilhouette.color => color;
        }

        public static readonly int SkinMatrixIndex = Shader.PropertyToID("_SkinMatrixIndex");
        public static readonly int ComputeMeshIndex = Shader.PropertyToID("_ComputeMeshIndex");
        public static readonly int SolidColor = Shader.PropertyToID("_SolidColor");

        public bool isAutoUpdate;
        public int blurIterCount = 1;
        public int downSample = 1;
        public float strength = 1.0f;
        public Vector2 blurScale = new Vector2(1.0F, 1.0F);
        public Shader outlineShader;
        public Shader silhouetteShader;

        /*public string[] solidShaderResourceNames =
        {
            "ZG/SolidColor",
            "ZG/SolidColorLinearBlendSkinning",
            "ZG/SolidColorComputeDeformation"
        };*/

        private bool __isDirty;
        private Material __outlineMaterial;
        private Material __silhouetteMaterial;
        private CommandBuffer __renderCommand;
        private Pool<ISilhouette> __silhouettes;


        public bool isVail
        {
            get
            {
                enabled = false;

                return silhouetteCount > 0;
            }
        }

        public bool needDepthSort => false;

        public int silhouetteCount
        {
            get
            {
                return __silhouettes == null ? 0 : __silhouettes.count;
            }
        }

        public IEnumerable<KeyValuePair<int, ISilhouette>> silhouettes
        {
            get
            {
                return __silhouettes;
            }
        }

        public RenderBlurOutlineData data
        {
            get
            {
                RenderBlurOutlineData result;
                result.blurIterCount = blurIterCount;
                result.downSample = downSample;
                result.strength = strength;
                result.blurScale = blurScale;

                return result;
            }
        }

        public void MaskDirty()
        {
            __isDirty = true;
        }

        public int Add(ISilhouette silhouette)
        {
            if (!__isDirty && isActiveAndEnabled)
            {
                if (__renderCommand == null)
                {
                    __renderCommand = new CommandBuffer();

                    __renderCommand.ClearRenderTarget(true, true, Color.clear);
                }

                if (__silhouetteMaterial == null)
                    __silhouetteMaterial = new Material(silhouetteShader);
                
                __renderCommand.SetGlobalColor("_SolidColor", silhouette.color);
                silhouette.Draw(__renderCommand, __silhouetteMaterial);
            }

            if (__silhouettes == null)
                __silhouettes = new Pool<ISilhouette>();

            return __silhouettes.Add(silhouette);
        }

        public bool Remove(int index)
        {
            if (__silhouettes != null && __silhouettes.RemoveAt(index))
            {
                __isDirty = true;

                return true;
            }

            return false;
        }

        public void Draw(
            Material[] silhouetteMaterials, 
            CommandBuffer cmd,
            ref ScriptableRenderContext context,
            ref CullingResults cullingResults,
            ref DrawingSettings drawingSettings)
        {
            enabled = false;

            int offset;
            SilhouetteType silhouetteType;
            ISilhouette silhouette;
            Material material;
            //Shader shader;
            foreach (var pair in silhouettes)
            {
                silhouette = pair.Value;
                if (!silhouette.GetTypeAndOffset(out silhouetteType, out offset))
                    continue;

                material = silhouetteMaterials[(int)silhouetteType];
                /*if (material == null)
                {
                    shader = Shader.Find(solidShaderResourceNames[(int)silhouetteType]);

                    material = shader == null ? null : new Material(shader);

                    __silhouetteMaterials[(int)silhouetteType] = material;
                }*/

                if (material == null)
                    continue;

                switch (silhouetteType)
                {
                    case SilhouetteType.LinearBlendSkinning:
                        cmd.SetGlobalInt(SkinMatrixIndex, offset);
                        break;
                    case SilhouetteType.ComputeDeformation:
                        cmd.SetGlobalInt(ComputeMeshIndex, offset);
                        break;
                }

                cmd.SetGlobalColor(SolidColor, silhouette.color);

                silhouette.Draw(cmd, material);
            }
        }

        void OnEnable()
        {
            if (IRenderBlurOutline.instance == null)
                IRenderBlurOutline.instance = this;
        }

        void OnDisable()
        {
            if(IRenderBlurOutline.instance == (IRenderBlurOutline)this)
                IRenderBlurOutline.instance = null;
        }

        void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (__silhouettes == null || __silhouettes.count < 1)
            {
                Graphics.Blit(src, dest);

                return;
            }

            if (__silhouetteMaterial == null)
                __silhouetteMaterial = new Material(silhouetteShader);

            if (__isDirty)
            {
                if(isAutoUpdate)
                    __isDirty = false;

                if (__renderCommand == null)
                    __renderCommand = new CommandBuffer();
                else
                    __renderCommand.Clear();

                __renderCommand.ClearRenderTarget(true, true, Color.clear);

                foreach (ISilhouette silhouette in (IEnumerable<ISilhouette>)__silhouettes)
                {
                    __renderCommand.SetGlobalColor("_SolidColor", silhouette.color);
                    silhouette.Draw(__renderCommand, __silhouetteMaterial);
                }
            }
            else if (__renderCommand == null)
                return;

            //__silhouetteMaterial.SetColor("_SolidColor", outlineColor);
            RenderTexture solidSilhouette = RenderTexture.GetTemporary(Screen.width, Screen.height);
            Graphics.SetRenderTarget(solidSilhouette);
            Graphics.ExecuteCommandBuffer(__renderCommand);

            int width = Screen.width >> downSample, height = Screen.height >> downSample;
            RenderTexture blurSilhouette = RenderTexture.GetTemporary(width, height);

            if (__outlineMaterial == null)
                __outlineMaterial = new Material(outlineShader);

            Graphics.Blit(solidSilhouette, blurSilhouette, __outlineMaterial, 0);

            RenderTexture blurTemp = RenderTexture.GetTemporary(width, height);
            
            for (int i = 0; i < blurIterCount; ++i)
            {
                __outlineMaterial.SetFloat("_BlurOffsetX", 0.0f);
                __outlineMaterial.SetFloat("_BlurOffsetY", blurScale.y);

                Graphics.Blit(blurSilhouette, blurTemp, __outlineMaterial, 1);

                __outlineMaterial.SetFloat("_BlurOffsetX", blurScale.x);
                __outlineMaterial.SetFloat("_BlurOffsetY", 0.0f);

                Graphics.Blit(blurTemp, blurSilhouette, __outlineMaterial, 1);
            }

            __outlineMaterial.SetTexture("_BlurTex", blurSilhouette);
            Graphics.Blit(solidSilhouette, blurTemp, __outlineMaterial, 2);

            __outlineMaterial.SetFloat("_Strength", strength);
            __outlineMaterial.SetTexture("_BlurTex", blurTemp);
            Graphics.Blit(src, dest, __outlineMaterial, 3);
            
            RenderTexture.ReleaseTemporary(solidSilhouette);
            RenderTexture.ReleaseTemporary(blurSilhouette);
            RenderTexture.ReleaseTemporary(blurTemp);
        }
    }
}