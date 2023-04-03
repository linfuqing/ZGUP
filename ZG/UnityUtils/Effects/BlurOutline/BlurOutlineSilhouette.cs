using UnityEngine;
using static ZG.RenderBlurOutline;

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
            __index = instance.Add(new RendererSilhouette(_color, GetComponent<Renderer>()));
        }
    }
}