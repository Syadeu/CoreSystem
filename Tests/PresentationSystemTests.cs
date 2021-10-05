using NUnit.Framework;
using Syadeu;
using Syadeu.Presentation;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

public class PresentationSystemTests
{
    internal sealed class PresentationTestGroup : PresentationGroupEntity
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

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<Test123System>((other) => testsystem = other);

            return base.OnInitialize();
        }

        protected override PresentationResult OnPresentation()
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
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<TestSystem>((other) => testSystem = other);

            return base.OnInitialize();
        }
        protected override PresentationResult OnStartPresentation()
        {
            CoreSystem.Logger.Log(Channel.Core, "Test123System Starting");

            CoreSystem.Logger.NotNull(testSystem);

            return base.OnStartPresentation();
        }

        protected override PresentationResult BeforePresentation()
        {
            CoreSystem.Logger.LogError(Channel.Core, "NEVER RUN THIS");
            return base.BeforePresentation();
        }
        protected override PresentationResult BeforePresentationAsync()
        {
            CoreSystem.Logger.LogError(Channel.Core, "NEVER RUN THIS");
            return base.BeforePresentationAsync();
        }
        protected override PresentationResult OnPresentation()
        {
            CoreSystem.Logger.LogError(Channel.Core, "NEVER RUN THIS");
            return base.OnPresentation();
        }
        protected override PresentationResult OnPresentationAsync()
        {
            CoreSystem.Logger.LogError(Channel.Core, "NEVER RUN THIS");
            return base.OnPresentationAsync();
        }
        protected override PresentationResult AfterPresentation()
        {
            CoreSystem.Logger.LogError(Channel.Core, "NEVER RUN THIS");
            return base.AfterPresentation();
        }
        protected override PresentationResult AfterPresentationAsync()
        {
            CoreSystem.Logger.LogError(Channel.Core, "NEVER RUN THIS");
            return base.AfterPresentationAsync();
        }
    }

    [UnityTest]
    public IEnumerator RunPresentationGroupTest()
    {
        PresentationSystemGroup<PresentationTestGroup>.Start();

        CoreSystem.Logger.NotNull(PresentationSystem<TestSystem>.System);
        CoreSystem.Logger.NotNull(PresentationSystem<Test123System>.System);

        yield return new WaitForSeconds(10);

        PresentationSystemGroup<PresentationTestGroup>.Stop();
    }

    public struct TestComponent_1 : IEntityComponent
    {
        public int m_TestInt;
    }
    public struct TestComponent_2 : IEntityComponent
    {
        public float m_TestSingle;
    }
    public struct TestComponent_3 : IEntityComponent
    {
        public float m_TestSingle;
    }
    public struct TestComponent_4 : IEntityComponent
    {
        public float m_TestSingle;
        public bool m_TestBoolen;
    }

    [Test]
    public void QueryTest()
    {
        TypeInfo
            a0 = ComponentType<TestComponent_1>.TypeInfo,
            a1 = ComponentType<TestComponent_2>.TypeInfo,
            a2 = ComponentType<TestComponent_3>.TypeInfo,
            a3 = ComponentType<TestComponent_4>.TypeInfo;

        ComponentTypeQuery
            query1 = ComponentTypeQuery.Combine(a0, a1),
            query2 = ComponentTypeQuery.Combine(a1, a0),
            query3 = ComponentTypeQuery.Combine(a3, a2),
            query4 = ComponentTypeQuery.Combine(a2, a0);

        Debug.Log($"{query1.GetHashCode()} == {query2.GetHashCode()}");
        Debug.Log($"{query3.GetHashCode()}");
        Debug.Log($"{query4.GetHashCode()}");
    }
}
