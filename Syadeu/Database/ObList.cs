using System.Collections.Generic;

namespace Syadeu.Database
{
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
}
