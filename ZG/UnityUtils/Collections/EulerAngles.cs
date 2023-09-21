using System;
using UnityEngine;

namespace ZG
{
    [Serializable]
    public struct EulerAngle
    {
        public bool isUsed;
        public Vector3 value;

        public Quaternion rotation => isUsed ? Quaternion.Euler(value) : default;
        public Vector3 forward => isUsed ? Quaternion.Euler(value) * Vector3.forward : default;
    }
}