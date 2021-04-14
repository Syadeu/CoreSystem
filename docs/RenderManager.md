_Namespace: Syadeu.Mono_

```csharp
public sealed class RenderManager : StaticManager<RenderManager>
```

간단한 설명

**Inheritance**:  [StaticManager\<T>](https://github.com/Syadeu/CoreSystem/wiki/StaticManager) -> RenderManager

## Overview

* CoreSystem Framework 내 모든 매니저 객체들이 렌더링 관련 작업을 수행할때 사용하는 매니저 객체입니다.
* 설정한 카메라의 Projection 을 Matrix4x4 로 변환하여 입력한 월드좌표가 카메라 뷰 내 위치하는 지 알 수 있습니다.

## Remarks

## Description

## Examples



------

## Static Methods

| Name                                                      | Description                                                  |
| :-------------------------------------------------------- | ------------------------------------------------------------ |
| SetCamera                                                 | 렌더링 규칙을 적용할 카메라를 설정합니다.                    |
| IsInCameraScreen(UnityEngine.Vector3)                     | 해당 좌표가 RenderManager가 감시하는 카메라의 Matrix 내 위치하는지 반환합니다. |
| IsInCameraScreen(UnityEngine.Camera, UnityEngine.Vector3) | 해당 좌표가 입력한 카메라 내부에 위치하는지 반환합니다.      |
| GetScreenPoint                                            | 해당 월드 좌표를 입력한 Matrix 기반으로 2D 좌표값을 반환합니다. |



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

