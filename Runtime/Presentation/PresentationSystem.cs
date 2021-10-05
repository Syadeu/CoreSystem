#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Internal;
using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace Syadeu.Presentation
{
    /// <summary>
    /// 등록한 프레젠테이션 시스템을 받아오기 위한 struct 입니다.
    /// </summary>
    /// <remarks>
    /// 이 struct 로 시스템을 받아오려면 먼저 <seealso cref="PresentationSystemEntity{T}"/> 를 상속받고 시스템을 선언해야됩니다.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    [Obsolete("Use PresentationSystem<TGroup, TSystem> instead")]
    public struct PresentationSystem<T> : IValidation, IDisposable, IEquatable<PresentationSystem<T>> where T : PresentationSystemEntity
    {
        public static readonly PresentationSystem<T> Null = new PresentationSystem<T>(Hash.Empty, -1);
        private static PresentationSystem<T> s_Instance = Null;
        private static PresentationSystem<T> Instance
        {
            get
            {
                if (!((IValidation)s_Instance).IsValid())
                {
                    Hash hash = PresentationManager.GroupToHash(TypeHelper.TypeOf<DefaultPresentationGroup>.Type);

                    var list = PresentationManager.Instance.m_PresentationGroups[hash].Systems;
                    int idx = -1;
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i].GetType().Equals(TypeHelper.TypeOf<T>.Type))
                        {
                            idx = i;
                            break;
                        }
                    }
                    if (idx < 0)
                    {
                        return Null;
                    }
                    s_Instance = new PresentationSystem<T>(hash, idx);
                }
                return s_Instance;
            }
        }
        public static IPresentationSystemGroup SystemGroup
        {
            get
            {
                if (!IsValid()) throw new Exception();
                return PresentationManager.Instance.m_PresentationGroups[Instance.m_GroupHash].m_SystemGroup;
            }
        }
        public static T System
        {
            get
            {
                Assert.IsTrue(IsValid(), $"{TypeHelper.TypeOf<T>.Type.Name} System is not valid");
                CoreSystem.Logger.NotNull(PresentationManager.Instance, "1");
                CoreSystem.Logger.NotNull(PresentationManager.Instance.m_PresentationGroups, "2");
                CoreSystem.Logger.NotNull(PresentationManager.Instance.m_PresentationGroups[Instance.m_GroupHash], "3");
                CoreSystem.Logger.NotNull(PresentationManager.Instance.m_PresentationGroups[Instance.m_GroupHash].Systems[Instance.m_Index], "4");

                try
                {
                    return (T)PresentationManager.Instance.m_PresentationGroups[Instance.m_GroupHash].Systems[Instance.m_Index];
                }
                catch (Exception ex)
                {
                    $"{TypeHelper.TypeOf<T>.Type.Name}: {Instance.m_GroupHash}, {Instance.m_Index}".ToLog();

                    UnityEngine.Debug.LogError(ex);
                    throw;
                }
            }
        }
        public static PresentationSystemID<T> SystemID
        {
            get
            {
                PresentationSystem<T> ins = Instance;
                if (ins.m_GroupHash.IsEmpty() || ins.m_Index == -1)
                {
                    return PresentationSystemID<T>.Null;
                }
                return new PresentationSystemID<T>(ins.m_GroupHash, ins.m_Index);
            }
        }

        private readonly Hash m_GroupHash;
        private readonly int m_Index;

        private PresentationSystem(Hash groupHash, int idx)
        {
            m_GroupHash = groupHash;
            m_Index = idx;
        }

        bool IValidation.IsValid() => !m_GroupHash.IsEmpty() && m_Index >= 0;
        void IDisposable.Dispose()
        {
            s_Instance = Null;
            CoreSystem.Logger.Log(Channel.Presentation, $"Dispose public system struct of {TypeHelper.TypeOf<T>.Name}");
        }

        public static bool IsValid() => !Instance.Equals(Null);
        public static bool HasInitialized() => IsValid() && PresentationManager.Instance.m_PresentationGroups[Instance.m_GroupHash].m_MainInitDone;

        private sealed class SystemAwaiter : CustomYieldInstruction, ICustomYieldAwaiter
        {
            public override bool keepWaiting => ((ICustomYieldAwaiter)this).KeepWait;

            bool ICustomYieldAwaiter.KeepWait => !IsValid() || !HasInitialized();
        }
        public static ICustomYieldAwaiter GetAwaiter() => new SystemAwaiter();

        public bool Equals(PresentationSystem<T> other) => m_GroupHash.Equals(other.m_GroupHash) && m_Index.Equals(other.m_Index);
    }

    /// <summary>
    /// 등록한 프레젠테이션 시스템을 받아오기 위한 struct 입니다.
    /// </summary>
    /// <remarks>
    /// 이 struct 로 시스템을 받아오려면 먼저 <seealso cref="PresentationSystemEntity{T}"/> 를 상속받고 시스템을 선언해야됩니다.
    /// </remarks>
    /// <typeparam name="TGroup">
    /// <seealso cref="PresentationGroupEntity"/> 를 상속받는 시스템 그룹.
    /// 기본 시스템은 <seealso cref="DefaultPresentationGroup"/> 을 참조하세요.
    /// </typeparam>
    /// <typeparam name="TSystem">
    /// <seealso cref="PresentationSystemEntity{T}"/> 를 상속받는 시스템.
    /// </typeparam>
    public readonly struct PresentationSystem<TGroup, TSystem> : IValidation
        where TGroup : PresentationGroupEntity
        where TSystem : PresentationSystemEntity
    {
        public static readonly PresentationSystem<TGroup, TSystem> Null = new PresentationSystem<TGroup, TSystem>(Hash.Empty, -1);
        private static PresentationSystem<TGroup, TSystem> s_Instance = Null;
        
        private readonly Hash m_GroupHash;
        private readonly int m_Index;

        private static PresentationSystem<TGroup, TSystem> Instance
        {
            get
            {
#if DEBUG_MODE
                if (TypeHelper.TypeOf<TGroup>.Type.IsAbstract)
                {
                    CoreSystem.Logger.LogError(Channel.Presentation,
                        $"Group cannot be abstract.");
                    return Null;
                }
                if (TypeHelper.TypeOf<TSystem>.Type.IsAbstract)
                {
                    CoreSystem.Logger.LogError(Channel.Presentation,
                        $"System cannot be abstract.");
                    return Null;
                }
#endif
                if (!((IValidation)s_Instance).IsValid())
                {
                    if (!PresentationManager.TryGetSystem<TGroup, TSystem>(out _, out Hash gHash, out int systemIdx))
                    {
                        return Null;
                    }

                    s_Instance = new PresentationSystem<TGroup, TSystem>(gHash, systemIdx);
                }
                return s_Instance;
            }
        }
        /// <summary>
        /// 이 시스템(<typeparamref name="TSystem"/>) 이 속한 시스템 그룹(<seealso cref="PresentationGroupEntity"/>) 입니다.
        /// </summary>
        public static IPresentationSystemGroup SystemGroup
        {
            get
            {
                if (!IsValid()) throw new Exception();
                return PresentationManager.Instance.m_PresentationGroups[Instance.m_GroupHash].m_SystemGroup;
            }
        }
        /// <summary>
        /// 시스템 <typeparamref name="TSystem"/> 의 인스턴스 입니다.
        /// </summary>
        public static TSystem System
        {
            get
            {
#if DEBUG_MODE
                Assert.IsTrue(IsValid(), $"{TypeHelper.TypeOf<TSystem>.Type.Name} System is not valid");
#endif
                PresentationSystem<TGroup, TSystem> ins = Instance;
                return PresentationManager.GetSystem<TSystem>(in ins.m_GroupHash);
            }
        }
        /// <summary>
        /// 시스템 <typeparamref name="TSystem"/> 의 ID 입니다.
        /// </summary>
        public static PresentationSystemID<TSystem> SystemID
        {
            get
            {
                PresentationSystem<TGroup, TSystem> ins = Instance;
                if (ins.m_GroupHash.IsEmpty() || ins.m_Index == -1)
                {
                    return PresentationSystemID<TSystem>.Null;
                }
                return new PresentationSystemID<TSystem>(ins.m_GroupHash, ins.m_Index);
            }
        }

        private PresentationSystem(Hash groupHash, int idx)
        {
            m_GroupHash = groupHash;
            m_Index = idx;
        }

        private sealed class SystemAwaiter : CustomYieldInstruction, ICustomYieldAwaiter
        {
            public override bool keepWaiting => ((ICustomYieldAwaiter)this).KeepWait;

            bool ICustomYieldAwaiter.KeepWait => !IsValid() || !HasInitialized();
        }
        public static ICustomYieldAwaiter GetAwaiter() => new SystemAwaiter();
        bool IValidation.IsValid() => !m_GroupHash.IsEmpty() && m_Index >= 0;

        public static bool IsValid() => !Instance.Equals(Null);
        public static bool HasInitialized() => IsValid() && PresentationManager.Instance.m_PresentationGroups[Instance.m_GroupHash].m_MainInitDone;
    }
}
