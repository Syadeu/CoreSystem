using NUnit.Framework;
using Syadeu.Collections;
using Syadeu.Collections.Buffer.LowLevel;
using Syadeu.Presentation.Grid.LowLevel;
using System;
using Unity.Mathematics;
using UnityEngine;

public unsafe sealed class WorldGridTests
{
    [Test]
    public void a0_IndexTest()
    {
        AABB aabb = new AABB(3, new float3(100, 100, 100));
        float cellSize = 2.5f;

        float3 
            //position_1 = new float3(3.4f, 6, 85),
            position_1 = new float3(22, 15.52f, 12),
            position_2 = new float3(22, -15.52f, 12);

        float3 outPos_1, outPos_2;
        int3 location_1, location_2, min, max;
        ulong index_1, index_2;
        bool contains;

        BurstGridMathematics.minMaxLocation(aabb, cellSize, &min, &max);
        Debug.Log($"min : {min}, max : {max}");

        BurstGridMathematics.positionToLocation(in aabb, in cellSize, position_1, &location_1);
        BurstGridMathematics.locationToIndex(location_1, &index_1);

        BurstGridMathematics.indexToLocation(index_1, &location_2);
        BurstGridMathematics.locationToPosition(aabb, cellSize, location_2, &outPos_1);

        BurstGridMathematics.containIndex(aabb, cellSize, index_1, &contains);

        Debug.Log($"test1 {position_1} : {location_1} : {index_1} => {outPos_1} : {location_2}");
        Assert.AreEqual(location_1, location_2);
        Assert.IsTrue(contains);

        BurstGridMathematics.positionToLocation(in aabb, in cellSize, position_2, &location_1);
        BurstGridMathematics.locationToIndex(location_1, &index_1);

        BurstGridMathematics.indexToLocation(index_1, &location_2);
        BurstGridMathematics.locationToPosition(aabb, cellSize, location_2, &outPos_1);

        BurstGridMathematics.containIndex(aabb, cellSize, index_1, &contains);

        Debug.Log($"test2 {position_2} : {location_1} : {index_1} => {outPos_1} : {location_2}");
        Assert.AreEqual(location_1, location_2);
        Assert.IsTrue(contains);
    }

    [Test]
    public void b0_CLRIndexingTest()
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
    public void b1_BurstIndexingTest()
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

            ulong output;
            BurstGridMathematics.positionToIndex(in aabb, in cellSize, in position, &output);
        }
    }

    [Test]
    public void c0_CLRHeavyTest()
    {
        for (int i = 0; i < 1000000; i++)
        {
            func();
        }

        static void func()
        {
            float temp = 9999999;
            temp = math.sqrt(temp);
            temp = math.sqrt(temp);
            temp = math.sqrt(temp);
            temp = math.sqrt(temp);
            temp = math.sqrt(temp);
            temp = math.sqrt(temp);

            temp /= 397;
            temp /= 397;
        }
    }
    [Test]
    public void c0_BurstHeavyTest()
    {
        for (int i = 0; i < 1000000; i++)
        {
            func();
        }

        static void func()
        {
            BurstGridMathematics.heavyTest();
        }
    }

    [Test]
    public void c1_CLRAllocationTest()
    {
        TestStruct[] temp = new TestStruct[10000000];
    }
    [Test]
    public void c1_NativeAllocationTest()
    {
        UnsafeAllocator<TestStruct> temp = new UnsafeAllocator<TestStruct>(10000000, Unity.Collections.Allocator.Temp);
    }

    private struct TestStruct
    {
        public int x;
        public int y;
        public int z;
        public bool test;
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
