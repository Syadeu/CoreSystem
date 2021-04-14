_Namespace: Syadeu_
```csharp
public abstract class StaticManagerEntity : ManagerEntity
```

**Inheritance**: [ManagerEntity](https://github.com/Syadeu/CoreSystem/wiki/ManagerEntity) -> StaticManagerEntity 

**Derived**: [StaticManager\<T>](https://github.com/Syadeu/CoreSystem/wiki/StaticManager)

## Overview

## Remarks

## Description



------

## Protected Static Properties

| Name                                                         | Description                                                  |
| :----------------------------------------------------------- | ------------------------------------------------------------ |
| [System](https://github.com/Syadeu/CoreSystem/wiki/ManagerEntity-PSP-System) | [CoreSystem](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem) 의 인스턴스 객체를 반환합니다. |



## Properties

| Name                                                         | Description               |
| :----------------------------------------------------------- | ------------------------- |
| [Flag](https://github.com/Syadeu/CoreSystem/wiki/StaticManagerEntity-P-Flag) | 현재 시스템의 종류입니다. |



## Static Methods

| Name                                                         | Description                            |
| :----------------------------------------------------------- | -------------------------------------- |
| [ThreadAwaiter](https://github.com/Syadeu/CoreSystem/wiki/StaticManagerEntity-SM-ThreadAwaiter) | 해당 시간만큼 스레드를 `sleep` 합니다. |
| [AwaitForNotNull](https://github.com/Syadeu/CoreSystem/wiki/StaticManagerEntity-SM-AwaitForNotNull) |                                        |



------

## Inherited Members

### Protected Static Properties

| Name                                                         | Description                                              |
| :----------------------------------------------------------- | -------------------------------------------------------- |
| [InstanceGroupTr](https://github.com/Syadeu/CoreSystem/wiki/ManagerEntity-PSP-System) | 인스턴스 매니저들이 부모로 삼는 최상위 Transform 입니다. |
| [ManagerLock](https://github.com/Syadeu/CoreSystem/wiki/ManagerEntity-PSP-ManagerLock) | 사용하지마세요                                           |



### Static Properties

| Name                                                         | Description                      |
| :----------------------------------------------------------- | -------------------------------- |
| [MainThread](https://github.com/Syadeu/CoreSystem/wiki/ManagerEntity-SP-MainThread) | 유니티 메인 스레드를 반환합니다. |
| [BackgroundThread](https://github.com/Syadeu/CoreSystem/wiki/ManagerEntity-SP-BackgroundThread) | 백그라운드 스레드를 반환합니다.  |



### Protected Static Methods

| Name                                                         | Description                                                  |
| :----------------------------------------------------------- | ------------------------------------------------------------ |
| [IsMainthread](https://github.com/Syadeu/CoreSystem/wiki/ManagerEntity-PSM-IsMainThread) | 이 메소드가 실행된 스레드가 유니티 메인스레드인지 반환합니다. |



### Public Methods

| Name                                                         | Description                                                  |
| :----------------------------------------------------------- | ------------------------------------------------------------ |
| [StartUnityUpdate](https://github.com/Syadeu/CoreSystem/wiki/ManagerEntity-PM-StartUnityUpdate) | 입력한 `iterator`를 유니티 메인 스레드에서 `iteration` 합니다. |
| [StartBackgroundUpdate](https://github.com/Syadeu/CoreSystem/wiki/ManagerEntity-PM-StartBackgroundUpdate) | 입력한 `iterator`를 백그라운드 스레드에서 `iteration` 합니다. |
| [StopUnityUpdate](https://github.com/Syadeu/CoreSystem/wiki/ManagerEntity-PM-StopUnityUpdate) | 입력한 CoreRoutine을 정지합니다.                             |
| [StopBackgroundUpdate](https://github.com/Syadeu/CoreSystem/wiki/ManagerEntity-PM-StopBackgroundUpdate) | 입력한 CoreRoutine을 정지합니다.                             |

