_Namespace: Syadeu_

```csharp
public struct CoreRoutine : IValidation, IEquatable<CoreRoutine>
```

[CoreSystem](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem) 에서 관리하는 모든 `iterator` 들의 상태를 담고 있는 구조체입니다.

**Implements**: [IValidation](https://github.com/Syadeu/CoreSystem/wiki/IValidation), IEquatable\<T>

## Overview

* 해당 `iterator` 의 타입과 상태를 확인할 수 있습니다.

## Remarks

## Description

## Examples

```c#
using UnityEditor;
using Syadeu;

CoreRoutine m_Routine;

public IEnumerator Test()
{
	m_Routine = CoreSystem.StartBackgroundUpdate(this, TestIteration());
    yield return null;
    Debug.Log(m_Routine.IsRunning);
    
    yield return new WaitForSeconds(1);
    
    CoreSystem.RemoveBackgroundUpdate(m_Routine);
    yield return null;
    Debug.Log(m_Routine.IsRunning);
}

public IEnumerator TestIteration()
{
    while (m_Routine.IsRunning)
    {
        yield return null;
    }
    
    Debug.Log("This will never reached");
}
```



------

## Properties

| Name         | Description                            |
| :----------- | -------------------------------------- |
| IsEditor     | 에디터에서 실행되는 루틴인가요?        |
| IsBackground | 백그라운드에서 실행되는 루틴인가요?    |
| IsRunning    | 현재 이 루틴이 실행 중인지 반환합니다. |



## Public Methods

| Name    | Description                                         |
| :------ | --------------------------------------------------- |
| IsValid | 이 루틴이 유효한지 반환합니다.                      |
| Equals  | 다른 루틴과 비교했을때, 같은 루틴인지를 반환합니다. |

