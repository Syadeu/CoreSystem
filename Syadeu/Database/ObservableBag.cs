using System.Collections.Generic;

namespace Syadeu.Database
{
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
}
