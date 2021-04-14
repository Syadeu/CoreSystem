_Namespace: Syadeu_
```csharp
public class BackgroundJob : IJob
```

백그라운드 스레드에서 단일 [Action](https://docs.microsoft.com/ko-kr/dotnet/api/system.action?view=net-5.0)을 실행할 수 있는 잡 클래스입니다.

**Inheritance**: [Object](https://docs.microsoft.com/ko-kr/dotnet/api/system.object?view=net-5.0) -> BackgroundJob  
**Implements**: [IJob](https://github.com/Syadeu/CoreSystem/wiki/IJob)

## Overview
* 간편하게 람다로 메소드를 워커스레드에게 수행시키도록 할 수 있습니다.
* 여러 잡들을 묶어 병렬 수행을 시킬 수 있습니다.

## Remarks
BackgroundJob은 [IJob](https://github.com/Syadeu/CoreSystem/wiki/IJob)을 상속받아, 전혀 다른 스레드에서 수행되는 [ForegroundJob](https://github.com/Syadeu/CoreSystem/wiki/ForegroundJob)과도 연결되어 사용할 수 있습니다.
BackgroundJob은 delegate를 저장해야됨으로 객체로 구현이 되어있는데, 이로인한 상대적으로 느린 메모리 접근 속도를 항상 염두하며 사용하여야 됩니다.
반복적으로 호출되는 함수는 생성후 재사용(BackgroundJob.Start()는 할당한 메소드가 실패하지않았다면 자동으로 초기화하여 할당한 메소드를 다시 수행하도록 등록합니다)을 하여 최대한 Managed-Memory 영역에 할당비율을 줄이도록 해야할 것 입니다.

## Description
BackgroundJob은 유니티 스레드 내부에서 비용이 높은 메소드를 처리하기 위해 개발되었습니다. 멀티스레드임에도 높은 성능을 기대할 수 없지만, 비동기 작업을 간편하게 수행하여 결과값을 리턴받을 수 있는 것에 초점이 맞춰져있습니다. 

## Examples
일반적인 방법으로, 손쉽게 워커 스레드에게 잡을 할당하는 방법인
[CoreSystem](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem).
[AddBackgroundJob](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem-AddBackgroundJob)
(System.[Action](https://docs.microsoft.com/ko-kr/dotnet/api/system.action?view=net-5.0) action) 
을 통해 이 객체를 반환받을 수 있고, 해당 객체를 이용하여 iteration에 참조할 수 있습니다.  

아래는 가장 기본적인 사용법을 설명합니다.
```csharp
public class CoreSystemTests : StaticManager<CoreSystemTests>
{
    public void TestMethod()
    {
        UnityEngine.Debug.Log("Test Method Executed!");
    }
    public void AddBackgroundJob()
    {
        UnityEngine.Debug.Log("Adding BackgroundJob");
        BackgroundJob job = CoreSystem.AddBackgroundJob(TestMethod);
    }
    
    // Expected Logs
    //
    // Adding BackgroundJob
    // Test Method Executed!
}
```
`CoreSystemTests.Instance.AddBackgroundJob()`을 호출하게 되면, 
`TestMethod` 메소드를 `delegate`로 포장하여 [CoreSystem](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem) 의 백그라운드 스레드에게 넘겨줍니다.  
사용자는 이 포장한 형태를 BackgroundJob 인스턴스로 리턴받게됩니다. 이후, 백그라운드 스레드에서의 처리 속도에 따라 `TestMethod` 메소드가 워커 스레드에서 실행됩니다. 


생성자를 통한 백그라운드잡 생성은 보다 복잡한 작업을 가능하게 해줍니다.  
아래는 생성자를 통한 `delegate` 설정을 끝낸후, 병렬로 한번에 실행하는 방법에 대해 설명합니다.  
```csharp
public class CoreSystemTests
{
    public IEnumerator SyncBackgroundJobStart()
    {
        BackgroundJob job = new BackgroundJob(() => UnityEngine.Debug.Log("1. Test Method Executed!"));
        BackgroundJob job1 = new BackgroundJob(() => UnityEngine.Debug.Log("2. Test Method Executed!"));
        BackgroundJob job2 = new BackgroundJob(() => UnityEngine.Debug.Log("3. Test Method Executed!"));
        job.ConnectJob(job1).ConnectJob(job2);

        job.Start();
        int count = 0;
        while (!job.IsDone)
        {
            Assert.IsFalse(count > 1000);

            count++;
            yield return null;
        }

        UnityEngine.Debug.Log("Job is done");
    }
}
```

병렬로 지정한 배열을 탐색하거나, 배열 안에서 지정한 조건에 맞는 객체를 반환시킬 수 있습니다.  
아래는 배열을 병렬로 탐색하는 방법에 대해 설명합니다.

```csharp
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

    // Expected Logs
    // single job takes = 89
    // parallel job takes = 41
    // system.Threading.Task.Parallel job takes = 27
}
```

병렬 연산은 코어(BackgroundWorker 의 갯수)에 크게 영향을 받습니다.  
CoreSystem은 기본으로 32개의 코어를 가지고 시작하며, 사용자가 임의로 추가할 수 있습니다.  
시스템이 현재 가지고 있는 코어의 갯수와, 탐색할 배열의 길이를 염두하며 chunkSize 를 알맞게 지정하여야 최상의 결과를 도출할 수 있을 것 입니다.