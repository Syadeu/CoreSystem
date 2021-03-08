using System;
using System.Collections.Generic;

namespace Syadeu.Database
{
    [Obsolete("기능 수정 예정")]
    /// <summary>
    /// 컬렉션의 정보가 바뀌면 콜백을 호출하는 클래스들의 기본 클래스입니다
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ObservableBag<T> : IValidation, IDisposable
    {
        public delegate void ValueChangeAction(IEnumerable<T> target);

        protected abstract IEnumerable<T> List { get; }
        public IEnumerable<T> Value => List;
        public abstract int Count { get; }

        public bool Disposed { get; private set; } = false;

        public ObservableBag()
        {
            Initialize();
        }
        public abstract void Initialize();
        protected void OnChange() => OnValueChange?.Invoke(List);

        public event ValueChangeAction OnValueChange;

        public void Dispose()
        {
            Disposed = true;
        }
        public bool IsValid() => !Disposed;
    }
}
