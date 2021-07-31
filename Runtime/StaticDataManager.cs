using Syadeu.Database;
using Syadeu.Entities;
using Syadeu.Internal;
using System;
using UnityEngine;

namespace Syadeu
{
    public abstract class StaticDataManager<T> : IStaticDataManager 
        where T : class, IStaticDataManager, IDisposable
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
#if UNITY_EDITOR
                    if (CoreSystem.s_BlockCreateInstance)
                    {
                        LogManager.Log(Channel.Core, ResultFlag.Warning,
                            $"종료 중에 StaticDataManager<{TypeHelper.TypeOf<T>.Name}> 인스턴스를 생성하려 합니다.\n" +
                            $"이 요청은 무시됩니다.", true);
                        return null;
                    }
                    if (IsMainthread() && !Application.isPlaying) throw new CoreSystemException(CoreSystemExceptionFlag.Database,
                        $"StaticDataManager<{TypeHelper.TypeOf<T>.Name}>의 인스턴스 객체는 플레이중에만 생성되거나 받아올 수 있습니다.");
#endif

                    m_Instance = CoreSystem.GetManager<T>();
                    if (m_Instance != null)
                    {
                        return m_Instance;
                    }

                    T ins = Activator.CreateInstance<T>();
                    CoreSystem.DataManagers.Add(ins);
                    CoreSystem.InvokeManagerChanged();

                    ins.OnInitialize();
                    m_Instance = ins;
                    CoreSystem.Logger.Log(Channel.Core, $"{TypeHelper.TypeOf<T>.Name} is initialized");

                    ConfigLoader.LoadConfig(ins);
                    ins.OnStart();
                }

                return m_Instance;
            }
        }

        protected static System.Threading.Thread MainThread => ManagerEntity.MainThread;
        protected static System.Threading.Thread BackgroundThread => ManagerEntity.BackgroundThread;

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
        protected CoreRoutine StartUnityUpdate(System.Collections.IEnumerator enumerator) => CoreSystem.Instance.StartUnityUpdate(enumerator);
        /// <summary>
        /// OnBackgroundUpdate 보다 일찍 실행되는 커스텀 업데이트문을 넣을 수 있습니다.
        /// </summary>
        protected CoreRoutine StartBackgroundUpdate(System.Collections.IEnumerator enumerator) => CoreSystem.Instance.StartBackgroundUpdate(enumerator);
        protected void StopUnityUpdate(CoreRoutine routine) => CoreSystem.RemoveUnityUpdate(routine);
        protected void StopBackgroundUpdate(CoreRoutine routine) => CoreSystem.RemoveBackgroundUpdate(routine);

        public virtual void Dispose()
        {
            Disposed = true;

            CoreSystem.DataManagers.Remove(this);
            m_Instance = null;
            CoreSystem.Instance.m_CleanupManagers = true;

            CoreSystem.Logger.Log(Channel.Core, $"DataManager {TypeHelper.TypeOf<T>.Name} has been disposed.");
        }
    }
}
