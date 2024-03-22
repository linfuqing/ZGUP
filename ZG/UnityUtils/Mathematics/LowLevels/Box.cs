using System;
using Unity.Mathematics;

namespace ZG.Mathematics
{

    [Serializable]
    public struct Box
    {
        public float3 min;
        public float3 max;
        public quaternion rotation;

        public float3 center
        {
            get
            {
                return (max + min) * 0.5f;
            }
        }

        public float3 extents
        {
            get
            {
                return (max - min) * 0.5f;
            }
        }

        public float3 worldExtents
        {
            get
            {
                float3 right = math.abs(this.right),
                        up = math.abs(this.up),
                        forward = math.abs(this.forward),
                        extents = this.extents;//,
                        //center = this.center;

                return right * extents.x + up * extents.y + forward * extents.z;
            }
        }

        public float3 right
        {
            get
            {
                return math.mul(rotation, new float3(1.0f, 0.0f, 0.0f));
            }
        }

        public float3 up
        {
            get
            {
                return math.mul(rotation, new float3(0.0f, 1.0f, 0.0f));
            }
        }

        public float3 forward
        {
            get
            {
                return math.mul(rotation, new float3(0.0f, 0.0f, 1.0f));
            }
        }
        
        public Box(in float3 center, in float3 extents, in quaternion rotation)
        {
            min = center - extents;
            max = center + extents;
            this.rotation = rotation;
        }
        
        public Box(in float3 center, in float3 extents, in float4x4 matrix)
        {
            Math.Decompose(math.float3x3(matrix), out rotation, out var scale);
            
            var worldCenter = math.transform(matrix, center);
            min = worldCenter - extents * scale;
            max = worldCenter + extents * scale;
        }

        public Box(float width, float height, float length, float3 start, float3 end)
        {
            float3 distance = end - start, center = (start + end) * 0.5f, extents = new float3(width, height, math.length(distance) + length) * 0.5f;
            min = center - extents;
            max = center + extents;
            rotation = quaternion.LookRotation(distance, math.up());
        }

        public void GetCorners(
            out float3 leftUpForward,
            out float3 leftDownForward,
            out float3 rightUpForward,
            out float3 rightDownForward,

            out float3 leftUpBackward,
            out float3 leftDownBackward,
            out float3 rightUpBackward,
            out float3 rightDownBackward)
        {
            float3 center = this.center,
                extents = this.extents,
                right = this.right * extents.x,
                up = this.up * extents.y,
                forward = this.forward * extents.z;
            leftUpForward = center - right + up + forward;
            leftDownForward = center - right - up + forward;
            rightUpForward = center + right + up + forward;
            rightDownForward = center + right - up + forward;

            leftUpBackward = center - right + up - forward;
            leftDownBackward = center - right - up - forward;
            rightUpBackward = center + right + up - forward;
            rightDownBackward = center + right - up - forward;
        }

        public void GetInterval(in float3 onNormal, out float min, out float max)
        {
            float3 leftUpForward, leftDownForward, rightUpForward, rightDownForward,
                leftUpBackward, leftDownBackward, rightUpBackward, rightDownBackward;

            GetCorners(
                out leftUpForward,
                out leftDownForward,
                out rightUpForward,
                out rightDownForward,
                out leftUpBackward,
                out leftDownBackward,
                out rightUpBackward,
                out rightDownBackward);

            float leftUpForwardOnNormal = math.dot(leftUpForward, onNormal),
                leftDownForwardOnNormal = math.dot(leftDownForward, onNormal),
                rightUpForwardOnNormal = math.dot(rightUpForward, onNormal),
                rightDownForwardOnNormal = math.dot(rightDownForward, onNormal),
                leftUpBackwardOnNormal = math.dot(leftUpBackward, onNormal),
                leftDownBackwardOnNormal = math.dot(leftDownBackward, onNormal),
                rightUpBackwardOnNormal = math.dot(rightUpBackward, onNormal),
                rightDownBackwardOnNormal = math.dot(rightDownBackward, onNormal);

            min = leftUpForwardOnNormal;
            min = math.min(min, leftDownForwardOnNormal);
            min = math.min(min, rightUpForwardOnNormal);
            min = math.min(min, rightDownForwardOnNormal);

            min = math.min(min, leftUpBackwardOnNormal);
            min = math.min(min, leftDownBackwardOnNormal);
            min = math.min(min, rightUpBackwardOnNormal);
            min = math.min(min, rightDownBackwardOnNormal);

            max = leftUpForwardOnNormal;
            max = math.max(max, leftDownForwardOnNormal);
            max = math.max(max, rightUpForwardOnNormal);
            max = math.max(max, rightDownForwardOnNormal);

            max = math.max(max, leftUpBackwardOnNormal);
            max = math.max(max, leftDownBackwardOnNormal);
            max = math.max(max, rightUpBackwardOnNormal);
            max = math.max(max, rightDownBackwardOnNormal);
        }

        public bool IsContains(float3 point)
        {
            float3 position = center;
            point -= position;
            point = math.mul(math.inverse(rotation), point);
            point += position;

            return math.all(point > min) && math.all(point < max);
        }

        public bool Test(in Box other, in float3 onNormal)
        {
            float sourceMin, sourceMax, destinationMin, destinationMax;
            GetInterval(onNormal, out sourceMin, out sourceMax);
            other.GetInterval(onNormal, out destinationMin, out destinationMax);
            if (sourceMax < destinationMin || destinationMax < sourceMin)
                return false;

            return true;
        }

        public bool Test(in Box other)
        {
            float3 sourceRight = right, sourceUp = up, sourceForward = forward,
                destinationRight = other.right, destinationUp = other.up, destinationForward = other.forward;

            if (!Test(other, sourceRight))
                return false;

            if (!Test(other, sourceUp))
                return false;

            if (!Test(other, sourceForward))
                return false;

            if (!Test(other, destinationRight))
                return false;

            if (!Test(other, destinationUp))
                return false;

            if (!Test(other, destinationForward))
                return false;

            if (!Test(other, math.cross(sourceRight, destinationRight)))
                return false;

            if (!Test(other, math.cross(sourceUp, destinationRight)))
                return false;

            if (!Test(other, math.cross(sourceForward, destinationRight)))
                return false;

            if (!Test(other, math.cross(sourceRight, destinationUp)))
                return false;

            if (!Test(other, math.cross(sourceUp, destinationUp)))
                return false;

            if (!Test(other, math.cross(sourceForward, destinationUp)))
                return false;

            if (!Test(other, math.cross(sourceRight, destinationForward)))
                return false;

            if (!Test(other, math.cross(sourceUp, destinationForward)))
                return false;

            if (!Test(other, math.cross(sourceForward, destinationForward)))
                return false;

            return true;
        }

        public bool Test(float radius, float3 center)
        {
            float3 position = (min + max) * 0.5f;
            center -= position;
            center = math.mul(math.inverse(rotation), center);
            center += position;

            return math.all(center > min - radius) && math.all(center < max + radius);
        }

        public float3 ClosestPoint(in float3 point)
        {
            float3 result = center,
                distance = point - result,
                extents = this.extents,
                direction = right;
            
            result += math.clamp(math.dot(distance, direction), -extents.x, extents.x) * direction;

            direction = up;
            result += math.clamp(math.dot(distance, direction), -extents.y, extents.y) * direction;
            
            direction = forward;
            result += math.clamp(math.dot(distance, direction), -extents.z, extents.z) * direction;

            return result;
        }
        
        public bool Raycast(in float3 origin, in float3 direction, ref float distance, out float3 point, out float3 normal)
        {
            float3 center = this.center;
            quaternion inverRot = math.inverse(rotation);

            if (!Math.IntersectRayAABB(math.mul(inverRot, origin - center), math.mul(inverRot, direction), extents, ref distance, out point, out normal))
                return false;

            point = math.mul(rotation, point + center);
            normal = math.mul(rotation, normal);

            return true;
        }


        /*public bool IsIntersects(float radius, float3 start, float3 end)
        {
            float3 leftUpForward, leftDownForward, rightUpForward, rightDownForward,
                leftUpBackward, leftDownBackward, rightUpBackward, rightDownBackward;

            GetCorners(
                out leftUpForward,
                out leftDownForward,
                out rightUpForward,
                out rightDownForward,
                out leftUpBackward,
                out leftDownBackward,
                out rightUpBackward,
                out rightDownBackward);

            return Math.IsIntersect(radius, start, end, leftUpForward) ||
                Math.IsIntersect(radius, start, end, leftDownForward) ||
                Math.IsIntersect(radius, start, end, rightUpForward) ||
                Math.IsIntersect(radius, start, end, rightDownForward) ||
                Math.IsIntersect(radius, start, end, leftUpBackward) ||
                Math.IsIntersect(radius, start, end, leftDownBackward) ||
                Math.IsIntersect(radius, start, end, rightUpBackward) ||
                Math.IsIntersect(radius, start, end, rightDownBackward);
        }*/
    }
}
