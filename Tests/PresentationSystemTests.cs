using NUnit.Framework;
using Syadeu;
using Syadeu.Collections;
using Syadeu.Presentation;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Grid;
using System;
using System.Collections;
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
}

public sealed class WorldGridTests
{
    [Test]
    public unsafe void a0_IndexTest()
    {
        AABB aabb = new AABB(0, new float3(100, 100, 100));
        float cellSize = 2.5f;

        float3 
            //position_1 = new float3(3.4f, 6, 85),
            position_1 = new float3(22, 15.52f, 12),
            position_2 = new float3(22, -15.52f, 12);

        float3 outPos_1, outPos_2;
        int3 location_1, location_2, min, max;
        int index_1, index_2;
        bool contains;

        BurstGridMathematics.minMaxLocation(aabb, cellSize, &min, &max);
        Debug.Log($"min : {min}, max : {max}");

        BurstGridMathematics.positionToLocation(in aabb, in cellSize, position_1, &location_1);
        BurstGridMathematics.locationToIndex(aabb, cellSize, location_1, &index_1);

        BurstGridMathematics.indexToLocation(aabb, cellSize, index_1, &location_2);
        BurstGridMathematics.locationToPosition(aabb, cellSize, location_2, &outPos_1);

        BurstGridMathematics.containIndex(aabb, cellSize, index_1, &contains);

        Debug.Log($"test1 {position_1} : {location_1} : {index_1} => {outPos_1} : {location_2}");
        Assert.AreEqual(location_1, location_2);
        Assert.IsTrue(contains);

        BurstGridMathematics.positionToLocation(in aabb, in cellSize, position_2, &location_1);
        BurstGridMathematics.locationToIndex(aabb, cellSize, location_1, &index_1);

        BurstGridMathematics.indexToLocation(aabb, cellSize, index_1, &location_2);
        BurstGridMathematics.locationToPosition(aabb, cellSize, location_2, &outPos_1);

        BurstGridMathematics.containIndex(aabb, cellSize, index_1, &contains);

        Debug.Log($"test2 {position_2} : {location_1} : {index_1} => {outPos_1} : {location_2}");
        Assert.AreEqual(location_1, location_2);
        Assert.IsTrue(contains);
    }

    [Test]
    public unsafe void b0_CLRIndexingTest()
    {
        for (int i = 0; i < 1000000; i++)
        {
            func();
        }

        static void func()
        {
            AABB aabb = new AABB(0, new float3(100, 100, 100));
            float cellSize = 2.5f;
            float3 position = new float3(22, 15.52f, 12);

            float
                half = cellSize * .5f,
                firstCenterX = aabb.min.x + half,
                firstCenterZ = aabb.max.z - half;

            int
                x = math.abs(Convert.ToInt32((position.x - firstCenterX) / cellSize)),
                z = math.abs(Convert.ToInt32((position.z - firstCenterZ) / cellSize)),
                y = Convert.ToInt32(math.round(position.y));

            int
                zSize = Convert.ToInt32(math.floor(aabb.size.z / cellSize)),
                calculated = zSize * z + x,
                index;

            if (y == 0)
            {
                index = calculated;
                index ^= 0b1011101111;
                return;
            }

            int
                xSize = Convert.ToInt32(math.floor(aabb.size.x / cellSize)),
                dSize = xSize * zSize;

            index = calculated + (dSize * math.abs(y));
            index ^= 0b1011101111;

            if (y < 0)
            {
                index *= -1;
            }
        }
    }
    [Test]
    public unsafe void b1_BurstIndexingTest()
    {
        for (int i = 0; i < 1000000; i++)
        {
            func();
        }

        static void func()
        {
            AABB aabb = new AABB(0, new float3(100, 100, 100));
            float cellSize = 2.5f;
            float3 position = new float3(22, 15.52f, 12);

            int output;
            BurstGridMathematics.positionToIndex(in aabb, in cellSize, in position, &output);
        }
    }

    private float3 RandomFloat3(float min, float max)
    {
        return new float3(
         UnityEngine.Random.Range(min, max),
         UnityEngine.Random.Range(min, max),
         UnityEngine.Random.Range(min, max)
         );
    }
}
