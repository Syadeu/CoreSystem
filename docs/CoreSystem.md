_Namespace: Syadeu_
```csharp
public sealed class CoreSystem : StaticManager<CoreSystem>
```

[StaticManager](https://github.com/Syadeu/CoreSystem/wiki/StaticManager), [StaticDataManager](https://github.com/Syadeu/CoreSystem/wiki/StaticDataManager), [MonoManager](https://github.com/Syadeu/CoreSystem/wiki/MonoManager) 를 참조한 모든 객체들의 매니저입니다.

**Inheritance**: [StaticManager\<T>](https://github.com/Syadeu/CoreSystem/wiki/StaticManager) -> [CoreSystem](https://github.com/Syadeu/CoreSystem/blob/main/Runtime/CoreSystem.cs)  

## Overview
* Unity Coroutine 와 흡사한 방법으로 `iteration` 을 백그라운드 스레드에서 실행할 수 있습니다.
* System.ComponentModel 컬렉션을 사용하여 간편하게 워커스레드에 함수를 할당하여 실행 할 수 있습니다.
* 외부 스레드에서 접근하지 못하거나 실행을 허락하지 않는 UnityEngine 네임스페이스 함수 혹은 프로퍼티들을  
메인 스레드에서 가져오거나 실행하도록 넘겨줄 수 있습니다.
* 백그라운드 스레드에서 수행하는 모든 작업들을 Unity Profiler를 통하여 모니터링이 가능합니다.

## Remarks
CoreSystem 개발은 멀티스레드에서 오는 장점을 유니티에서도 사용할 수 있게 초점이 맞춰져 있습니다. UnityEngine 네임스페이스 안의 거의 대부분의 메소드들은 외부 사용자 스레드에서 작동을 허락하지않지만, 이를 살짝 우회하여 유니티 스레드에게 문제되는 해당 메소드만을 넘겨줘 처리하는 방식으로 백그라운드에서 대부분의 연산을 처리하고, 적용 및 표기만 유니티 스레드에서 처리하는 것을 핵심으로 개발 중 입니다.  

이 시스템은 백그라운드 스레드 단 한개와, 복합 병렬 연산을 위한 다수의 워커스레드를 관리하고 있습니다.  

백그라운드 스레드와 유니티 스레드와의 원활한 통신을 위한 [ForegroundJob](https://github.com/Syadeu/CoreSystem/wiki/ForegroundJob) 이 개발되었고, 메인스레드에서 비동기 작업 이후, 결과를 리턴받기 위한 [BackgroundJob](https://github.com/Syadeu/CoreSystem/wiki/BackgroundJob) 도 개발되었습니다.  

유니티 Coroutine 과 마찬가지로, `iterator`를 CoreSystem에서 동일한 방법으로 백그라운드에서 실행할 수 있습니다. 몇몇 정보를 알 수 없는 유니티 내부 `yield` 타입을 제외한 나머지는 대부분 지원합니다. 지원되지 않는 `yield` 타입은 [CoreSystemException](https://github.com/Syadeu/CoreSystem/wiki/CoreSystemException)을 `throw` 합니다. 

## Description
![1](https://user-images.githubusercontent.com/59386372/111844440-662eef00-8946-11eb-92f1-d43907a11102.PNG)
![2](https://user-images.githubusercontent.com/59386372/111844447-67f8b280-8946-11eb-83a7-da8cdcb7cb3b.PNG)

## Examples

아래는 CoreSystem에서 관리하는 `iterator` 에서 유니티 Coroutine을 실행하는 방법에 대해 설명합니다.

```c#
public class CoreSystemTest : MonoBehaviour
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
    public IEnumerator Start()
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
    
    // Expected Logs
    //
    // Wait 2 seconds
    // true isrunning
    // remove routine
    // false isrunning
}
```

CoreSystem에 해당 `iterator`를 등록하는 것이지, 즉시 시작하는 것이 아님을 유의하여야합니다.

아래는 CoreSystem에서 관리하는 백그라운드 스레드에서 `iterator`를 실행하는 방법에 대해 설명합니다.

```c#
public class CoreSystemTest : MonoBehaviour
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
    public IEnumerator Start()
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
    
    // Expected Logs
    //
    // Wait 2 seconds
    // true isrunning
    // remove routine
    // false isrunning
}
```



아래는 간단한 Action `delegate`를 워커 스레드에게 할당하는 방법에 대해 설명합니다.

```c#
CoreSystem.AddBackgroundJob(
    () =>
    {
		Debug.Log("Hello World!");
    });
```

아래는 조금 더 복잡한 잡에 대해 설명합니다.

```c#
CoreSystem.AddBackgroundJob(
    () => 
    {
        ForegroundJob foreJob = CoreSystem.AddForegroundJob(
    	    () => 
                {
                    Debug.Log("Hello World!");
                });
        while (!foreJob.IsDone)
        {
            CoreSystem.ThreadAwaiter(1);
        }
        
        Debug.Log("All job is done");
    });

// Expected Logs
//
// Hello World!
// All job is done
```

사용자가 새로운 워커 스레드를 생성하고, 해당 워커스레드에게 순차적으로 `delegate`들을 실행하도록 설정할 수 있습니다. 아래는 해당 방법에 대해 설명합니다.

```c#
public class CoreSystemTest : MonoBehaviour
{
    private int m_WorkerIdx = -1;
    
    public IEnumerator Start()
    {
        m_WorkerIdx = CoreSystem.CreateNewBackgroundJobWorker(isStandAlone: true);

        CoreSystem.AddBackgroundJob(m_WorkerIdx, () =>
        {
            Debug.Log("1. Job Started!");
        }, out BackgroundJob job1);
        CoreSystem.AddBackgroundJob(m_WorkerIdx, () =>
        {
            Debug.Log("2. Job Started!");
        }, out BackgroundJob job2);
        CoreSystem.AddBackgroundJob(m_WorkerIdx, () =>
        {
            Debug.Log("3. Job Started!");
        }, out BackgroundJob job3);

        job1.ConnectJob(job2).ConnectJob(job3);
        yield return new WaitForBackgroundJob(job1);

        Debug.Log("All job is done");
    }
    
    // Expected Logs
    //
    // 1. Job Started!
    // 2. Job Started!
    // 3. Job Started!
    // All job is done
}
```



------

## Delegates

| Name           | Description |
| :------------- | ----------- |
| Awaiter        |             |
| BackgroundWork |             |
| UnityWork      |             |



## Static Methods

| Name                                                         | Description                                                  |
| :----------------------------------------------------------- | ------------------------------------------------------------ |
| [GetStaticManagers](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem-SM-GetStaticManagers) | [StaticManager](https://github.com/Syadeu/CoreSystem/wiki/StaticManager), 혹은 [MonoManager](https://github.com/Syadeu/CoreSystem/wiki/MonoManager)를 참조하고, `DontDestroy` 가 `true` 인 매니저 객체들의 목록을 반환합니다. |
| [GetInstanceManagers](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem-SM-GetInstanceManager) | [StaticManager](https://github.com/Syadeu/CoreSystem/wiki/StaticManager), 혹은 [MonoManager](https://github.com/Syadeu/CoreSystem/wiki/MonoManager)를 참조하고, `DontDestroy` 가 `false` 인 매니저 객체들의 목록을 반환합니다. |
| [GetDataManagers](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem-SM-GetDataManager) | [StaticDataManager](https://github.com/Syadeu/CoreSystem/wiki/StaticDataManager) 를 참조하는 모든 데이터 매니저 객체들의 목록을 반환합니다. |
| [GetManager\<T>](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem-SM-GetManager) | 해당 타입의 매니저가 있으면 반환합니다.                      |
| [CreateNewBackgroundJobWorker](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem-SM-CreateNewBackgroundJobWorker) | 새로운 백그라운드잡 Worker 를 생성하고, 인덱스 번호를 반환합니다. |
| [ChangeSettingBackgroundWorker](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem-SM-ChangeSettingBackgroundWorker) |                                                              |
| [GetBackgroundWorker](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem-PM-GetBackgroundWorker) | 놀고있는 백그라운드잡 Worker를 반환합니다. 놀고있는 워커가 없을경우, False 를 반환합니다. |
| [AddBackgroundJob](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem-SM-AddBackgroundJob) | 워커 스레드에게 해당 잡을 수행하도록 등록합니다.             |
| [AddForegroundJob](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem-SM-AddForegroundJob) | 유니티 메인 스레드에 해당 잡을 수행하도록 등록합니다.        |
| [IsThisMainthread](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem-SM-IsThisMainthread) | 이 메소드가 실행된 스레드가 유니티 메인스레드인지 반환합니다. |
| [StartBackgroundUpdate](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem-SM-StartBackgroundUpdate) | 새로운 백그라운드 업데이트 루틴을 등록합니다.                |
| [StartUnityUpdate](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem-SM-StartUnityUpdate) | 새로운 유니티 업데이트 루틴을 등록합니다.                    |
| [RemoveUnityUpdate](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem-SM-RemoveUnityUpdate) | 커스텀 관리되던 해당 유니티 업데이트 루틴을 제거합니다.      |
| [RemoveBackgroundUpdate](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem-SM-RemoveBackgroundUpdate) | 커스텀 관리되던 해당 백그라운드 업데이트 루틴을 제거합니다.  |

## Static Events

| Name                    | Description                                                  |
| :---------------------- | ------------------------------------------------------------ |
| OnBackgroundStart       | 백그라운드 스레드에서 한번만 실행될 함수를 넣을 수 있습니다. |
| OnBackgroundUpdate      | 백그라운드 스레드에서 반복적으로 실행될 함수를 넣을 수 있습니다. |
| OnBackgroundAsyncUpdate | 백그라운드 스레드에서 유니티 프레임 동기화하여 반복적으로 실행될 함수를 넣을 수 있습니다. |
| OnUnityStart            | 유니티 업데이트 전 한번만 실행될 함수를 넣을 수 있습니다.    |
| OnUnityUpdate           | 유니티 프레임에 맞춰 반복적으로 실행될 함수를 넣을 수 있습니다. |



------

## Inherited Members

### Static Properties

| Name                                                         | Description                                      |
| :----------------------------------------------------------- | ------------------------------------------------ |
| [Initialized](https://github.com/Syadeu/CoreSystem/wiki/StaticManager-SP-Initialized) | 이 매니저가 생성되고, 초기화되었는지 반환합니다. |
| [Instance](https://github.com/Syadeu/CoreSystem/wiki/StaticManager-SP-Instance) | 싱글톤입니다.                                    |
| [MainThread](https://github.com/Syadeu/CoreSystem/wiki/ManagerEntity-SP-MainThread) | 유니티 메인 스레드를 반환합니다.                 |
| [BackgroundThread](https://github.com/Syadeu/CoreSystem/wiki/ManagerEntity-SP-BackgroundThread) | 백그라운드 스레드를 반환합니다.                  |



### Protected Static  Properties

| Name                                                         | Description                                                  |
| :----------------------------------------------------------- | ------------------------------------------------------------ |
| [System](https://github.com/Syadeu/CoreSystem/wiki/ManagerEntity-PSP-System) | [CoreSystem](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem) 의 인스턴스 객체를 반환합니다. |



### Properties

| Name                                                         | Description                                                  |
| :----------------------------------------------------------- | ------------------------------------------------------------ |
| [DisplayName](https://github.com/Syadeu/CoreSystem/wiki/StaticManager-P-DisplayName) | Hierarchy에서 표기될 이름을 설정합니다. 빌드에서는 아무런 기능을 하지 않습니다. |
| [DontDestroy](https://github.com/Syadeu/CoreSystem/wiki/StaticManager-P-DontDestroy) | 씬이 전환되어도 파괴되지 않을 것인지를 설정합니다.           |
| [HideInHierarchy](https://github.com/Syadeu/CoreSystem/wiki/StaticManager-P-HideInHierarchy) | Hierarchy에 표시될지를 설정합니다. 빌드에서는 아무런 기능을 하지 않습니다. |
| [ManualInitialize](https://github.com/Syadeu/CoreSystem/wiki/StaticManager-P-ManualInitialize) | 사용자에 의해 수동으로 초기화 할지를 설정합니다. StaticManager를 상속받고 있으면 값은 무조건 `false`이며 `override` 될 수 없습니다. |
| [Flag](https://github.com/Syadeu/CoreSystem/wiki/StaticManagerEntity-P-Flag) | 현재 시스템의 종류입니다.                                    |



### Protected Static Methods

| Name                                                         | Description                                                  |
| :----------------------------------------------------------- | ------------------------------------------------------------ |
| [IsMainthread](https://github.com/Syadeu/CoreSystem/wiki/ManagerEntity-PSM-IsMainThread) | 이 메소드가 실행된 스레드가 유니티 메인스레드인지 반환합니다. |



### Static Methods

| Name                                                         | Description                            |
| :----------------------------------------------------------- | -------------------------------------- |
| [ThreadAwaiter](https://github.com/Syadeu/CoreSystem/wiki/StaticManagerEntity-SM-ThreadAwaiter) | 해당 시간만큼 스레드를 `sleep` 합니다. |
| [AwaitForNotNull](https://github.com/Syadeu/CoreSystem/wiki/StaticManagerEntity-SM-AwaitForNotNull) |                                        |



### Public Methods

| Name                                                         | Description                                                  |
| :----------------------------------------------------------- | ------------------------------------------------------------ |
| [OnInitialize](https://github.com/Syadeu/CoreSystem/wiki/StaticManager-PM-OnInitialize) | 초기화 될 때 실행될 함수입니다.                              |
| [OnStart](https://github.com/Syadeu/CoreSystem/wiki/StaticManager-PM-OnStart) | 초기화가 다 끝나고 실행될 함수입니다.                        |
| [Initialize](https://github.com/Syadeu/CoreSystem/wiki/StaticManager-PM-Initialize) | 초기화 함수입니다.                                           |
| [StartUnityUpdate](https://github.com/Syadeu/CoreSystem/wiki/ManagerEntity-PM-StartUnityUpdate) | 입력한 `iterator`를 유니티 메인 스레드에서 `iteration` 합니다. |
| [StartBackgroundUpdate](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem-SM-StartBackgroundUpdate) | 입력한 `iterator`를 백그라운드 스레드에서 `iteration` 합니다. |
| [StopUnityUpdate](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem-SM-RemoveUnityUpdate) | 입력한 CoreRoutine을 정지합니다.                             |
| [StopBackgroundUpdate](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem-SM-RemoveBackgroundUpdate) | 입력한 CoreRoutine을 정지합니다.                             |

