_Namespace: Syadeu_
```csharp
public sealed class ForegroundJob : IJob
```

유니티 메인 스레드에서 단일 [Action](https://docs.microsoft.com/ko-kr/dotnet/api/system.action?view=net-5.0)을 실행할 수 있는 잡 클래스입니다.

**Inheritance**: [Object](https://docs.microsoft.com/ko-kr/dotnet/api/system.object?view=net-5.0) -> ForegroundJob

**Implements**: [IJob](https://github.com/Syadeu/CoreSystem/wiki/IJob)

## Overview
* 간편하게 람다로 메소드를 유니티 스레드에게 수행시키도록 할 수 있습니다.
* 여러 잡들을 묶어 병렬 수행을 시킬 수 있습니다.

## Remarks

## Description