namespace ZG
{
    public interface IRandomWrapper<T>
    {
        float NextFloat(ref T value);
    }

    public struct RandomSelector<TRandom, TWrapper> where TWrapper : IRandomWrapper<TRandom>
    {
        private TWrapper __wrapper;
        private float __currentRandomValue;
        private float __totalChance;
        private bool __isSelected;

        public RandomSelector(ref TRandom random, in TWrapper wrapper = default)
        {
            __wrapper = wrapper;
            __currentRandomValue = wrapper.NextFloat(ref random);
            __totalChance = 0.0f;
            __isSelected = false;
        }

        public bool Select(ref TRandom random, float chance)
        {
            if (chance < 1.0f)
                __totalChance += chance;
            else
                __totalChance = 2.0f;
            
            if (__totalChance > 1.0f)
            {
                __totalChance -= 1.0f;
                
                __currentRandomValue = __wrapper.NextFloat(ref random);

                __isSelected = false;
            }
            
            if(__isSelected || __totalChance < __currentRandomValue)
                return false;

            __isSelected = true;

            return true;
        }
    }
}