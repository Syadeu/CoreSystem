﻿using System;
using System.Collections.Generic;

namespace Syadeu.Collections
{
    [Obsolete("기능 수정 예정")]
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
        public void Clear() => ts.Clear();

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
