# StartBackgroundUpdate
```csharp
public static CoreRoutine StartBackgroundUpdate(object obj, IEnumerator update)
```

## Parameters
- [Object](https://docs.microsoft.com/ko-kr/dotnet/api/system.object?view=net-5.0) **obj**
- [IEnumerator](https://docs.microsoft.com/ko-kr/dotnet/api/system.collections.ienumerator?view=net-5.0) **update**

## Return
- [CoreRoutine](https://github.com/Syadeu/CoreSystem/wiki/CoreRoutine)

## Exceptions

## Examples
아래는 [CoreSystem](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem) 객체에서 관리 중인 Background Thread 에 Unity Coroutine과 거의 동일한 방식으로 `iteration` 을 요청하는 방법을 설명합니다.
```csharp
public class CoreSystemTests
{
    public IEnumerator TestWhileCoreRoutine()
    {
        Debug.Log("Routine Start");
        yield return null;

        while (true)
        {
            yield return null;
        }
    }
    public IEnumerator BackgroundUpdateTest()
    {
        CoreRoutine routine = CoreSystem.StartBackgroundUpdate(this, TestWhileCoreRoutine());

        UnityEngine.Debug.Log("Wait 2 seconds");
        yield return new WaitForSeconds(2);

        UnityEngine.Debug.Log($"{routine.IsRunning} isrunning");
        yield return null;

        BackgroundJob job = CoreSystem.RemoveBackgroundUpdate(routine);
        yield return new WaitForBackgroundJob(job);
        UnityEngine.Debug.Log("remove routine");

        UnityEngine.Debug.Log($"{routine.IsRunning} isrunning");
  
        // Expected Logs
        //
        // Routine Start (or) Wait 2 seconds
        // Wait 2 seconds (or) Routine Start
        // True isrunning
        // remove routine
        // False isrunning
    }
}
```