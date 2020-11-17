﻿using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Syadeu
{
    /// <summary>
    /// 재사용 가능 데이터 객체입니다.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class RecycleableDatabase<T> where T : class
    {
        public static int InstanceCount => Instances.Count;

        private static ConcurrentDictionary<int, T> InstanceList { get; } = new ConcurrentDictionary<int, T>();
        public static List<T> Instances { get; } = new List<T>();

        public static T GetInstance(int index)
        {
            //InstanceList.TryGetValue(index, out var value);
            //return value;
            return Instances[index];
        }
        public bool Activated { get; private set; } = false;
        public int DataIndex { get; }
        /// <summary>
        /// 이 객체의 재사용리스트에서 사용중이지않은 객체를 뽑아봅니다.<br/>
        /// null일 수 있고, 그 경우에는 new 키워드를 사용하세요
        /// </summary>
        public static T GetDatabase()
        {
            T temp = null;
            foreach (var item in Instances)
            {
                if (!(item as RecycleableDatabase<T>).Activated)
                {
                    temp = item;
                    (item as RecycleableDatabase<T>).Initialize();
                }
            }
            return temp;
        }
        public static T GetDatabase(int index)
        {
            T temp = GetInstance(index);
            if ((temp as RecycleableDatabase<T>).Activated)
            {
                (temp as RecycleableDatabase<T>).Terminate();
            }

            (temp as RecycleableDatabase<T>).Initialize();
            return temp;
        }
        public RecycleableDatabase()
        {
            DataIndex = InstanceList.Count;

            Initialize();

            InstanceList.TryAdd(DataIndex, this as T);
            Instances.Add(this as T);
        }

        /// <summary>
        /// 초기화 함수, 이 객체가 재사용리스트에서 불러왔을경우 호출됩니다.
        /// </summary>
        protected virtual void OnInitialize() { }
        private void Initialize()
        {
            Activated = true;
            OnInitialize();
        }
        /// <summary>
        /// Termintate하기 전 호출되는 함수입니다.
        /// </summary>
        protected virtual void OnTerminate() { }
        /// <summary>
        /// 이 객체가 더 이상 사용되지 않을 때 호출하세요
        /// </summary>
        public void Terminate()
        {
            Activated = false;
            OnTerminate();
        }

        protected static bool IsMainthread()
        {
            if (ManagerEntity.MainThread == null || System.Threading.Thread.CurrentThread == ManagerEntity.MainThread)
            {
                return true;
            }
            return false;
        }
    }
}
