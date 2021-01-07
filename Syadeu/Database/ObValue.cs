using Syadeu.Extentions.EditorUtils;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Syadeu.Database
{
    /// <summary>
    /// 값을 체크하는 방식을 설정합니다.
    /// </summary>
    public enum ObValueDetection
    {
        /// <summary>
        /// 어떤값이 들어와도 항상 체크합니다.
        /// </summary>
        Constant,
        /// <summary>
        /// 값이 이전값이랑 다를때만 체크합니다.
        /// </summary>
        Changed
    }
    /// <summary>
    /// Value의 값이 바뀌면 OnValueChange event를 호출하는 클래스입니다.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class ObValue<T> where T : struct, IConvertible
    {
        public delegate void ValueChangeAction(T current, T target);

        public ObValueDetection DetectionFlag { get; }

        private T m_Value;
        public T Value
        {
            get { return m_Value; }
            set
            {
                T temp = m_Value;
                m_Value = value;

                if (DetectionFlag == ObValueDetection.Constant)
                {
                    OnValueChange?.Invoke(temp, value);
                }
                else if (DetectionFlag == ObValueDetection.Changed)
                {
                    if (!temp.Equals(value)) OnValueChange?.Invoke(temp, value);
                }
            }
        }

        public event ValueChangeAction OnValueChange;
        public ObValue(ObValueDetection flag = ObValueDetection.Constant)
        {
            DetectionFlag = flag;
        }
    }

    public sealed class ObDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>, IDictionary<TKey, TValue>
    {
        private ConcurrentDictionary<TKey, TValue> m_Dictionary { get; }
        public TValue this[TKey key]
        {
            get
            {
                return m_Dictionary[key];
            }
            set
            {
                m_Dictionary[key] = value;

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

    /// <summary>
    /// 컬렉션의 정보가 바뀌면 콜백을 호출하는 클래스들의 기본 클래스입니다
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ObservableBag<T>
    {
        public delegate void ValueChangeAction(IEnumerable<T> target);

        protected abstract IEnumerable<T> List { get; }
        public IEnumerable<T> Value => List;
        public abstract int Count { get; }

        public ObservableBag()
        {
            Initialize();
        }
        public abstract void Initialize();
        protected void OnChange() => OnValueChange?.Invoke(List);

        public event ValueChangeAction OnValueChange;
    }
    /// <summary>
    /// 리스트의 값이 바뀌면 OnValueChange event를 호출하는 클래스입니다.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class ObList<T> : ObservableBag<T>
    {
        private List<T> ts;
        protected override IEnumerable<T> List => ts;
        public override int Count => ts.Count;

        public override void Initialize()
        {
            ts = new List<T>();
        }

        public void Add(T item)
        {
            ts.Add(item);
            OnChange();
        }
        public void Remove(T item)
        {
            ts.Remove(item);
            OnChange();
        }
        public void RemoveAt(int i)
        {
            ts.RemoveAt(i);
            OnChange();
        }

        public T this[int i]
        {
            get => ts[i];
            set
            {
                ts[i] = value;
                OnChange();
            }
        }
    }
    /// <summary>
    /// 리스트의 값이 바뀌면 OnValueChange event를 호출하는 클래스입니다.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class ObArray<T> : ObservableBag<T>
    {
        private readonly T[] ts;
        public override int Count => ts.Length;
        protected override IEnumerable<T> List => ts;

        public ObArray(int lenght)
        {
            ts = new T[lenght];
        }
        public override void Initialize()
        {
        }

        public T this[int i]
        {
            get => ts[i];
            set
            {
                ts[i] = value;
                OnChange();
            }
        }
    }
    /// <summary>
    /// 리스트의 값이 바뀌면 OnValueChange event를 호출하는 클래스입니다.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ObQueue<T> : ObservableBag<T>
    {
        private ConcurrentQueue<T> ts;
        protected override IEnumerable<T> List => ts;
        public override int Count => ts.Count;

        public override void Initialize()
        {
            ts = new ConcurrentQueue<T>();
        }

        public T Dequeue()
        {
            TryDequeue(out T value);
            OnChange();
            return value;
        }
        public bool TryDequeue(out T value)
        {
            bool temp = ts.TryDequeue(out value);
            OnChange();
            return temp;
        }
        public void Enqueue(T item)
        {
            ts.Enqueue(item);
            OnChange();
        }
    }
}
