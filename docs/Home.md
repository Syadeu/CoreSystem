## [CoreSystem](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem) 위키에 오신 걸 환영합니다.

[CoreSystem](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem) 은 Unity Background-Thread 프로젝트입니다.
기본적으로 Unity는 멀티스레드를 권장하지 않지만, 게임에 있어 멀티스레드가 달성할 수 있는 목표는 매우 많습니다. 예를 들어 대규모 연산 작업, 게임에 있어 꼭 필요하지만 연산 코스트가 큰 메소드들을 [CoreSystem](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem) 을 통해 쉽게 달성할 수 있습니다.

백그라운드 스레드에서 Mono 객체들을 접근하기위한 각종 Thread-Safe Entity abstruct class 들을 지원하며, 이를 통해 백그라운드에서 Mono 객체를 안전하게 접근하여 사용 할 수 있습니다.

쉬운 FMOD 사용을 위한 컬렉션도 제공됩니다.  
이 프로젝트는 FMOD Unity Integration package가 유니티 프로젝트에 설치되어있어야됩니다.

## Quick Overview
* [CoreSystem](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem) 은 Mono 기반 Background-Thread framework 입니다.
* [CoreSystem](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem) 은 백그라운드 스레드에서 Unity 스레드와의 연결을 완전히 보장하지 않습니다.
(UnityEngine 네임스페이스안 거의 대부분의 메소드들은 백그라운드에서 사용하지 못합니다)
* [CoreSystem](https://github.com/Syadeu/CoreSystem/wiki/CoreSystem) 는 모든 백그라운드 스레드를 사용할때 사용하는 기본 매니저 객체입니다.
* [StaticManager](https://github.com/Syadeu/CoreSystem/wiki/StaticManager) 는 백그라운드 스레드에서 즉시 싱글톤 객체를 만들 수 있는 Mono abstruct class입니다.
* [MonoManager](https://github.com/Syadeu/CoreSystem/wiki/MonoManager) 는 사용자가 싱글톤 객체를 만들 수 있게 도와주는 Mono abstruct class입니다.
* [StaticSettingEntity](https://github.com/Syadeu/CoreSystem/wiki/StaticSettingEntity) 는 백그라운드 스레드에서 즉시 싱글톤 [ScriptableObject](https://docs.unity3d.com/ScriptReference/ScriptableObject.html) 객체를 만들 수 있는 Mono abstruct class입니다.
* [BackgroundJob](https://github.com/Syadeu/CoreSystem/wiki/BackgroundJob), [ForegroundJob](https://github.com/Syadeu/CoreSystem/wiki/ForegroundJob) 는 각각 백그라운드, 유니티 스레드에서 delegate를 실행할 수 있는 객체입니다.
* [SQLiteDatabase](https://github.com/Syadeu/CoreSystem/wiki/SQLiteDatabase) 는 비동기 및 편리한 작업을 위한 데이터베이스 시스템입니다.