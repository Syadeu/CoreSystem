## [CoreSystem](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem) 위키에 오신 걸 환영합니다.

[CoreSystem](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem) 은 Unity Background-Thread 프로젝트입니다.
기본적으로 Unity는 멀티스레드를 권장하지 않지만, 게임에 있어 멀티스레드가 달성할 수 있는 목표는 매우 많습니다. 예를 들어 대규모 연산 작업, 게임에 있어 꼭 필요하지만 연산 코스트가 큰 메소드들을 [CoreSystem](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem) 을 통해 쉽게 달성할 수 있습니다.

백그라운드 스레드에서 Mono 객체들을 접근하기위한 각종 Thread-Safe Entity abstract class 들을 지원하며, 이를 통해 백그라운드에서 Mono 객체를 안전하게 접근하여 사용 할 수 있습니다.

쉬운 FMOD 사용을 위한 컬렉션도 제공됩니다.

[![Test project](https://github.com/Syadeu/CoreSystem/actions/workflows/test-runner.yml/badge.svg)](https://github.com/Syadeu/CoreSystem/actions/workflows/test-runner.yml)

## Quick Overview

* [CoreSystem](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem) 은 Mono 기반 Multi-Threaded framework 입니다.
* [CoreSystem](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem) 은 백그라운드 스레드에서 Unity 스레드와의 연결을 완전히 보장하지 않습니다.
  (UnityEngine 네임스페이스안 거의 대부분의 메소드들은 백그라운드에서 사용하지 못합니다)
* [CoreSystem](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem) 는 모든 백그라운드 스레드를 사용할때 사용하는 기본 매니저 객체입니다.
* [StaticManager](https://github.com/Syadeu/CoreSystem/wiki/StaticManager) 는 백그라운드 스레드에서 즉시 싱글톤 객체를 만들 수 있는 Mono abstruct class입니다.
* [MonoManager](https://github.com/Syadeu/CoreSystem/wiki/MonoManager) 는 사용자가 싱글톤 객체를 만들 수 있게 도와주는 Mono abstruct class입니다.
* [StaticSettingEntity](https://github.com/Syadeu/CoreSystem/wiki/StaticSettingEntity) 는 백그라운드 스레드에서 즉시 싱글톤 [ScriptableObject](https://docs.unity3d.com/ScriptReference/ScriptableObject.html) 객체를 만들 수 있는 Mono abstruct class입니다.
* [BackgroundJob](https://github.com/Syadeu/CoreSystem/wiki/BackgroundJob), [ForegroundJob](https://github.com/Syadeu/CoreSystem/wiki/ForegroundJob) 는 각각 백그라운드, 유니티 스레드에서 delegate를 실행할 수 있는 객체입니다.
* [PresentationManager](https://github.com/Syadeu/CoreSystem/wiki/PresentationManager) 는 Entity Component System (ECS) 에서 영감을 받아 게임을 보다 효율적으로 보여주기 위해 시스템들로 구성된 그룹을 관리하는 객체입니다.
* [SQLiteDatabase](https://github.com/Syadeu/CoreSystem/wiki/SQLiteDatabase) 는 비동기 및 편리한 작업을 위한 데이터베이스 시스템입니다.

------

*CoreSystem 은 현재 개발 중인 프레임워크로, 완전히 개발된 상태가 아님을 참고바랍니다.*

## 시작하기

[[/uploads/SetupWizard_1.PNG]]

CoreSystem 으로 개발을 시작하려면 먼저 게임을 시스템에 맞게 설정하여야 합니다. 상단의 CoreSystem/Setup Wizard 메뉴를 통해 쉽게 설정할 수 있습니다.

필요한 모든 데이터를 로드할 수 있는 Master Scene, 실제 게임으로 진입하기전 점검할 수 있는 Start Scene (메인메뉴), 로딩 중에 노출될 Loading Scene, 그리고 게임에서 사용할 씬들을 Scenes 안에 추가할 수 있습니다.

[[/uploads/SetupWizard_2.PNG]]

CoreSystem 은 Addressable 을 통해 리소스를 관리합니다. 이후 [EntitySystem](https://github.com/Syadeu/CoreSystem/wiki/EntitySystem) 에서 Entity 가 사용할 Prefab 를 추가하고나서 Rebase 를 하여야지 EntitySystem 에 반영됩니다.

[[/uploads/SetupWizard_3.PNG]]

CoreSystem 은 기본으로 PrefabList 이름으로 Addressable Group 을 생성합니다. 이곳에 에셋을 추가하거나, 사용자가 추가한 Addressable Group 에 Prefab List Schema 를 추가하면 정상적으로 반영됩니다.

[[/uploads/SetupWizard_4.PNG]]

