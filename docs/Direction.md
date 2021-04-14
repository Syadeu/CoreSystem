_Namespace: Syadeu.Database_
```csharp
public enum Direction
```

**Inheritance**: 
[Object](https://docs.microsoft.com/ko-kr/dotnet/api/system.object?view=net-5.0) -> [ValueType](https://docs.microsoft.com/en-us/dotnet/api/system.valuetype?view=net-5.0) -> [Enum](https://docs.microsoft.com/en-us/dotnet/api/system.enum?view=net-5.0) -> Direction

## Fields
| Name            | Value                 | Description        |
|-----------------|-----------------------|--------------------|
| NONE            | 0                     | 방향이 없습니다.   |
| Up              | 1 << 0                | 위쪽               |
| Down            | 1 << 1                | 아래쪽             |
| Left            | 1 << 2                | 왼쪽               |
| Right           | 1 << 3                | 오른쪽             |
| UpDown          | Up \| Down            | 위 아래            |
| UpLeft          | Up \| Left            | 왼쪽 위            |
| UpRight         | Up \| Right           | 오른쪽 위          |
| DownLeft        | Down \| Left          | 왼쪽 아래          |
| DownRight       | Down \| Right         | 오른쪽 아래        |
| LeftRight       | Left \| Right         | 왼쪽 오른쪽        |
| UpLeftDown      | Up \| Left \| Down    | 위쪽 왼쪽 아래쪽   |
| UpRightDown     | Up \| Right \| Down   | 위쪽 오른쪽 아래쪽 |
| LeftUpRight     | Left \| Up \| Right   | 왼쪽 위쪽 오른쪽   |
| LeftDownRight   | Left \| Down \| Right | 왼쪽 아래쪽 오른쪽 |
| UpRightCorner   | Up \| Right           | 오른쪽 위          |
| UpLeftCorner    | Up \| Left            | 왼쪽 위            |
| DownRightCorner | Down \| Right         | 오른쪽 아래        |
| DownLeftCorner  | Down \| Left          | 왼쪽 아래          |
| UpDownLeftRight | ~0                    | 전 방향            |