using System;
using System.Collections.Generic;

namespace Syadeu.Collections
{
    [Obsolete("기능 수정 예정")]
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
            get
            {
                try
                {
                    return ts[i];
                }
                catch (ArgumentOutOfRangeException ex)
                {
#if UNITY_EDITOR
                    throw new CoreSystemException(CoreSystemExceptionFlag.Database,
                        $"ObArray<{typeof(T).Name}> 에서 {i} 인덱스 값이 없음", ex);
#else
                    CoreSystemException.SendCrash(CoreSystemExceptionFlag.Background,
                        $"ObArray<{typeof(T).Name}> 에서 {i} 인덱스 값이 없음", ex);
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
                    ts[i] = value;
                }
                catch (ArgumentOutOfRangeException ex)
                {
#if UNITY_EDITOR
                    throw new CoreSystemException(CoreSystemExceptionFlag.Database,
                        $"ObArray<{typeof(T).Name}> 에서 {i} 인덱스 값이 없음", ex);
#else
                    CoreSystemException.SendCrash(CoreSystemExceptionFlag.Background,
                        $"ObArray<{typeof(T).Name}> 에서 {i} 인덱스 값이 없음", ex);
#endif
                }
                catch (Exception)
                {
                    throw;
                }
                
                OnChange();
            }
        }
    }
}
