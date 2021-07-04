﻿namespace Syadeu.Presentation
{
    /// <summary>
    /// <see cref="PresentationManager"/>에게 시스템을 등록하기 위한 레지스터 클래스입니다.
    /// </summary>
    public abstract class PresentationRegisterEntity : IPresentationRegister
    {
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
