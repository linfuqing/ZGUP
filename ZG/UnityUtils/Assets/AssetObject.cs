using System.Collections;
using UnityEngine;

namespace ZG
{
    public class AssetObject : MonoBehaviour
    {
        internal AssetBundleLoader<GameObject> _loader;

        private void OnDestroy()
        {
            _loader.Dispose();
        }
    }

    public abstract class AssetObjectBase : MonoBehaviour
    {
        public enum Space
        {
            Local, 
            World
        }

        private AssetBundleLoader<GameObject> __loader;

        public event System.Action<GameObject> onCreated;

        public abstract Space space { get; }

        public abstract float time { get; }

        public abstract string fileName { get; }

        public abstract AssetManager assetManager { get; }

        public GameObject target
        {
            get;

            private set;
        }

        protected void OnEnable()
        {
            __loader = new AssetBundleLoader<GameObject>(fileName, name, assetManager);

            StartCoroutine(__Load());
        }

        protected void OnDisable()
        {
            if (target == null)
                __loader.Dispose();
            else
            {
                Destroy(target, time);

                target = null;
            }

            /*var assetManager = this.assetManager;
            if(assetManager != null)
                assetManager.Unload<GameObject>(__fileName, __assetName);*/
        }

        private IEnumerator __Load()
        {
            yield return __loader;

            var gameObject = __loader.value;
            if (gameObject == null)
            {
                Debug.LogError($"Asset Object {name} Load Fail.", this);

                yield break;
            }

            var transform = this.transform;
            gameObject = space == Space.World ? Instantiate(gameObject, transform.position, transform.rotation) : Instantiate(gameObject, transform);

            var target = gameObject.AddComponent<AssetObject>();
            target._loader = __loader;

            __loader = default;

            if (onCreated != null)
                onCreated(gameObject);
        }
    }
}