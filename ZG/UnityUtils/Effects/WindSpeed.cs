using UnityEngine;

public class WindSpeed : MonoBehaviour
{
    public float value;

    public Vector2 direction = new Vector2(1.0f, 0.0f);
    public float speed = 3.0f;
    public float positionScale = 10.0f;

    void OnDisable()
    {
        Shader.SetGlobalFloat("g_Windspeed", 0.0f);
    }

    void Update()
    {
        Shader.SetGlobalFloat("g_Windspeed", value);

        Shader.SetGlobalVector("g_WindParams", new Vector4(direction.x, direction.y, speed, positionScale));
    }
}
