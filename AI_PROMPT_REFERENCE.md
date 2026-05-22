# Bowling Champion — AI 프롬프트 작성용 프로젝트 구조 레퍼런스

> 본 문서는 **다른 AI 모델이 본 프로젝트에 코드를 추가/수정하기 위한 프롬프트를 작성할 때** 참고하는 단일 출처(single source of truth) 다.
> 코드 작성 시 본 문서의 이름·시그니처·계약을 그대로 따라야 한다.

---

## 0. 프로젝트 한눈에 보기

- **이름**: Bowling Champion (캐주얼 볼링)
- **엔진**: Unity 6.x LTS + URP
- **언어/플랫폼**: C#, PC Windows Standalone
- **입력**: Unity Input System (스페이스바 단일 키 기반 / R 키는 디버그 리셋)
- **저장**: JSON (예정)
- **핵심 규칙**: **독립 프레임 점수 방식**. 다음 프레임이 이전 프레임 점수에 영향 없음.
  - 스페어 = `10 + SPARE_BONUS(3)` = **13점**
  - 스트라이크 = `10 + STRIKE_BONUS(5)` = **15점**
  - 만점 = `15 × frameCount` (쇼트 75 / 풀 150)

---

## 1. 어셈블리 / 네임스페이스 구조

| 네임스페이스 | 어셈블리 | 목적 | UnityEngine 의존 |
|---|---|---|---|
| `Bowling.Scoring` | `Bowling.Domain` | 순수 도메인(점수 규칙·프레임) — Unity 비의존 | ❌ (※ `FrameManager`, `BowlingRuleConfig` 만 예외적으로 Unity 의존) |
| `BowlingGame` | `Assembly-CSharp` | 게임플레이/입력/UI/물리/씬 컴포넌트 | ⭕ |
| `Bowling.EditMode.Tests` | `Bowling.EditMode.Tests` | EditMode 유닛 테스트 (NUnit) | 일부 ⭕ |

> **중요**: `BowlingRuleConfig` 는 **`BowlingGame` 네임스페이스**에 있고 `FrameManager` 는 **`Bowling.Scoring`** 에 있다. 자주 혼동되므로 `using` 선언 시 주의.

---

## 2. 디렉토리 구조 (작업 대상)

```
Assets/
├── Scripts/
│   ├── Core/           GameManager, GameStateManager, GameState(enum)
│   ├── Gameplay/       BowlingBall, BallAimer, Pin, PinManager,
│   │                   InputController, CameraFollow,
│   │                   PhysicsSettleDetector, ThrowTransitionController
│   ├── Scoring/        Frame, FrameType, FrameManager, ScoreCalculator,
│   │                   ScoringConstants, BowlingRuleConfig
│   ├── UI/             PowerGaugeUI, ScoreboardUI
│   ├── Persistence/    GameRecord, SaveData            (※ TODO: 직렬화 미구현)
│   └── Debug/          DebugResetController            (R키 → RestartGame)
├── Tests/EditMode/     ScoreCalculatorTests, FrameManagerTests
├── Scenes/             Game.unity, mainmenu.unity
├── Configs/            ShortModeRule.asset             (※ FullModeRule.asset 미생성)
└── Settings/           URP 렌더 파이프라인 (건드리지 말 것)
```

---

## 3. 핵심 컴포넌트 카탈로그

### 3-1. 도메인 (Bowling.Scoring) — Unity 비의존

#### `ScoringConstants` (static)
- `SPARE_BONUS = 3`, `STRIKE_BONUS = 5`
- **밸런스 조정 단일 지점**. 다른 파일에 하드코딩 금지.

#### `FrameType` (enum)
- `Normal`, `Spare`, `Strike`

#### `Frame` (POCO)
- `int Ball1`, `int Ball2`, `FrameType FrameType`, `int FrameScore`
- 모든 setter는 `internal` — **외부 코드는 절대 직접 대입하지 말 것**. `FrameManager` 만 갱신 권한 보유.
- 헬퍼: `IsStrike()`, `IsSpare()`

#### `ScoreCalculator` (정적 메서드 모음, 순수 함수)
```csharp
static int       CalculateFrameScore(int ball1, int ball2);
static FrameType DetermineFrameType (int ball1, int ball2);
static int       CalculateTotalScore(List<Frame> frames);
```
- 입력 검증 정책:
  - `ball ∈ [0,10]` 위반 → `ArgumentOutOfRangeException`
  - 비-스트라이크에서 `ball1 + ball2 > 10` → `ArgumentException`
  - 스트라이크(`ball1==10`)일 때 `ball2` 무시 (검증 안 함)
  - `frames == null` → `ArgumentNullException`

### 3-2. 룰 데이터

#### `BowlingRuleConfig` (ScriptableObject, namespace = `BowlingGame`)
- 메뉴 경로: `Create → Bowling → Rule Config`
- 프로퍼티 (getter only, public):
  - `string ModeName`, `int FrameCount`, `int PinCount`
  - `float BallSpeed`, `float PowerGaugeSpeed`
  - `int GetPerfectScore()` — `(10 + STRIKE_BONUS) × FrameCount`
- 인스펙터 필드명(직렬화 대상, **이름 변경 금지** — 기존 에셋과 연결 끊김):
  - `modeName`, `frameCount`, `pinCount`, `ballSpeed`, `powerGaugeSpeed`
- 에셋: `Assets/Configs/ShortModeRule.asset` (생성됨), `FullModeRule.asset` (※ 미생성 — 풀모드 구현 시 만들 것)

### 3-3. 게임 흐름

#### `GameState` (enum, namespace = `BowlingGame`)
`Ready → AimingPosition → AimingPower → Rolling → Scoring → (반복 또는 GameOver)`

#### `GameStateManager` (MonoBehaviour, **싱글톤**)
- `static Instance`, `GameState CurrentState`
- `event Action<GameState, GameState> OnStateChanged` (prev, next)
- `void ChangeState(GameState newState)`
- 첫 상태 전이는 본인이 아니라 **`GameManager.BeginGame()`** 이 트리거함.

#### `GameManager` (MonoBehaviour, **싱글톤**, `[DefaultExecutionOrder(1000)]`)
- 흐름 조정자. 상태 머신은 `GameStateManager` 에 위임.
- 인스펙터 직렬화 필드(이름 변경 금지):
  - `ruleConfig` (BowlingRuleConfig), `pinManager`, `settleDetector`, `transitionController`, `ball` (BowlingBall), `frameManager`
- 공개 메서드: `void RestartGame()`
- 공개 프로퍼티: `FrameManager FrameManager`, `GameState CurrentState`
- 의존성 누락 시 `Debug.LogError` 후 초기화 중단 (Validate 가 false 반환).

### 3-4. 물리/게임플레이

#### `BowlingBall` (MonoBehaviour) — `Rigidbody` 필요
- 인스펙터 필드: `minForce = 8f`, `maxForce = 18f`
- 공개 API:
  - `void Launch(Vector3 startPos, float normalizedForce)` — normalizedForce ∈ [0,1]
  - `void ResetBall(Vector3 position)`
  - `void ResetToStartPosition()` — `BallSpawnPoint` 이름의 GameObject 검색, 없으면 `(0, 0.15, 0.5)`
  - `bool IsRolling`, `bool IsInGutter` (※ IsInGutter 는 현재 호출처 없음)
- **씬 의존**: `BallSpawnPoint` 라는 이름의 GameObject 가 씬에 있어야 정확한 위치로 리셋됨.

#### `BallAimer` (MonoBehaviour) — `BowlingBall` 과 같은 GameObject
- 인스펙터 필드: `laneHalfWidth = 0.43f`, `oscSpeed = 1.2f`, `ballStartZ = 0.5f`
- 공개 프로퍼티: `Vector3 ConfirmedPosition`
- `AimingPosition` 상태에서 좌우 PingPong 이동, 스페이스바로 확정 → `AimingPower` 로 전이.

#### `Pin` (MonoBehaviour) — `Rigidbody` 필요
- public 필드: `int pinId` (1~10, 인스펙터 지정)
- 직렬화 필드: `fallThreshold = 45f`, `restVelocityEpsilonSqr = 0.0025f`
- 이벤트: `event Action<Pin> OnPinFallen` (최초 쓰러진 순간 1회만)
- API: `bool IsFallen()`, `bool IsAtRest()`, `void ResetToInitialState()`, `void SetActive(bool)`
- **⚠ 내부 필드 `_fallen` 은 `DebugResetController` 가 리플렉션으로 접근할 수 있음 — 이름 변경 금지** (현재 사용처는 폐기되었으나 주석상 보존 권고).

#### `PinManager` (MonoBehaviour)
- 직렬화 필드: `List<Pin> pins` (비우면 자식에서 자동 수집)
- 핵심 API:
  - `void SnapshotBeforeThrow()` — **투구 직전** 반드시 호출
  - `int GetNewlyFallenCount()` — 스냅샷과 비교해 새로 쓰러진 핀 수 반환 (`RecordThrow` 에 전달)
  - `void RemoveFallenPins()` — 쓰러진 핀 비활성화 (1구 → 2구)
  - `void ResetAllPins()` — 전체 활성화 + 초기 위치 복원 (프레임 시작)
  - `bool AreAllPinsAtRest()` — 모든 활성 핀 정지 여부
  - 조회: `GetStandingPins`, `GetFallenPins`, `CountStanding`, `CountFallen`

#### `PhysicsSettleDetector` (MonoBehaviour)
- 직렬화 필드: `ballRigidbody`, `pinManager`, `velocityEpsilon = 0.01f`, `angularEpsilon = 0.01f`, `settleTime = 0.5f`, `maxWaitTime = 10f`
- 이벤트: `event Action OnSettled` (정지 또는 타임아웃 시 1회)
- API: `void StartDetection()`, `void StopDetection()`

#### `ThrowTransitionController` (MonoBehaviour) — 투구 후처리 코디네이터
- 직렬화 필드: `pinManager`, `ball`
- 주입: `void Initialize(FrameManager frameManager)` — `GameManager` 가 호출
- 이벤트: `event Action<TransitionResult> OnTransitionComplete`
- API: `TransitionResult HandlePostThrow()`
- **호출 순서 엄수** (어기면 카운트 오류 발생):
  1. `GetNewlyFallenCount()` ← 핀이 아직 씬에 남아있는 상태
  2. `RecordThrow(count)`
  3. 분기 (핀 제거 또는 리셋, 공 리셋)
  4. 다음 투구가 있으면 `SnapshotBeforeThrow()`

#### `TransitionResult` (enum)
- `ProceedToBall2`, `ProceedToNextFrame`, `GameOver`

#### `CameraFollow` (MonoBehaviour)
- 직렬화 필드: `stopPosition`, `followSmoothTime = 0.25f`, `returnSmoothTime = 0.4f`, `stopOffsetFromHeadPin = 2.4f`
- `Rolling` 진입 시 공 추적, `AimingPosition` 진입 시 원위치 복귀.
- `Pin_01` (pinId==1) 위치 기반으로 `stopPosition.z` 자동 보정.

### 3-5. 입력 / UI

#### `InputController` (MonoBehaviour, **싱글톤**)
- `static Instance`
- 이벤트: `event Action OnConfirmPressed` — 키 바인딩 **`<Keyboard>/space`**
- **현재 매핑이 스페이스바로 하드코딩** — 키 리바인딩은 미구현(TODO).

#### `PowerGaugeUI` (MonoBehaviour)
- 직렬화 필드: `arrowShaft` (RectTransform), `powerValueText` (TMP_Text), `minHeight = 20f`, `maxHeight = 160f`, `gaugeSpeed = 1.5f`
- 공개 프로퍼티: `float ConfirmedNormalized` (0~1)
- 색상 상수 내장: 0~40% `#00C853`, ~70% `#FFD700`, 이상 `#FF1744`

#### `ScoreboardUI` (MonoBehaviour)
- 직렬화 필드 (씬 인스펙터에서 직접 주입 — 싱글톤 우회):
  - `frameManager` (FrameManager) — `Bowling.Scoring` 어셈블리. `using Bowling.Scoring;` 필요
  - `totalScoreText`, `currentFrameText`, `frameFirstText`, `frameSecText` (TMP_Text)
- 표시 규칙 상수 (단일 출처):
  - `STRIKE_FIRST_DISPLAY = "<b>X</b>"`, `STRIKE_SEC_DISPLAY = "-"`, `SPARE_SEC_DISPLAY = "<b>/</b>"`, `EMPTY_DISPLAY = ""`
- 핸들러 5개: `FrameManager` 의 모든 이벤트 구독 (`OnGameInitialized`, `OnFrameStarted`, `OnThrowRecorded`, `OnFrameCompleted`, `OnGameOver`)
- 헬퍼 (순수 함수): `FormatFirstThrow(Frame)`, `FormatSecondThrow(Frame)`, `FormatFrameNumber(int)` (0-base → 1-base), `FormatTotalScore(int)`
- 사이드이펙트 헬퍼: `ClearAllText()` — 유일하게 UI 갱신
- 표시 규칙 결정사항:
  - 1구 거터(`Ball1==0`) / 2구 0핀(`Ball2==0`)도 `"0"` 표시 (대시 사용 안 함)
  - 스트라이크 시 `frame_N_sec` 의 `"-"` 는 `HandleFrameCompleted` 에서 채움 (`OnThrowRecorded(throw=2)` 가 발행되지 않으므로)
  - `total_score_n` 은 `OnFrameCompleted` 시점에만 갱신 (사용자 요구사항 — throw 시점에 갱신하지 않음)
  - `OnGameOver` 시 전체 클리어 (최종 점수는 별도 ResultUI 책임)
- **Rich Text 전제**: TMP_Text `richText` 가 true 여야 `<b>X</b>` 가 동작. 기본값 true.
- **폰트 주의**: 현재 씬 폰트는 `NotoSansKR-Black SDF` — 이미 weight 900 이라서 `<b>` 의 시각적 굵기 변화 거의 없음. 강조가 필요하면 색상/크기 태그로 변경 권장.

### 3-6. FrameManager — 진행 상태 관리자 (`Bowling.Scoring`)
**도메인이지만 MonoBehaviour** — `Bowling.Domain` 어셈블리 내에서 Unity 의존.

- 메서드:
  - `void Initialize(BowlingRuleConfig config)` — 게임 시작 / 재시작
  - `void RecordThrow(int pinsKnockedDown)` — 0~10
  - `void AdvanceToNextFrame()` — 마지막 프레임에서는 호출 금지
  - 조회: `IsFrameComplete()`, `IsGameOver()`, `GetCurrentFrameIndex()` (0-base), `GetCurrentThrowNumber()` (1|2), `GetTotalScore()`, `GetFrame(int index)`, `GetFrameCount()`
- 이벤트 (발행 순서):
  - `OnGameInitialized()` (페이로드 없음)
  - `OnFrameStarted(int frameIndex)` — 0-base
  - `OnThrowRecorded(int frameIndex, int throwNumber, Frame frame)` — throwNumber: 1|2
  - `OnFrameCompleted(int frameIndex, Frame frame)`
  - `OnGameOver()` — `RecordThrow` 내부에서 마지막 프레임 완료 시 발행
- **비정상 호출 정책**: 경고 로그 + 무시(fail-safe). 단 `pinsKnockedDown ∈ [0,10]` 위반만 예외 throw.
- **⚠ 테스트(`FrameManagerTests`)는 일부 케이스에서 `InvalidOperationException` 을 기대** — 현재 구현과 명세가 어긋남 (테스트 리팩토링 대기 중).

### 3-7. 영속화 (TODO 영역)

#### `GameRecord` (`[Serializable]`)
- `string modeName`, `int frameCount`, `int score`, `string playedAt` (ISO 8601)

#### `SaveData` (`[Serializable]`)
- `List<GameRecord> highScores`, `string selectedBallSkin`, `string selectedCharacterSkin`
- 저장 위치(예정): `Application.persistentDataPath/save.json`

---

## 4. 상태 흐름 / 이벤트 시퀀스

### 4-1. 정상 1투구 진행 시퀀스 (게임 시작 후)
```
[GameStateManager.ChangeState(AimingPosition)]
  → BallAimer.HandleStateChanged → isAiming = true, ball kinematic
  → BowlingBall.HandleStateChanged → ResetBall(BallSpawnPoint)
  → CameraFollow → Returning 모드 (이미 Idle이면 무시)

[스페이스바] InputController.OnConfirmPressed
  → BallAimer.OnConfirmInput → ConfirmedPosition 캐싱 → ChangeState(AimingPower)

[ChangeState(AimingPower)]
  → PowerGaugeUI.HandleStateChanged → SetActive(true), pingPong 시작

[스페이스바] InputController.OnConfirmPressed
  → PowerGaugeUI.OnConfirmInput → ConfirmedNormalized 캐싱 → ChangeState(Rolling)

[ChangeState(Rolling)]
  → BowlingBall.HandleStateChanged → ExecuteLaunch (Launch 호출)
  → CameraFollow → Following 모드
  → GameManager.OnEnterRolling → settleDetector.StartDetection()

[정지 감지] settleDetector.OnSettled
  → GameManager.OnPhysicsSettled → ChangeState(Scoring)

[ChangeState(Scoring)]
  → GameManager.OnEnterScoring → transitionController.HandlePostThrow()
    1) pinManager.GetNewlyFallenCount()
    2) frameManager.RecordThrow(count)           ← OnThrowRecorded, (완료 시) OnFrameCompleted, (마지막이면) OnGameOver
    3) 분기: 2구 진행 / 다음 프레임 / 게임 종료
    4) ball.ResetToStartPosition()
       pinManager.RemoveFallenPins() 또는 ResetAllPins()
       (다음 투구 있으면) pinManager.SnapshotBeforeThrow()
       (다음 프레임이면) frameManager.AdvanceToNextFrame() → OnFrameStarted(newIdx)
    5) OnTransitionComplete(result) 발행

  → GameManager.OnTransitionComplete → ChangeState(AimingPosition or GameOver)
```

### 4-2. 게임 시작 시퀀스 (`GameManager.BeginGame`)
```
pinManager.ResetAllPins()
  → ball.ResetToStartPosition()
  → pinManager.SnapshotBeforeThrow()      ← 첫 투구 직전 스냅샷
  → stateManager.ChangeState(AimingPosition)
```

---

## 5. 명명 규칙

- **네임스페이스**: PascalCase, 점 구분 (`Bowling.Scoring`)
- **클래스/메서드/프로퍼티**: PascalCase
- **public 상수**: UPPER_SNAKE_CASE (`SPARE_BONUS`, `STRIKE_BONUS`)
- **private/internal 상수**: PascalCase (`FollowOffsetZ`, `ArriveSqrEpsilon`)
- **private 필드**: camelCase (직렬화 필드는 `[SerializeField] private` + camelCase)
- **private 필드 (밑줄 접두)**: 일부 클래스(`Pin`)에서 `_camelCase` — 일관성 부족하지만 기존 컨벤션 존중
- **이벤트**: `On` + 과거형 동사 (`OnSettled`, `OnFrameCompleted`, `OnThrowRecorded`)
- **로그 prefix**: `[클래스 약식이름]` 예: `[FrameManager]`, `[SettleDetector]`, `[Transition]`, `[Ball]`, `[State]`, `[GameManager]`
- **주석/디버그 메시지**: 한국어
- **public API 문서 주석**: XML doc 한국어 (`<summary>`, `<remarks>`, `<param>`)

---

## 6. 중복/혼동 위험이 큰 이름 — 사용 시 주의

| 이름 | 위치 | 헷갈리기 쉬운 대상 |
|---|---|---|
| `GameManager` | `BowlingGame` | `GameStateManager` 와 별개 — GameManager 는 흐름 조정자, GameStateManager 는 순수 상태 머신 |
| `GameStateManager` | `BowlingGame` | 위와 혼동 금지 |
| `FrameManager` | `Bowling.Scoring` | **도메인 네임스페이스에 있음**. `BowlingGame` 아님 |
| `BowlingRuleConfig` | `BowlingGame` | **게임 네임스페이스에 있음**. `Bowling.Scoring` 아님 |
| `Instance` | 4개 클래스 모두 사용 | `GameManager.Instance`, `GameStateManager.Instance`, `InputController.Instance` — 정확한 타입 명시 |
| `OnStateChanged` | `GameStateManager` 이벤트 | 다른 클래스에서 같은 이름 안 씀 (예약) |
| `OnConfirmPressed` | `InputController` 이벤트 | 단일 입력 이벤트 — 다른 키 추가 시 같은 이름 재사용 금지 |
| `ConfirmedPosition` / `ConfirmedNormalized` | BallAimer / PowerGaugeUI | 각각 위치(Vector3) / 세기(float 0~1) — 혼동 주의 |
| `ResetBall` vs `ResetToStartPosition` | `BowlingBall` | 임의 위치 vs `BallSpawnPoint` 기준 |
| `ResetToInitialState` (Pin) vs `ResetAllPins` (PinManager) | Pin / PinManager | 단일 vs 전체 |
| `RecordThrow` / `AdvanceToNextFrame` | FrameManager | 호출 순서 엄수 — `AdvanceToNextFrame` 은 프레임 완료 후, 마지막 프레임에서는 금지 |
| `SnapshotBeforeThrow` | PinManager | **투구 직전** 한 번만 호출 (게임 시작·1구→2구·프레임 전환) — 위치 어기면 카운트 오류 |
| `frameIndex` (0-base) vs `frameNumber/currentFrameNo` (1-base) | 코드 전반 | 이벤트 페이로드 / 조회 API 는 0-base, **로그 출력은 1-base** |
| `throwNumber` (1 or 2) | FrameManager 이벤트 | 0-base 아님 |
| `Ball1` / `Ball2` (Frame) | `int` | 스트라이크 시 `Ball2 = 0` 으로 유지 — "굴리지 않음" 표현용 |
| `BallSpeed` (BowlingRuleConfig) vs `oscSpeed` (BallAimer) | 다른 의미 | Config 는 데이터, BallAimer 는 실제 사용값. 현재는 BallAimer 에 직접 직렬화돼 있음 — 향후 통합 시 주의 |
| `PowerGaugeSpeed` (Config) vs `gaugeSpeed` (PowerGaugeUI) | 같음 | 위와 동일 통합 미완료 |
| `total_score` (씬 GameObject) vs `total_score_n` (자식 GameObject) | 다른 노드 | 부모는 "TOTAL SCORE" 라벨용 TMP, 자식이 실제 숫자 표시 TMP. `ScoreboardUI.totalScoreText` 는 **자식** 노드를 가리킨다 |
| `current_frame` vs `frame_n` | 위와 동일 패턴 | 부모는 라벨, 자식이 숫자. `ScoreboardUI.currentFrameText` 는 자식 |
| `frame_N_first`, `frame_N_sec` | Canvas 직속 | 위와 달리 별도 라벨 없이 직접 텍스트만 표시 |
| `Canvas` vs `HUD_Canvas` | Game.unity 의 두 Canvas | 점수판은 `Canvas`(layer=UI, 5개 자식) 측. `HUD_Canvas` 는 다른 HUD 용도(자식 1개) |
| `GameManager` GameObject 2개 | 같은 이름 다른 컴포넌트 | (a) GameStateManager+InputController+DebugResetController 호스트, (b) GameManager+PhysicsSettleDetector+ThrowTransitionController+FrameManager 호스트 — **FrameManager 는 (b) 쪽** |

---

## 7. 절대 건드리지 말 것 (불변 영역)

1. **`ScoringConstants.SPARE_BONUS` / `STRIKE_BONUS` 값 자체** — 룰 변경 시 README, 테스트 기대값까지 같이 수정해야 함. 코드 한 줄로 끝나지 않음.
2. **`Frame` 의 setter (`internal set`)** — 외부에서 직접 대입 금지. 점수 갱신은 반드시 `FrameManager` 경유.
3. **`BowlingRuleConfig` 의 `[SerializeField]` 필드명** (`modeName`, `frameCount`, `pinCount`, `ballSpeed`, `powerGaugeSpeed`) — 변경 시 기존 `.asset` 의 직렬화 데이터 연결이 끊김.
4. **`Pin` 의 `_fallen` 필드명** — 리플렉션 접근 가능성(주석 명시). 변경 시 외부 도구가 깨질 위험.
5. **`GameManager` 의 `[SerializeField]` 필드명** (`ruleConfig`, `pinManager`, `settleDetector`, `transitionController`, `ball`, `frameManager`) — 변경 시 씬의 컴포넌트 참조 끊김.
6. **`InputController.OnConfirmPressed` 시그니처** — 모든 입력 처리 컴포넌트가 구독. 페이로드 추가 시 광범위 수정 필요.
7. **`GameStateManager.OnStateChanged` 시그니처** `Action<GameState, GameState>` (prev, next) — 6개 이상의 구독자 존재.
8. **`HandlePostThrow` 내부 호출 순서** (GetNewlyFallenCount → RecordThrow → 분기 → Snapshot) — 어기면 카운트 오류.
9. **`Bowling.Domain` 어셈블리에 UnityEngine 의존성 추가** — `ScoreCalculator` 가 EditMode 단독 테스트 가능해야 한다는 설계 원칙 위반.
10. **`Assets/Settings/` 의 URP 렌더 파이프라인 자산** — 렌더링 깨질 위험.
11. **UI 표시 규칙은 표시 컴포넌트(UI 클래스) 내부 상수에만 둔다** — 도메인(`FrameManager`, `Frame`, `ScoreCalculator`)에 "X", "/", "-" 같은 표시 문자열 절대 도입 금지. `ScoreboardUI.STRIKE_FIRST_DISPLAY` 등의 상수 패턴이 단일 출처.
12. **이벤트 핸들러에 표시 로직 인라인 금지** — `ScoreboardUI.Handle*` 처럼 핸들러는 헬퍼 호출과 어느 UI 갱신할지만 결정. `<b>X</b>` 같은 문자열을 핸들러 본문에 직접 쓰지 말 것.

---

## 8. 적극적으로 참고할 것 (확장/수정 시)

1. **점수 규칙 관련**: `Assets/Scripts/Scoring/` 전체 + `README.md` §3 + `Assets/Tests/EditMode/ScoreCalculatorTests.cs`
2. **새 모드 추가**: `BowlingRuleConfig` 새 에셋 생성 (현재 `ShortModeRule.asset` 만 존재, `FullModeRule.asset` 미생성)
3. **상태 머신 흐름**: `GameManager.cs` 의 클래스 헤더 `<remarks>` "순서 보장 체크리스트" + 본 문서 §4
4. **이벤트 구독 패턴**: `Start()` 에서 구독, `OnDestroy()` 에서 해제. 모든 컴포넌트가 동일 패턴 — 깨지 말 것.
5. **싱글톤 패턴**: `GameManager`, `GameStateManager`, `InputController` 셋 다 동일한 `Awake` 패턴 사용 — 새 싱글톤 추가 시 같은 패턴 답습.
6. **로그 메시지 스타일**: `[Prefix] 한국어 설명 (필요 시 변수 보간)` — 일관성 유지.
7. **테스트 작성**: `Assets/Tests/EditMode/` 의 기존 NUnit 패턴. 점수 도메인은 ScriptableObject 도 리플렉션으로 frameCount 주입하는 헬퍼 사용 (`MakeRule`).

---

## 9. 현재 미구현 / TODO (작업 후보)

| 영역 | 상태 | 비고 |
|---|---|---|
| `FullModeRule.asset` (10프레임) | ❌ 미생성 | Editor 메뉴로 생성 후 인스펙터 값 입력 |
| `Persistence/` 직렬화 로직 (`SaveSystem`) | ❌ 미구현 | 클래스 정의만 존재. `Application.persistentDataPath/save.json` |
| 메인메뉴 → 게임 씬 전이 | ❌ 미구현 | `mainmenu.unity` 존재하지만 연결 없음 |
| 접근성 옵션 (TTS, 색각, 자동 보정, 깜빡임 제거, 키 리바인딩) | ❌ 미구현 | M4 마일스톤 |
| 거터 처리 (`BowlingBall.IsInGutter`) | 일부 | 판정만 존재, 호출처 없음 |
| `FrameManagerTests` 의 `InvalidOperationException` 기대 테스트 | ⚠ 명세 불일치 | 실제 구현은 경고+무시. 테스트 리팩토링 필요 |
| 스킨 시스템 (`selectedBallSkin`, `selectedCharacterSkin`) | ❌ 미구현 | SaveData 필드만 존재 |
| 사운드 (BGM/효과음) | ❌ 미구현 | Phase 7 |
| 점수판 UI (현재 프레임의 1구/2구/총점) | ✅ 구현 완료 | `ScoreboardUI` — Game.unity 의 Canvas 에 배선됨. Play 시 3개 로그 시퀀스 검증 완료. |
| 점수판 UI (10프레임 모두 동적 생성) | ❌ 미구현 | 현재는 "현재 프레임" 한 칸만 표시. 전체 프레임 표시는 별도 컴포넌트 또는 ScoreboardUI 확장 필요 |
| `using System;` (ScoreboardUI) | 🟡 미사용 | 단계 4 끝까지 System 타입 직접 참조 없음. 필요 없으면 정리 후보 |

---

## 10. 코드 작성 프롬프트에 반드시 포함할 것

다른 AI 에게 코드 작성을 시킬 때 프롬프트에 **다음 항목을 명시**해야 한다:

1. **대상 네임스페이스** (`BowlingGame` vs `Bowling.Scoring`) — 어셈블리가 다르다
2. **이 컴포넌트가 의존하는 싱글톤** (`GameManager.Instance` / `GameStateManager.Instance` / `InputController.Instance`) — 어느 것을 쓰는지
3. **구독할 이벤트와 발행할 이벤트**의 정확한 시그니처
4. **Start 에서 구독, OnDestroy 에서 해제** 패턴을 지킬 것
5. **로그 prefix 컨벤션** `[CompName] 한국어 메시지`
6. **인스펙터 직렬화 필드명을 명시** (기존 씬과 호환되어야 한다면 절대 변경 금지)
7. **`Bowling.Domain` 어셈블리에는 UnityEngine 의존 코드를 추가하지 말 것** (`FrameManager`, `BowlingRuleConfig` 외에는)
8. **호출 순서 의존성** (특히 PinManager 의 Snapshot 타이밍, ThrowTransitionController 의 GetNewlyFallenCount → RecordThrow 순서)
9. **점수/보너스 값은 `ScoringConstants` 만 사용** — 매직 넘버 금지

---

## 11. Game.unity 씬 구조 (2026-05-23 기준 실측)

UI 작업 시 어느 노드를 가리켜야 하는지 빠르게 확인하기 위한 단면. 좌표·instanceID 는 변할 수 있으므로 **이름·계층 구조만 신뢰**할 것.

### 루트 GameObject (10개)
- `Main Camera` — Camera + AudioListener + UniversalAdditionalCameraData + **CameraFollow**
- `Directional Light`
- `Ground` — MeshCollider
- `Lane_Root` — 레인/거터/핀 하위 그룹 (자식 6)
- `BowlingBall` — Rigidbody + **BowlingBall** + **BallAimer**
- `GameManager` ⓐ — **GameStateManager** + **InputController** + **DebugResetController**
- `GameManager` ⓑ — **GameManager** + **PhysicsSettleDetector** + **ThrowTransitionController** + **FrameManager**
- `HUD_Canvas` — 별도 Canvas (자식 1개, 점수판과 무관)
- `EventSystem` — InputSystemUIInputModule
- `Canvas` — 점수판 호스트 (layer=UI, 자식 5개) + **ScoreboardUI** (Step 5에서 부착)

> **GameManager 두 개는 의도된 분리**. ScoreboardUI 인스펙터 배선 시 `frameManager` 필드는 ⓑ 쪽 FrameManager 컴포넌트를 가리킨다.

### Canvas 자식 (점수판 영역)
```
Canvas
├── total_score                    (TMP_Text: "TOTAL SCORE" 라벨)
│   └── total_score_n              (TMP_Text: 누적 점수 숫자)   ← ScoreboardUI.totalScoreText
├── current_frame                  (TMP_Text: "FRAME" 라벨)
│   └── frame_n                    (TMP_Text: 프레임 번호)       ← ScoreboardUI.currentFrameText
├── frame_N_first                  (TMP_Text: 1구 결과)          ← ScoreboardUI.frameFirstText
├── frame_/                        (TMP_Text: "/" 시각적 구분자, 정적)
└── frame_N_sec                    (TMP_Text: 2구 결과)          ← ScoreboardUI.frameSecText
```

### 폰트
- 4개 TMP 모두 `NotoSansKR-Black SDF` (Bold 페이스 미내장, weight 900).
- `<b>` 태그가 시각적으로 더 굵게 보이지 않으므로 강조 필요 시 `<color>` / `<size>` 사용 권장.

---

## 12. 검증된 동작 시퀀스 (Game.unity Play 진입 시)

ScoreboardUI 가 GameManager(ⓑ) 의 `[DefaultExecutionOrder(1000)]` 보다 앞서 `Start()` 를 실행하므로, **이벤트 구독 → 발행 → 핸들 순서가 보장**된다. 정상 진입 시 콘솔에 다음 3개 로그가 순서대로 떠야 한다:

```
[Scoreboard] 초기화 완료 — FrameManager 이벤트 구독 시작
[Scoreboard] 게임 초기화 — UI 클리어
[Scoreboard] 프레임 1 시작 — first/sec 클리어
```

이 시퀀스가 어긋난다면 다음을 점검:
- ScoreboardUI 의 `[DefaultExecutionOrder]` 가 (실수로) 1000 이상으로 설정되어 GameManager 보다 늦게 실행
- frameManager 인스펙터 참조가 GameManager(ⓐ) 의 GameObject 로 잘못 연결 (FrameManager 컴포넌트가 없는 쪽)
- 같은 씬에 ScoreboardUI 가 중복 배치

---

*이 문서는 코드 변경과 함께 갱신되어야 함. 시그니처·이벤트·필드명이 본 문서와 코드 사이에서 어긋나는 경우, **코드를 기준으로 본 문서를 수정**한다.*
