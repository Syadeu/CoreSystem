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
                    if (!PresentationManager.Instance.m_RegisteredGroup.TryGetValue(TypeHelper.TypeOf<T>.Type, out Hash hash))
                    {
                        return Null;
                    }
                    var list = PresentationManager.Instance.m_PresentationGroups[hash].m_Systems;
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
                CoreSystem.Logger.NotNull(PresentationManager.Instance.m_PresentationGroups[Instance.m_GroupHash].m_Systems[Instance.m_Index], "4");

                try
                {
                    return (T)PresentationManager.Instance.m_PresentationGroups[Instance.m_GroupHash].m_Systems[Instance.m_Index];
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
}
