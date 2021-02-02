using System.Collections.Generic;

namespace Syadeu.Database
{
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
}
