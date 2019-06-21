using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace UnityEngine.Rendering.LWRP
{
    class ObjectPool<T> where T : new()
    {
        public struct PooledObject : IDisposable
        {
            readonly T m_ToReturn;
            readonly ObjectPool<T> m_Pool;

            internal PooledObject(T value, ObjectPool<T> pool)
            {
                m_ToReturn = value;
                m_Pool = pool;
            }

            void IDisposable.Dispose() => m_Pool.Release(m_ToReturn);
        }

        readonly Stack<T> m_Stack = new Stack<T>();
        readonly UnityAction<T> m_ActionOnGet;
        readonly UnityAction<T> m_ActionOnRelease;

        public int countAll { get; private set; }
        public int countActive { get { return countAll - countInactive; } }
        public int countInactive { get { return m_Stack.Count; } }

        public ObjectPool(UnityAction<T> actionOnGet, UnityAction<T> actionOnRelease)
        {
            m_ActionOnGet = actionOnGet;
            m_ActionOnRelease = actionOnRelease;
        }

        public T Get()
        {
            T element;
            if (m_Stack.Count == 0)
            {
                element = new T();
                countAll++;
            }
            else
            {
                element = m_Stack.Pop();
            }
            if (m_ActionOnGet != null)
                m_ActionOnGet(element);
            return element;
        }

        public PooledObject Get(out T v) => new PooledObject(v = Get(), this);

        public void Release(T element)
        {
            if (m_Stack.Count > 0 && ReferenceEquals(m_Stack.Peek(), element))
                Debug.LogError("Internal error. Trying to destroy object that is already released to pool.");
            if (m_ActionOnRelease != null)
                m_ActionOnRelease(element);
            m_Stack.Push(element);
        }
    }

    internal static class GenericPool<T>
        where T : new()
    {
        // Object pool to avoid allocations.
        static readonly ObjectPool<T> s_Pool = new ObjectPool<T>(null, null);

        public static T Get() => s_Pool.Get();

        public static ObjectPool<T>.PooledObject Get(out T value) => s_Pool.Get(out value);

        public static void Release(T toRelease) => s_Pool.Release(toRelease);
    }
}
