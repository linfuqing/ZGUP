using System;
using UnityEngine;

namespace ZG
{
    public class Grass : MonoBehaviour
    {
        [Serializable]
        public struct Obstacle
        {
            public float radius;
            public Vector3 center;
            public Transform transform;
        }

        public int minDistance;
        public int maxDistance;

        [SerializeField]
        internal int _maxCount = 32;

        public Obstacle[] obstacles;

        private Vector4[] __obstacles;

        protected void Update()
        {
            Shader.SetGlobalFloat("g_GrassMinDistance", minDistance);
            Shader.SetGlobalFloat("g_GrassMaxDistance", maxDistance);

            int numObstacles = obstacles.Length;

            Shader.SetGlobalInt("g_GrassObstacleCount", numObstacles);

            if (__obstacles == null)
                __obstacles = new Vector4[_maxCount];

            Obstacle obstacle;
            Vector3 position;
            for (int i = 0; i < numObstacles; ++i)
            {
                obstacle = obstacles[i];
                position = obstacle.transform.position + obstacle.center;
                __obstacles[i] = new Vector4(position.x, position.y, position.z, obstacle.radius);
            }

            Shader.SetGlobalVectorArray("g_GrassObstacles", __obstacles);
        }
    }
}