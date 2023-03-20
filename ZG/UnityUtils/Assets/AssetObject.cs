using UnityEngine;

namespace ZG
{
    public abstract class AssetObject : MonoBehaviour
    {
        public event System.Action<GameObject> onCreated;

        public abstract string fileName { get; }

        public abstract AssetManager assetManager { get; }

        private string __assetName;

        private string __fileName;

        public GameObject target
        {
            get;

            private set;
        }

        protected void OnEnable()
        {
            __assetName = name;
            __fileName = fileName;

            StartCoroutine(assetManager.Load<GameObject>(__fileName, __assetName, null, (assetBundle, gameObject) =>
            {
                if (gameObject == null)
                {
                    Debug.LogError($"Asset Object {name} Load Fail.", this);

                    return;
                }

                target = Instantiate(gameObject, transform);

                //assetBundle.Unload(false);

                //Destroy(assetBundle);

                if (onCreated != null)
                    onCreated(target);
            }));
        }

        protected void OnDisable()
        {
            if (target != null)
            {
                Destroy(target);

                target = null;
            }

            var assetManager = this.assetManager;
            if(assetManager != null)
                assetManager.Unload<GameObject>(__fileName, __assetName);
        }
    }
}