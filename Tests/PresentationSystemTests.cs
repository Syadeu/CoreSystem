﻿using NUnit.Framework;
using Syadeu;
using Syadeu.Presentation;
using Syadeu.Presentation.Entities;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

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
            CoreSystem.Log(Channel.Core, "Test123System Starting");

            CoreSystem.NotNull(testSystem);

            return base.OnStartPresentation();
        }

        protected override PresentationResult BeforePresentation()
        {
            CoreSystem.LogError(Channel.Core, "NEVER RUN THIS");
            return base.BeforePresentation();
        }
        protected override PresentationResult BeforePresentationAsync()
        {
            CoreSystem.LogError(Channel.Core, "NEVER RUN THIS");
            return base.BeforePresentationAsync();
        }
        protected override PresentationResult OnPresentation()
        {
            CoreSystem.LogError(Channel.Core, "NEVER RUN THIS");
            return base.OnPresentation();
        }
        protected override PresentationResult OnPresentationAsync()
        {
            CoreSystem.LogError(Channel.Core, "NEVER RUN THIS");
            return base.OnPresentationAsync();
        }
        protected override PresentationResult AfterPresentation()
        {
            CoreSystem.LogError(Channel.Core, "NEVER RUN THIS");
            return base.AfterPresentation();
        }
        protected override PresentationResult AfterPresentationAsync()
        {
            CoreSystem.LogError(Channel.Core, "NEVER RUN THIS");
            return base.AfterPresentationAsync();
        }
    }

    [UnityTest]
    public IEnumerator RunPresentationGroupTest()
    {
        PresentationSystemGroup<PresentationTestGroup>.Start();

        CoreSystem.NotNull(PresentationSystem<TestSystem>.System);
        CoreSystem.NotNull(PresentationSystem<Test123System>.System);

        yield return new WaitForSeconds(10);

        PresentationSystemGroup<PresentationTestGroup>.Stop();
    }
}
