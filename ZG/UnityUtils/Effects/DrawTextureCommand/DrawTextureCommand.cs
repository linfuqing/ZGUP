using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ZG
{
    [RequireComponent(typeof(Camera))]
    public class DrawTextureCommand : MonoBehaviour
    {
        public bool isUseDepthTexture = false;
        public CameraEvent cameraEvent;

        [SerializeField]
        internal List<Texture> _textures;

        private Camera __camera;
        private CameraEvent __cameraEvent;
        private CommandBuffer __commandBuffer;
        private Material __material;
        private static Shader __shader;
        //private static Mesh __mesh;

        public Material material
        {
            get
            {
                if (__material == null)
                {
                    if (__shader == null)
                        __shader = Shader.Find("ZG/Background");

                    __material = new Material(__shader);
                }

                return __material;
            }
        }

        public IReadOnlyList<Texture> textures => _textures;
        
        public void AddTexture(Texture texture)
        {
            if (_textures == null)
                _textures = new List<Texture>();

            _textures.Add(texture);

            if (isActiveAndEnabled)
            {
                OnDisable();
                OnEnable();
            }
        }

        public void RemoveTexture(Texture texture)
        {
            if (_textures == null || !_textures.Remove(texture))
                return;

            if (isActiveAndEnabled)
            {
                OnDisable();
                OnEnable();
            }
        }
        
        void OnEnable()
        {
            if (_textures == null || _textures.Count < 1)
                return;

            if(__camera == null)
                __camera = GetComponent<Camera>();

            if (__camera == null)
                return;

            var material = this.material;

            //__material.mainTexture = _texture;

            /*if (__mesh == null)
            {
                __mesh = new Mesh();

                __mesh.vertices = new Vector3[]
                {
                    new Vector3(-1.0f, -1.0f, 1.0f),
                    new Vector3(1.0f, -1.0f, 1.0f),
                    new Vector3(-1.0f, 1.0f, 1.0f),
                    new Vector3(1.0f, 1.0f, 1.0f)
                };

                if (SystemInfo.graphicsUVStartsAtTop)
                {
                    __mesh.uv = new Vector2[]
                    {
                        new Vector2(0.0f, 1.0f),
                        new Vector2(1.0f, 1.0f),

                        new Vector2(0.0f, 0.0f),
                        new Vector2(1.0f, 0.0f)
                    };
                }
                else
                {
                    __mesh.uv = new Vector2[]
                    {
                        new Vector2(0.0f, 0.0f),
                        new Vector2(1.0f, 0.0f),

                        new Vector2(0.0f, 1.0f),
                        new Vector2(1.0f, 1.0f)
                    };
                }

                __mesh.triangles = new int[]
                {
                    0, 1, 2, 2, 1, 3
                };
            }*/

            if (__commandBuffer == null)
                __commandBuffer = new CommandBuffer();
            else
                __commandBuffer.Clear();

            RenderTargetIdentifier renderTargetIdentifier = new RenderTargetIdentifier(BuiltinRenderTextureType.CurrentActive);
            foreach (Texture texture in _textures)
                __commandBuffer.Blit(texture, renderTargetIdentifier, material, isUseDepthTexture && (__camera.depthTextureMode & DepthTextureMode.Depth) == DepthTextureMode.Depth ? 1 : 0);

            //__commandBuffer.DrawMesh(__mesh, Matrix4x4.identity, __material, 0);
            __camera.AddCommandBuffer(cameraEvent, __commandBuffer);

            __cameraEvent = cameraEvent;
        }

        void OnDisable()
        {
            if (__camera != null && __commandBuffer != null)
                __camera.RemoveCommandBuffer(__cameraEvent, __commandBuffer);

            if (__commandBuffer != null)
            {
                __commandBuffer.Dispose();

                __commandBuffer = null;
            }
        }

        void OnDestroy()
        {
            if (__material != null)
            {
                Destroy(__material);

                __material = null;
            }
        }
    }
}