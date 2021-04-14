_Namespace: Syadeu.Mono_

```csharp
public abstract class RecycleableMonobehaviour : MonoBehaviour, IRecycleable, ITerminate
```

간단한 설명

**Inheritance**: MonoBehaviour -> RecycleableMonobehaviour

**Derived**: [ManagedRecycleObject](https://github.com/Syadeu/CoreSystem/wiki/ManagedRecycleObject)

**Implements**: [IRecycleable](https://github.com/Syadeu/CoreSystem/wiki/IRecycleable), [ITerminate](https://github.com/Syadeu/CoreSystem/wiki/ITerminate)

## Overview

* [PrefabManager](https://github.com/Syadeu/CoreSystem/wiki/PrefabManager) 의 풀링 시스템에서 사용하는 abstract 객체입니다.
* 간단하게 풀링 시스템에 편입 할 수 있는 Mono 객체를 작성 할 수 있습니다.

## Remarks

## Description

[PrefabManager](https://github.com/Syadeu/CoreSystem/wiki/PrefabManager) 에서 이 abstract를 상속받는 오브젝트를 불러오게되면, 기본 컴포넌트로 [ManagedRecycleObject](https://github.com/Syadeu/CoreSystem/wiki/ManagedRecycleObject) 컴포넌트를 추가하여 반환합니다.

이 abstract를 참조함으로써, 불필요한 컴포넌트 객체 갯수를 줄이고, 빠르게 풀링 시스템에 편입함으로서 게임에서의 성능 최적화를 기대할 수 있습니다.

## Examples

아래는 간단한 풀링 객체를 생성하기 위해 Mono 컴포넌트를 작성하는 방법에 대해 설명합니다.

```c#
public sealed class ManagedRecycleObject : RecycleableMonobehaviour
{
    // 이 예제에서는 간단히 delegate를 씬과 함께 저장할 수 있는
    // UnityEngine.Events.UnityEvent 로 유동적인 사용자 함수 호출을 보여줍니다.
    
    public UnityEvent onCreation;
    public UnityEvent onInitializion;
    public UnityEvent onTermination;
    
    // PrefabManager 인스펙터 뷰에서 보여질 이름을 설정합니다.
    // 런타임에는 아무런 영향을 끼치지 않습니다.
    public override string DisplayName => name;
    
    // 이 재사용 오브젝트가 PrefabManager에 의해 처음으로 생성되었을때 호출되는 함수입니다.
    public override void OnCreated() => onCreation?.Invoke();
    
    // 이 재사용 오브젝트가 재사용 풀에서 대기 중, 요청에 의해 다시 꺼내져왔을때 호출되는 함수입니다.
    public override void OnInitialize() => onInitializion?.Invoke();
    
    // 이 재사용 오브젝트가 사용을 마치고, 재사용 풀로 되돌아 갈때 호출되는 함수입니다.
    public override void OnTerminate() => onTermination?.Invoke();
}
```



------

## Delegates

| Name                       | Description |
| :------------------------- | ----------- |
| bool TerminatedCondition() |             |



## Members

| Name              | Description                                                  |
| :---------------- | ------------------------------------------------------------ |
| onTerminateAction | 이 객체가 재사용 풀로 되돌아갈때 실행되는 `delegate` 입니다. |
| onTerminate       | 이 객체가 재사용 풀로 되돌아갈때 실행되는 `delegate` 입니다. |
| OnActivated       | 이 `delegate` 는 할당되었을 경우, 매 프레임마다 호출되며 `false` 를 반환시키면 이 모노객체는 즉시 재사용 풀로 돌아갑니다. |



## Properties

| Name            | Description                                                  |
| :-------------- | ------------------------------------------------------------ |
| DisplayName     | [PrefabManager](https://github.com/Syadeu/CoreSystem/wiki/PrefabManager) 인스펙터 뷰에서 보여질 이름을 설정합니다. |
| Activated       | 현재 이 재사용 객체가 사용 중 인가요?                        |
| WaitForDeletion | 이 재사용 객체가 [PrefabManager](https://github.com/Syadeu/CoreSystem/wiki/PrefabManager) 에 의한 최적화로 파괴될 예정인가요? |



## Public Methods

| Name                  | Description                                                  |
| :-------------------- | ------------------------------------------------------------ |
| `internal` Initialize | PrefabManager 에서 사용하는 메소드입니다.                    |
| OnCreated             | 이 오브젝트가 생성되었을때 한번만 실행하는 함수입니다.       |
| OnInitialize          | 이 오브젝트가 재사용 풀에서 대기 중, 요청에 의해 다시 꺼내져왔을때 호출되는 함수입니다. |
| OnTerminate           | 이 오브젝트가 사용을 마치고, 재사용 풀로 되돌아 갈때 호출되는 함수입니다. |
| Terminate             | 이 오브젝트의 사용이 완료되었음을 알리는 함수입니다.         |

