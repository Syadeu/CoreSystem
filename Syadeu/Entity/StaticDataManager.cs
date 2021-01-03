using System;

namespace Syadeu
{
    public abstract class StaticDataManager<T> : IStaticManager where T : class
    {
        public static bool Initialized { get; private set; } = false;
        public SystemFlag Flag { get; protected set; }

        private static T m_Instance;
        public static T Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    T ins = Activator.CreateInstance<T>();

                    (ins as IStaticManager).OnInitialize();

                    m_Instance = ins;
                    Initialized = true;
                }

                return m_Instance;
            }
        }

        public static System.Threading.Thread MainThread => ManagerEntity.MainThread;
        public static System.Threading.Thread BackgroundThread => ManagerEntity.BackgroundThread;

        protected static bool IsMainthread()
            => CoreSystem.IsThisMainthread();
        protected static void ThreadAwaiter(int milliseconds)
            => StaticManagerEntity.ThreadAwaiter(milliseconds);

        public virtual void OnInitialize() { }
        public virtual void Initialize(SystemFlag flag = SystemFlag.Data)
        {
            Flag = flag;
        }

        /// <summary>
        /// OnUnityUpdate 보다 일찍 실행되는 커스텀 업데이트문을 넣을 수 있습니다.
        /// </summary>
        public void StartUnityUpdate(System.Collections.IEnumerator enumerator) => CoreSystem.Instance.StartUnityUpdate(enumerator);
        /// <summary>
        /// OnBackgroundUpdate 보다 일찍 실행되는 커스텀 업데이트문을 넣을 수 있습니다.
        /// </summary>
        public void StartBackgroundUpdate(System.Collections.IEnumerator enumerator) => CoreSystem.Instance.StartBackgroundUpdate(enumerator);
        public void StopUnityUpdate(System.Collections.IEnumerator enumerator) => CoreSystem.RemoveUnityUpdate(this, enumerator);
        public void StopBackgroundUpdate(System.Collections.IEnumerator enumerator) => CoreSystem.RemoveBackgroundUpdate(this, enumerator);
    }
}
