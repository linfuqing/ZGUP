using UnityEngine;

namespace ZG
{
    public enum RotationType
    {
        Direction
    }

    public class RotationAttribute : PropertyAttribute
    {
        public RotationType type;

        public RotationAttribute(RotationType type)
        {
            this.type = type;
        }
    }
}