using UnityEngine;
using UnityEngine.Rendering;

namespace ZG
{
    [RequireComponent(typeof(Camera))]
    public class DrawRendererCommand : MonoBehaviour
    {
        public CameraEvent cameraEvent;
        [SerializeField]
        internal Renderer _renderer = null;
        private Camera __camera;
        private CameraEvent __cameraEvent;
        private CommandBuffer __commandBuffer;
        //private static Mesh __mesh;
        
        void OnEnable()
        {
            Material material = _renderer == null ? null : _renderer.material;
            if (material == null)
                return;

            if (__camera == null)
                __camera = GetComponent<Camera>();

            if (__camera == null)
                return;

            if (__commandBuffer == null)
                __commandBuffer = new CommandBuffer();
            else
                __commandBuffer.Clear();
            
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

            __commandBuffer.DrawRenderer(_renderer, material);
            //__commandBuffer.DrawMesh(__mesh, Matrix4x4.identity, __material, 0);
            __camera.AddCommandBuffer(cameraEvent, __commandBuffer);

            __cameraEvent = cameraEvent;
        }

        void OnDisable()
        {
            if (__camera != null && __commandBuffer != null)
                __camera.RemoveCommandBuffer(__cameraEvent, __commandBuffer);
        }

        void OnDestroy()
        {
            if (__commandBuffer != null)
            {
                __commandBuffer.Dispose();

                __commandBuffer = null;
            }
        }
    }
}