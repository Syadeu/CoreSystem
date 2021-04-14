_Namespace: Syadeu.Mono_

```csharp
public struct Grid : IValidation, IEquatable<Grid>, IDisposable
```

[GridCell](https://github.com/Syadeu/CoreSystem/wiki/GridManager-GridCell)들을 담고있는 최상위 구조체입니다.

**Inheritance**: [Object](https://docs.microsoft.com/ko-kr/dotnet/api/system.object?view=net-5.0) -> Grid

**Implements**: [IValidation](https://github.com/Syadeu/CoreSystem/wiki/IValidation), IEquatable\<T>, [IDisposable](https://docs.microsoft.com/ko-kr/dotnet/api/system.idisposable?view=net-5.0)

## Overview

## Remarks

## Description

## Examples

```c#
using UnityEngine;
using Syadeu;
using Syadeu.Mono;

public class TestMono : Monobehaviour
{
    private int m_GridIdx;
    
    public void CreateGrid()
    {
        m_GridIdx = GridManager.CreateGrid
        (
        	// 그리드의 총 크기를 넣습니다.
        	new Bounds(Vector3.zero, Vector3.one * 10),
        	// 그리드 셀의 크기를 결정합니다.
        	// 그리드 셀은 무조건 정사각형으로 생성되며,
        	// 입력한 값은 세로폭, 가로폭이 됩니다.
        	gridCellSize: 1,
        	// 이 그리드가 유니티 NavMesh 에 영향받는 그리드인지를 설정합니다.
        	enableNavMesh: false
	    );
    }
    public ref GridManager.Grid GetGrid()
    {
        // 앞에서 생성한 그리드를 가져옵니다.
        return ref GridManager.GetGrid(m_GridIdx);
    }
    
    private void Awake()
    {
        ref GridManager.Grid _grid = GetGrid();
        Debug.Log($"{m_GridIdx} == {_grid.Idx}");
    }
}
```



------

## Properties

| Name          | Description                                                  |
| :------------ | ------------------------------------------------------------ |
| Guid          | 이 그리드의 고유 Guid 입니다.                                |
| Idx           | 이 그리드의 고유 인덱스 입니다.                              |
| CellSize      | 이 그리드 내 [GridCell](https://github.com/Syadeu/CoreSystem/wiki/GridManager-GridCell)의 실제 넓이와 폭 길이 입니다. |
| Length        | 이 그리드이 가진 [GridCell](https://github.com/Syadeu/CoreSystem/wiki/GridManager-GridCell)의 총 갯수입니다. |
| EnableNavMesh | Unity NavMesh 에 영향받는 그리드인가요?                      |
| EnableDrawGL  | [GridCell](https://github.com/Syadeu/CoreSystem/wiki/GridManager-GridCell)들을 GL로 그릴까요? |
| EnableDrawIdx | 디버그용으로 Unity 씬뷰에 [GridCell](https://github.com/Syadeu/CoreSystem/wiki/GridManager-GridCell) 인덱스를 표기할까요? |



## Static Methods

| Name      | Description                       |
| :-------- | --------------------------------- |
| FromBytes | 바이너리에서 그리드로 변환합니다. |



## Public Methods

| Name                          | Description                                                  |
| :---------------------------- | ------------------------------------------------------------ |
| IsValid                       | 이 그리드가 유효한지 반환합니다.                             |
| Equals                        | 이 그리드와 해당 그리드가 같은 그리드인지 반환합니다.        |
| HasCell                       | 해당 값에 [GridCell](https://github.com/Syadeu/CoreSystem/wiki/GridManager-GridCell)이 존재하는지 반환합니다. |
| `Unsafe Only` GetCellPointer  | 해당 [GridCell](https://github.com/Syadeu/CoreSystem/wiki/GridManager-GridCell)의 포인터를 가져옵니다. |
| GetCell                       | 해당 [GridCell](https://github.com/Syadeu/CoreSystem/wiki/GridManager-GridCell)을 가져옵니다. |
| GetCells                      | 해당 조건에 맞는 [GridCell](https://github.com/Syadeu/CoreSystem/wiki/GridManager-GridCell)들을 가져옵니다. |
| GetRange                      | 지정한 [GridCell](https://github.com/Syadeu/CoreSystem/wiki/GridManager-GridCell) 인덱스를 기준으로 입력한 범위내 모든 셀을 [GridRange](https://github.com/Syadeu/CoreSystem/wiki/GridManager-GridRange) 에 담아 반환합니다. |
| For                           | 빠르게 [GridCell](https://github.com/Syadeu/CoreSystem/wiki/GridManager-GridCell)을 탐색합니다. |
| For\<T>                       | 빠르게 해당 타입의 커스텀 데이터를 가지고있는 셀을 탐색합니다. |
| `Unsafe Only` ParallelFor     | 병렬로 [GridCell](https://github.com/Syadeu/CoreSystem/wiki/GridManager-GridCell)을 탐색합니다. |
| `Unsafe Only` ParallelFor\<T> | 병렬로 해당 타입의 커스텀 데이터를 가지고 있는 [GridCell](https://github.com/Syadeu/CoreSystem/wiki/GridManager-GridCell)을 탐색합니다. |
| GetCustomData                 | 커스텀 데이터를 가져옵니다.                                  |
| GetCustomData\<T>             | 해당 타입의 커스텀 데이터를 가져옵니다.                      |
| SetCustomData                 | 입력한 데이터를 이 [GridCell](https://github.com/Syadeu/CoreSystem/wiki/GridManager-GridCell)에 넣습니다. |
| RemoveCustomData              | 이 셀의 커스텀 데이터를 삭제합니다.                          |
| ConvertToWrapper              | 바이너리로 변환 가능한 형태로 변환하여 반환합니다.           |
| OnDirtyMarked                 | [GridCell](https://github.com/Syadeu/CoreSystem/wiki/GridManager-GridCell)이 Dirty 마크 되었을때, 유니티 스레드에서 실행할 `delegate`를 설정합니다. |
| OnDirtyMarkedAsync            | [GridCell](https://github.com/Syadeu/CoreSystem/wiki/GridManager-GridCell)이 Dirty 마크 되었을때, 백그라운드 스레드에서 실행할 `delegate`를 설정합니다. |
| Dispose                       | 사용하지마세요. 이 그리드를 방출합니다.                      |

