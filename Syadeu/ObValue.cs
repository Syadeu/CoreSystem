using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Syadeu
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
    public sealed class ObValue<T> where T : struct, System.IConvertible
    {
        public delegate void ValueChangeAction(T current, T target);

        public ObValueDetection DetectionFlag = ObValueDetection.Constant;
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
    }
    /// <summary>
    /// Value의 값이 바뀌면 OnValueChange event를 호출하는 클래스입니다.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class ObClass<T> where T : class
    {
        public delegate void ValueChangeAction(T current, T target);

        public ObValueDetection DetectionFlag = ObValueDetection.Constant;
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
    }

    public abstract class ObservableBag<T>
    {
        public delegate void ValueChangeAction(IEnumerable<T> target);

        protected abstract IEnumerable<T> m_List { get; }
        public IEnumerable<T> Value => m_List;
        public abstract int Count { get; }

        public ObservableBag()
        {
            Initialize();
        }
        public abstract void Initialize();
        protected void OnChange() => OnValueChange?.Invoke(m_List);

        public event ValueChangeAction OnValueChange;
    }
    /// <summary>
    /// 리스트의 값이 바뀌면 OnValueChange event를 호출하는 클래스입니다.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class ObList<T> : ObservableBag<T>
    {
        private List<T> ts;
        protected override IEnumerable<T> m_List => ts;
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
        protected override IEnumerable<T> m_List => ts;

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
        protected override IEnumerable<T> m_List => ts;
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
