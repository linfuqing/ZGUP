using UnityEngine;

public class FogFactor : MonoBehaviour
{
    public float value;
    public float min = 0.5f;
    public float max = 1.0f;

    void Update()
    {
        Shader.SetGlobalFloat("g_FogFactor", 1.0f - Mathf.Clamp(value, min, max));
    }

    void OnDisable()
    {
        Shader.SetGlobalFloat("g_FogFactor", 1.0f - Mathf.Clamp(0.0f, min, max));
    }
}
