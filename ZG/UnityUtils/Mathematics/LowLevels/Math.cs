using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace ZG.Mathematics
{
    public static class Math
    {
        public const float DOT_EPSILON = 1e-4f;
        public const float DISTANCE_EPSILON = 1e-4f;
        public const float DISTANCE_SQ_EPSILON = DISTANCE_EPSILON * DISTANCE_EPSILON;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetHighestBit(byte value)
        {
            return 32 - math.lzcnt((uint)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetLowerstBit(byte value)
        {
            return GetHighestBit((byte)((value - 1) ^ value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetHighestBit(uint value)
        {
            return 32 - math.lzcnt(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetHighestBit(int value)
        {
            return 32 - math.lzcnt(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetLowerstBit(uint value)
        {
            return GetHighestBit((value - 1) ^ value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetLowerstBit(int value)
        {
            return GetHighestBit((value - 1) ^ value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Approximately(this float3 x, float3 y)
        {
            return math.distancesq(x, y) < DISTANCE_SQ_EPSILON;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Approximately(this quaternion x, quaternion y)
        {
            return math.dot(x, y) + DOT_EPSILON > 1.0f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Approximately(this RigidTransform x, RigidTransform y)
        {
            return Approximately(x.rot, y.rot) && Approximately(x.pos, y.pos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Round(float value, int decimals)
        {
            float number = math.pow(10.0f, decimals);
            return math.round(value * number) / number;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Round(float3 value, int decimals)
        {
            float number = math.pow(10.0f, decimals);
            return math.round(value * number) / number;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Repeat(float t, float length)
        {
            return math.clamp(t - math.floor(t / length) * length, 0.0f, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Max(this float3 value)
        {
            return math.max(math.max(value.x, value.y), value.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IntersectRaySphere(float3 origin, float3 direction, float3 center, float radiusSquare, out float t0, out float t1)
        {
            t0 = 0.0f;
            t1 = 0.0f;

            float3 oc = origin - center; float dotOCD = math.dot(direction, oc);
            if (dotOCD > 0)
                return false;

            float dotOC = math.dot(oc, oc);
            float discriminant = dotOCD * dotOCD - dotOC + radiusSquare;

            if (discriminant < 0.0f)
                return false;

            discriminant = math.sqrt(discriminant);
            t0 = -dotOCD - discriminant;
            t1 = -dotOCD + discriminant;
            if (t0 < 0.0f)
                t0 = t1;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IntersectRayAABB(float3 origin, float3 direction, float3 dimension, ref float distance, out float3 point, out float3 normal)
        {
            float3 minPos = -dimension, maxPos = dimension, maxT = -1.0f;
            bool isInside = true;

            point = float3.zero;
            for (int i = 0; i < 3; i++)
            {
                if (origin[i] < minPos[i])
                {
                    point[i] = minPos[i];
                    isInside = false;
                    if (math.abs(direction[i]) > math.FLT_MIN_NORMAL)
                        maxT[i] = (minPos[i] - origin[i]) / direction[i];
                }
                else if (origin[i] > maxPos[i])
                {
                    point[i] = maxPos[i];
                    isInside = false;
                    if (math.abs(direction[i]) > math.FLT_MIN_NORMAL)
                        maxT[i] = (maxPos[i] - origin[i]) / direction[i];
                }
            }

            // Ray origin inside bounding box
            if (isInside)
            {
                point = origin;
                distance = 0;
                normal = -direction;

                return true;
            }

            // Get largest of the maxT's for final choice of intersection
            int whichPlane = 0;
            if (maxT[1] > maxT[whichPlane])
                whichPlane = 1;

            if (maxT[2] > maxT[whichPlane])
                whichPlane = 2;

            normal = float3.zero;

            //Ray distance large than ray cast ditance
            if (maxT[whichPlane] > distance)
                return false;

            // Check final candidate actually inside box
            for (int i = 0; i < 3; i++)
            {
                if (i != whichPlane)
                {
                    point[i] = origin[i] + maxT[whichPlane] * direction[i];
                    if (point[i] < minPos[i] - math.FLT_MIN_NORMAL || point[i] > maxPos[i] + math.FLT_MIN_NORMAL)
                        return false;

                    //	if (hitInfo.point[i] < minPos[i] || hitInfo.point[i] > maxPos[i])
                    //	return false;
                }
            }

            distance = maxT[whichPlane];
            normal[whichPlane] = (point[whichPlane] > 0) ? 1 : -1;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float PlaneDistanceToPoint(in float4 plane, in float3 point)
        {
            return math.dot(plane.xyz, point) + plane.w;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DeltaAngle(float value)
        {
            float result = Repeat(value, math.PI * 2.0f);
            if (result > math.PI)
                result -= math.PI * 2.0f;

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 DeltaAngle(float3 value)
        {
            return math.float3(DeltaAngle(value.x), DeltaAngle(value.y), DeltaAngle(value.z));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DeltaAngle(float source, float destination)
        {
            return DeltaAngle(destination - source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Angle(float3 from, float3 to)
        {
            float num = math.sqrt(math.lengthsq(from) * math.lengthsq(to));
            if (num > math.FLT_MIN_NORMAL)
                return math.acos(math.clamp(math.dot(from, to) / num, -1.0f, 1.0f));

            return 0.0f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SignedAngle(float2 from, float2 to)
        {
            float result = math.acos(math.clamp(math.dot(from, to), -1.0f, 1.0f));
            float sign = from.x * to.y - from.y * to.x;
            result *= sign < 0.0f ? -1.0f : 1.0f;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SignedAngle(float3 from, float3 to, float3 axis)
        {
            float result = math.acos(math.clamp(math.dot(from, to), -1.0f, 1.0f));
            //result *= math.sign(math.dot(axis, math.cross(from, to)));
            //fix: 
            float sign = math.dot(axis, math.cross(from, to));
            result *= sign < 0.0f ? -1.0f : 1.0f;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float LerpAngle(float a, float b, float t)
        {
            return a + DeltaAngle(a, b) * t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RigidTransform Lerp(RigidTransform x, RigidTransform y, float t)
        {
            return math.RigidTransform(math.nlerp(x.rot, y.rot, t), math.lerp(x.pos, y.pos, t));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Project(this float3 vector, float3 onNormal)
        {
            return onNormal * math.dot(vector, onNormal);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ProjectSafe(this float3 vector, float3 onNormal)
        {
            float dot = math.lengthsq(onNormal);
            if (dot > math.FLT_MIN_NORMAL)
                return onNormal * (math.dot(vector, onNormal) / dot);

            return float3.zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ProjectOnPlane(this float3 vector, float3 planeNormal)
        {
            return vector - Project(vector, planeNormal);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ProjectOnPlaneSafe(this float3 vector, float3 planeNormal)
        {
            return vector - ProjectSafe(vector, planeNormal);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 MoveTowards(float3 current, float3 target, float maxDistanceDelta)
        {
            maxDistanceDelta = math.max(maxDistanceDelta, math.FLT_MIN_NORMAL);

            float3 distance = target - current;
            float magnitude = math.length(distance);
            if (magnitude > maxDistanceDelta)
                return current + distance / magnitude * maxDistanceDelta;

            return target;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion RotateTowards(this quaternion from, quaternion to, float maxRadiansDelta)
        {
            float angle = Angle(from, to);
            if (angle < math.FLT_MIN_NORMAL)
                return to;

            return math.slerp(from, to, math.min(1f, maxRadiansDelta / angle));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Angle(this quaternion x, quaternion y)
        {
            float dot = math.dot(x, y);

            return dot < 1.0f ? (math.acos(math.min(math.abs(dot), 1f)) * 2f) : 0f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetEulerY(this quaternion rotation)
        {
            float3 forward = math.forward(rotation);

            return math.atan2(forward.x, forward.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetEulerZ(this quaternion rotation)
        {
            float3 forward = math.forward(rotation);

            return math.atan2(forward.y, forward.x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ToAngular(this quaternion q)
        {
            if (math.abs(q.value.w) > 1023.75f / 1024.0f)
                return float3.zero;

            var wSign = q.value.w >= 0.0f ? 1f : -1f;
            var angle = math.acos(wSign * q.value.w);
            var gain = wSign * 2f * angle / math.sin(angle);

            return math.float3(q.value.xyz * gain);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 FromToRotationAxis(float3 from, float3 to)
        {
            float3 axis = math.normalizesafe(math.cross(from, to), float3.zero);
            if (math.all(axis == float3.zero))
                return float3.zero;

            return axis * math.acos(math.clamp(math.dot(from, to), -1f, 1f));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion FromToRotation(float3 from, float3 to)
        {
            float3 axis = math.normalizesafe(math.cross(from, to), float3.zero);
            if (math.all(axis == float3.zero))
                return quaternion.identity;

            return quaternion.AxisAngle(axis, math.acos(math.clamp(math.dot(from, to), -1f, 1f)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Transform(
            quaternion rotation,
            float3 scale,
            float3 position,
            ref float3 min,
            ref float3 max)
        {
            float3 absAxisX = math.abs(math.mul(rotation, new float3(1.0f, 0.0f, 0.0f))),
                absAxisY = math.abs(math.mul(rotation, new float3(0.0f, 1.0f, 0.0f))),
                absAxisZ = math.abs(math.mul(rotation, new float3(0.0f, 0.0f, 1.0f))),
                size = max - min,
                worldExtents = (absAxisX * size.x + absAxisY * size.y + absAxisZ * size.z) * scale * 0.5f,
                worldPosition = math.mul(rotation, (min + max) * 0.5f) + position;

            min = worldPosition - worldExtents;
            max = worldPosition + worldExtents;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InverseTransform(
            quaternion rotation,
            float3 scale,
            float3 position,
            ref float3 min,
            ref float3 max)
        {
            min -= position;
            max -= position;

            float3 center = (min + max) * 0.5f, extents = (max - min) / scale * 0.5f;
            min = center - extents;
            max = center + extents;

            Transform(math.inverse(rotation), 1.0f, 0.0f, ref min, ref max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SmoothDamp(float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed, float deltaTime)
        {
            float num = 2f / smoothTime;
            float num2 = num * deltaTime;
            float num3 = 1f / (1f + num2 + 0.48f * num2 * num2 + 0.235f * num2 * num2 * num2);
            float value = current - target;
            float num4 = target;
            float num5 = maxSpeed * smoothTime;
            value = math.clamp(value, 0.0f - num5, num5);
            target = current - value;
            float num6 = (currentVelocity + num * value) * deltaTime;
            currentVelocity = (currentVelocity - num * num6) * num3;
            float num7 = target + (value + num6) * num3;
            if (num4 - current > 0f == num7 > num4)
            {
                num7 = num4;
                currentVelocity = (num7 - num4) / deltaTime;
            }
            return num7;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double SmoothDamp(double current, double target, ref double currentVelocity, double smoothTime, double maxSpeed, double deltaTime)
        {
            double num = 2 / smoothTime;
            double num2 = num * deltaTime;
            double num3 = 1 / (1 + num2 + 0.48 * num2 * num2 + 0.235 * num2 * num2 * num2);
            double value = current - target;
            double num4 = target;
            double num5 = maxSpeed * smoothTime;
            value = math.clamp(value, 0.0f - num5, num5);
            target = current - value;
            double num6 = (currentVelocity + num * value) * deltaTime;
            currentVelocity = (currentVelocity - num * num6) * num3;
            double num7 = target + (value + num6) * num3;
            if (num4 - current > 0f == num7 > num4)
            {
                num7 = num4;
                currentVelocity = (num7 - num4) / deltaTime;
            }
            return num7;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SmoothDampAngle(float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed, float deltaTime)
        {
            target = current + DeltaAngle(current, target);
            return SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 SmoothDamp(float3 current, float3 target, ref float3 currentVelocity, float smoothTime, float maxSpeed, float deltaTime)
        {
            current.x = SmoothDamp(current.x, target.x, ref currentVelocity.x, smoothTime, maxSpeed, deltaTime);
            current.y = SmoothDamp(current.y, target.y, ref currentVelocity.y, smoothTime, maxSpeed, deltaTime);
            current.z = SmoothDamp(current.z, target.z, ref currentVelocity.z, smoothTime, maxSpeed, deltaTime);

            return current;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 CalculateLinearTarget(float3 targetPosition, float3 targetVelocity, float time)
        {
            return targetPosition + targetVelocity * time;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CalculateLinearTrajectory(
            float speed, 
            float3 position, 
            float3 targetPosition, 
            float3 targetDirection, 
            float targetVelocity, 
            out float3 hitPoint)
        {
            float3 distance = position - targetPosition;
            float theta = Angle(distance, targetDirection),
                lengthsq = math.lengthsq(distance),
                a = 1 - math.pow((speed / targetVelocity), 2),
                b = -(2 * math.sqrt(lengthsq) * math.cos(theta)),
                bb = b * b, 
                delta = bb - 4.0f * a * lengthsq;
            if (delta < 0.0f)
            {
                hitPoint = default;

                return false;
            }

            float c = 0.5f / a, d = -b * c, e = math.sqrt(bb - 4 * a * lengthsq) * c, fraction = math.min(d + e, d - e);

            hitPoint = targetPosition + targetDirection * fraction;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 CalculateParabolaAngleAndTime(float speed, float gravity, float distance, float height)
        {
            if (gravity > 0.0f)
            {
                float sqrSpeed = speed * speed,
                    gravityDistance = gravity * distance,
                    delta = sqrSpeed * sqrSpeed - gravity * (gravityDistance * distance + 2 * height * sqrSpeed);
                if (delta < 0.0f)
                    return 0.0f;

                sqrSpeed /= gravityDistance;
                delta = math.sqrt(delta) / gravityDistance;
                float alpha = math.atan(sqrSpeed + delta);
                float beta = math.atan(sqrSpeed - delta);
                float theta = math.min(alpha, beta);
                float time = distance / (speed * math.cos(theta));

                return new float2(theta, time);
            }
            else
            {
                float theta = math.atan(height / distance);
                float time = speed > 0.0f ? (height / math.sin(theta)) / speed : 0.0f;
                return new float2(theta, time);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 CalculateParabolaAngleAndTime(float speed, float gravity, float3 distance, ref float3 direction)
        {
            if (math.abs(distance.x) < math.FLT_MIN_NORMAL)
                return float2.zero;

            quaternion rotation = quaternion.AxisAngle(math.up(), -math.atan2(distance.x, distance.z));
            float3 point = math.mul(rotation, distance);
            float2 result = CalculateParabolaAngleAndTime(speed, gravity, point.z, point.y);

            if (result.y > 0.0f)
                direction = math.normalize(new float3(distance.x, math.abs(point.z) * math.tan(result.x), distance.z));
            else
                direction = math.normalizesafe(distance, direction);

            return result;
        }

        public static bool CalculateParabolaTrajectory(
            float diff, 
            float accuracy,
            float gravity,
            float speed,
            float3 targetVelocity,
            float3 targetPosition,
            float3 position,
            float3 hitPoint,
            out float3 distance)
        {
            float3 direction = math.float3(hitPoint.x, position.y, hitPoint.z) - position;
            quaternion rotation = FromToRotation(direction, math.float3(0.0f, 0.0f, 1.0f));
            float3 localHitPoint = math.rotate(rotation, hitPoint - position);
            float2 angleAndTime = CalculateParabolaAngleAndTime(speed, gravity, localHitPoint.z, localHitPoint.y);
            if (angleAndTime.y > math.FLT_MIN_NORMAL)
            {
                float3 newHitPoint = CalculateLinearTarget(targetPosition, targetVelocity, angleAndTime.y);
                float newDiff = math.lengthsq(newHitPoint - hitPoint);
                if (newDiff < accuracy)
                {
                    distance = math.rotate(math.inverse(rotation), math.float3(0.0f, math.tan(angleAndTime.x) * localHitPoint.z, localHitPoint.z));

                    return true;
                }

                if (newDiff < diff)
                    return CalculateParabolaTrajectory(
                        newDiff, 
                        accuracy,
                        gravity,
                        speed,
                        targetVelocity,
                        targetPosition,
                        position,
                        newHitPoint,
                        out distance);
            }

            distance = float3.zero;

            return false;
        }

        public static bool CalculateParabolaTrajectory(
            float accuracy,
            float gravity,
            float speed,
            float targetVelocity, 
            float3 targetDirection,
            float3 targetPosition,
            float3 position,
            out float3 distance)
        {
            if (!CalculateLinearTrajectory(speed, position, targetPosition, targetDirection, targetVelocity, out var hitPoint))
            {
                distance = float3.zero;

                return false;
            }

            return CalculateParabolaTrajectory(
                float.MaxValue,
                accuracy,
                gravity,
                speed,
                targetVelocity * targetDirection,
                targetPosition,
                position,
                hitPoint,
                out distance);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Swap<T>(ref T x, ref T y)
        {
            var temp = x;
            x = y;
            y = temp;
        }

    }
}