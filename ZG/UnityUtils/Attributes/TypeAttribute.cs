using System;
using UnityEngine;

namespace ZG
{
    public class TypeAttribute : PropertyAttribute
    {
        public Type[] attributeTypes;

        public TypeAttribute(params Type[] attributeTypes)
        {
            this.attributeTypes = attributeTypes;
        }
    }
}