using System;
using UnityEngine;

namespace ZG
{
    public class AudioBehaviour : StateMachineBehaviour
    {
        public enum Type
        {
            Normal,
            Positive,
            Negative
        }

        [Serializable]
        public struct Item
        {
            public bool isForce;
            public int styleIndex;
            [Index("database.items", pathLevel = 2, uniqueLevel = 1)]
            [UnityEngine.Serialization.FormerlySerializedAs("index")]
            public int itemIndex;
            public float time;
        }

        public Type type;
        public string blendTreeParameter;
        public AudioDatabase database;
        [UnityEngine.Serialization.FormerlySerializedAs("nodes")]
        public Item[] items;
        private int[] __indices;
        private bool __isInvert;
        private int __index;
        private float __time;

        // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (animator == null)
                return;

            switch (type)
            {
                case Type.Positive:
                    __isInvert = false;
                    break;
                case Type.Negative:
                    __isInvert = true;
                    break;
            }

            bool isSort = false;
            int numItems = items == null ? 0 : items.Length, i;
            if ((__indices == null ? 0 : __indices.Length) != numItems)
            {
                __indices = numItems > 0 ? new int[numItems] : null;
                for (i = 0; i < numItems; ++i)
                    __indices[i] = i;

                isSort = true;
            }

            float time = string.IsNullOrEmpty(blendTreeParameter) ? stateInfo.normalizedTime * stateInfo.length * stateInfo.speed * stateInfo.speedMultiplier : animator.GetFloat(blendTreeParameter);
            bool isInvert = time < __time;
            if (isInvert != __isInvert)
            {
                if (type == Type.Normal)
                {
                    __isInvert = isInvert;

                    isSort = true;
                }
                else if (!isSort)
                    __index = 0;
            }

            if (isSort)
            {
                Array.Sort(__indices, __Comparsion);

                __index = 0;
            }

            for (i = __index; i < numItems; ++i)
            {
                if (__isInvert == items[__indices[i]].time < time)
                    break;
            }

            if (type == Type.Normal)
            {
                if (isSort)
                    __index = Mathf.Max(i - 1, 0);
            }
            else if (isInvert != __isInvert)
                __index = i;

            while (__index < i)
            {
                Item item = items[__indices[__index]];
                AudioMain main = animator.GetComponent<AudioMain>();
                if (main == null)
                {
                    GameObject gameObject = animator.gameObject;
                    if (gameObject != null)
                        main = gameObject.AddComponent<AudioMain>();
                }

                //Debug.Log($"{stateInfo.fullPathHash} : {stateInfo.normalizedTime} : {stateInfo.loop} : {__index}");

                if (main != null)
                    main.Play(item.isForce, item.styleIndex, item.itemIndex, (double)(time - item.time), database);

                ++__index;
            }

            __time = time;
        }

        // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            __index = 0;
            __time = animator == null || string.IsNullOrEmpty(blendTreeParameter) ? stateInfo.normalizedTime * stateInfo.length * stateInfo.speed * stateInfo.speedMultiplier : animator.GetFloat(blendTreeParameter);
        }


        // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
        //override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        //
        //}

        // OnStateMove is called right after Animator.OnAnimatorMove(). Code that processes and affects root motion should be implemented here
        //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        //
        //}

        // OnStateIK is called right after Animator.OnAnimatorIK(). Code that sets up animation IK (inverse kinematics) should be implemented here.
        //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        //
        //}

        private int __Comparsion(int x, int y)
        {
            int result = (int)Mathf.Sign(items[x].time - items[y].time);
            if (__isInvert)
                result = -result;

            return result;
        }
    }
}