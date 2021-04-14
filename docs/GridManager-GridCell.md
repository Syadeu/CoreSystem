_Namespace: Syadeu.Mono_

```csharp
public struct GridCell : IValidation, IEquatable<GridCell>, IDisposable
```

[Grid](https://github.com/Syadeu/CoreSystem/wiki/GridManager-Grid) 내 각 타일입니다.

**Inheritance**: [Object](https://docs.microsoft.com/ko-kr/dotnet/api/system.object?view=net-5.0) -> GridCell

**Implements**: [IValidation](https://github.com/Syadeu/CoreSystem/wiki/IValidation), IEquatable\<T>, [IDisposable](https://docs.microsoft.com/ko-kr/dotnet/api/system.idisposable?view=net-5.0)

## Overview

## Remarks

## Description

## Examples



------

## Properties

| Name                | Description                                                  |
| :------------------ | ------------------------------------------------------------ |
| Idxes               | 부모 [Grid](https://github.com/Syadeu/CoreSystem/wiki/GridManager-Grid) 인덱스 x 와 이 셀의 인덱스 y 를 int2 에 담아 반환합니다. |
| ParentIdx           | 부모 [Grid](https://github.com/Syadeu/CoreSystem/wiki/GridManager-Grid) 인덱스를 반환합니다. |
| Idx                 | 이 셀의 인덱스를 반환합니다.                                 |
| Location            | 이 셀의 부모 [Grid](https://github.com/Syadeu/CoreSystem/wiki/GridManager-Grid) 상 로컬 좌표를 반환합니다. |
| Bounds              | 이 셀의 월드 [Bounds](https://docs.unity3d.com/ScriptReference/Bounds.html) 를 반환합니다. |
| IsRoot              | 이 셀이 루트 셀인가요?                                       |
| HasDependency       | 이 셀의 루트 셀이 있는지 반환합니다.                         |
| DependencyTarget    | 이 셀이 자식 셀이면, 루트 셀의 Idxes 를 반환합니다. 없다면 (-1, -1)을 반환합니다. |
| HasDependencyChilds | 이 셀이 루트 셀이고, 자식 셀이 있는지 반환합니다.            |
| Enabled             | 이 셀이 활성화 되었는지 반환합니다. 루트가 존재한다면, 부모의 값을 반환합니다. |
| Highlighted         | 이 셀이 강조되었는지 반환합니다.                             |
| Color               | 이 셀의 컬러값을 Enabled, Highlighted, BlockedByNavMesh 로 판단하여 반환합니다. 루트가 존재한다면, 루트의 값을 반환합니다. |
| BlockedByNavMesh    | 이 셀이 Unity NavMesh에 의해 비활성화 되었는지 반환합니다. 부모 그리드가 NavMesh에 영향받지 않는다면, 무조건 `false` 를 반환합니다. |



## Public Methods

| Name                       | Description                                                  |
| :------------------------- | ------------------------------------------------------------ |
| IsValid                    | 이 셀이 유효한지 반환합니다.                                 |
| Equals                     | 이 셀과 타겟 셀이 같은지 반환합니다.                         |
| IsVisible                  | 이 셀이 현재 [RenderManager](https://github.com/Syadeu/CoreSystem/wiki/RenderManager)에서 감시 중인 카메라 뷰 내에 존재하는지 반환합니다. |
| HasCell                    | 해당 방향에 셀이 존재하는지 반환합니다.                      |
| FindCell                   | 주어진 방향의 셀을 반환합니다.                               |
| GetCustomData              | 이 셀의 커스텀 데이터를 반환합니다. 루트가 존재한다면, 루트의 값을 반환합니다. |
| GetCustomData\<T>          | 이 셀의 커스텀 데이터를 해당 타입으로 반환합니다. 루트가 존재한다면, 루트의 값을 반환합니다. |
| SetCustomData\<T>          | 이 셀의 커스텀 데이터를 지정합니다. 루트가 존재한다면, 루트의 커스텀 데이터를 저장합니다. |
| RemoveCustomData           | 이 셀의 커스텀 데이터를 제거합니다. 루트가 존재한다면, 루트의 커스텀 데이터를 제거합니다. |
| MoveCustomData             | 이 셀의 커스텀 데이터를 해당 인덱스로 이동합니다.            |
| HasTargetDependency        | 이 셀이 루트 셀이고,  입력한 인덱스의 자식 셀이 있는지 반환합니다. |
| EnableDependency           | 이 셀을 입력한 셀의 자식 셀로 설정합니다. 이후 GetCustomData 와 같은 메소드들은 루트 셀의 메소드로 연결됩니다. |
| DisableDependency          | 이 셀을 독립시킵니다.                                        |
| DisableAllChildsDependency | 이 셀이 루트 셀이고, 자식 셀이 있다면 모두 독립시킵니다.     |
| SetDirty                   | 이 셀을 Dirty 마크합니다. 루트가 존재한다면, 루트와 루트의 자식 전부를 Dirty 마크하고, 루트라면 자식셀도 전부 Dirty 마크합니다. |
| Dispose                    | 사용하지마세요. 이 셀을 방출합니다.                          |

