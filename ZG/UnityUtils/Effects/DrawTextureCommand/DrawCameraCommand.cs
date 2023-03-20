using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ZG
{
    [RequireComponent(typeof(Camera))]
    public class DrawCameraCommand : MonoBehaviour
    {
        public CameraEvent cameraEvent;

        internal struct Buffer
        {
            public RenderTexture rt0;
            public RenderTexture rt1;
            public RenderTexture rt2;
        }

        [SerializeField]
        internal Dictionary<Camera, Buffer> _buffers;

        private Camera __camera;
        private CameraEvent __cameraEvent;
        private CommandBuffer __commandBuffer;
        private Material __material;
        private static Shader __shader;
        //private static Mesh __mesh;

        public void AddTexture(Camera camera)
        {
            int w = __camera.pixelWidth, h = __camera.pixelHeight;

            Buffer buffer;
            // Create render textures
            // albedo & ao
            buffer.rt0 = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
            // spec & roughness
            buffer.rt1 = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
            // normals
            buffer.rt2 = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB2101010);
            // emission, lighting, and eventually frame buffer. only one with depth!
            //buffer.rt3 = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.DefaultHDR);

            RenderBuffer[] renderBuffers = new RenderBuffer[4];
            renderBuffers[0] = buffer.rt0.colorBuffer; // SV_Target0
            renderBuffers[1] = buffer.rt1.colorBuffer; // SV_Target1
            renderBuffers[2] = buffer.rt2.colorBuffer; // SV_Target2
            renderBuffers[3] = __camera.targetTexture.colorBuffer; // SV_Target3
            camera.SetTargetBuffers(renderBuffers, __camera.targetTexture.depthBuffer);

            if (_buffers == null)
                _buffers = new Dictionary<Camera, Buffer>();

            _buffers.Add(camera, buffer);

            if (isActiveAndEnabled)
            {
                OnDisable();
                OnEnable();
            }
        }

        public void RemoveCamera(Camera camera)
        {
            if (_buffers == null || !_buffers.Remove(camera))
                return;

            if(_buffers.TryGetValue(camera, out var buffer))
            {
                RenderTexture.ReleaseTemporary(buffer.rt0);
                RenderTexture.ReleaseTemporary(buffer.rt1);
                RenderTexture.ReleaseTemporary(buffer.rt2);
            }
            
            if (isActiveAndEnabled)
            {
                OnDisable();
                OnEnable();
            }
        }

        void OnEnable()
        {
            if (_buffers == null || _buffers.Count < 1)
                return;

            if (__camera == null)
                __camera = GetComponent<Camera>();

            if (__camera == null)
                return;

            if (__material == null)
            {
                if (__shader == null)
                    __shader = Shader.Find("ZG/Background");

                __material = new Material(__shader);
            }
            
            if (__commandBuffer == null)
                __commandBuffer = new CommandBuffer();
            else
                __commandBuffer.Clear();

            RenderTargetIdentifier renderTargetIdentifier = new RenderTargetIdentifier(BuiltinRenderTextureType.CurrentActive);
            foreach (Camera camera in _buffers.Keys)
                __commandBuffer.Blit(camera.targetTexture, renderTargetIdentifier, __material, 1);

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