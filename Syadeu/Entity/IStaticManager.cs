﻿using System;

namespace Syadeu
{
    public enum SystemFlag
    {
        None = 0,

        MainSystem = 1 << 0,
        SubSystem = 1 << 1,
        MonoSystems = MainSystem | SubSystem,

        Data = 1 << 2,

        All = ~0
    }
    public interface IStaticManager
    {
        /// <summary>
        /// 인스턴트가 생성될때 한번 실행할 함수입니다.
        /// </summary>
        void OnInitialize();
        /// <summary>
        /// 초기화 작업이 완료되고 마지막에 실행되는 함수입니다.
        /// </summary>
        void OnStart();
        /// <summary>
        /// 초기화 함수입니다.
        /// </summary>
        void Initialize(SystemFlag flag = SystemFlag.SubSystem);
    }
    public interface IStaticDataManager : IStaticManager, IDisposable
    {
        bool Disposed { get; }
    }
    public interface IStaticMonoManager : IStaticManager
    {
        /// <summary>
        /// Hierarchy에서 표시될 이름을 설정합니다.
        /// 런타임에 아무런 영향을 주지 않습니다.
        /// </summary>
        string DisplayName { get; }
        /// <summary>
        /// true 일 경우, 씬이 전환되어도 파괴되지 않습니다.
        /// </summary>
        bool DontDestroy { get; }
        /// <summary>
        /// Hierarchy 에서 이 매니저 객체를 표시할지 결정합니다.
        /// <see cref="Mono.SyadeuSettings.m_VisualizeObjects"/> 가 true일 경우 영향받지 않습니다.
        /// </summary>
        bool HideInHierarchy { get; }

#pragma warning disable IDE1006 // Naming Styles
        UnityEngine.GameObject gameObject { get; }
        UnityEngine.Transform transform { get; }
#pragma warning restore IDE1006 // Naming Styles
    }
}
