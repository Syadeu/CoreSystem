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
    /// <typeparam name="T"></typeparam>
    public struct PresentationSystemGroup<T> : IPresentationSystemGroup where T : PresentationGroupEntity
    {
        public static PresentationSystemGroup<T> Null = new PresentationSystemGroup<T>(Hash.Empty);
        private static PresentationSystemGroup<T> s_Instance = Null;
        private static PresentationSystemGroup<T> Instance
        {
            get
            {
                if (!((IValidation)s_Instance).IsValid())
                {
                    s_Instance = new PresentationSystemGroup<T>(Hash.NewHash(TypeHelper.TypeOf<T>.Name));
                }
                return s_Instance;
            }
        }

        private readonly Hash m_GroupHash;
        IReadOnlyList<PresentationSystemEntity> IPresentationSystemGroup.Systems
        {
            get
            {
                if (!IsValid())
                {
                    throw new Exception();
                }
                return PresentationManager.Instance.m_PresentationGroups[m_GroupHash].m_Systems;
            }
        }

        /// <inheritdoc cref="IPresentationSystemGroup.Systems"/>
        public static IReadOnlyList<PresentationSystemEntity> Systems => ((IPresentationSystemGroup)Instance).Systems;

        private PresentationSystemGroup(Hash groupHash)
        {
            m_GroupHash = groupHash;
        }

        bool IValidation.IsValid() => !m_GroupHash.Equals(Hash.Empty);
        ICustomYieldAwaiter IPresentationSystemGroup.Start() => PresentationManager.Instance.StartPresentation(m_GroupHash);
        void IPresentationSystemGroup.Stop() => PresentationManager.Instance.StopPresentation(m_GroupHash);

        public static bool IsValid() => ((IValidation)Instance).IsValid();
        /// <inheritdoc cref="IPresentationSystemGroup.Start"/>
        public static ICustomYieldAwaiter Start() => ((IPresentationSystemGroup)Instance).Start();
        /// <inheritdoc cref="IPresentationSystemGroup.Stop"/>
        public static void Stop() => ((IPresentationSystemGroup)Instance).Stop();
    }
}
