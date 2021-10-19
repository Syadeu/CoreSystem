using Unity.Collections;

namespace Syadeu.Collections
{
    public struct FixedReferenceList16<T> : IFixedReferenceList<T>
        where T : class, IObject
    {
        private FixedList128Bytes<Hash> m_Hashes;

        public int Length => m_Hashes.Length;

        IFixedReference<T> IFixedReferenceList<T>.this[int index]
        {
            get => new FixedReference<T>(m_Hashes[index]);
            set => m_Hashes[index] = value.Hash;
        }
        public FixedReference<T> this[int index]
        {
            get => new FixedReference<T>(m_Hashes[index]);
            set => m_Hashes[index] = value.Hash;
        }

        public void Clear()
        {
            m_Hashes.Clear();
        }
        public void Add(FixedReference<T> reference)
        {
            m_Hashes.Add(reference.Hash);
        }
        public void Add(Hash hash)
        {
            m_Hashes.Add(hash);
        }
        public void Remove(FixedReference<T> reference)
        {
            m_Hashes.Remove(reference.Hash);
        }
        public void Remove(Hash hash)
        {
            m_Hashes.Remove(hash);
        }
        public void RemoveAt(int index)
        {
            m_Hashes.RemoveAt(index);
        }

        public bool Contains(IFixedReference<T> other)
        {
            for (int i = 0; i < m_Hashes.Length; i++)
            {
                if (m_Hashes[i].Equals(other.Hash)) return true;
            }
            return false;
        }
    }
}
