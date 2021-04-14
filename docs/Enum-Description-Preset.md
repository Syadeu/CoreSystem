_Namespace: Syadeu_
```csharp
public abstract class StaticManager<T> : StaticManagerEntity, IStaticMonoManager where T : Component, IStaticMonoManager
```

간단한 설명

**Inheritance**: [Object](https://docs.microsoft.com/ko-kr/dotnet/api/system.object?view=net-5.0) -> [ValueType](https://docs.microsoft.com/en-us/dotnet/api/system.valuetype?view=net-5.0) -> [Enum](https://docs.microsoft.com/en-us/dotnet/api/system.enum?view=net-5.0) -> type

## Fields
<https://www.tablesgenerator.com/markdown_tables>
| Name          | Value | Description                                   |
|---------------|-------|-----------------------------------------------|
| Editor        | 0     | 에디터에서 발생한 예외사항입니다.             |
| Jobs          | 1     | 잡을 실행하는 도중 발생한 예외사항입니다.     |
| ECS           | 2     | ECS에서 발생한 예외사항입니다.                |
| Background    | 3     | 백그라운드 스레드에서 발생한 예외사항입니다.  |
| Foreground    | 4     | 메인 유니티 스레드에서 발생한 예외사항입니다. |
| RecycleObject | 5     | 재사용 오브젝트에서 발생한 예외사항입니다.    |
| Render        | 6     | 랜더 매니저에서 발생한 예외사항입니다.        |
| Console       | 7     | 콘솔에서 발생한 예외사항입니다.               |
| Database      | 8     | 데이터관련 메소드에서 예외사항입니다.         |
| Mono          | 9     | 모노 기반 오브젝트에서 발생한 예외사항입니다. |