using System;
using UnityEngine;

public class AutoRenderTexture : UnityEngine.EventSystems.UIBehaviour
{
    [Serializable]
    public class ChangeEvent : UnityEngine.Events.UnityEvent<RenderTexture>
    {
        public new void Invoke(RenderTexture renderTexture)
        {
            base.Invoke(renderTexture);

            int count = GetPersistentEventCount();
            Camera camera;
            for(int i = 0; i < count; ++i)
            {
                camera = GetPersistentTarget(i) as Camera;
                if (camera != null && GetPersistentMethodName(i) == "set_targetTexture")
                    camera.targetTexture = renderTexture;
            }
        }
    }

    [UnityEngine.Serialization.FormerlySerializedAs("onChanged")]
    public ChangeEvent onEnable;
    public ChangeEvent onDisable;

    public int depthBuffer;
    public int antiAliasing;
    [ZG.Mask]
    public RenderTextureMemoryless memorylessMode;
    public VRTextureUsage vrUsage;
    public bool useDynamicScale;

    private RenderTexture __renderTexture;

    protected override void OnRectTransformDimensionsChange()
    {
        RectTransform rectTransform = base.transform as RectTransform;
        Transform root = rectTransform.root;
        Canvas canvas = root == null ? null : root.GetComponent<Canvas>();
        if (canvas == null)
            return;

        float scaleFactor = canvas.scaleFactor;
        Rect rect = RectTransformUtility.PixelAdjustRect(rectTransform, canvas);
        int width = Mathf.RoundToInt(rect.width * scaleFactor), height = Mathf.RoundToInt(rect.height * scaleFactor);
        if (width == 0 || height == 0)
            return;

        if (__renderTexture != null)
        {
            if (__renderTexture.width == width && __renderTexture.height == height)
                return;

            if (onDisable != null)
                onDisable.Invoke(__renderTexture);

            RenderTexture.ReleaseTemporary(__renderTexture);
        }

        __renderTexture = RenderTexture.GetTemporary(
            width, 
            height, 
            depthBuffer, 
            RenderTextureFormat.Default, 
            RenderTextureReadWrite.Default, 
            antiAliasing, 
            memorylessMode, 
            vrUsage, 
            useDynamicScale);

        __renderTexture.filterMode = FilterMode.Point;
        __renderTexture.wrapMode = TextureWrapMode.Clamp;

        if (onEnable != null)
            onEnable.Invoke(__renderTexture);
    }

    /*protected override void OnEnable()
    {
        base.OnEnable();

        OnRectTransformDimensionsChange();
    }*/

    protected override void OnDisable()
    {
        if (__renderTexture != null)
        {
            if (onDisable != null)
                onDisable.Invoke(__renderTexture);

            RenderTexture.ReleaseTemporary(__renderTexture);

            __renderTexture = null;
        }

        base.OnDisable();
    }
}
