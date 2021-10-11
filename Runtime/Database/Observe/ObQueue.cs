using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Syadeu.Collections
{
    [Obsolete("기능 수정 예정")]
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
