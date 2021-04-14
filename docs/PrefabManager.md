_Namespace: Syadeu.Mono_

```csharp
public class PrefabManager : StaticManager<PrefabManager>
```

[PrefabList](https://github.com/Syadeu/CoreSystem/wiki/PrefabList) 의 리스트를 기반으로 작동하는 프리팹 기반 오브젝트 풀링 매니저입니다.

**Inheritance**: [StaticManager\<T>](https://github.com/Syadeu/CoreSystem/wiki/StaticManager) -> PrefabManager

## Overview

* 간편하게 오브젝트들을 풀링할 수 있습니다.
* 자동으로 오브젝트 인스턴스 갯수를 관리하므로, 시스템 비용을 절감합니다.

## Remarks

이 매니저는 상단 메뉴의 Syadeu -> Edit Prefab List 을 선택하면 나오는 [PrefabList](https://github.com/Syadeu/CoreSystem/wiki/PrefabList) ScriptableObject를 기반으로 작동합니다. 해당 에셋내 아무 리스트가 없으면 사용시 에러가 발생합니다.

## Description

## Examples

아래는 간단하게, 아무런 스크립트도 부착하지않은 프리팹을 풀링 시스템에 편입하는 방법에 대해 설명합니다.

```c#
using UnityEngine;
using Syadeu.Mono;

public void PrefabPullingTest()
{
    // PrefabList내 프리팹 리스트의 0번째 인덱스는 아무것도 달리지않은 오브젝트라 가정하고,
    // 해당 프리팹을 풀링 시스템으로 편입시키기 위해, PrefabManager는 해당 프리팹 최상단에
    // ManagedRecycleObject 컴포넌트를 부착하여 반환합니다.
    RecycleableMonobehaviour recycleObj = PrefabManager.GetRecycleObject(0);
    
    // 사용이 끝나면, 사용이 끝남을 선언하여야합니다.
    // 선언 후에는 유후 인스턴스가 되어 다른 요청이 있을때 반환합니다.
    recycleObj.Terminate();
    
    // Terminated 된 인스턴스들을 강제로 방출합니다.
    // PrefabList 에서 방출 타이머 설정을 하지않았다면 이 메소드 호출이 필요하지만,
    // 설정되있다면 굳이 필요하지 않습니다.
    PrefabManager.ReleaseTerminatedObjects();
}
```

이제는 [ManagedRecycleObject](https://github.com/Syadeu/CoreSystem/wiki/ManagedRecycleObject) 컴포넌트가 부착되있거나,  [RecycleableMonobehaviour](https://github.com/Syadeu/CoreSystem/wiki/RecycleableMonobehaviour)를 참조한 모노스크립트를 포함하는 프리팹을 풀링하는 방법에 대해 설명합니다.

```c#	
using UnityEngine;
using Syadeu.Mono;

public class TestRecycle : RecycleableMonobehaviour
{
    /* 
    	...... Some Codes ......
    */
    
    public void CallLog()
    {
        Debug.Log("I\'m Called!'");
    }
}

public void TestPullingTestRecycle()
{
    // 받아올 타입을 지정하면, 자동으로 해당 스크립트가 달린 유후 인스턴스를 받아옵니다.
    TestRecycle obj = PrefabManager.GetRecycleObject<TestRecycle>();
    obj.CallLog();
    obj.Terminate();
}

// Expected Logs
//
// I'm Called!
```



------

## Static Methods

| Name                     | Description                                                  |
| :----------------------- | ------------------------------------------------------------ |
| GetRecycleObject         | 해당 타입과 일치하는 리사이클 인스턴스를 받아옵니다.         |
| GetRecycleObject\<T>     | [PrefabList](https://github.com/Syadeu/CoreSystem/wiki/PrefabList) 에서의 리스트 인덱스(PrefabList.m_ObjectSettings) 값으로 재사용 인스턴스를 받아옵니다. |
| ReleaseTerminatedObjects | 현재 Terminated 처리된 재사용 오브젝트들을 메모리에서 방출합니다. |

## Public Methods

| Name                 | Description                                                  |
| :------------------- | ------------------------------------------------------------ |
| GetInstanceCount     | 해당 인덱스의 재사용 오브젝트들의 인스턴스 갯수를 반환합니다. |
| GetInstances         | 해당 인덱스의 모든 인스턴스들을 리스트에 담아 반환합니다.    |
| GetRecycleObjectList | 등록된 모든 재사용 모노 프리팹을 받아옵니다. 인덱스 번호와 프리팹을 담아서 리스트로 반환합니다. |



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
| [StartBackgroundUpdate](https://github.com/Syadeu/CoreSystem/wiki/ManagerEntity-PM-StartBackgroundUpdate) | 입력한 `iterator`를 백그라운드 스레드에서 `iteration` 합니다. |
| [StopUnityUpdate](https://github.com/Syadeu/CoreSystem/wiki/ManagerEntity-PM-StopUnityUpdate) | 입력한 CoreRoutine을 정지합니다.                             |
| [StopBackgroundUpdate](https://github.com/Syadeu/CoreSystem/wiki/ManagerEntity-PM-StopBackgroundUpdate) | 입력한 CoreRoutine을 정지합니다.                             |

