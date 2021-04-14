_Namespace: Syadeu.Mono_

```csharp
public sealed class ConsoleWindow : StaticManager<ConsoleWindow>
```

런타임 개발자용 콘솔입니다.

**Inheritance**: [StaticManager\<T>](https://github.com/Syadeu/CoreSystem/wiki/StaticManager) -> ConsoleWindow  

## Overview

* 런타임 콘솔 창 입니다.
* 각종 디버깅을 위한 로그 커맨드를 지원합니다.
* 런타임 중, 지정한 명령어를 통해 사용자가 원하는 메소드를 실행 할 수 있습니다.

## Remarks

## Description

## Examples

아래는 사용자가 유니티에서 CommandDefinition 을 생성 후, 내부에 CommandField 를 할당했다는 전제 하에 명령어 등록 방법에 대해 설명합니다.

```c#
public void TestMethod(int i)
{
    ConsoleWindow.Log($"Input: {i}");
}
public void AddCommand()
{    
    ConsoleWindow.AddCommand(
        (rest) =>
        {
            // rest 는 명령문을 제외한 나머지 텍스트문을 받아옵니다.
            // 이 string 을 활용하면 다음과 같은 변수를 사용자에 의해 지정받아,
            // 메소드를 실행 할 수 있습니다.
            
            if (!int.TryParse(rest, out int i))
            {
                ConsoleWindow.Log($"형식에 맞지않는 입력값: expected => {typeof(int)}");
                return;
            }
            TestMethod(i);
        }, 
        // 이 경우, ConsoleDefinition 의 좌표는 testDef 이고,
        // 하위 testField 의 명령단에 위의 람다를 할당합니다.
        "testDef", "testField");
}
```

위의 예제에서 사용자가 콘솔 명령창에 `testDef testField 10` 을 입력하였을 때, 콘솔은 다음과 같은 메세지를 표시하게 됩니다.

`Input: 10`



------

## Events

| Name            | Description                                             |
| :-------------- | ------------------------------------------------------- |
| OnErrorReceived | 에러가 전달되었을 때 실행하는 이벤트 `delegate` 입니다. |



## Properties

| Name   | Description                             |
| :----- | --------------------------------------- |
| Opened | 이 콘솔창이 현재 열려있는지 반환합니다. |



## Public Methods

| Name          | Description                                         |
| :------------ | --------------------------------------------------- |
| Log           | 해당 `string` 을 콘솔창에 표시합니다.               |
| LogAssert     |                                                     |
| AddCommand    | 만들어진 명령 계층에 `delegate` 를 선언합니다.      |
| CreateCommand | 새로 명령 계층을 구성하여 `delegate` 를 선언합니다. |



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
