_Namespace: Syadeu_

```csharp
[AttributeUsage(AttributeTargets.Class)]
public sealed class StaticManagerIntializeOnLoadAttribute : Attribute
```

간단한 설명

**Inheritance**: Attribute -> StaticManagerIntializeOnLoadAttribute

## Overview

* [StaticManager\<T>](https://github.com/Syadeu/CoreSystem/wiki/StaticManager)를 상속받는 매니저 객체를 게임 시작 즉시 생성하도록 할 수 있습니다.

## Remarks

이 어트리뷰트를 사용하는 것은 전역 매니저이고, 반드시 있어야되는 객체에만 적용할 것을 추천합니다.

## Description

이 어트리뷰트를 사용하려면 SyadeuSettings.m_EnableAutoStaticInitialize 가 true 이어야 합니다.

이후, [SyadeuSettings](https://github.com/Syadeu/CoreSystem/wiki/SyadeuSettings).m_AutoInitializeTargetAssembly 에 입력된 Assemble 의 이름 내, 해당 어트리뷰트를 참조한 객체에만 적용됩니다.

## Examples

아래는 간단하게 어트리뷰트를 추가하여, 게임내 중요 매니저 객체를 즉시 생성하는 방법에 대해 설명합니다.

```c#
using UnityEngine;
using Syadeu;

[StaticManagerIntializeOnLoad]
public sealed class GameManager : StaticManager<GameManager>
{
    /*
    
    Some codes....
    
    */
    
    public override OnInitialize()
    {
        Debug.Log("Game is Started!");
    }
}

// Expected Logs On Game Start
//
// Game is Started!
```



------

