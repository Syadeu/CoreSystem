using Syadeu.Database;
using Syadeu.Presentation.Internal;
using System;
using System.Collections;

namespace Syadeu.Presentation.Entities
{
    /// <summary>
    /// <seealso cref="PresentationManager"/>에서 수행할 시스템의 Entity 클래스입니다.
    /// </summary>
    /// <remarks>
    /// 인스턴스를 생성하여 인스턴스 값으로만 작동하도록 제작되었습니다.<br/>
    /// <see langword="static"/> 값이 있으면 <see cref="Dispose"/>를 override 하여 해당 값을 초기화하세요.<br/>
    /// C#에서는 클래스가 소멸해도 해당 인스턴스의 <see langword="static"/> 필드, 프로퍼티 값은 메모리에서 방출되지 않습니다.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public abstract class PresentationSystemEntity<T> : PresentationSystemEntity where T : PresentationSystemEntity
    {
        public override bool IsStartable => true;

        public PresentationSystemEntity()
        {
            ConfigLoader.LoadConfig(this);
        }
        ~PresentationSystemEntity()
        {
            Dispose();
        }

        public override PresentationResult OnInitialize() { return PresentationResult.Normal; }
        public override PresentationResult OnInitializeAsync() { return PresentationResult.Normal; }

        public override PresentationResult OnStartPresentation() { return PresentationResult.Normal; }

        public override PresentationResult BeforePresentation() { return PresentationResult.Normal; }
        public override PresentationResult BeforePresentationAsync() { return PresentationResult.Normal; }

        public override PresentationResult OnPresentation() { return PresentationResult.Normal; }
        public override PresentationResult OnPresentationAsync() { return PresentationResult.Normal; }

        public override PresentationResult AfterPresentation() { return PresentationResult.Normal; }
        public override PresentationResult AfterPresentationAsync() { return PresentationResult.Normal; }

        public override void Dispose() { }

        /// <summary>
        /// <see cref="OnInitialize"/> 혹은 <see cref="OnInitializeAsync"/> 에서만 수행되야됩니다.
        /// </summary>
        /// <typeparam name="TA"></typeparam>
        /// <param name="setter"></param>
        protected void RequestSystem<TA>(Action<TA> setter) where TA : PresentationSystemEntity
            => PresentationManager.RegisterRequestSystem<T, TA>(setter);

        protected CoreRoutine StartCoroutine(IEnumerator cor) => CoreSystem.StartUnityUpdate(this, cor);
        protected CoreRoutine StartBackgroundCoroutine(IEnumerator cor) => CoreSystem.StartBackgroundUpdate(this, cor);
        protected void StopCoroutine(CoreRoutine routine) => CoreSystem.RemoveUnityUpdate(routine);
        protected void StopBackgroundCoroutine(CoreRoutine routine) => CoreSystem.RemoveBackgroundUpdate(routine);
    }
}
