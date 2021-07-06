using NUnit.Framework;
using Syadeu;
using Syadeu.Presentation;
using Syadeu.Presentation.Entities;

public class PresentationSystemTests
{
    internal sealed class PresentationTestGroup : PresentationRegisterEntity
    {
        public override void Register()
        {
            RegisterSystem(
                typeof(TestSystem),
                typeof(Test123System));
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

    [Test]
    public void RunPresentationGroupTest()
    {
        PresentationSystemGroup<PresentationTestGroup>.Start();

        CoreSystem.IsNotNull(PresentationSystem<TestSystem>.GetSystem());
        CoreSystem.IsNotNull(PresentationSystem<Test123System>.GetSystem());
    }
}
