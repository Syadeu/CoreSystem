using Syadeu.Collections;
using System;

namespace Syadeu.Collections
{
    /// <summary>
    /// Contains only instance
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct Instance<T> : IInstance<T>, IEquatable<Instance<T>>
        where T : class, IObject
    {
        public static readonly Instance<T> Empty = new Instance<T>(Hash.Empty);

        private Hash m_Idx;

        public Hash Idx => m_Idx;

        public Instance(Hash idx)
        {
            m_Idx = idx;
        }
        public Instance(InstanceID id)
        {
            m_Idx = id.Hash;
        }
        public Instance(IEntityDataID entity)
        {
            m_Idx = entity.Idx;
        }
        public Instance(IObject obj)
        {
            if (obj.Idx.IsEmpty())
            {
                UnityEngine.Debug.LogError(
                    $"Object({obj.Name}) is not an instance.");
                m_Idx = Hash.Empty;
                return;
            }
            if (!(obj is T))
            {
                UnityEngine.Debug.LogError(
                    $"Object({obj.Name}) is not a {TypeHelper.TypeOf<T>.Name}.");
                m_Idx = Hash.Empty;
                return;
            }

            m_Idx = obj.Idx.Hash;
        }

        public bool IsEmpty() => Equals(Empty);
        public bool Equals(Instance<T> other) => m_Idx.Equals(other.m_Idx);
        public bool Equals(IInstance other) => m_Idx.Equals(other.Idx);
    }

    public struct Instance : IInstance, IEquatable<Instance>
    {
        public static readonly Instance Empty = new Instance(Hash.Empty);

        private Hash m_Idx;

        public Hash Idx => m_Idx;

        public Instance(InstanceID idx)
        {
            m_Idx = idx.Hash;
        }
        public Instance(IObject obj)
        {
            if (obj.Idx.IsEmpty())
            {
                UnityEngine.Debug.LogError(
                    $"Object({obj.Name}) is not an instance.");
                m_Idx = Hash.Empty;
                return;
            }

            m_Idx = obj.Idx.Hash;
        }

        public bool IsEmpty() => Equals(Empty);
        public bool Equals(Instance other) => m_Idx.Equals(other.m_Idx);
        public bool Equals(IInstance other) => m_Idx.Equals(other.Idx);
    }
}
