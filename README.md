# CoreSystem

CoreSystem은 Unity Open-source Background-Thread 프로젝트입니다. 기본적으로 Unity는 백그라운드 스레드를 권장하지 않지만, 게임에 있어 백그라운드 스레드가 달성할 수 있는 목표는 매우 많습니다. 예를 들어, 구조체들을 이용한 대규모 연산 작업, 게임에 있어 꼭 필요하지만 연산 코스트가 큰 메소드들을 CoreSystem을 통해 쉽게 달성할 수 있습니다.
백그라운드 스레드에서 Mono 객체들을 접근하기위한 각종 Thread-Safe Entity abstruct class 들을 지원하며, 이를 통해 백그라운드에서 Mono 객체를 안전하게 접근하여 사용 할 수 있습니다.

부가적으로, 쉬운 FMOD 사용을 위한 컬렉션도 제공됩니다.
FMOD 가 프로젝트에 설치되있어야 정상적으로 CoreSystem 이 동작합니다.

## Quick overview
* CoreSystem 은 Mono 기반 Background-Thread framework 입니다.
* CoreSystem 은 백그라운드 스레드에서 유니티 스레드와의 연결을 완전히 보장하지 않습니다.
(UnityEngine 네임스페이스안 거의 대부분의 메소드들은 백그라운드에서 사용하지 못합니다)
* CoreSystem.cs 는 모든 백그라운드 스레드를 사용할때 사용하는 기본 매니저 객체입니다.
* StaticManager.cs 는 백그라운드 스레드에서 즉시 싱글톤 객체를 만들 수 있는 Mono abstruct class입니다.
* MonoManager.cs 는 사용자가 싱글톤 객체를 만들 수 있게 도와주는 Mono abstruct class입니다.
* StaticSettingEntity.cs 는 백그라운드 스레드에서 즉시 싱글톤 ScriptableObject 객체를 만들 수 있는 Mono abstruct class입니다.
* BackgroundJob.cs, ForegroundJob.cs 는 각각 백그라운드, 유니티 스레드에서 void Action을 실행할 수 있는 객체입니다.
