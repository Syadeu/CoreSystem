_Namespace: Syadeu_
```csharp
#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public abstract class StaticSettingEntity<T> : SettingEntity, IStaticSetting where T : ScriptableObject, IStaticSetting
```

모든 싱글톤 [ScriptableObject](https://docs.unity3d.com/ScriptReference/ScriptableObject.html) 들의 기본 `abstract class` 입니다.

**Inheritance**: [SettingEntity](https://github.com/Syadeu/CoreSystem/wiki/SettingEntity) -> StaticSettingEntity 

**Implements**: [IStaticSetting](https://github.com/Syadeu/CoreSystem/wiki/IStaticSetting)

## Overview

## Remarks

## Description

## Examples

```c#
using Syadeu;
#if UNITY_EDITOR
using UnityEditor;
#endif

// 사용자가 설정한 폴더내에 저장합니다.
// 경로는 /Assets/Resources/ 부터 시작합니다.
[CustomStaticSetting("Syadeu/SettingFolder")]
public sealed class TestSetting : StaticSettingEntity<CreatureList>
{
#if UNITY_EDITOR
    [MenuItem("Syadeu/Edit TestSetting")]
    public static void MenuItem()
    {
        Selection.activeObject = Instance;
        EditorApplication.ExecuteMenuItem("Window/General/Inspector");
    }
#endif
}
```



------

## Static Properties

| Name     | Description             |
| :------- | ----------------------- |
| Instance | 이 객체의 싱글톤입니다. |



## Properties

| Name              | Description                                                  |
| :---------------- | ------------------------------------------------------------ |
| Initialized       | 이 객체가 초기화되었는지 반환합니다.                         |
| RuntimeModifiable | 이 [ScriptableObject](https://docs.unity3d.com/ScriptReference/ScriptableObject.html) 가 런타임 중 변경되어 저장될 수 있는지 설정합니다. |



## Public Methods

| Name          | Description                               |
| :------------ | ----------------------------------------- |
| OnInitialized | 이 객체가 초기화 될 때 실행될 함수입니다. |
| Initialize    | 이 객체를 초기화합니다.                   |



------

## Inherited Members

### Protected Static Methods

| Name         | Description                                                  |
| :----------- | ------------------------------------------------------------ |
| IsMainthread | 이 메소드가 실행된 스레드가 유니티 메인 스레드인지 반환합니다. |

