using System;

namespace Syadeu
{
    public abstract class StaticDataManager<T> : IStaticDataManager 
        where T : class, IStaticDataManager
    {
        public static bool Initialized => m_Instance != null;
        public SystemFlag Flag => SystemFlag.Data;

        internal static T m_Instance;
        public static T Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    T ins = Activator.CreateInstance<T>();

                    ins.OnInitialize();

                    CoreSystem.DataManagers.Add(ins);
                    m_Instance = ins;

                    ins.OnStart();
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

        public bool Disposed { get; private set; } = false;

        public virtual void OnInitialize() { }
        public virtual void OnStart() { }
        public virtual void Initialize(SystemFlag flag = SystemFlag.Data) { }

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

        public virtual void Dispose()
        {
            Disposed = true;

            CoreSystem.Instance.m_CleanupManagers = true;
        }
    }
}
