using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ZG
{
    //[Serializable]
    public struct CallbackHandle : IEquatable<CallbackHandle>
    {
        public int index;
        public int version;

        public readonly static CallbackHandle Null = new CallbackHandle { index = -1, version = -1 };

        public bool Equals(CallbackHandle other)
        {
            return index == other.index && version == other.version;
        }

        public override int GetHashCode()
        {
            return index;
        }

        public override string ToString()
        {
            return "(index: " + index + ", version: " + version + ')';
        }
    }

        //[Serializable]
    public struct CallbackHandle<T> : IEquatable<CallbackHandle<T>>
    {
        public CallbackHandle value;

        public readonly static CallbackHandle<T> Null = new CallbackHandle<T> { value = CallbackHandle.Null };

        public bool Equals(CallbackHandle<T> other)
        {
            return value.Equals(other.value);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }
    }

    public static class CallbackManager<T> where T : Delegate
    {
        private struct Handler
        {
            public int version;
            public T value;
        }

        private static Pool<Handler> __handlers;

        public static bool IsVail(in CallbackHandle handle)
        {
            if (__handlers == null)
                return false;

            Handler handler;
            return __handlers.TryGetValue(handle.index, out handler) && handler.version == handle.version;
        }

        public static bool Unregister(in CallbackHandle handle)
        {
            if (__handlers == null)
                return false;

            if (!__handlers.TryGetValue(handle.index, out Handler handler) || handler.version != handle.version)
                return false;

            return __handlers.RemoveAt(handle.index);
        }

        public static CallbackHandle Register([NotNull]T value)
        {
            UnityEngine.Assertions.Assert.IsNotNull(value);

            if (__handlers == null)
                __handlers = new Pool<Handler>();

            CallbackHandle handle;
            handle.index = __handlers.nextIndex;
            __handlers.TryGetValue(handle.index, out Handler handler);
            handle.version = ++handler.version;

            handler.value = value;
            __handlers.Insert(handle.index, handler);

            return handle;
        }

        public static bool Combine([NotNull] T value, in CallbackHandle handle)
        {
            UnityEngine.Assertions.Assert.IsNotNull(value);

            if (__handlers == null || 
                !__handlers.TryGetValue(handle.index, out var handler) || 
                handler.version != handle.version)
                return false;

            handler.value = (T)Delegate.Combine(handler.value, value);
            __handlers.Insert(handle.index, handler);

            return true;
        }

        public static bool Invoke(in CallbackHandle handle, out T value)
        {
            value = default;

            if (__handlers == null)
                return false;

            if (!__handlers.TryGetValue(handle.index, out Handler handler) ||
                handler.version != handle.version)
                return false;

            value = handler.value;

            return true;
        }
    }

    public static class CallbackManager
    {
        public static bool IsVail(this in CallbackHandle handle) => CallbackManager<Action>.IsVail(handle);

        public static bool IsVail<T>(this in CallbackHandle<T> handle) => CallbackManager<Action<T>>.IsVail(handle.value);

        public static bool Unregister(this in CallbackHandle handle) => CallbackManager<Action>.Unregister(handle);

        public static bool Unregister<T>(this in CallbackHandle<T> handle) => CallbackManager<Action<T>>.Unregister(handle.value);

        public static CallbackHandle Register(this Action value) => CallbackManager<Action>.Register(value);

        public static bool Combine(this Action value, in CallbackHandle handle) => CallbackManager<Action>.Combine(value, handle);

        public static CallbackHandle<T> Register<T>(this Action<T> value)
        {
            CallbackHandle<T> handle;
            handle.value = CallbackManager<Action<T>>.Register(value);
            return handle;
        }

        public static void Combine<T>(this Action<T> value, in CallbackHandle handle) => CallbackManager<Action<T>>.Combine(value, handle);

        public static bool Invoke(this in CallbackHandle handle)
        {
            if (!CallbackManager<Action>.Invoke(handle, out Action value))
                return false;

            try
            {
                value();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }

            return true;
        }

        public static bool InvokeAndUnregister(this in CallbackHandle handle)
        {
            if (!CallbackManager<Action>.Invoke(handle, out Action value))
                return false;

            try
            {
                value();
            }
            catch(Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }

            return Unregister(handle);
        }

        public static bool Invoke<T>(this in CallbackHandle<T> handle, T parameter)
        {
            if (!CallbackManager<Action<T>>.Invoke(handle.value, out Action<T> value))
                return false;

            try
            {
                value(parameter);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }

            return true;
        }

        public static bool InvokeAndUnregister<T>(this in CallbackHandle<T> handle, T parameter)
        {
            if (!CallbackManager<Action<T>>.Invoke(handle.value, out Action<T> value))
                return false;

            try
            {
                value(parameter);

            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }

            return Unregister(handle);
        }
    }
}