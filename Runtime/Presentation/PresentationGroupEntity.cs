﻿using Syadeu.Presentation.Internal;
using System;

namespace Syadeu.Presentation
{
    /// <summary>
    /// <see cref="PresentationManager"/>에게 시스템을 등록하기 위한 레지스터 클래스입니다. 참조한 클래스는 그룹이 됩니다.
    /// </summary>
    /// <remarks>
    /// 시스템 그룹으로 묶을 클래스에 이 <see langword="abstract"/>를 참조하고, 
    /// <see cref="Register"/> 메소드에서 <see cref="RegisterSystem(System.Type[])"/>을 호출하여 시스템 그룹으로 묶을 수 있습니다.<br/>
    /// 등록할 시스템 객체는 <see cref="PresentationSystemEntity{T}"/>를 참조하여야 합니다.<br/>
    /// <br/>
    /// 묶은 시스템은 이후 <see cref="PresentationSystemGroup{T}"/>로 그룹을 호출할 수 있습니다.
    /// </remarks>
    public abstract class PresentationGroupEntity : IPresentationRegister
    {
        public virtual bool StartOnInitialize => false;
        public virtual Type DependenceGroup => null;
        public virtual SceneReference DependenceScene => null;

        public abstract void Register();

        /// <summary>
        /// 해당 시스템을 등록합니다.
        /// </summary>
        /// <remarks>
        /// <see cref="Register"/>에서만 실행되어야 됩니다.
        /// </remarks>
        /// <param name="systems"></param>
        protected void RegisterSystem(params System.Type[] systems) => PresentationManager.RegisterSystem(GetType(), DependenceScene, systems);
    }
}
