_Namespace: Syadeu.Mono_

```csharp
public sealed class ManagedRecycleObject : RecycleableMonobehaviour
```



**Inheritance**: [RecycleableMonobehaviour](https://github.com/Syadeu/CoreSystem/wiki/RecycleableMonoBehaviour) -> ManagedRecycleObject

## Overview

* 간단하게 사용자가 설정한 `delegate`를 상황에 맞게 수행시킵니다.

## Remarks

## Description

아래는 인스펙터 뷰 입니다.

[[/uploads/ManagedRecycleObject/1.PNG]]

1. On Created 는 이 인스턴스를 처음으로 생성했을때 한번만 실행합니다.
2. On Initialize 는 이 인스턴스를 재사용하기위해 호출했을때 실행합니다.
3. On Terminate 는 이 인스턴스를 다시 풀링 시스템에 반환할때 실행합니다.



## Examples



------

