_Namespace: Syadeu.Mono_

```csharp
public sealed class PrefabList : StaticSettingEntity<PrefabList>
```

**Inheritance**: [StaticSettingEntity\<T>](https://github.com/Syadeu/CoreSystem/wiki/StaticSettingEntity) -> PrefabList

## Overview

* [PrefabManager](https://github.com/Syadeu/CoreSystem/wiki/PrefabManager) 에서 사용할 프리팹에 대한 설정을 담을 수 있습니다.

## Remarks

이 객체는 유니티 에셋 형태로 /Assets/Resources/Syadeu/ 폴더에 저장되며, [PrefabManager](https://github.com/Syadeu/CoreSystem/wiki/PrefabManager) 에서만 사용됩니다.

## Description

## Examples

아래는 간단하게, 아무런 스크립트도 부착하지않은 프리팹을 풀링 시스템에 편입하는 방법에 대해 설명합니다.

```c#
using UnityEngine;
using Syadeu.Mono;

public void PrefabPullingTest()
{
    // PrefabList내 프리팹 리스트의 0번째 인덱스는 아무것도 달리지않은 오브젝트라 가정하고,
    // 해당 프리팹을 풀링 시스템으로 편입시키기 위해, PrefabManager는 해당 프리팹 최상단에
    // ManagedRecycleObject 컴포넌트를 부착하여 반환합니다.
    RecycleableMonobehaviour recycleObj = PrefabManager.GetRecycleObject(0);
    
    // 사용이 끝나면, 사용이 끝남을 선언하여야합니다.
    // 선언 후에는 유후 인스턴스가 되어 다른 요청이 있을때 반환합니다.
    recycleObj.Terminate();
    
    // Terminated 된 인스턴스들을 강제로 방출합니다.
    // PrefabList 에서 방출 타이머 설정을 하지않았다면 이 메소드 호출이 필요하지만,
    // 설정되있다면 굳이 필요하지 않습니다.
    PrefabManager.ReleaseTerminatedObjects();
}
```

------

## Static Methods

| Name                   | Description                                                  |
| :--------------------- | ------------------------------------------------------------ |
| GetRecycleObject       | ObjectSettings에서 리스트 인덱스 값으로 재사용 인스턴스를 받아옵니다. |
| GetRecycleObject\<T>   | 해당 타입과 일치하는 리사이클 인스턴스를 받아옵니다.         |
| `EDITOR ONLY` MenuItem |                                                              |



## Properties

| Name           | Description                               |
| :------------- | ----------------------------------------- |
| ObjectSettings | 재사용 오브젝트들을 정의한 리스트 입니다. |



------

## Inherited Members

### Protected Static Methods

| Name         | Description                                                  |
| :----------- | ------------------------------------------------------------ |
| IsMainthread | 이 메소드가 실행된 스레드가 유니티 메인 스레드인지 반환합니다. |



### Static Properties

| Name     | Description             |
| :------- | ----------------------- |
| Instance | 이 객체의 싱글톤입니다. |



### Properties

| Name              | Description                                                  |
| :---------------- | ------------------------------------------------------------ |
| Initialized       | 이 객체가 초기화되었는지 반환합니다.                         |
| RuntimeModifiable | 이 [ScriptableObject](https://docs.unity3d.com/ScriptReference/ScriptableObject.html) 가 런타임 중 변경되어 저장될 수 있는지 설정합니다. |



### Public Methods

| Name          | Description                               |
| :------------ | ----------------------------------------- |
| OnInitialized | 이 객체가 초기화 될 때 실행될 함수입니다. |
| Initialize    | 이 객체를 초기화합니다.                   |

