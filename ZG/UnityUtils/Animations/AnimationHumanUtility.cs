#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEditor;

namespace ZG
{
    public static class AnimationHumanUtility
    {
        [Flags]
        public enum BakeFlag
        {
            LockRootRotation = 0x01,
            LockRootHeightY = 0x02,
            LockRootPositionXZ = 0x04, 
        }

        public struct PropertyID : IEquatable<PropertyID>
        {
            public Type type;
            public string path;
            public string propertyName;

            public PropertyID(Type type, string path, string propertyName)
            {
                this.type = type;
                this.path = path;
                this.propertyName = propertyName;
            }

            public bool Equals(PropertyID other)
            {
                return type == other.type && path == other.path && propertyName == other.propertyName;
            }

            public static implicit operator PropertyID(EditorCurveBinding curveBinding)
            {
                return new PropertyID(curveBinding.type, curveBinding.path, curveBinding.propertyName);
            }
        }

        public static readonly string[] JointPropertyNames = new string[]
        {
            "m_LocalPosition.x",
            "m_LocalPosition.y",
            "m_LocalPosition.z",

            "m_LocalRotation.x",
            "m_LocalRotation.y",
            "m_LocalRotation.z",
            "m_LocalRotation.w",
        };

        public static readonly string[] AnimatorSuffixNames = new string[]
        {
            "T.x",
            "T.y",
            "T.z",

            "Q.x",
            "Q.y",
            "Q.z",
            "Q.w",
        };

        public static BakeFlag GetBakeFlag(AnimationClip animationClip, out float heightOffset)
        {
            heightOffset = 0.0f;

            var path = AssetDatabase.GetAssetPath(animationClip);
            var modelImporter = AssetImporter.GetAtPath(path) as ModelImporter;
            var clipAnimations = modelImporter == null ? null : modelImporter.clipAnimations;
            if (clipAnimations != null)
            {
                BakeFlag bakeFlag = 0;
                string name = animationClip.name;
                foreach (var clipAnimation in clipAnimations)
                {
                    if (clipAnimation.name == name)
                    {
                        heightOffset = clipAnimation.heightOffset;

                        if (clipAnimation.lockRootRotation)
                            bakeFlag |= BakeFlag.LockRootRotation;

                        if (clipAnimation.lockRootHeightY)
                            bakeFlag |= BakeFlag.LockRootHeightY;

                        if (clipAnimation.lockRootPositionXZ)
                            bakeFlag |= BakeFlag.LockRootPositionXZ;

                        return bakeFlag;
                    }
                }
            }

            return 0;
        }

        public static string PathToName(string path)
        {
            int index = path.LastIndexOf('/');
            return index == -1 ? path : path.Substring(index + 1);
        }

        public static Dictionary<PropertyID, int> CreateSampleIndices(string[] jointPaths, Dictionary<string, string> humanBoneNames)
        {
            var sampleIndices = new Dictionary<PropertyID, int>();
            int i, j, index = 0, numJointPaths = jointPaths.Length;
            string jointPath, humanBoneName;
            for (i = 0; i < numJointPaths; ++i)
            {
                jointPath = jointPaths[i];

                for (j = 0; j < 7; ++j)
                    sampleIndices.Add(new PropertyID(typeof(Transform), jointPath, JointPropertyNames[j]), index++);

                if (humanBoneNames.TryGetValue(PathToName(jointPath), out humanBoneName))
                {
                    index -= 7;
                    for (j = 0; j < 7; ++j)
                        sampleIndices.Add(new PropertyID(typeof(Animator), string.Empty, humanBoneName + AnimatorSuffixNames[j]), index++);
                }
            }

            return sampleIndices;
        }

        public static bool Sample(
            AnimationClip animationClip,
            float time,
            Dictionary<PropertyID, int> sampleIndices,
            HashSet<PropertyID> outPropertyIDs,
            ref NativeArray<float> outSamples)
        {
            bool result = false;
            int index;
            PropertyID propertyID;
            AnimationCurve animationCurve;
            var cureBindingArray = AnimationUtility.GetCurveBindings(animationClip);
            foreach (var cureBinding in cureBindingArray)
            {
                animationCurve = AnimationUtility.GetEditorCurve(animationClip, cureBinding);

                propertyID = cureBinding;
                if (sampleIndices.TryGetValue(propertyID, out index))
                {
                    outSamples[index] = animationCurve.Evaluate(time);

                    outPropertyIDs.Add(propertyID);

                    result = true;
                }
            }

            return result;
        }

        public static void Sample(
            AnimationClip animationClip,
            float time,
            ref HumanPose humanPose,
            HashSet<PropertyID> outPropertyIDs)
        {
            var muscleNames = HumanTrait.MuscleName;

            var cureBindingArray = AnimationUtility.GetCurveBindings(animationClip);
            int muscleIndex;

            AnimationCurve animationCurve;
            foreach (var cureBinding in cureBindingArray)
            {
                if (cureBinding.type != typeof(Animator))
                    continue;

                if (!outPropertyIDs.Add(cureBinding))
                    continue;

                animationCurve = AnimationUtility.GetEditorCurve(animationClip, cureBinding);
                switch (cureBinding.propertyName)
                {
                    case "RootT.x":
                        humanPose.bodyPosition.x = animationCurve.Evaluate(time);
                        break;
                    case "RootT.y":
                        humanPose.bodyPosition.y = animationCurve.Evaluate(time);
                        break;
                    case "RootT.z":
                        humanPose.bodyPosition.z = animationCurve.Evaluate(time);
                        break;
                    case "RootQ.x":
                        humanPose.bodyRotation.x = animationCurve.Evaluate(time);
                        break;
                    case "RootQ.y":
                        humanPose.bodyRotation.y = animationCurve.Evaluate(time);
                        break;
                    case "RootQ.z":
                        humanPose.bodyRotation.z = animationCurve.Evaluate(time);
                        break;
                    case "RootQ.w":
                        humanPose.bodyRotation.w = animationCurve.Evaluate(time);
                        break;
                    default:
                        muscleIndex = Array.IndexOf(muscleNames, cureBinding.propertyName);

                        if (muscleIndex == -1)
                        {
                            muscleIndex = Array.IndexOf(muscleNames, cureBinding.propertyName.Replace("Hand", string.Empty).Replace(".", " "));
                            if (muscleIndex == -1)
                            {
                                Debug.LogWarning($"Missing muscle {cureBinding.propertyName}");

                                continue;
                            }
                        }

                        humanPose.muscles[muscleIndex] = animationCurve.Evaluate(time);
                        break;
                }
            }
        }

        public static Matrix4x4 GetLocalToRootMatrix(int index, int[] parentIndices, in NativeArray<float> values)
        {
            int offset;
            var matrix = Matrix4x4.identity;
            while(index != -1)
            {
                offset = index * 7;
                matrix = Matrix4x4.TRS(
                    new Vector3(
                        values[offset + 0],
                        values[offset + 1],
                        values[offset + 2]),
                    new Quaternion(
                        values[offset + 3],
                        values[offset + 4],
                        values[offset + 5],
                        values[offset + 6]),
                    Vector3.one) * matrix;

                index = parentIndices[index];
            }

            return matrix;
        }

        public static AnimationClip ResampleToPose(
            this AnimationClip animationClip,
            HumanPoseHandler humanPoseHandler,
            string[] jointPaths,
            int[] parentIndices,
            Dictionary<PropertyID, int> sampleIndices,
            BakeFlag bakeFlag = 0, 
            int translationDecimals = 3,
            int rotationDecimals = 5)
        {
            bakeFlag = GetBakeFlag(animationClip, out float heightOffset) | bakeFlag;

            PropertyID propertyID;
            Dictionary<PropertyID, int> animatorSampleIndices = null, legacySampleIndices = null;
            if (sampleIndices != null)
            {
                foreach (var pair in sampleIndices)
                {
                    propertyID = pair.Key;
                    if (propertyID.type == typeof(Animator))
                    {
                        if (animatorSampleIndices == null)
                            animatorSampleIndices = new Dictionary<PropertyID, int>();

                        animatorSampleIndices.Add(propertyID, pair.Value);
                    }
                    else
                    {
                        if (legacySampleIndices == null)
                            legacySampleIndices = new Dictionary<PropertyID, int>();

                        legacySampleIndices.Add(propertyID, pair.Value);
                    }
                }
            }

            var humanPose = new HumanPose();
            humanPoseHandler.GetInternalHumanPose(ref humanPose);

            var animationClipSettings = AnimationUtility.GetAnimationClipSettings(animationClip);
            int numJointPaths = jointPaths.Length, numTransformValues = numJointPaths * 7;
            var sourceBoneTransforms = new NativeArray<float>(numTransformValues, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            humanPoseHandler.GetInternalAvatarPose(sourceBoneTransforms);

            int leftFootIndex = -1, rightFootIndex = -1, index;
            float footY = 0.0f;
            Vector3 originalPosition = /*Quaternion.Inverse(humanPose.bodyRotation) * */humanPose.bodyPosition;
            Quaternion originalRotation = humanPose.bodyRotation;
            Matrix4x4 matrix;
            var humanBoneNames = HumanTrait.BoneName;
            /*if(animationClipSettings.keepOriginalOrientation || animationClipSettings.keepOriginalPositionY || animationClipSettings.keepOriginalPositionXZ)
            {
                if (sampleIndices.TryGetValue(new PropertyID(typeof(Animator), string.Empty, humanBoneNames[(int)HumanBodyBones.Hips] + AnimatorSuffixNames[0]), out index))
                {
                    matrix = GetLocalToRootMatrix(index / 7, parentIndices, sourceBoneTransforms);

                    var position = matrix.GetColumn(3);
                    originalPosition.y -= position.y;

                    originalRotation = originalRotation * Quaternion.Inverse(matrix.rotation);
                }
                else
                {
                    originalPosition = Vector3.zero;
                    originalRotation = Quaternion.identity;
                }
            }
            else
            {
                originalPosition = Vector3.zero;
                originalRotation = Quaternion.identity;
            }*/

            if (animationClipSettings.heightFromFeet)
            {
                footY = float.MaxValue;
                if (sampleIndices.TryGetValue(new PropertyID(typeof(Animator), string.Empty, humanBoneNames[(int)HumanBodyBones.LeftFoot] + AnimatorSuffixNames[0]), out index))
                {
                    leftFootIndex = index / 7;

                    //if ((bakeFlag & BakeFlag.LockRootHeightY) == BakeFlag.LockRootHeightY)
                    {
                        matrix = GetLocalToRootMatrix(leftFootIndex, parentIndices, sourceBoneTransforms);

                        if (footY > matrix.m13)
                            footY = matrix.m13;
                    }
                }

                if (sampleIndices.TryGetValue(new PropertyID(typeof(Animator), string.Empty, humanBoneNames[(int)HumanBodyBones.RightFoot] + AnimatorSuffixNames[0]), out index))
                {
                    rightFootIndex = index / 7;
                    //if ((bakeFlag & BakeFlag.LockRootHeightY) == BakeFlag.LockRootHeightY)
                    {
                        matrix = GetLocalToRootMatrix(rightFootIndex, parentIndices, sourceBoneTransforms);

                        if (footY > matrix.m13)
                            footY = matrix.m13;
                    }
                }
            }

            int i, j, offset;
            float legnth = animationClip.length, delta = 1.0f / animationClip.frameRate;
            Quaternion rotation;
            NativeArray<float> destinationBoneTransforms = new NativeArray<float>(sourceBoneTransforms, Allocator.Temp);
            HashSet<PropertyID> propertyIDs = new HashSet<PropertyID>(), propertyIDsTemp = new HashSet<PropertyID>();
            var animationCurves = new Dictionary<(int, int), AnimationCurve>();
            for (float time = 0.0f; time <= legnth; time += delta)
            {
                propertyIDsTemp.Clear();
                if (animatorSampleIndices != null && Sample(animationClip, time, animatorSampleIndices, propertyIDsTemp, ref destinationBoneTransforms))
                {
                    humanPoseHandler.SetInternalAvatarPose(destinationBoneTransforms);

                    humanPoseHandler.GetInternalHumanPose(ref humanPose);
                }

                Sample(animationClip, time, ref humanPose, propertyIDsTemp);

                if (propertyIDsTemp.Count > 0)
                {
                    if ((bakeFlag & BakeFlag.LockRootRotation) != BakeFlag.LockRootRotation)
                    {
                        if (animationClipSettings.keepOriginalOrientation)
                            humanPose.bodyRotation = originalRotation;
                        else
                            humanPose.bodyRotation = Quaternion.identity;
                    }

                    if (animationClipSettings.heightFromFeet)
                        humanPose.bodyPosition.y -= footY;
                    else if ((bakeFlag & BakeFlag.LockRootHeightY) != BakeFlag.LockRootHeightY)
                    {
                        if (animationClipSettings.keepOriginalPositionY)
                            humanPose.bodyPosition.y = originalPosition.y;
                        /*else if(animationClipSettings.heightFromFeet)
                        {
                            footY = float.MaxValue;
                            if (leftFootIndex != -1)
                            {
                                matrix = GetLocalToRootMatrix(leftFootIndex, parentIndices, destinationBoneTransforms);

                                if (footY > matrix.m13)
                                    footY = matrix.m13;
                            }

                            if (rightFootIndex != -1)
                            {
                                matrix = GetLocalToRootMatrix(rightFootIndex, parentIndices, destinationBoneTransforms);

                                if (footY > matrix.m13)
                                    footY = matrix.m13;
                            }

                            humanPose.bodyPosition.y -= footY;
                        }*/
                        else
                            humanPose.bodyPosition.y = 0.0f;
                    }

                    humanPose.bodyPosition.y -= heightOffset;

                    if ((bakeFlag & BakeFlag.LockRootPositionXZ) != BakeFlag.LockRootPositionXZ)
                    {
                        if (animationClipSettings.keepOriginalPositionXZ)
                        {
                            //var position = humanPose.bodyRotation * originalPosition;

                            humanPose.bodyPosition.x = originalPosition.x; //position.x;
                            humanPose.bodyPosition.z = originalPosition.z;// position.z;
                        }
                        else
                        {
                            humanPose.bodyPosition.x = 0.0f;
                            humanPose.bodyPosition.z = 0.0f;
                        }
                    }

                    rotation = Quaternion.Euler(0.0f, animationClipSettings.orientationOffsetY, 0.0f);

                    humanPose.bodyRotation = rotation * humanPose.bodyRotation;

                    humanPose.bodyPosition = rotation * humanPose.bodyPosition;

                    humanPoseHandler.SetInternalHumanPose(ref humanPose);

                    humanPoseHandler.GetInternalAvatarPose(destinationBoneTransforms);
                }

                if (legacySampleIndices != null && Sample(animationClip, time, legacySampleIndices, propertyIDsTemp, ref destinationBoneTransforms))
                {
                    humanPoseHandler.SetInternalAvatarPose(destinationBoneTransforms);

                    humanPoseHandler.GetInternalHumanPose(ref humanPose);
                }

                /*if(animationClipSettings.heightFromFeet)
                {
                    minY = destinationBoneTransforms[1];
                    minPosition = new Vector3(destinationBoneTransforms[0], minY, destinationBoneTransforms[2]);
                    for (i = 8; i < numTransformValues; i += 7)
                    {
                        minY = destinationBoneTransforms[i];

                        if(minY < minPosition.y)
                            minPosition = new Vector3(destinationBoneTransforms[i - 1], minY, destinationBoneTransforms[i + 1]);
                    }

                    humanPose.bodyPosition.y -= minPosition.y;
                }*/

                propertyIDs.UnionWith(propertyIDsTemp);

                for (i = 0; i < numJointPaths; ++i)
                {
                    offset = i * 7;

                    for (j = 0; j < 3; ++j)
                    {
                        index = offset + j;
                        if (Math.Round(sourceBoneTransforms[index], translationDecimals) != Math.Round(destinationBoneTransforms[index], translationDecimals))
                            break;
                    }

                    if (j < 3)
                    {
                        for (j = 0; j < 3; ++j)
                        {
                            index = offset + j;

                            __AddKey(animationCurves, i, j, sourceBoneTransforms[index], destinationBoneTransforms[index], time);
                        }
                    }

                    for (j = 3; j < 7; ++j)
                    {
                        index = offset + j;

                        if (Math.Round(sourceBoneTransforms[index], rotationDecimals) != Math.Round(destinationBoneTransforms[index], rotationDecimals))
                            break;
                    }

                    if (j < 7)
                    {
                        for (j = 3; j < 7; ++j)
                        {
                            index = offset + j;

                            __AddKey(animationCurves, i, j, sourceBoneTransforms[index], destinationBoneTransforms[index], time);
                        }
                    }
                }

                sourceBoneTransforms.CopyFrom(destinationBoneTransforms);
            }

            sourceBoneTransforms.Dispose();
            destinationBoneTransforms.Dispose();

            var result = UnityEngine.Object.Instantiate(animationClip);

            var cureBindingArray = AnimationUtility.GetCurveBindings(result);
            foreach (var cureBinding in cureBindingArray)
            {
                if (propertyIDs.Contains(cureBinding))
                    AnimationUtility.SetEditorCurve(result, cureBinding, null);
            }

            (int, int) jointProperty;
            foreach (var pair in animationCurves)
            {
                jointProperty = pair.Key;

                UnityEngine.Assertions.Assert.IsNotNull(pair.Value);

                AnimationUtility.SetEditorCurve(
                    result,
                    EditorCurveBinding.FloatCurve(jointPaths[jointProperty.Item1], typeof(Transform), JointPropertyNames[jointProperty.Item2]),
                    pair.Value);
            }

            return result;
        }

        private static void __AddKey(
            Dictionary<(int, int), AnimationCurve> animationCurves,
            int jointIndex,
            int curveType,
            float sourceValue,
            float destinationValue,
            float time)
        {
            if (!animationCurves.TryGetValue((jointIndex, curveType), out var animationCurve))
            {
                animationCurve = new AnimationCurve();

                if (time > Mathf.Epsilon)
                    animationCurve.AddKey(0.0f, sourceValue);

                animationCurves[(jointIndex, curveType)] = animationCurve;

                //AnimationUtility.SetEditorCurve(result, EditorCurveBinding.FloatCurve(jointPaths[i], typeof(Transform), "m_LocalPosition.x"), animationCurve);
            }

            animationCurve.AddKey(time, destinationValue);
        }
    }
}
#endif