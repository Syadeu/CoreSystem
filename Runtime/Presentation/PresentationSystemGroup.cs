#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Internal;
using System;
using System.Collections.Generic;

namespace Syadeu.Presentation
{
    /// <summary>
    /// <see cref="PresentationManager"/>에서 수행되는 시스템의 그룹입니다.
    /// </summary>
    /// <remarks>
    /// 특정 시스템만 불러오려면 <seealso cref="PresentationSystem{T}"/>으로 호출하세요.
    /// </remarks>
    /// <typeparam name="TGroup">
    /// <seealso cref="PresentationGroupEntity"/> 를 상속받는 시스템 그룹.
    /// </typeparam>
    public struct PresentationSystemGroup<TGroup> : IPresentationSystemGroup, IEquatable<PresentationSystemGroup<TGroup>>
        where TGroup : PresentationGroupEntity
    {
        public static readonly PresentationSystemGroup<TGroup> Null = new PresentationSystemGroup<TGroup>(Hash.Empty);
        private static PresentationSystemGroup<TGroup> s_Instance = Null;
        private static PresentationSystemGroup<TGroup> Instance
        {
            get
            {
                if (!((IValidation)s_Instance).IsValid())
                {
                    s_Instance = new PresentationSystemGroup<TGroup>(Hash.NewHash(TypeHelper.TypeOf<TGroup>.Name));
                }
                return s_Instance;
            }
        }

        private readonly Hash m_GroupHash;

        #region Interface Implement Properties

        Hash IPresentationSystemGroup.GroupHash => m_GroupHash;
        IReadOnlyList<PresentationSystemEntity> IPresentationSystemGroup.Systems
        {
            get
            {
                if (!IsValid())
                {
                    throw new Exception();
                }
                return PresentationManager.Instance.m_PresentationGroups[m_GroupHash].Systems;
            }
        }

        #endregion

        /// <inheritdoc cref="IPresentationSystemGroup.Systems"/>
        public static IReadOnlyList<PresentationSystemEntity> Systems => ((IPresentationSystemGroup)Instance).Systems;

        private PresentationSystemGroup(Hash groupHash)
        {
            m_GroupHash = groupHash;
        }

        #region Interface Implement Methods

        bool IValidation.IsValid() => !m_GroupHash.Equals(Hash.Empty);
        ICustomYieldAwaiter IPresentationSystemGroup.Start() => PresentationManager.Instance.StartPresentation(m_GroupHash);
        void IPresentationSystemGroup.Stop() => PresentationManager.Instance.StopPresentation(m_GroupHash);

        bool IEquatable<IPresentationSystemGroup>.Equals(IPresentationSystemGroup other)
        {
            return m_GroupHash.Equals(other.GroupHash);
        }
        bool IEquatable<PresentationSystemGroup<TGroup>>.Equals(PresentationSystemGroup<TGroup> other)
        {
            return m_GroupHash.Equals(other.m_GroupHash);
        }

        #endregion

        public static TSystem GetSystem<TSystem>()
            where TSystem : PresentationSystemEntity
        {
            return PresentationManager.GetSystem<TGroup, TSystem>(out _);
        }

        /// <summary>
        /// 이 그룹이 유효한지 반환합니다.
        /// </summary>
        /// <returns></returns>
        public static bool IsValid() => ((IValidation)Instance).IsValid();
        /// <inheritdoc cref="IPresentationSystemGroup.Start"/>
        public static ICustomYieldAwaiter Start() => ((IPresentationSystemGroup)Instance).Start();
        /// <inheritdoc cref="IPresentationSystemGroup.Stop"/>
        public static void Stop() => ((IPresentationSystemGroup)Instance).Stop();
    }
}
