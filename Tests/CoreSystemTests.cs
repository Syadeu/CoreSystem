using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using Syadeu;
using UnityEngine.Diagnostics;
using System.Linq;

public class CoreSystemTests
{
    public void TestMath()
    {
        float test = 99999;
        test = Mathf.Sqrt(test);
        test = Mathf.Sqrt(test);
        test = Mathf.Sqrt(test);
        test = Mathf.Sqrt(test);
        test = Mathf.Sqrt(test);
        test = Mathf.Sqrt(test);
        test = Mathf.Sqrt(test);
        test = Mathf.Sqrt(test);
        test = Mathf.Sqrt(test);
        test = Mathf.Sqrt(test);
        test = Mathf.Sqrt(test);
        test = Mathf.Sqrt(test);

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
