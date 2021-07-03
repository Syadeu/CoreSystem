//#undef UNITY_ADDRESSABLES


#if UNITY_EDITOR
#endif

#if UNITY_ADDRESSABLES
#endif


using UnityEngine.Assertions;

namespace Syadeu.Presentation
{
    internal sealed class DefaultPresentationGroup : IPresentationRegister
    {
        public void Register()
        {
            System.Type t = typeof(DefaultPresentationGroup);
            PresentationManager.RegisterSystem(t, new ScenePresentationSystem());
            PresentationManager.RegisterSystem(t, new TestSystem());
            PresentationManager.RegisterSystem(t, new Test123System());
        }
    }
    public abstract class PresentationRegisterEntity : IPresentationRegister
    {
        public abstract void Register();

        protected void RegisterSystem<T>(params T[] systems) where T : IPresentationSystem
            => PresentationManager.RegisterSystem(GetType(), systems);
    }
    public sealed class TestSystem : PresentationSystemEntity<TestSystem>
    {
        Test123System testsystem;

        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => true;
        public override bool EnableAfterPresentation => false;

        public override PresentationResult OnInitialize()
        {
            RequestSystem<Test123System>((other) => testsystem = other);

            return base.OnInitialize();
        }

        public override PresentationResult OnPresentation()
        {
            //$"123123 system == null = {testsystem == null}".ToLog();
            Assert.IsNotNull(testsystem);
            return base.OnPresentation();
        }
    }
    public sealed class Test123System : PresentationSystemEntity<Test123System>
    {
        TestSystem testSystem;

        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => true;
        public override bool EnableAfterPresentation => false;

        public override PresentationResult OnInitialize()
        {
            RequestSystem<TestSystem>((other) => testSystem = other);

            return base.OnInitialize();
        }

        public override PresentationResult OnPresentation()
        {
            //$"system == null = {testSystem == null}".ToLog();
            Assert.IsNotNull(testSystem);
            return base.OnPresentation();
        }
    }
}
