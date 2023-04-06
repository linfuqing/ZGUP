using UnityEngine;

namespace ZG
{
    //[ExecuteAlways]
    public class BlurOutlineSilhouette : MonoBehaviour
    {
        [SerializeField]
        internal Color _color;

        private int __index = -1;
        private bool __isStart;

        protected void Start()
        {
            __Start();

            __isStart = true;
        }

        protected void OnEnable()
        {
            if(__isStart)
                __Start();
        }

        protected void OnDisable()
        {
            var instance = IRenderBlurOutline.instance as RenderBlurOutline;
            if(instance != null)
                instance.Remove(__index);

            __index = -1;
        }

        protected void OnValidate()
        {
            if (__index != -1)
            {
                OnDisable();

                __Start();
            }
        }

        private void __Start()
        {
            var instance = IRenderBlurOutline.instance as RenderBlurOutline;
            if (instance != null)
                __index = instance.Add(new RenderBlurOutline.RendererSilhouette(_color, GetComponent<Renderer>()));
        }
    }
}