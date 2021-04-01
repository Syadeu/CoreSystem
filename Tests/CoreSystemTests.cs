using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using Syadeu;
using UnityEngine.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using Syadeu.Mono;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System;

public class CoreSystemTests
{
    public void TestMath()
    {
        double test = 99999999999;
        test = System.Math.Sqrt(test);
        test = System.Math.Sqrt(test);
        test = System.Math.Sqrt(test);
        test = System.Math.Sqrt(test);
        test = System.Math.Sqrt(test);
        test = System.Math.Sqrt(test);
        test = System.Math.Sqrt(test);
        test = System.Math.Sqrt(test);
        test = System.Math.Sqrt(test);
        test = System.Math.Sqrt(test);
        test = System.Math.Sqrt(test);
        test = System.Math.Sqrt(test);

        test /= 3;
        test /= 3;
        test /= 3;
        test /= 3;
        test /= 3;
        test /= 3;
        test /= 3;
    }
    public class TestClass
    {
        public TestInnerClass TestObj;
    }
    public class TestInnerClass
    {
        public int testInt = 0;
    }

    public IEnumerator TestWhileCoreRoutine()
    {
        Debug.Log("Routine Start");
        yield return null;

        while (true)
        {
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator ParallelJobTest()
    {
        int[] testIntList = new int[1000000];

        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        BackgroundJob job0 = CoreSystem.AddBackgroundJob(() =>
        {
            for (int i = 0; i < testIntList.Length; i++)
            {
                TestMath();
            }
        });
        yield return new WaitForBackgroundJob(job0);
        stopwatch.Stop();
        $"single job takes = {stopwatch.ElapsedMilliseconds}".ToLog();

        stopwatch.Reset();
        stopwatch.Start();
        BackgroundJob job1 = BackgroundJob.ParallelFor(testIntList, (i, value) =>
        {
            TestMath();

        }, chunkSize: 50000);

        yield return new WaitForBackgroundJob(job1);
        stopwatch.Stop();
        $"parallel job takes = {stopwatch.ElapsedMilliseconds}".ToLog();

        stopwatch.Reset();
        stopwatch.Start();
        var result = Parallel.For(0, testIntList.Length, (i) =>
        {
            TestMath();
        });
        while (!result.IsCompleted)
        {
            yield return null;
        }
        stopwatch.Stop();
        $"system.Threading.Task.Parallel job takes = {stopwatch.ElapsedMilliseconds}".ToLog();
    }

    [UnityTest]
    public IEnumerator UnityUpdateTest()
    {
        CoreRoutine routine = CoreSystem.StartUnityUpdate(this, TestWhileCoreRoutine());

        Debug.Log("Wait 2 seconds");
        yield return new WaitForSeconds(2);

        Debug.Log($"{routine.IsRunning} isrunning");
        yield return new WaitForSeconds(1);

        BackgroundJob job = CoreSystem.RemoveUnityUpdate(routine);
        yield return new WaitForBackgroundJob(job);
        Debug.Log("remove routine");

        Debug.Log($"{routine.IsRunning} isrunning");
    }
    [UnityTest]
    public IEnumerator BackgroundUpdateTest()
    {
        CoreRoutine routine = CoreSystem.StartBackgroundUpdate(this, TestWhileCoreRoutine());

        Debug.Log("Wait 2 seconds");
        yield return new WaitForSeconds(2);

        Debug.Log($"{routine.IsRunning} isrunning");
        yield return new WaitForSeconds(1);

        BackgroundJob job = CoreSystem.RemoveBackgroundUpdate(routine);
        yield return new WaitForBackgroundJob(job);
        Debug.Log("remove routine");

        Debug.Log($"{routine.IsRunning} isrunning");
    }

    #region Job Test

    //[UnityTest]
    public IEnumerator ErrorBackgroundJobTest()
    {
        var job1 = CoreSystem.AddBackgroundJob(ErrorMethod);

        yield return new WaitForBackgroundJob(job1);

        var job2 = CoreSystem.AddBackgroundJob(() =>
        {
            Vector3.Distance(Vector3.zero, Vector3.one);
        });

        yield return new WaitForBackgroundJob(job2);
    }
    //[Test]
    public void ErrorForegroundJobTest()
    {
        try
        {
            CoreSystem.AddForegroundJob(ErrorMethod);
        }
        catch (CoreSystemException)
        {
        }
    }
    void ErrorMethod()
    {
        TestClass test = null;
        test.TestObj.testInt = 5;
    }

    [Test]
    public void ACreateNewBackgroundJobWorker()
    {
        CoreSystem.CreateNewBackgroundJobWorker(true);

        CoreSystem.CreateNewBackgroundJobWorker(false);
        CoreSystem.CreateNewBackgroundJobWorker(false);
        CoreSystem.CreateNewBackgroundJobWorker(false);
        CoreSystem.CreateNewBackgroundJobWorker(false);
        CoreSystem.CreateNewBackgroundJobWorker(false);
    }
    [Test]
    public void GetBackgroundWorker()
    {
        CoreSystem.GetBackgroundWorker(out int _);
    }
    [Test]
    public void AddBackgroundJob()
    {
        CoreSystem.AddBackgroundJob(0, new BackgroundJob(TestMath));
        CoreSystem.AddBackgroundJob(new BackgroundJob(TestMath));
        CoreSystem.AddBackgroundJob(0, TestMath, out BackgroundJob _);
        CoreSystem.AddBackgroundJob(TestMath);
    }
    [Test]
    public void AddForegroundJob()
    {
        CoreSystem.AddForegroundJob(new ForegroundJob(TestMath));
        CoreSystem.AddForegroundJob(TestMath);
    }
    [UnityTest]
    public IEnumerator ConnectBackgroundJobOnly()
    {
        BackgroundJob job = CoreSystem.AddBackgroundJob(TestMath);
        BackgroundJob job1 = CoreSystem.AddBackgroundJob(TestMath);
        BackgroundJob job2 = CoreSystem.AddBackgroundJob(TestMath);
        job.ConnectJob(job1).ConnectJob(job2);

        int count = 0;
        while (!job.IsDone)
        {
            Assert.IsFalse(count > 1000);

            count++;
            yield return null;
        }

        if (!job.IsDone) throw new AssertionException("job is not done");
        if (!job1.IsDone) throw new AssertionException("job1 is not done");
        if (!job2.IsDone) throw new AssertionException("job2 is not done");
    }
    [UnityTest]
    public IEnumerator ConnectForegroundJobOnly()
    {
        ForegroundJob job = CoreSystem.AddForegroundJob(TestMath);
        ForegroundJob job1 = CoreSystem.AddForegroundJob(TestMath);
        ForegroundJob job2 = CoreSystem.AddForegroundJob(TestMath);
        job.ConnectJob(job1).ConnectJob(job2);

        int count = 0;
        while (!job.IsDone)
        {
            Assert.IsFalse(count > 1000);

            count++;
            yield return null;
        }

        if (!job.IsDone) throw new AssertionException("job is not done");
        if (!job1.IsDone) throw new AssertionException("job1 is not done");
        if (!job2.IsDone) throw new AssertionException("job2 is not done");
    }
    [UnityTest]
    public IEnumerator ConnectMixedJob()
    {
        BackgroundJob job = CoreSystem.AddBackgroundJob(TestMath);
        BackgroundJob job1 = CoreSystem.AddBackgroundJob(TestMath);
        BackgroundJob job2 = CoreSystem.AddBackgroundJob(TestMath);

        ForegroundJob job3 = CoreSystem.AddForegroundJob(TestMath);
        ForegroundJob job4 = CoreSystem.AddForegroundJob(TestMath);
        ForegroundJob job5 = CoreSystem.AddForegroundJob(TestMath);

        job.ConnectJob(job1).ConnectJob(job2).ConnectJob(job3).ConnectJob(job4)
            .ConnectJob(job5);

        int count = 0;
        while (!job.IsDone)
        {
            Assert.IsFalse(count > 1000);

            count++;
            yield return null;
        }

        if (!job.IsDone) throw new AssertionException("job is not done");
        if (!job1.IsDone) throw new AssertionException("job1 is not done");
        if (!job2.IsDone) throw new AssertionException("job2 is not done");
        if (!job3.IsDone) throw new AssertionException("job3 is not done");
        if (!job4.IsDone) throw new AssertionException("job4 is not done");
        if (!job5.IsDone) throw new AssertionException("job5 is not done");
    }

    [UnityTest]
    public IEnumerator SyncBackgroundJobStart()
    {
        BackgroundJob job = new BackgroundJob(TestMath);
        BackgroundJob job1 = new BackgroundJob(TestMath);
        BackgroundJob job2 = new BackgroundJob(TestMath);
        job.ConnectJob(job1).ConnectJob(job2);

        job.Start();
        int count = 0;
        while (!job.IsDone)
        {
            Assert.IsFalse(count > 1000);

            count++;
            yield return null;
        }

        if (!job.IsDone) throw new AssertionException("job is not done");
        if (!job1.IsDone) throw new AssertionException("job1 is not done");
        if (!job2.IsDone) throw new AssertionException("job2 is not done");
    }
    [UnityTest]
    public IEnumerator SyncForegroundJobStart()
    {
        ForegroundJob job = new ForegroundJob(TestMath);
        ForegroundJob job1 = new ForegroundJob(TestMath);
        ForegroundJob job2 = new ForegroundJob(TestMath);
        job.ConnectJob(job1).ConnectJob(job2);

        job.Start();
        int count = 0;
        while (!job.IsDone)
        {
            Assert.IsFalse(count > 1000);

            count++;
            yield return null;
        }

        if (!job.IsDone) throw new AssertionException("job is not done");
        if (!job1.IsDone) throw new AssertionException("job1 is not done");
        if (!job2.IsDone) throw new AssertionException("job2 is not done");
    }
    [UnityTest]
    public IEnumerator SyncMixedJobStart()
    {
        BackgroundJob job = new BackgroundJob(TestMath);
        BackgroundJob job1 = new BackgroundJob(TestMath);
        BackgroundJob job2 = new BackgroundJob(TestMath);
        ForegroundJob job3 = new ForegroundJob(TestMath);
        ForegroundJob job4 = new ForegroundJob(TestMath);
        ForegroundJob job5 = new ForegroundJob(TestMath);
        job.ConnectJob(job1).ConnectJob(job2).ConnectJob(job3).ConnectJob(job4)
            .ConnectJob(job5);

        job.Start();
        int count = 0;
        while (!job.IsDone)
        {
            Assert.IsFalse(count > 1000);

            count++;
            yield return null;
        }

        if (!job.IsDone) throw new AssertionException("job is not done");
        if (!job1.IsDone) throw new AssertionException("job1 is not done");
        if (!job2.IsDone) throw new AssertionException("job2 is not done");
        if (!job3.IsDone) throw new AssertionException("job3 is not done");
        if (!job4.IsDone) throw new AssertionException("job4 is not done");
        if (!job5.IsDone) throw new AssertionException("job5 is not done");
    }

    #endregion
}

public class GridTests
{
    static Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 1000);

    private GridManager.Grid CreateTestGrid()
    {
        int gridIdx = GridManager.CreateGrid(bounds, 1, false);
        ref GridManager.Grid grid = ref GridManager.GetGrid(in gridIdx);

        return grid;
    }

    [Test]
    public void GridForTest()
    {
        int gridIdx = GridManager.CreateGrid(bounds, 1, false);
        ref GridManager.Grid grid = ref GridManager.GetGrid(in gridIdx);
        
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

        stopwatch.Start();

        grid.For((in int i, ref GridManager.GridCell data) =>
        {

        });
        
        stopwatch.Stop();
        $"1. {stopwatch.ElapsedTicks}".ToLog();
        stopwatch.Reset();

        stopwatch.Start();

        grid.For((in int i, ref GridManager.GridCell data) =>
        {

        });

        stopwatch.Stop();
        $"2. {stopwatch.ElapsedTicks}".ToLog();

        grid.Dispose();
    }

    private Vector3 GetRndPos()
    {
        return new Vector3(123 + UnityEngine.Random.Range(-100, 1000), 0, 123 + UnityEngine.Random.Range(-100, 1000));
    }

    [UnityTest]
    public IEnumerator GridWorldPositionTest()
    {
        yield return null;

        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

        yield return null;

        stopwatch.Reset();
        stopwatch.Start();
        int gridIdx = GridManager.CreateGrid(bounds, 1, false);
        stopwatch.Stop();
        $"created grid :: {stopwatch.ElapsedTicks}".ToLog();
        stopwatch.Reset();

        GridManager.Grid grid = GridManager.GetGrid(in gridIdx);

        //Vector3 pos = new Vector3(123, 0, 123);
        yield return new WaitForSeconds(1);

        for (int i = 0; i < 10000; i++)
        {
            stopwatch.Start();
            var cell = grid.GetCell(GetRndPos());

            stopwatch.Stop();
            $"{cell.Location} :: {stopwatch.ElapsedTicks}".ToLog();
            stopwatch.Reset();
        }

        grid.Dispose();
    }
}

public unsafe class UnsafeTests
{
    [Test]
    public unsafe void UnsafeIntTest()
    {
        int a = 5;

        int* b = &a;

        int c = *b;

        Debug.Log($"{a} == {c}");
    }
    [Test]
    public unsafe void UnsafeIntArrayTest()
    {
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        int lenght = 10000000;

        TestStruct[] a = new TestStruct[lenght];

        stopwatch.Start();
        ref TestStruct val1 = ref a[lenght - 1];
        val1.str.a = 10;
        stopwatch.Stop();

        $"{stopwatch.ElapsedTicks} :: 1 {val1.str.a}".ToLog();

        stopwatch.Reset();
        stopwatch.Start();
        TestStruct* val2;
        fixed (TestStruct* ptr1 = a)
        {
            val2 = (ptr1 + (lenght - 1));
        }
        stopwatch.Stop();

        $"{stopwatch.ElapsedTicks} :: 2 {(*val2).str.a}".ToLog();

        //Debug.Log($"{a[int.MaxValue - 1]} == {c}");
    }

    [Test]
    public unsafe void UnsafeClassTest()
    {
        TestClass a = new TestClass() { a = 5 };

        TypedReference tr = __makeref(a);
        TestClass val1 = __refvalue(tr, TestClass);

        Debug.Log($"{a.a} == {val1.a}");
    }
    [Test]
    public unsafe void UnsafeStructTest()
    {
        TestStruct b = new TestStruct() { b = 2 };

        TestStruct* tr = &b;
        TestStruct val1 = Marshal.PtrToStructure<TestStruct>((IntPtr)tr);

        Debug.Log($"{b.b} == {val1.b} == ");
    }

    [Test]
    public unsafe void UnsafeTestTest()
    {
        // pointer move to next test
        int[] arr = { 2, 4, 6, 8, 10, 12, 14, 16, 18, 20 };
        unsafe
        {
            fixed (int* parr = arr)
            {
                IntPtr ptr = new IntPtr(parr);
                // Get the size of an array element.
                int size = sizeof(int);
                for (int ctr = 0; ctr < arr.Length; ctr++)
                {
                    IntPtr newPtr = IntPtr.Add(ptr, ctr * size);
                    int* target = parr + ctr;
                    Debug.Log($"{Marshal.ReadInt32(newPtr)} :: {(*target)}");
                }
            }
        }
    }

    //private void* getPointer<T>(ref T t) where T : struct
    //{
    //    //TypedReference tr = __makeref(t);
    //    void* temp = &tr;
    //    return temp;
    //}
    //private void* getPointer2<T>(ref T t) where T : struct
    //{
    //    IntPtr temp = Marshal.AllocHGlobal(Marshal.SizeOf<T>());
    //    Marshal.StructureToPtr(t, temp, true);
    //    //TypedReference tr = __makeref(t);
    //    //void* temp = &tr;
    //    return temp;
    //}

    //public TestClass _A;
    //public TestClass* _B;

    
}

public class TestClass
{
    public int a;
}
public struct TestStruct
{
    public int a;
    public int b;
    public int c;
    public int d;
    public int f;
    public int g;
    public int h;
    public int i;
    public int j;
    public int k;
    public int l;
    public int m;

    public TestInnerStruct str;
}
public struct TestInnerStruct
{
    public int a;
    public int b;
    public int c;
}
