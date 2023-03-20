using System;
using UnityEngine;
using System.Runtime.InteropServices;

namespace ZG
{
    public interface IAnimatorController
    {
        Component instance { get; }

        int GetParameterID(string name);

        float GetFloat(int id);

        int GetInteger(int id);

        void ResetTrigger(int id);

        void SetTrigger(int id);

        void SetBool(int id, bool value);

        void SetInteger(int id, int value);

        void SetFloat(int id, float value);

        void SetLayerWeight(int layer, float weight);

        void RestoreValues();

        void PlaybackValues();
    }

    public class AnimatorWrapper : MonoBehaviour, IAnimatorController
    {
        [StructLayout(LayoutKind.Explicit)]
        public struct ParameterValue
        {
            [FieldOffset(0)]
            public float floatValue;
            [FieldOffset(0)]
            public int intValue;
            [FieldOffset(0)]
            public bool boolValue;

            public void Restore(AnimatorControllerParameter parameter, Animator animator)
            {
                switch(parameter.type)
                {
                    case AnimatorControllerParameterType.Float:
                        floatValue = animator.GetFloat(parameter.nameHash);
                        break;
                    case AnimatorControllerParameterType.Int:
                        intValue = animator.GetInteger(parameter.nameHash);
                        break;
                    case AnimatorControllerParameterType.Bool:
                        boolValue = animator.GetBool(parameter.nameHash);
                        break;
                }
            }

            public void Playback(AnimatorControllerParameter parameter, Animator animator)
            {
                switch (parameter.type)
                {
                    case AnimatorControllerParameterType.Float:
                        animator.SetFloat(parameter.nameHash, floatValue);
                        break;
                    case AnimatorControllerParameterType.Int:
                        animator.SetInteger(parameter.nameHash, intValue);
                        break;
                    case AnimatorControllerParameterType.Bool:
                        animator.SetBool(parameter.nameHash, boolValue);
                        break;
                }
            }
        }

        public event Action onInit;

        private Animator __animator;
        private float[] __layerWeights;
        private AnimatorControllerParameter[] __parameters;
        private ParameterValue[] __values;

        public Component instance => __animator;

        public bool ContainsParameter(int id, AnimatorControllerParameterType type)
        {
            if (!__Init())
                return false;

            foreach (AnimatorControllerParameter parameter in __parameters)
            {
                if (parameter != null && parameter.nameHash == id)
                    return parameter.type == type;
            }

            return false;
        }

        public bool ContainsParameter(string name, AnimatorControllerParameterType type)
        {
            if (!__Init())
                return false;

            foreach (AnimatorControllerParameter parameter in __parameters)
            {
                if (parameter != null && parameter.name == name)
                    return parameter.type == type;
            }

            return false;
        }

        public AnimatorControllerParameterType ContainsParameter(string name)
        {
            if (!__Init())
                return 0;

            foreach (AnimatorControllerParameter parameter in __parameters)
            {
                if (parameter != null && parameter.name == name)
                    return parameter.type;
            }

            return 0;
        }

        public int GetParameterID(string name)
        {
            return ContainsParameter(name) != 0 ? Animator.StringToHash(name) : 0;
        }

        public float GetFloat(int id)
        {
            return __animator == null || !ContainsParameter(id, AnimatorControllerParameterType.Float) ? 0.0f : __animator.GetFloat(id);
        }

        public int GetInteger(int id)
        {
            return __animator == null || !ContainsParameter(id, AnimatorControllerParameterType.Int) ? 0 : __animator.GetInteger(id);
        }

        public void Do(Action action)
        {
            if (__animator == null)
                return;

            if (__Init())
                action();
            else
            {
                Action onInit = null;
                onInit = () =>
                {
                    this.onInit -= onInit;

                    action();
                };

                this.onInit += onInit;
            }
        }

        public void ResetTrigger(int id, bool isStrict)
        {
            if (isStrict ? ContainsParameter(id, AnimatorControllerParameterType.Trigger) : __animator != null)
                __animator.ResetTrigger(id);
        }

        public void ResetTrigger(int id) => ResetTrigger(id, false);

        public void ResetTrigger(string name, bool isStrict)
        {
            if (isStrict ? ContainsParameter(name, AnimatorControllerParameterType.Trigger) : __animator != null)
                __animator.ResetTrigger(name);
        }

        public void ResetTrigger(string name) => ResetTrigger(name, false);

        public void ResetTriggerOnInit(string name) => Do(() => __animator.ResetTrigger(name));

        public void SetTrigger(int id, bool isStrict)
        {
            if (isStrict ? ContainsParameter(id, AnimatorControllerParameterType.Trigger) : __animator != null)
                __animator.SetTrigger(id);
        }

        public void SetTrigger(int id) => SetTrigger(id, false);

        public void SetTrigger(string name, bool isStrict)
        {
            if (isStrict ? ContainsParameter(name, AnimatorControllerParameterType.Trigger) : __animator != null)
                __animator.SetTrigger(name);
        }

        public void SetTrigger(string name) => SetTrigger(name, false);

        public void SetTriggerOnInit(string name) => Do(() => __animator.SetTrigger(name));

        public void SetBool(int id, bool value, bool isStrict)
        {
            Do(() =>
            {
                if (!isStrict || ContainsParameter(id, AnimatorControllerParameterType.Bool))
                    __animator.SetBool(id, value);
            });
        }

        public void SetBool(int id, bool value) => SetBool(id, value, false);

        public void SetBool(string name, bool value, bool isStrict = false)
        {
            Do(() =>
            {
                if (!isStrict || ContainsParameter(name, AnimatorControllerParameterType.Bool))
                    __animator.SetBool(name, value);
            });
        }

        public void SetInteger(int id, int value, bool isStrict)
        {
            Do(() =>
            {
                if (!isStrict || ContainsParameter(id, AnimatorControllerParameterType.Int))
                    __animator.SetInteger(id, value);
            });
        }

        public void SetInteger(int id, int value) => SetInteger(id, value, false);

        public void SetInteger(string name, int value, bool isStrict = false)
        {
            Do(() =>
            {
                if (!isStrict || ContainsParameter(name, AnimatorControllerParameterType.Int))
                    __animator.SetInteger(name, value);
            });
        }

        public void SetFloat(int id, float value)
        {
            if (__animator != null)
                __animator.SetFloat(id, value);
        }

        public void SetFloat(string name, float value)
        {
            if (__animator != null)
                __animator.SetFloat(name, value);
        }

        public void SetLayerWeight(int layer, float weight)
        {
            Do(() => __animator.SetLayerWeight(layer, weight));
        }

        public void RestoreValues()
        {
            if (__animator == null)
                return;

            int numLayers = __animator.layerCount;
            Array.Resize(ref __layerWeights, numLayers);
            for (int i = 0; i < numLayers; ++i)
                __layerWeights[i] = __animator.GetLayerWeight(i);

            int numParameters = __parameters == null ? 0 : __parameters.Length;
            Array.Resize(ref __values, numParameters);
            for(int i = 0; i < numParameters; ++i)
                __values[i].Restore(__parameters[i], __animator);
        }

        public void PlaybackValues()
        {
            if (__animator == null)
                return;

            int numLayers = Mathf.Min(__layerWeights == null ? 0 : __layerWeights.Length, __animator.layerCount);
            for (int i = 0; i < numLayers; ++i)
                __animator.SetLayerWeight(i, __layerWeights[i]);

            int numParameters = Mathf.Min(__values == null ? 0 : __values.Length, __parameters == null ? 0 : __parameters.Length);
            for (int i = 0; i < numParameters; ++i)
                __values[i].Playback(__parameters[i], __animator);
        }

        protected void OnEnable()
        {
            if (!__Init() && __animator != null)
                StartCoroutine(__WaitToInit());
        }

        public bool __Init()
        {
            if (__parameters == null)
            {
                if (__animator == null)
                {
                    __animator = GetComponentInChildren<Animator>();
                    if(__animator != null)
                        __animator.logWarnings = false;
                }

                if (__animator != null && __animator.isInitialized)
                {
                    __parameters = __animator.parameters;

                    if (onInit != null)
                        onInit();
                }
                else
                    return false;
            }

            return true;
        }

        private System.Collections.IEnumerator __WaitToInit()
        {
            yield return null;

            if (!__animator.isInitialized)
            {
                __animator.Rebind();

                yield return null;
            }

            //UnityEngine.Assertions.Assert.IsTrue(__animator.isInitialized);

            //yield return new WaitUntil(() => __animator.isInitialized);

            if (__parameters == null)
            {
                __parameters = __animator.parameters;

                if (onInit != null)
                    onInit.Invoke();
            }
        }
    }
}