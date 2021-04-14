_Namespace: Syadeu.Mono_

```csharp
public struct GridRange
```

검색한 [GridCell](https://github.com/Syadeu/CoreSystem/wiki/GridManager-GridCell) 들을 담고있는 Native Container 입니다.

**Inheritance**: [Object](https://docs.microsoft.com/ko-kr/dotnet/api/system.object?view=net-5.0) -> GridRange

## Overview

## Remarks

## Description

## Examples

아래는 `#define CORESYSTEM_UNSAFE` 되었을 때 사용 방법입니다.

```c#
#define CORESYSTEM_UNSAFE

public void GridRangeTest()
{
    int _gridIdx = GridManager.CreateGrid(bounds, 1, false);
    ref GridManager.Grid grid = ref GridManager.GetGrid(in _gridIdx);
    
    GridManager.GridRange _range = grid.GetRange(0, 5);
    unsafe
    {
        for (int i = 0; i < _range.Length; i++)
        {
            Debug.Log((*_range[i]).Location);
        }
    }
}
```



아래는 일반적인 사용 방법입니다.

```c#		
public void GridRangeTest()
{
    int _gridIdx = GridManager.CreateGrid(bounds, 1, false);
    ref GridManager.Grid grid = ref GridManager.GetGrid(in _gridIdx);
    
    GridManager.GridRange _range = grid.GetRange(0, 5);
    for (int i = 0; i < _range.Length; i++)
    {
        Debug.Log(_range[i].Location);
    }
}
```



------

## Public Methods

| Name    | Description            |
| :------ | ---------------------- |
| Dispose | 메모리에서 방출합니다. |

