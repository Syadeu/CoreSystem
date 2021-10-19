using Unity.Collections;

namespace Syadeu.Collections
{
    public struct FixedInstanceList16<T>
        where T : class, IObject
    {
        private FixedList128Bytes<Hash> m_Hashes;

        public int Length => m_Hashes.Length;

        public Instance<T> this[int index]
        {
            get => new Instance<T>(m_Hashes[index]);
            set => m_Hashes[index] = value.Idx;
        }

        public void Clear()
        {
            m_Hashes.Clear();
        }
        public void Add(Instance<T> reference)
        {
            m_Hashes.Add(reference.Idx);
        }
        public void Add(Hash hash)
        {
            m_Hashes.Add(hash);
        }
        public void Remove(Instance<T> reference)
        {
            m_Hashes.Remove(reference.Idx);
        }
        public void Remove(Hash hash)
        {
            m_Hashes.Remove(hash);
        }
        public void RemoveAt(int index)
        {
            m_Hashes.RemoveAt(index);
        }
    }
}
