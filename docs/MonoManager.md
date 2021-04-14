_Namespace: Syadeu_
```csharp
public abstract class MonoManager<T> : ManagerEntity, IStaticMonoManager where T : Component, IStaticMonoManager
```

**Inheritance**: [ManagerEntity](https://github.com/Syadeu/CoreSystem/wiki/ManagerEntity) -> MonoManager\<T> 

**Implements**: [IStaticMonoManager](https://github.com/Syadeu/CoreSystem/wiki/IStaticMonoManager)

## Overview
* 사용자가 모노스크립트내, 설정값이나 씬마다 유동적으로 배치 할 수 있는 싱글톤 객체를 만들 수 있습니다. 
* [StaticManager](https://github.com/Syadeu/CoreSystem/wiki/StaticManager)와 기능과 작동부는 거의 같지만, MonoManager는 초기화 함수가 Awake에서 작동한다는 점이 가장 다릅니다. 

## Remarks
MonoManager는 게임 전역에서 사용되지않거나, 해당 씬에서만 사용되어야되는 싱글톤 객체를 만들기 위한 abstract class 입니다.  

[StaticManager](https://github.com/Syadeu/CoreSystem/wiki/StaticManager)와 동일한 override Property이 존재하고, override ManualInitialize 로 Awake 함수에서 자동으로 초기화 되는 것이 아닌, 사용자가 초기화 타이밍을 결정 할 수 있는 것이 가장 큰 차이점이라고 말할 수 있습니다.  

Override로 [StaticManager](https://github.com/Syadeu/CoreSystem/wiki/StaticManager)와 유사하게 만들 수 있지만, Instant 변수에 접근시 싱글톤이 즉시 생성되는 것이 아니기에 이 점을 유의하여야됩니다. 

## Description

## Examples

아래는 간단한 MonoManager를 만드는 방법에 대해 설명합니다.

```c#
public sealed class TestMonoManager : MonoManager<TestMonoManager>
{
    // 기본값은 null 입니다.
    public override string DisplayName => "IMTESTMONO";
    
    // 기본값은 false 입니다.
    public override bool DontDestory => false;
    
    // 기본값은 false 입니다.
    public override bool HideInHierarchy => false;
    
    // 이 매니저가 Awake 함수에서 Initialize 함수를 호출할 것인지를 설정합니다.
    // 기본값은 false 입니다.
    public override bool ManualInitialize => false;
    
    public string m_UserStringValue = null;
    public int m_UserIntValue = 0;
    
    protected override void Awake()
    {
        Debug.Log($"1. Has Instance = {HasInstance}\n1. Is Initialized = {Initialized}");
        // 이 base.Awake()를 절때 지우면 안됩니다.
        base.Awake();
        Debug.Log($"2. Has Instance = {HasInstance}\n2. Is Initialized = {Initialized}");
    }
    public override void OnInitialize()
    {
        Debug.Log("Initialized");
    }
    public override void OnStart()
    {
        Debug.Log("Started");
    }
    
    // Expected Log
    //
    // 1. Has Instance = false
    // 1. Is Initialized = false
    // Initialized
    // Started
    // 2. Has Instance = true
    // 2. Is Initialized = true
}
```

