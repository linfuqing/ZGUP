using UnityEngine;

namespace ZG.Effects
{
    [ExecuteInEditMode]
    public class Clip : MonoBehaviour
    {
        public static Clip instance;

        public float distance = 5.0f;
        public float near = 1.0f;
        public float far = 60.0f;

        public Transform target;

        public void OnEnable()
        {
            instance = this;
        }

        public void OnDisable()
        {
            if(instance == this)
                instance = null;
        }

        public void Update()
        {
            Shader.SetGlobalFloat("g_ClipInvDist", 1.0f / distance);
            Shader.SetGlobalFloat("g_ClipNearDivDist", near / distance);
            Shader.SetGlobalFloat("g_ClipFarDivDist", far / distance);

            if (target != null)
                Shader.SetGlobalVector("g_ClipTargetPosition", target.position);
        }
    }
}