_Namespace: Syadeu.Database_

```csharp
public struct SQLiteColumn : IValidation
```

**Inheritance**: Object -> SQLiteColumn  
**Implements**: [IValidation](https://github.com/Syadeu/CoreSystem/wiki/IValidation)  

## Overview

* 상위 구조체인 [SQLiteTable](https://github.com/Syadeu/CoreSystem/wiki/SQLiteTable) 의 행 데이터를 담는 구조체입니다.

## Remarks

## Description

## Examples



------

## Properties

| Name   | Description                                                  |
| :----- | ------------------------------------------------------------ |
| Type   | 실제 db에 담긴 데이터 형식입니다. 사용자 지정된 타입은 string 으로 저장됩니다. |
| Name   | 이 컬럼의 이름입니다.                                        |
| Values | 이 컬럼의 값들입니다.                                        |



## Public Methods

| Name         | Description                |
| :----------- | -------------------------- |
| GetValue\<T> | 해당 열의 값을 반환합니다. |
| IsValid      | 이 컬럼이 유효한가요?      |

