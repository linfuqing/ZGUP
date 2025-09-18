using UnityEngine;

namespace ZG
{
    public struct RandomSelector
    {
        private struct Wrapper : IRandomWrapper<int>
        {
            public float NextFloat(ref int _) => Random.value;
        }

        private RandomSelector<int, Wrapper> __instance;

        public RandomSelector(int seed)
        {
            if (seed != 0)
                Random.InitState(seed);

            __instance = new RandomSelector<int, Wrapper>(ref seed);
        }

        public bool Select(float chance)
        {
            int _ = 0;
            return __instance.Select(ref _, chance);
        }
    }
}