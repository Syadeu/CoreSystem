_Namespace: Syadeu_
```csharp
[AttributeUsage(AttributeTargets.Class)]
public sealed class CustomStaticSettingAttribute : Attribute
```

[StaticSettingEntity](https://github.com/Syadeu/CoreSystem/wiki/StaticSettingEntity)를 상속받는 [ScriptableObject](https://docs.unity3d.com/ScriptReference/ScriptableObject.html) 의 에셋 저장 경로를 
지정한 경로로 설정합니다.  

Assets/Resources/{CustomPath}

**Inheritance**: [Object](https://docs.microsoft.com/ko-kr/dotnet/api/system.object?view=net-5.0)  -> Attribute -> CustomStaticSettingAttribute  

## Overview

## Remarks

이 어트리뷰트가 지정되지 않은 [StaticSettingEntity\<T>](https://github.com/Syadeu/CoreSystem/wiki/StaticSettingEntity) 를 상속받는 모든 [ScriptableObject](https://docs.unity3d.com/ScriptReference/ScriptableObject.html) 들은 기본 경로인 /Assets/Resources/Syadeu/{className}.asset 으로 저장됩니다.

## Description

## Examples

```c#
using Syadeu;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

// Expected Asset Folder
// {UNITY_PATH}/Assets/Resources/Syadeu/SettingFolder
```

입력한 경로가 없으면 자동으로 폴더를 생성하여 에셋을 생성합니다.



------

## Properties

| Name       | Description                                                  |
| :--------- | ------------------------------------------------------------ |
| CustomPath | 지정한 경로에 이 어트리뷰트가 있는 [ScriptableObject](https://docs.unity3d.com/ScriptReference/ScriptableObject.html) 를 .asset 형태로 저장합니다. |

