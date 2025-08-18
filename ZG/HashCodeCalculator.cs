using System;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;

public static class HashCodeCalculator
{
    private static readonly ConditionalWeakTable<object, object> VisitedObjects = 
        new ConditionalWeakTable<object, object>();

    /// <summary>
    /// 计算对象的深度哈希值
    /// </summary>
    /// <param name="obj">要计算哈希值的对象</param>
    /// <returns>对象的哈希值</returns>
    public static int ComputeHashCode(object obj)
    {
        // 重置已访问对象集合
        VisitedObjects.Clear();
        return __ComputeHashCodeRecursive(obj);
    }

    private static int __ComputeHashCodeRecursive(object obj)
    {
        if (obj == null) 
            return 0;
        
        Type type = obj.GetType();
        
        // 处理基本类型
        if (type.IsPrimitive || type == typeof(decimal) || type == typeof(string))
            return obj.GetHashCode();

        // 处理枚举类型
        if (type.IsEnum)
            return obj.GetHashCode();
        
        // 检查循环引用
        if (VisitedObjects.TryGetValue(obj, out object hashCodeObject))
            return (int)hashCodeObject; // 已访问过，返回固定值防止循环

        VisitedObjects.Add(obj, 0); // 标记为已访问

        int result;
        // 处理数组
        if (type.IsArray)
            result = __ComputeArrayHashCode((Array)obj);
        else
        // 处理集合
        if (obj is IEnumerable enumerable)
            result = __ComputeEnumerableHashCode(enumerable);
        else
            result = __ComputeObjectFieldsHashCode(obj);

        VisitedObjects.AddOrUpdate(obj, result); // 标记为已访问

        return result;
    }

    private static int __ComputeArrayHashCode(Array array)
    {
        int hash = 0;
        int rank = array.Rank;
        
        if (rank == 1) // 一维数组
        {
            for (int i = 0; i < array.Length; i++)
                hash = __CombineHashes(hash, __ComputeHashCodeRecursive(array.GetValue(i)));
        }
        else // 多维数组
        {
            // 创建索引器并遍历所有维度
            int[] indices = new int[rank];
            __TraverseArrayDimensions(array, 0, indices, ref hash);
        }
        
        return hash;
    }

    private static void __TraverseArrayDimensions(Array array, int dimension, int[] indices, ref int hash)
    {
        if (dimension == array.Rank)
        {
            object value = array.GetValue(indices);
            hash = __CombineHashes(hash, __ComputeHashCodeRecursive(value));
            return;
        }

        for (int i = 0; i < array.GetLength(dimension); i++)
        {
            indices[dimension] = i;
            __TraverseArrayDimensions(array, dimension + 1, indices, ref hash);
        }
    }

    private static int __ComputeEnumerableHashCode(IEnumerable enumerable)
    {
        int hash = 0;
        foreach (object item in enumerable)
        {
            hash = __CombineHashes(hash, __ComputeHashCodeRecursive(item));
        }
        return hash;
    }

    private static int __ComputeObjectFieldsHashCode(object obj)
    {
        Type type = obj.GetType();
        int hash = 0;
        
        // 处理所有字段（包括私有字段）
        FieldInfo[] fields = type.GetFields(
            BindingFlags.Instance | 
            BindingFlags.Public | 
            BindingFlags.NonPublic);
        
        foreach (FieldInfo field in fields)
        {
            // 跳过委托类型和迭代器状态机
            if (typeof(Delegate).IsAssignableFrom(field.FieldType) ||
                field.Name.Contains("__state"))
            {
                continue;
            }
            
            try
            {
                object value = field.GetValue(obj);
                int fieldHash = __ComputeHashCodeRecursive(value);
                hash = __CombineHashes(hash, fieldHash);
            }
            catch
            {
                // 忽略访问错误
            }
        }
        
        return hash;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int __CombineHashes(int h1, int h2)
    {
        // 优化的哈希组合算法
        unchecked
        {
            return ((h1 << 5) + h1) ^ h2;
        }
    }
}