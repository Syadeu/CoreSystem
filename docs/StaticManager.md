_Namespace: Syadeu_
```csharp
public abstract class StaticManager<T> : StaticManagerEntity, IStaticMonoManager where T : Component, IStaticMonoManager>
```

객체 자동 생성 Static 매니저 기본 클래스입니다.

**Inheritance**: [StaticManagerEntity](https://github.com/Syadeu/CoreSystem/wiki/StaticManagerEntity) -> StaticManager\<T> 

**Derived**: [CoreSystem](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem), [GridManager](https://github.com/Syadeu/CoreSystem/wiki/GridManager), [PrefabManager](https://github.com/Syadeu/CoreSystem/wiki/PrefabManager), [RenderManager](https://github.com/Syadeu/CoreSystem/wiki/RenderManager) 

**Implements**: [IStaticMonoManager](https://github.com/Syadeu/CoreSystem/wiki/IStaticMonoManager) 

## Overview
* 사용자가 씬에 일일이 오브젝트를 생성하여 컴포넌트를 붙여넣지 않아도 시스템에서 필요에 의해 생성 가능하도록 함
* 백그라운드 스레드에서 접근하여 생성할때도 안전하게 싱글톤을 생성하여 반환하도록 함

## Remarks


## Examples
```csharp
public sealed class ExampleManager : StaticManager<ExampleManager>
{
     // Hierarchy에서 표시될 이름을 설정 할 수 있습니다.
     // 에디터에서만 활용되며, 실제 빌드에는 아무런 기능을 하지 않습니다.
     public override string DisplayName => "Example Manager";
     
     // 이 객체가 씬이 전환될때 파괴될 것인지를 설정 할 수 있습니다.
     // 기본 값은 true입니다.
     public override bool DontDestroy => false;

     // 이 객체가 Hierarchy에서 표시 될 것 인지를 설정 할 수 있습니다.
     // 기본 값은 true입니다.
     public override bool HideInHierarchy => false;

     // 이 객체가 생성될 때, 한번만 호출하는 함수입니다.
     // 백그라운드 스레드에서도 부를 수 있으니 사용에 유의해야 됩니다.
     public override void OnInitialize()
     {
          UnityEngine.Debug.Log("Hello World!");
     }
     // 이 객체가 생성되고 초기화 된 뒤, 맨 마지막에 한번만 호출하는 함수입니다.
     // 백그라운드 스레드에서도 부를 수 있으니 사용에 유의해야 됩니다.
     public override void OnStart()
     {
          UnityEngine.Debug.Log("Hello! I\'m Started!");
     }

     public void TestMethod()
     {
          UnityEngine.Debug.Log("Test Method Executed!");
     }
}
```
ExampleManager.Instance.TestMethod() 를 호출시 자동으로 ExampleManager의 싱글톤 객체를 생성하여 반환합니다.  
예상되는 로그는 다음과 같습니다.

1. `Hello World!`  
2. `Hello! I\'m Started!`  
3. `Test Method Executed!`  

이 싱글톤 객체가 만들어지는 순서는 다음과 같습니다.

1. 현재 이 객체의 싱글톤 객체가 씬 안에 존재하는지 체크합니다.  
존재한다면 6번으로 건너뛰고, 아니라면 새로운 GameObject 생성합니다.  
2. OnInitialize()를 호출합니다.  
3. DontDestroy 의 값에 따라, 전역 오브젝트가 될지, 현재씬에서만 사용되고 전환시 버려지는 오브젝트가 될지 설정합니다.  
4. Instance에 생성된 싱글톤을 할당합니다.  
5. OnStart()를 호출합니다.  
6. 새로 생성된 싱글톤 인스턴스 객체를 반환합니다.  



------

## Static Properties

| Name                                                         | Description                                      |
| :----------------------------------------------------------- | ------------------------------------------------ |
| [Initialized](https://github.com/Syadeu/CoreSystem/wiki/StaticManager-SP-Initialized) | 이 매니저가 생성되고, 초기화되었는지 반환합니다. |
| [Instance](https://github.com/Syadeu/CoreSystem/wiki/StaticManager-SP-Instance) | 싱글톤입니다.                                    |



## Properties

| Name                                                         | Description                                                  |
| :----------------------------------------------------------- | ------------------------------------------------------------ |
| [DisplayName](https://github.com/Syadeu/CoreSystem/wiki/StaticManager-P-DisplayName) | Hierarchy에서 표기될 이름을 설정합니다. 빌드에서는 아무런 기능을 하지 않습니다. |
| [DontDestroy](https://github.com/Syadeu/CoreSystem/wiki/StaticManager-P-DontDestroy) | 씬이 전환되어도 파괴되지 않을 것인지를 설정합니다.           |
| [HideInHierarchy](https://github.com/Syadeu/CoreSystem/wiki/StaticManager-P-HideInHierarchy) | Hierarchy에 표시될지를 설정합니다. 빌드에서는 아무런 기능을 하지 않습니다. |
| [ManualInitialize](https://github.com/Syadeu/CoreSystem/wiki/StaticManager-P-ManualInitialize) | 사용자에 의해 수동으로 초기화 할지를 설정합니다. StaticManager를 상속받고 있으면 값은 무조건 `false`이며 `override` 될 수 없습니다. |



## Public Methods

| Name                                                         | Description                           |
| :----------------------------------------------------------- | ------------------------------------- |
| [OnInitialize](https://github.com/Syadeu/CoreSystem/wiki/StaticManager-PM-OnInitialize) | 초기화 될 때 실행될 함수입니다.       |
| [OnStart](https://github.com/Syadeu/CoreSystem/wiki/StaticManager-PM-OnStart) | 초기화가 다 끝나고 실행될 함수입니다. |
| [Initialize](https://github.com/Syadeu/CoreSystem/wiki/StaticManager-PM-Initialize) | 초기화 함수입니다.                    |



------

## Inherited Members

### Protected Static  Properties

| Name                                                         | Description                                                  |
| :----------------------------------------------------------- | ------------------------------------------------------------ |
| [System](https://github.com/Syadeu/CoreSystem/wiki/ManagerEntity-PSP-System) | [CoreSystem](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem) 의 인스턴스 객체를 반환합니다. |
| [InstanceGroupTr](https://github.com/Syadeu/CoreSystem/wiki/ManagerEntity-PSP-System) | 인스턴스 매니저들이 부모로 삼는 최상위 Transform 입니다.     |
| [ManagerLock](https://github.com/Syadeu/CoreSystem/wiki/ManagerEntity-PSP-ManagerLock) | 사용하지마세요                                               |



### Static Properties

| Name                                                         | Description                      |
| :----------------------------------------------------------- | -------------------------------- |
| [MainThread](https://github.com/Syadeu/CoreSystem/wiki/ManagerEntity-SP-MainThread) | 유니티 메인 스레드를 반환합니다. |
| [BackgroundThread](https://github.com/Syadeu/CoreSystem/wiki/ManagerEntity-SP-BackgroundThread) | 백그라운드 스레드를 반환합니다.  |



### Properties

| Name                                                         | Description               |
| :----------------------------------------------------------- | ------------------------- |
| [Flag](https://github.com/Syadeu/CoreSystem/wiki/StaticManagerEntity-P-Flag) | 현재 시스템의 종류입니다. |



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
| [StartUnityUpdate](https://github.com/Syadeu/CoreSystem/wiki/ManagerEntity-PM-StartUnityUpdate) | 입력한 `iterator`를 유니티 메인 스레드에서 `iteration` 합니다. |
| [StartBackgroundUpdate](https://github.com/Syadeu/CoreSystem/wiki/ManagerEntity-PM-StartBackgroundUpdate) | 입력한 `iterator`를 백그라운드 스레드에서 `iteration` 합니다. |
| [StopUnityUpdate](https://github.com/Syadeu/CoreSystem/wiki/ManagerEntity-PM-StopUnityUpdate) | 입력한 CoreRoutine을 정지합니다.                             |
| [StopBackgroundUpdate](https://github.com/Syadeu/CoreSystem/wiki/ManagerEntity-PM-StopBackgroundUpdate) | 입력한 CoreRoutine을 정지합니다.                             |

