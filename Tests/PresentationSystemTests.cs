using NUnit.Framework;
using Syadeu;
using Syadeu.Collections;
using Syadeu.Presentation;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Grid;
using System;
using System.Collections;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
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
    public void HalfTest()
    {
        Debug.Log($"{UnsafeUtility.SizeOf<half>()}");
    }

    [UnityTest]
    public IEnumerator QueryTest()
    {
        //yield return new WaitForSeconds(1);
        yield return PresentationSystem<DefaultPresentationGroup, EntitySystem>.GetAwaiter();

        TypeInfo
            a0 = TypeStatic<TestComponent_1>.TypeInfo,
            a1 = TypeStatic<TestComponent_2>.TypeInfo,
            a2 = TypeStatic<TestComponent_3>.TypeInfo,
            a3 = TypeStatic<TestComponent_4>.TypeInfo;

        ComponentTypeQuery
            query1 = ComponentTypeQuery.Combine(a0, a1),
            query2 = ComponentTypeQuery.Combine(a1, a0),
            query3 = ComponentTypeQuery.Combine(a3, a2, a0),
            query4 = ComponentTypeQuery.Combine(a2, a0, a3),

            query5 = ComponentTypeQuery.Combine(a2, a3, a1),

            query6 = ComponentTypeQuery.Combine(a2, a0, a3, a1),
            query8 = ComponentTypeQuery.Combine(a2, a0, a3, a1) ^ ComponentTypeQuery.ReadWrite,
            query9 = ComponentTypeQuery.Combine(a2, a0, a3, a1) ^ ComponentTypeQuery.WriteOnly;

        Debug.Log($"Default: {query5} :: a0: {a0.GetHashCode()}");

        Debug.Log($"{query5.Has(a0)}");

        //Debug.Log($"{query9.GetHashCode() | ComponentTypeQuery.WriteOnly} == {query9 ^ ComponentTypeQuery.WriteOnly}\n" +
        //    $"{(query9.GetHashCode() | ComponentTypeQuery.WriteOnly) == (query9 ^ ComponentTypeQuery.WriteOnly).GetHashCode()}");



        //if ((query9.GetHashCode() ^ ComponentTypeQuery.ReadOnly) == (query9.GetHashCode() ^ ComponentTypeQuery.ReadOnly))
        //{
        //    Debug.Log("query9 is write only");
        //}

        //if ((query9.GetHashCode() & a0.GetHashCode()) == a0.GetHashCode())
        //{
        //    Debug.Log("qua9 has a0");
        //}

        //var temp = query8 ^ ComponentTypeQuery.ReadOnly;

        //Debug.Log($"original : {query6} :: write : {query9} \n readwrite {query8}");

        //Debug.Log($"rww{(query9 ^ ComponentTypeQuery.ReadOnly)}, rd{(query9 ^ ComponentTypeQuery.WriteOnly)}");
        //Debug.Log($"{ComponentTypeQuery.ReadOnly} :: {ComponentTypeQuery.WriteOnly}");
        //Debug.Log(
        //    $"{temp.GetHashCode()} == {query9.GetHashCode()} ? {temp.GetHashCode() == query9.GetHashCode()}");

        //Debug.Log(
        //    $"{query1.GetHashCode()} == {query2.GetHashCode()} ? {query1.GetHashCode() == query2.GetHashCode()}");
        //Debug.Log($"{query3.GetHashCode()} == {query4.GetHashCode()} ? {query3.GetHashCode() == query4.GetHashCode()}");
        //Debug.Log($"{query5.GetHashCode()} == {query6.GetHashCode()} ? {query5.GetHashCode() == query6.GetHashCode()}");
        //Debug.Log($"{query7.GetHashCode()} == {query1.GetHashCode()} ? {query7.GetHashCode() == query1.GetHashCode()}");
    }

    [Test]
    public void CommonTypeInfo()
    {
        Log<TypeInfo>();

        void Log<T>() where T : struct
        {
            Debug.Log($"{TypeHelper.TypeOf<T>.Type.FullName} = {UnsafeUtility.SizeOf<T>()} : {UnsafeUtility.AlignOf<T>()}");
        }
    }

    [Test]
    public void CheckSumTest()
    {
        byte
            a = 0x84,
            b = 0xF2,
            c = 0x10,
            d = 0x55

            ;
        CheckSum checkSum = CheckSum.CalculateBytes(new byte[] { a, b, c, d });
        //$"{checkSum} : {Convert.ToString(checkSum, toBase: 2)}".ToLog();

        Assert.AreEqual(0x25, checkSum);

        bool check2 = checkSum.Validate(new byte[] { a, b, c, d });
        Assert.IsTrue(check2); 
        //Assert.AreEqual(check2, 0);
    }
}
