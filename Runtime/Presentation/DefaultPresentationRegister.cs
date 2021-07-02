//#undef UNITY_ADDRESSABLES


#if UNITY_EDITOR
#endif

#if UNITY_ADDRESSABLES
#endif


using UnityEngine.Assertions;

namespace Syadeu.Presentation
{
    internal sealed class DefaultPresentationRegister : IPresentationRegister
    {
        public void Register()
        {
            PresentationManager.RegisterSystem(new ScenePresentationSystem());
            PresentationManager.RegisterSystem(new TestSystem());
            PresentationManager.RegisterSystem(new Test123System());
        }
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
