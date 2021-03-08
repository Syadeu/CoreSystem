using Syadeu.Extentions.EditorUtils;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Syadeu.Database
{
    public sealed class ObDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>, IDictionary<TKey, TValue>
    {
        private ConcurrentDictionary<TKey, TValue> m_Dictionary { get; }
        public TValue this[TKey key]
        {
            get
            {
                try
                {
                    return m_Dictionary[key];
                }
                catch (KeyNotFoundException ex1)
                {
#if UNITY_EDITOR
                    throw new CoreSystemException(CoreSystemExceptionFlag.Database, 
                        $"ObDictionary<{typeof(TKey).Name}, {typeof(TValue).Name}> 에서" +
                        $"{key} 값이 존재하지 않음", ex1);
#else
                    CoreSystemException.SendCrash(CoreSystemExceptionFlag.Background,
                        $"ObDictionary<{typeof(TKey).Name}, {typeof(TValue).Name}> 에서" +
                        $"{key} 값이 존재하지 않음", ex1);
                    throw;
#endif
                }
                catch (Exception)
                {
                    throw;
                }
            }
            set
            {
                try
                {
                    m_Dictionary[key] = value;
                }
                catch (KeyNotFoundException ex1)
                {
#if UNITY_EDITOR
                    throw new CoreSystemException(CoreSystemExceptionFlag.Database,
                        $"ObDictionary<{typeof(TKey).Name}, {typeof(TValue).Name}> 에서" +
                        $"{key} 값이 존재하지 않음", ex1);
#else
                    CoreSystemException.SendCrash(CoreSystemExceptionFlag.Background,
                        $"ObDictionary<{typeof(TKey).Name}, {typeof(TValue).Name}> 에서" +
                        $"{key} 값이 존재하지 않음", ex1);
#endif
                }
                catch (Exception)
                {
                    throw;
                }

                if (value == null)
                {
                    RemoveModified(key);
                }
                else AddModified(key);
            }
        }

        public List<TKey> ModifiedKeys { get; }
        public List<TKey> RemovedKeys { get; }

        public void ClearModified()
        {
            ModifiedKeys.Clear();
            RemovedKeys.Clear();
        }
        private void AddModified(TKey key)
        {
            if (!ModifiedKeys.Contains(key))
            {
                ModifiedKeys.Add(key);

                for (int i = 0; i < RemovedKeys.Count; i++)
                {
                    if (RemovedKeys[i].Equals(key))
                    {
                        RemovedKeys.RemoveAt(i);
                        return;
                    }
                }
            }
        }
        private void RemoveModified(TKey key)
        {
            if (!RemovedKeys.Contains(key))
            {
                RemovedKeys.Add(key);

                for (int i = 0; i < ModifiedKeys.Count; i++)
                {
                    if (ModifiedKeys[i].Equals(key))
                    {
                        ModifiedKeys.RemoveAt(i);
                        return;
                    }
                }
            }
        }

        public ICollection<TKey> Keys => m_Dictionary.Keys;
        public ICollection<TValue> Values => m_Dictionary.Values;

        public bool GetModifiedValues(out TValue[] added, out TKey[] removed)
        {
            added = new TValue[ModifiedKeys.Count];
            for (int i = 0; i < ModifiedKeys.Count; i++)
            {
                added[i] = m_Dictionary[ModifiedKeys[i]];
            }
            removed = RemovedKeys.ToArray();

            return added.Length != 0 && removed.Length != 0;
        }

        public int Count => m_Dictionary.Count;
        public bool IsReadOnly => false;

        public ObDictionary()
        {
            m_Dictionary = new ConcurrentDictionary<TKey, TValue>();
            ModifiedKeys = new List<TKey>();
            RemovedKeys = new List<TKey>();
        }
        public ObDictionary(ConcurrentDictionary<TKey, TValue> dictionary)
        {
            m_Dictionary = dictionary;
            ModifiedKeys = new List<TKey>();
            RemovedKeys = new List<TKey>();
        }

        public void Add(TKey key, TValue value, bool passModified = false)
        {
            if (!passModified) AddModified(key);

            if (!m_Dictionary.TryAdd(key, value))
            {
                Add(key, value, true);
            }
        }
        public void Add(TKey key, TValue value)
        {
            AddModified(key);

            if (!m_Dictionary.TryAdd(key, value))
            {
                $"Dictionary Add failed {key}: {value}".ToLog();
            }
        }
        public bool TryGetValue(TKey key, out TValue value) => m_Dictionary.TryGetValue(key, out value);
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => m_Dictionary.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => m_Dictionary.GetEnumerator();
        public bool ContainsKey(TKey key) => m_Dictionary.ContainsKey(key);

        public bool Remove(TKey key)
        {
            bool temp = m_Dictionary.TryRemove(key, out var unused);
            if (temp)
            {
                RemoveModified(key);
            }
            return temp;
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            AddModified(item.Key);

            m_Dictionary.TryAdd(item.Key, item.Value);
        }

        public void Clear()
        {
            m_Dictionary.Clear();
            ClearModified();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item) => m_Dictionary.Contains(item);

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentException("array");
            }
            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException("arrayIndex", arrayIndex, "The index is negative or outside the bounds of the collection.");
            }

            TKey[] keys = Keys.ToArray();
            TValue[] values = Values.ToArray();
            for (int index = 0; index != Keys.Count && arrayIndex < array.Length; ++index, ++arrayIndex)
            {
                var key = keys[index];
                var value = values[index];
                array[arrayIndex] = new KeyValuePair<TKey, TValue>(key, value);
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            bool temp = m_Dictionary.TryRemove(item.Key, out var unused);
            if (temp)
            {
                RemoveModified(item.Key);
            }
            return temp;
        }
    }
}
