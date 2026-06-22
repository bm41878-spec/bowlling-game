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
│   ├── Core/           GameManager, GameStateManager, GameState(enum),
│   │                   GameModeSelector (DontDestroyOnLoad 싱글톤)
│   ├── Gameplay/       BowlingBall, BallAimer, Pin, PinManager,
│   │                   InputController, CameraFollow,
│   │                   PhysicsSettleDetector, ThrowTransitionController
│   ├── Scoring/        Frame, FrameType, FrameManager, ScoreCalculator,
│   │                   ScoringConstants, BowlingRuleConfig
│   ├── UI/             PowerGaugeUI, ScoreboardUI, MainMenuUI,
│   │                   GameOverUI (Gameover_scene 부착),
│   │                   ResultUI (※ obsolete — Gameover_scene 로 대체, 파일 잔존)
│   ├── Persistence/    GameRecord, SaveData (version=1),
│   │                   SaveSystem, HighScoreService   (✅ Phase 8 완료)
│   ├── Core/           GameResultHolder (DontDestroyOnLoad 싱글톤)
│   └── Debug/          DebugResetController            (R키 → RestartGame)
├── Tests/EditMode/     ScoreCalculatorTests, FrameManagerTests
├── Scenes/             mainmenu.unity (idx 0), Game.unity (idx 1), Gameover_scene.unity (idx 2)
├── Configs/            ShortModeRule.asset (5프레임), FullModeRule.asset (10프레임)
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
- **ModeSelector 폴백 (Start 첫 분기)**: `GameModeSelector.Instance?.SelectedRule` 이 null 이 아니면 인스펙터 `ruleConfig` 를 덮어쓴다. 없으면 인스펙터 값 사용 → `Game.unity` 단독 Play 호환성 유지.

#### `GameModeSelector` (MonoBehaviour, **싱글톤**, DontDestroyOnLoad)
- 메인메뉴에서 선택한 모드를 게임 씬으로 전달.
- `static Instance`, `BowlingRuleConfig SelectedRule { get; private set; }`
- `void SelectMode(BowlingRuleConfig rule)` — null 가드 후 캐시, 로그 출력 (`[ModeSelector]`).
- 씬 전이 자체는 본 클래스가 아니라 호출자(`MainMenuUI`) 가 `SceneManager.LoadScene` 으로 수행.
- 씬 배치: `mainmenu.unity` 에 단일 GameObject. Awake 에서 DontDestroyOnLoad. 중복 인스턴스는 Awake 에서 자기 자신 Destroy.
- **메인메뉴 복귀 시 SelectedRule 유지** — 재선택 전까지 직전 모드 보존.
- `Game.unity` 단독 Play 시 Instance 가 null — GameManager 의 인스펙터 폴백이 처리.

### 3-4. 물리/게임플레이

#### `BowlingBall` (MonoBehaviour) — `Rigidbody` 필요
- 인스펙터 필드: `minForce = 8f`, `maxForce = 18f`
- 공개 API:
  - `void Launch(Vector3 startPos, float normalizedForce)` — normalizedForce ∈ [0,1]
  - `void ResetBall(Vector3 position)` — **6단계 안전 리셋** (아래 참조)
  - `void ResetToStartPosition()` — Awake 캐싱된 `BallSpawnPoint` Transform 사용, 미발견 시 `FallbackSpawnPosition = (0, 0.15, 0.5)`
  - `bool IsRolling`, `bool IsInGutter` (※ IsInGutter 는 현재 호출처 없음)
- **씬 의존**: `BallSpawnPoint` 라는 이름의 GameObject 가 씬에 존재해야 정확한 위치로 리셋. Awake 1회 검색·Transform 캐싱 (게임당 수십 회의 `GameObject.Find` 풀-스캔 회피).
- **`HandleStateChanged` 책임 범위**: `next == Rolling` 일 때 `ExecuteLaunch` 만 호출. **AimingPosition 진입 시 위치 리셋은 본 클래스가 하지 않음** — `GameManager.BeginGame` 과 `ThrowTransitionController.HandlePostThrow` 가 단일 리셋 경로.
- **`ResetBall(Vector3)` 6단계 패턴 (순서 엄수 — 어기면 y 드리프트 회귀)**:
  1. velocity / angularVelocity = 0 (동적 일 때만 — kinematic 상태에서 setter 호출 시 Unity warning).
  2. `rb.isKinematic = true` (CCD / Interpolation / 중력 영향 차단).
  3. `transform.position` / `rotation` 갱신 (kinematic 이라 안전).
  4. **`Physics.SyncTransforms()`** — Unity 6 기본값 `autoSyncTransforms = false` 환경에서 Transform → Rigidbody.position 강제 동기화. 누락 시 stale 위치에서 시뮬레이션 시작.
  5. **`rb.Sleep()`** — 누적 force / contact buffer / Interpolation 의 `previousPosition` 등 숨은 내부 상태 정리. 누락 시 매 리셋마다 잔여 상태 누적되어 y 드리프트.
  6. `rb.isKinematic = false` 동적 복귀.
- **씬의 Rigidbody 설정 (이름·값 변경 금지)**: `m_Interpolate: 1` (Interpolation 활성), `m_CollisionDetection: 2` (ContinuousDynamic). 위 6단계 패턴이 이 두 설정의 결합 부작용을 대처하기 위한 것이므로 함께 다뤄야 함.

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

#### `InputController` (MonoBehaviour, **싱글톤**, **DontDestroyOnLoad**)
- `static Instance` — mainmenu.unity 의 단독 GameObject `InputController` 가 진입점 (2026-06-22 변경). settings / Game 으로 살아서 넘어감
- **Awake 의 중복 처리**: `Destroy(this)` (컴포넌트만 파괴) — Game.unity 의 GameManager(ⓐ) 에 부착된 기존 InputController 컴포넌트가 같이 사라지는 것이 아니라 자기 자신만 사라져 GameStateManager / DebugResetController 보호. mainmenu 경유 진입 시 Game 의 InputController 컴포넌트는 self-destroy 되고 mainmenu 의 것이 유지된다
- **Game.unity 단독 Play 호환**: GameManager(ⓐ) 의 InputController 가 첫 인스턴스로 등록 + DontDestroyOnLoad(gameObject) — 디버깅 경로. mainmenu 경유가 정상 흐름
- 이벤트: `event Action OnConfirmPressed` — 키보드/게임패드 어느 쪽이 눌려도 1회 발화
- **InputAction `confirmAction`** — 항상 활성, binding 2개 (인덱스 고정):
  - `[0]` = `<Keyboard>/space` — `KeyboardBindingIndex` 상수
  - `[1]` = `<Gamepad>/buttonSouth` — `GamepadBindingIndex` 상수 (Xbox A / PS Cross / DualSense Cross)
- 공개 API (리바인딩 / 직렬화 — SettingsUI 가 사용):
  - `InputAction ConfirmAction { get; }` — `PerformInteractiveRebinding` 호출 위해 외부에서 참조
  - `string SaveBindingOverridesJson()` — 현재 override 상태를 JSON 으로
  - `void LoadBindingOverridesJson(string)` — null/empty 가드 (구 SaveData 호환)
  - `void ResetAllBindingsToDefault()` / `void ResetBindingToDefault(int)` — RemoveBindingOverride 래핑
  - `string GetBindingDisplayString(int)` — 사람 읽기용 ("Space", "A")
- **Start** 에서 `SaveSystem.Load().inputOverridesJson` 자동 적용 — AudioManager 의 음량 복원과 동일 패턴
- 게임패드 자동 인식: Unity Input System 의 `Gamepad` 표준 추상화 — DualSense/DualShock/Xbox 모두 동일 binding 으로 동작. 별도 활성 토글 불필요.

#### `PowerGaugeUI` (MonoBehaviour)
- 직렬화 필드: `arrowShaft` (RectTransform), `powerValueText` (TMP_Text), `minHeight = 20f`, `maxHeight = 160f`, `gaugeSpeed = 1.5f`
- 공개 프로퍼티: `float ConfirmedNormalized` (0~1)
- 색상 상수 내장: 0~40% `#00C853`, ~70% `#FFD700`, 이상 `#FF1744`

#### `ScoreboardUI` (MonoBehaviour) — 2026-06-23 재작성 (이름 유지, 내부 완전 교체)
- **책임**: FrameManager 이벤트 수집 + 누적 점수 / 현재 프레임 라벨 갱신만 담당. 화면 표시는 `ScoreboardLayoutRenderer` 구현체에 위임.
- 직렬화 필드 (이름 변경 금지 — 씬 배선과 직결):
  - `frameManager` (FrameManager) — `Bowling.Scoring` 어셈블리. Game.unity 의 GameManager(ⓑ) 의 FrameManager 컴포넌트 참조
  - `layout` (`ScoreboardLayoutRenderer`) — **추상 베이스**. 인스펙터에서 `CardLayoutRenderer` (옵션 B, 현재) 또는 `TableLayoutRenderer` (옵션 C, 추후) 로 교체 가능
  - `totalScoreText` (TMP_Text, 옵션) — 우측 큰 총점 ("TOTAL" 라벨 옆 숫자)
  - `currentFrameLabel` (TMP_Text, 옵션) — "프레임 N / M구" 진행 상태
- 핸들러 5개: FrameManager 의 모든 이벤트 구독 (`OnGameInitialized`, `OnFrameStarted`, `OnThrowRecorded`, `OnFrameCompleted`, `OnGameOver`) → 데이터 수집 후 `layout.*` 호출.
- 누적 점수 계산: `FrameManager.GetTotalScore()` 직접 사용. 독립 프레임 점수 방식 특성 — 미완료 프레임의 `FrameScore=0` 이므로 `OnFrameCompleted` 시점의 `GetTotalScore()` 가 정확히 "그 프레임까지 누적".
- 표시 규칙(`X`/`/`/숫자) 은 본 클래스가 모름 — `FrameCardUI.STRIKE_FIRST`/`STRIKE_SEC`/`SPARE_SEC`/`EMPTY` 상수에 캡슐화.
- 로그 prefix `[Scoreboard]`.

#### `ScoreboardLayoutRenderer` (abstract MonoBehaviour) — 2026-06-23 신설
- 추상 메서드 6개:
  - `Initialize(int frameCount)` — frameCount 만큼 카드/행 생성 (재초기화 시 기존 제거 후 새로)
  - `UpdateThrow(int frameIndex, int throwNumber, Frame frame)` — 매 투구 직후
  - `UpdateFrameComplete(int frameIndex, Frame frame, int cumulativeScore)` — 프레임 완료 직후
  - `SetActiveFrame(int frameIndex)` — 현재 진행 강조 위치 이동
  - `SetGameOver(int finalScore)` — 모든 강조 해제 + 종료 시각 마무리
  - `ClearAll()` — 텍스트만 빈 상태로 (생성된 카드 유지)
- 구현체:
  - `CardLayoutRenderer` (옵션 B, 2026-06-23 구현) — 카드 그리드. `cardContainer` (Transform, HorizontalLayoutGroup) + `cardPrefab` (GameObject, FrameCardUI 부착)
  - `TableLayoutRenderer` (옵션 C, **미구현** — 다음 세션) — 3행 테이블

#### `FrameCardUI` (MonoBehaviour) — 카드 1개 prefab 컴포넌트
- 위치: `Assets/Prefabs/FrameCard.prefab`
- 직렬화 필드: `frameNumberLabel`, `throwsLabel`, `scoreLabel` (TMP_Text), `background`, `highlight` (Image), `normalBgColor`/`activeBgColor`/`gameOverBgColor`
- 표시 규칙 상수 (단일 출처): `STRIKE_FIRST = "X"`, `STRIKE_SEC = "-"`, `SPARE_SEC = "/"`, `EMPTY = ""`
- 공개 API: `SetFrameNumber(int)`, `SetThrows(Frame, int throwNumber)`, `SetCumulativeScore(int)`, `SetActive(bool)`, `SetGameOver()`, `Clear()`
- 헬퍼 (순수 함수, public static): `FormatThrows(Frame, int throwNumber)` — "X  -" / "7  /" / "5  4" / 빈 칸 처리

#### `MainMenuUI` (MonoBehaviour) — `mainmenu.unity` 의 Canvas 에 부착
- 직렬화 필드 (5개, 인스펙터 배선 필수):
  - `shortModeRule`, `fullModeRule` (BowlingRuleConfig) — `ShortModeRule.asset`, `FullModeRule.asset` 직접 지정
  - `shortButton`, `fullButton` (Button)
  - `gameSceneName` (string, 기본값 `"Game"`) — Build Settings 등록 필수
- 동작: 버튼 클릭 → `GameModeSelector.Instance.SelectMode(rule)` → `SceneManager.LoadScene(gameSceneName)`
- 로그 prefix: `[MainMenu]`
- Start/OnDestroy/Validate 패턴은 ScoreboardUI 와 동일.

#### `ResultUI` (MonoBehaviour) — **골격만 구현** (단계 2~5 미완)
- 위치: `Assets/Scripts/UI/ResultUI.cs`
- 직렬화 필드 7개: `frameManager`, `panelRoot`, `finalScoreText`/`strikeCountText`/`spareCountText`, `restartButton`/`mainMenuButton`
- 메서드 스텁만 존재 (Start, OnDestroy, HandleGameInitialized, HandleGameOver, OnRestartClicked, OnMainMenuClicked)
- 진행 가이드: `NEXT_SESSION.md` §2~6
- 로그 prefix: `[Result]` (단계 2 부터 추가 예정)

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

### 3-7. 영속화 (Phase 8 — 코드 작성 완료, 컴파일·검증 대기 / 2026-06-19)

> 네임스페이스 `BowlingGame`, 어셈블리 `Assembly-CSharp` (UnityEngine 의존 OK — `Bowling.Domain` 밖). 직렬화는 `JsonUtility`.

#### `GameRecord` (`[Serializable]`)
- `string modeName`, `int frameCount`, `int score`, `string playedAt` (ISO 8601, `DateTime.UtcNow.ToString("o")`)

#### `SaveData` (`[Serializable]`)
- `int version` (= 1), `List<GameRecord> highScores`, `string selectedBallSkin`, `string selectedCharacterSkin`
- `float masterVolume` (=1.0f), `float sfxVolume` (=1.0f), `float bgmVolume` (=0.7f) — 0~1 선형. **구 save.json 호환**: 필드 미존재 시 `SaveSystem.NormalizeVolumes` 가 0f → 기본값으로 보정. 의도적 0(음소거) 표현은 향후 별도 mute 플래그로 분리 예정 (현재는 0=미마이그레이션으로 간주).

#### `SaveSystem` (static)
- `static string FilePath` — `Application.persistentDataPath/save.json`
- `static SaveData Load()` — 파일 없음/파싱 실패 시 빈 `SaveData` 반환 (예외 전파 안 함). 음량 0f 감지 시 `NormalizeVolumes` 로 보정 후 반환
- `static void Save(SaveData)` — `JsonUtility.ToJson(prettyPrint)` → `File.WriteAllText`. 실패 시 `LogWarning` 후 계속
- **fail-safe 원칙**: 모든 I/O 가 예외를 게임 흐름에 전파하지 않음. 로그 prefix `[SaveSystem]`

#### `HighScoreService` (static)
- `const int MaxPerMode = 10`
- `readonly struct RecordResult { bool IsNewRecord; int BestScore; }`
- `static RecordResult Record(string modeName, int frameCount, int score)` — Load → 직전 최고점 캡처 → 추가 → 모드별 Top N 정렬·trim → Save. `IsNewRecord = score > 직전최고`
- `static int GetBestScore(string modeName)` — 없으면 0
- `static List<GameRecord> GetHighScores(string modeName)` — 모드별 정렬된 상위 목록
- **모드별 분리**: `modeName` 기준 그룹화 (쇼트/풀 만점이 달라 통합 시 풀이 항상 이김). 로그 prefix `[HighScore]`
- **연동**: `GameManager.OnEnterGameOver` 가 `Record(...)` 호출 후 결과를 `GameResultHolder.SetResult(score, mode, bestScore, isNewRecord)` 로 인계 → `GameOverUI` 가 표시

### 3-8. 오디오 (2026-06-22 신설)

> 네임스페이스 `BowlingGame`, 어셈블리 `Assembly-CSharp`. 위치: `Assets/Scripts/Audio/`. 로그 prefix `[Audio]`.

#### `AudioManager` (MonoBehaviour, **싱글톤**, **DontDestroyOnLoad**)
- 진입점: `mainmenu.unity` 루트의 `AudioManager` GameObject. `GameModeSelector` 와 동일한 패턴 (Awake 중복 self-destroy + DontDestroyOnLoad).
- `static Instance` 노출.
- 직렬화 필드 (이름 변경 금지 — 씬 배선과 직결):
  - **Mixer**: `audioMixer` (AudioMixer), `sfxGroup` (AudioMixerGroup), `bgmGroup` (AudioMixerGroup)
  - **Sources**: `sfxSource` (일회성 PlayOneShot 용 AudioSource, `outputAudioMixerGroup=SFX`), `rollSource` (지속 굴림 전용 AudioSource, `loop=true`, `outputAudioMixerGroup=SFX`)
  - **Clips**: `pinHitClip`, `strikeClip`, `gutterClip`, `ballRollClip` (모두 `AudioClip`, **null 허용** — 미배선 시 LogWarning 후 무시)
- 공개 재생 API:
  - `void PlayPinHit()` / `PlayStrike()` / `PlayGutter()` — sfxSource.PlayOneShot
  - `void StartBallRoll()` / `StopBallRoll()` — rollSource.Play/Stop (중복 호출 안전)
- 공개 음량 API: `SetMasterVolume(float)` / `SetSFXVolume(float)` / `SetBGMVolume(float)` — 선형 0~1 입력, 내부에서 dB 변환 (`Log10(v)*20`, 0 입력은 -80dB 클램프)
- 이벤트 구독 라이프사이클 — `SceneManager.sceneLoaded` 후크로 씬 전이마다 Unwire/Wire:
  - `BowlingBall.OnFirstPinContact` → `PlayPinHit` + `StopBallRoll` (핀 접촉 순간 굴림 즉시 정지 — Rolling→Scoring 전이보다 빠른 청각 컷오프)
  - `BowlingBall.OnEnteredGutter` → `PlayGutter`
  - `FrameManager.OnFrameCompleted` → `frame.IsStrike()` 시 `PlayStrike` (GameManager `[DefaultExecutionOrder(1000)]` 보다 늦은 시점에 잡기 위해 2프레임 코루틴 폴백)
  - `GameStateManager.OnStateChanged` → `next==Rolling` Start, `prev==Rolling && next!=Rolling` Stop (안전망 — 핀 미접촉 종료에서도 정지 보장)
- 저장된 음량 복원: Start 에서 `SaveSystem.Load()` 호출 후 `ApplyVolumes` 로 mixer 에 반영.

#### `AudioMixer` 자산 — `Assets/Audio/MainMixer.mixer`
- 그룹 트리: `Master` → 자식 `SFX` (모든 효과음), `BGM` (예약 — 추후 배경음악)
- **Exposed Parameters (이름 정확 일치 필수)**: `MasterVolume`, `SFXVolume`, `BGMVolume` (각 그룹의 Volume 파라미터)
- 이 이름들은 `AudioManager.cs` 의 상수 `MasterParam`/`SfxParam`/`BgmParam` 과 동기화되어 있다. 한쪽만 변경하면 `SetFloat` 가 조용히 실패한다.

#### 오디오 클립 (`Assets/Audio/`)
- `ONHIT.wav` — pinHitClip 배선
- `BALL_LAINROLL.wav` — ballRollClip 배선 (loop)
- `379322__13fpanska_marval_lukas__bowling.wav` — 미사용 (추후 결정)
- `strikeClip` / `gutterClip` 은 클립 미배선 상태 — null 이어도 게임 정상 진행 (LogWarning 만).

#### `BowlingBall` 측 오디오 후크 (BowlingBall.cs)
- `event Action OnFirstPinContact` — `OnCollisionEnter` 에서 Pin 컴포넌트 보유 콜라이더 충돌 시 1회 발화. 게이트: `hasPlayedPinHitThisThrow` 플래그
- `event Action OnEnteredGutter` — `Update` 에서 `state==Rolling && IsInGutter` 폴링하여 1회 발화. 게이트: `hasEnteredGutterThisThrow` 플래그
- 두 플래그 모두 `ResetBall(Vector3)` 의 마지막 단계에서 `hasLaunched=false` 와 함께 리셋 → 다음 투구 / 리스폰 시 재발화 가능

#### 확장 가이드
- BGM 추가 시: 별도 AudioSource (loop=true, `outputAudioMixerGroup=BGM`) 를 `bgmSource` SerializeField 로 추가 + `PlayBGM(AudioClip)`/`StopBGM` API. 씬 전이 시 자동 정지 처리는 BGM 정책에 따라 별도 결정.
- 새 SFX 추가 시: `AudioClip newClip` SerializeField → `Play{Name}()` 메서드 → 이벤트 구독자(필요시) 등록. 표시 로직 인라인 금지 패턴(§7-12) 답습 — 핸들러는 `Play*` 호출만.
- 충돌 세기·랜덤 피치는 현 단계 미적용 (제안 보류) — 추가 시 `PlayOneShotSafe` 시그니처에 volume / pitch 인자 추가.

### 3-9. 설정 (2026-06-22 신설)

> 네임스페이스 `BowlingGame`. 위치: `Assets/Scripts/Settings/`, `Assets/Scripts/UI/SettingsUI.cs`. 로그 prefix `[Settings]`.

#### `SettingsApplier` (MonoBehaviour, **싱글톤**, **DontDestroyOnLoad**)
- 진입점: `mainmenu.unity` 의 `SettingsApplier` GameObject. AudioManager / GameModeSelector 와 동일한 패턴.
- `static Instance` 노출.
- 책임: `SaveData` 의 사용자 설정을 카테고리별 시스템에 일괄 적용하는 **단일 라우터**. 카테고리별 구현은 해당 시스템(AudioManager 등) 에 위임.
- `Start` 에서 `RefreshFromSave()` 1회 자동 호출.
- 공개 API: `RefreshFromSave()` — 설정 UI 변경 후 외부에서 호출 가능 (전 시스템 재동기화).
- 현재 적용 카테고리: 오디오만. 디스플레이 / 접근성 / 입력은 SaveData 필드 추가 후 다음 세션에 `ApplyXxx(save)` 한 줄씩 추가.

#### `SettingsUI` (MonoBehaviour) — `settings.unity` 의 `SettingsPanel` 에 부착
- 직렬화 필드 (이름 변경 금지):
  - **탭**: `tabPanels[5]` (GameObject 배열, 인덱스 0=Audio, 1=Display, 2=Controls, 3=Accessibility, 4=UX), `tabButtons[5]` (Button 배열, 같은 인덱스), `defaultTab` (int, 기본 0)
  - **Audio 탭**: `masterSlider`, `sfxSlider`, `bgmSlider` (Slider, 0~1), `muteToggle` (Toggle), `masterValueLabel`, `sfxValueLabel`, `bgmValueLabel` (TMP_Text)
  - **Controls 탭**: `rebindKeyboardButton`, `rebindGamepadButton`, `resetControlsButton` (Button), `keyboardBindingLabel`, `gamepadBindingLabel`, `connectedDeviceLabel` (TMP_Text), `rebindOverlay` (GameObject, 평소 비활성), `rebindOverlayLabel` (TMP_Text "키를 누르세요" / "버튼을 누르세요")
  - **Footer**: `backToMainMenuButton` (Button), `mainMenuSceneName` (string, 기본 `"mainmenu"`)
- 값 변경 흐름 (Audio): `Slider.onValueChanged` → 로컬 `_saveCache` 갱신 → `AudioManager.Instance.SetXxxVolume` 즉시 호출 → `SaveSystem.Save(_saveCache)` → 라벨 갱신
- 값 변경 흐름 (Controls): 재설정 버튼 → `InputController.ConfirmAction.PerformInteractiveRebinding(idx).WithControlsExcluding(...).WithCancelingThrough("<Keyboard>/escape").OnComplete(...)` → 완료 시 `SaveBindingOverridesJson` → `_saveCache.inputOverridesJson` 갱신 → Save → 라벨 갱신. 리바인딩 중 confirmAction 은 Disable.
- 초기화 시 `_initializingUI` 가드로 UI 동기화 중 발화되는 콜백이 불필요 Save 를 호출하지 않게 차단.
- `OnDestroy` 에서 진행 중 `_activeRebind` 가 있으면 Dispose + confirmAction Enable 복구 (씬 전환 누수 방지).
- Display / Accessibility / UX 탭은 placeholder TMP ("준비 중") 만 표시. 다음 세션에 채워짐.

#### `settings.unity` 구조 — `Build Settings index 3`
```
settings.unity
├── Main Camera (Camera + AudioListener)
├── Directional Light
├── EventSystem (EventSystem + InputSystemUIInputModule)
└── Canvas (ScreenSpaceOverlay, 1920×1080 ScaleWithScreenSize, GraphicRaycaster)
    └── SettingsPanel (Image 배경 + SettingsUI)
        ├── Header (TMP "설정")
        ├── TabBar (HorizontalLayoutGroup, 5 Tab_* 버튼)
        │   ├── Tab_Audio / Tab_Display / Tab_Controls / Tab_Accessibility / Tab_UX
        ├── Content
        │   ├── AudioPanel (VerticalLayoutGroup)
        │   │   ├── Row_Master (Label "마스터" + Slider + Value "100%")
        │   │   ├── Row_SFX
        │   │   ├── Row_BGM
        │   │   └── Row_Mute (Label + Toggle_Mute + Spacer)
        │   ├── ControlsPanel (VerticalLayoutGroup)
        │   │   ├── Row_Keyboard (Label "확정 (키보드)" + ValueFrame "Space" + RebindButton "재설정")
        │   │   ├── Row_Gamepad  (Label "확정 (게임패드)" + ValueFrame "A / Cross" + RebindButton "재설정")
        │   │   ├── Row_Device   (Label "연결된 컨트롤러" + DeviceName)
        │   │   └── Row_Reset    (ResetButton "기본값 복원")
        │   ├── DisplayPanel / AccessibilityPanel / UXPanel (placeholder)
        ├── RebindOverlay (평소 비활성 — 리바인딩 중 화면 위에 어둡게 덮음, GraphicRaycaster 로 입력 차단)
        └── BackButton ("메인메뉴" — onClick: SceneManager.LoadScene("mainmenu"))
```

#### `MainMenuUI` 갱신 (2026-06-22)
- `settingsButton` SerializeField (옵셔널, null 허용), `settingsSceneName` (string 기본 `"settings"`) 추가.
- `OnSettingsClicked` → `SceneManager.LoadScene(settingsSceneName)`.
- mainmenu 의 Canvas 자식에 `SettingsButton` GameObject 신설 (ShortButton/FullButton 아래, y=-300, label "설정").

#### 확장 가이드
- 새 탭 채우기: `SettingsUI` 의 Audio 탭과 동일 패턴 — SerializeField 추가 → BindXxxTab() → OnXxxChanged() 핸들러 → SaveSystem.Save. 표시 로직 인라인 금지 패턴(§7-12) 답습.
- 새 카테고리 시스템 적용: `SettingsApplier` 에 `Apply{Category}(save)` 메서드 추가 + `RefreshFromSave` 에 호출 줄 추가.
- 설정 화면이 다중 진입점(mainmenu 외에 Game 중 ESC 등) 을 가지려면 settings 씬 대신 DontDestroyOnLoad 패널로 전환 필요 — 현재 구조는 mainmenu → settings 전용.

### 3-10. 타이틀 / 대기 화면 (2026-06-23 신설, Cinemachine 기반)

> 네임스페이스 `BowlingGame`. 위치: `Assets/Scripts/UI/TitleScreenController.cs`. 로그 prefix `[Title]`.
> 의존 패키지: `com.unity.cinemachine` 3.1.7 (Unity 6 호환).

#### `TitleScreenController` (MonoBehaviour) — `title.unity` 의 TitleCanvas 에 부착
- 진입: 앱 실행 시 첫 씬 (Build Settings idx 0). 입력 감지 시 `SceneManager.LoadScene(mainMenuSceneName)` 로 mainmenu 전환.
- **`CinematicShot` (Serializable struct)** — Cinemachine 기반:
  - `string label`
  - `CinemachineCamera virtualCamera` — 인스펙터에서 vcam GameObject 의 시작 위치/회전, LookAt slot 설정
  - `Vector3 endPosition` — 시작 위치(vcam.transform.position)에서 endPosition 으로 SmoothStep 보간
  - `float duration` / `fadeInTime` / `fadeOutTime` / `holdAtBlack`
- 직렬화 필드 (이름 변경 금지):
  - `shots` (`CinematicShot[]`) — 인스펙터 조정. 초기 3샷: Pin Closeup → PinVCam / Ball Closeup → BallVCam / Lane Overview → LaneVCam
  - `fadeImage` (Image) — TitleCanvas 의 FadePanel
  - `anyKeyHint` (TMP_Text, 옵션)
  - `versionLabel` (TMP_Text, 옵션) — Start 에서 `"v" + Application.version` 자동
  - `anyKeyBlinkInterval` (0.8) / `transitionFadeOutTime` (0.4) / `mainMenuSceneName` ("mainmenu")
- **샷 라이프사이클**:
  1. PlayShot 진입 시 활성 vcam 의 Priority 를 10, 나머지 1 로 설정 — 페이드 hold 동안 컷 전환되어 사용자 비가시
  2. `cachedStartPositions[idx]` 의 시작 좌표 복원 (인스펙터 값)
  3. 페이드 인 → vcam.transform.position 을 `Mathf.SmoothStep` 으로 보간 (ease-in-out) → 페이드 아웃 → 시작 위치 복원 → hold
  4. 회전은 vcam 의 LookAt 슬롯이 자동 처리 (카메라 이동 중에도 target 응시)
- 입력 감지: `Update` 폴링 — Keyboard anyKey + Mouse 3버튼 + Gamepad 8버튼

#### `title.unity` 구조 — Build Settings index 0 (Cinemachine 기반)
```
title.unity
├── Main Camera         (Camera + AudioListener + CinemachineBrain — Default Blend EaseInOut)
├── Directional Light
├── Ground
├── Lane_Root           (Game.unity 와 동일 비주얼)
├── BowlingBall         (Rigidbody.isKinematic=true, 컴포넌트 비활성)
├── EventSystem
├── CinematicTargets    (LookAt 더미 컨테이너)
│   ├── PinTarget       (0, 0.5, 9.5)  — 핀 모인 위치
│   ├── BallTarget      (볼 위치)
│   └── LaneTarget      (2, 0.3, 8)    — vcam 좌측 + 정면 lane 방향 응시
├── PinVCam             (CinemachineCamera + CinemachineRotationComposer, Position=(0.5, 1.8, 5), LookAt=PinTarget, Priority=10 활성)
├── BallVCam            (CinemachineCamera + CinemachineRotationComposer, Position=(-0.4, 0.4, -0.3), LookAt=BallTarget, Priority=1)
├── LaneVCam            (CinemachineCamera + CinemachineRotationComposer, Position=(-3, 2.2, 0), LookAt=LaneTarget, Priority=1)
└── TitleCanvas (ScreenSpaceOverlay 1920×1080, TitleScreenController 부착)
    ├── AnyKeyHint     (TMP "아무 키나 누르세요", 화면 가운데 하단)
    ├── VersionLabel   (TMP "v" + Application.version, 우측 하단)
    ├── FadePanel      (Image 전체 stretch, 검은 알파 1 시작)
    └── GameTitle      (TMP "Bowling Champion", 화면 상단 가운데, 120pt — 자식 인덱스 마지막 = FadePanel 위, 항상 표시)
```
- vcam 의 인스펙터 Position = 샷 시작 위치. 보간 종료 위치는 TitleScreenController.shots[i].endPosition 슬롯.
- LookAt 더미 GameObject 를 이동/회전하면 모든 vcam 의 응시 대상이 자동 변경 (예: 핀 배치 변경 시 PinTarget 만 옮기면 됨).
- Pin (10개) Rigidbody.isKinematic + Pin 컴포넌트 비활성 — 정적 디스플레이.

#### Cinemachine 가 가져온 변화 (수동 Lerp 대비)
- **회전 자동화** — LookAt 슬롯만 설정하면 카메라 이동에 따라 자연스럽게 응시. 이전 `startEuler`/`endEuler` 슬롯 제거.
- **시각 디버깅** — Scene View 에 vcam Gizmo + Frustum + 추적 라인 표시. 좌표 조정이 쉬워짐.
- **확장 여지** — 추후 Game 씬 카메라까지 Cinemachine 으로 통일 시 `CameraFollow.cs` 대체 가능 (현재 보류).

---

## 4. 상태 흐름 / 이벤트 시퀀스

### 4-1. 정상 1투구 진행 시퀀스 (게임 시작 후)
```
[GameStateManager.ChangeState(AimingPosition)]
  → BallAimer.HandleStateChanged → isAiming = true, ball kinematic
  → CameraFollow → Returning 모드 (이미 Idle이면 무시)
  (※ 공 위치 리셋은 본 상태 전이가 아니라 GameManager.BeginGame /
     ThrowTransitionController.HandlePostThrow 에서 명시적으로 호출됨 — 단일 리셋 경로)

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
| `Instance` | 7개 클래스 모두 사용 | `GameManager.Instance`, `GameStateManager.Instance`, `InputController.Instance`, `GameModeSelector.Instance`, `AudioManager.Instance`, `SettingsApplier.Instance` — 정확한 타입 명시. `GameModeSelector` / `AudioManager` / `SettingsApplier` 는 DontDestroyOnLoad 라 씬 전이 후에도 살아있음 |
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
| ~~`total_score`/`total_score_n`/`current_frame`/`frame_n`/`frame_N_first`/`frame_/`/`frame_N_sec`~~ | 2026-06-23 **제거됨** | 점수판 재작성 — 카드 그리드 (CardLayoutRenderer) 기반으로 전환. `ScoreboardTop/CardContainer/TotalScorePanel/CurrentFrameLabel` 신규 노드 참조 |
| `Canvas` vs `HUD_Canvas` | Game.unity 의 두 Canvas | 점수판은 `Canvas`(layer=UI, 5개 자식) 측. `HUD_Canvas` 는 다른 HUD 용도(자식 1개) |
| `Canvas` (Game.unity) vs `Canvas` (mainmenu.unity) | 다른 씬, 같은 이름 | Game.unity 의 Canvas 는 ScoreboardUI, mainmenu.unity 의 Canvas 는 MainMenuUI. 씬을 먼저 명시 |
| `ruleConfig` (인스펙터) vs `SelectedRule` (ModeSelector) | GameManager 의 두 룰 소스 | Start() 첫 분기에서 SelectedRule 이 있으면 ruleConfig 덮어쓰기. 인스펙터는 폴백 (Game.unity 단독 Play 호환). 둘 다 유지 |
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
13. **`GameManager.Start()` 의 ModeSelector 폴백 분기 제거 금지** — `if (selector != null && selector.SelectedRule != null) ruleConfig = ...` 패턴은 `Game.unity` 단독 Play 호환과 mainmenu 경유 진입을 동시에 지원하는 단일 지점. 어느 한쪽으로만 강제하면 다른 경로가 깨진다.
14. **`mainmenu.unity` 의 `GameModeSelector` GameObject** — DontDestroyOnLoad 의 진입점. 제거 시 모드 전달 끊김. 중복 배치도 금지 (Awake 에서 자기 자신 Destroy 처리됨).
15. **`BowlingBall.ResetBall` 의 6단계 순서** — `Physics.SyncTransforms()` / `rb.Sleep()` 누락 시 스트라이크/스페어 후 y 드리프트 회귀. Unity 6 의 `autoSyncTransforms=false` + Interpolation + ContinuousDynamic CCD 결합에서 발생하는 stale Rigidbody.position / 누적 internal state 문제를 대처. 자세한 진단은 `SESSION_2026-06-05.md` §2 참조.
16. **공 위치 리셋 경로 단일화** — 현재 `GameManager.BeginGame` 과 `ThrowTransitionController.HandlePostThrow` 두 곳만 `ball.ResetToStartPosition()` 호출. `BowlingBall.HandleStateChanged` 의 AimingPosition 분기에 리셋 로직 다시 넣지 말 것 (이중 호출 → §4-1 시퀀스 깨짐).
17. **`AudioManager` 의 `[SerializeField]` 필드명** (`audioMixer`, `sfxGroup`, `bgmGroup`, `sfxSource`, `rollSource`, `pinHitClip`, `strikeClip`, `gutterClip`, `ballRollClip`) — 변경 시 `mainmenu.unity` 의 와이어링이 끊김.
18. **AudioMixer `MainMixer.mixer` 의 Exposed Parameter 이름** (`MasterVolume`, `SFXVolume`, `BGMVolume`) — `AudioManager.cs` 의 상수와 동기화되어 있어 한쪽만 변경 시 `SetFloat` 가 조용히 실패하여 음량 조절 불가.
19. **`BowlingBall.OnFirstPinContact` / `OnEnteredGutter` 이벤트 1회 발화 게이트** — `hasPlayedPinHitThisThrow` / `hasEnteredGutterThisThrow` 플래그는 `ResetBall(Vector3)` 의 마지막 단계에서만 리셋된다. 다른 곳에서 임의로 리셋 시 한 투구당 여러 번 발화 발생.
20. **`SettingsUI` 의 `[SerializeField]` 필드명** (`tabPanels`, `tabButtons`, `defaultTab`, `masterSlider`, `sfxSlider`, `bgmSlider`, `muteToggle`, `masterValueLabel`, `sfxValueLabel`, `bgmValueLabel`, `backToMainMenuButton`, `mainMenuSceneName`) — 변경 시 `settings.unity` 의 와이어링 끊김.
21. **`SaveData.isMuted` 의 의미** — "사용자가 의도적으로 음소거함" 의 영구 상태. 음량 0 (`NormalizeVolumes` 가 미마이그레이션으로 간주) 과 분리되어야 한다. 음소거 UI 는 반드시 `AudioManager.SetMuted(bool)` 만 호출, 음량 슬라이더는 0 으로 내리지 말 것.
22. **`InputController.KeyboardBindingIndex` / `GamepadBindingIndex` 상수 값** (각각 0, 1) — `SaveData.inputOverridesJson` 의 직렬화 결과가 binding 순서에 의존. binding 추가 순서를 바꾸면 구 save.json 의 override 가 잘못된 binding 에 적용된다. 새 binding 추가는 반드시 **끝에 append**.
23. **`ScoreboardUI` 의 `layout` 필드를 `ScoreboardLayoutRenderer` 추상 타입으로 유지** — CardLayoutRenderer 직접 타입 캐스팅 금지. 추후 TableLayoutRenderer 추가 시 인스펙터 교체만으로 전환 가능해야 함.
24. **`FrameCardUI.STRIKE_FIRST/SEC` / `SPARE_SEC` / `EMPTY` 상수 값** — 도메인(Frame/FrameManager/ScoreCalculator) 에 표시 문자열 도입 금지 패턴의 단일 출처. 변경 시 카드 표시 일관성 깨짐.
25. **각 Cinemachine vcam 의 `CinemachineRotationComposer` (또는 다른 Aim) 컴포넌트** — 제거 시 LookAt 슬롯이 무시되고 vcam Transform 회전이 그대로 사용되어 카메라가 엉뚱한 방향을 응시한다. Cinemachine 3.x 의 기본 동작 — Body / Aim 컴포넌트가 없으면 LookAt / Follow 자동 처리 안 됨.

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
| `FullModeRule.asset` (10프레임) | ✅ 생성 | `Assets/Configs/FullModeRule.asset`, 퍼펙트 150점 |
| 메인메뉴 → 게임 씬 전이 + 모드 선택 | ✅ 완료 | `mainmenu.unity` 구성 + `MainMenuUI` + `GameModeSelector` + Build Settings 등록 + 양 모드 Play 검증 |
| `ResultUI` (결과 화면 오버레이) | ⚪ obsolete | Game.unity 오버레이 패널 구조에서 별도 씬(`Gameover_scene` + `GameOverUI`) 로 대체됨 (2026-06-19). 파일 잔존 — Validate 실패로 자동 비활성. 정리 원하면 삭제 가능 |
| `Persistence/` 직렬화 로직 (`SaveSystem` / `HighScoreService`) | ✅ 완료 (2026-06-19) | 컴파일·Gameover_scene 노드 배선·프로그램 검증 7+3 시나리오 PASS. `GameManager.OnEnterGameOver` 후크 정상 동작. 실제 게임 플레이 end-to-end 수동 검증만 잔여 |
| 접근성 옵션 (TTS, 색각, 자동 보정, 깜빡임 제거, 키 리바인딩) | ❌ 미구현 | M4 마일스톤 |
| 거터 처리 (`BowlingBall.IsInGutter`) | 일부 | 판정만 존재, 호출처 없음 |
| `FrameManagerTests` 의 `InvalidOperationException` 기대 테스트 | ⚠ 명세 불일치 | 실제 구현은 경고+무시. 테스트 리팩토링 필요 |
| 스킨 시스템 (`selectedBallSkin`, `selectedCharacterSkin`) | ❌ 미구현 | SaveData 필드만 존재 |
| 사운드 (BGM/효과음) | ❌ 미구현 | Phase 7 |
| 점수판 UI (현재 프레임의 1구/2구/총점) | ✅ 구현 완료 | `ScoreboardUI` — Game.unity 의 Canvas 에 배선됨. Play 시 3개 로그 시퀀스 검증 완료. |
| 점수판 UI (10프레임 모두 동적 생성) | ❌ 미구현 | 현재는 "현재 프레임" 한 칸만 표시. 전체 프레임 표시는 별도 컴포넌트 또는 ScoreboardUI 확장 필요 |
| 튜토리얼 화면 | ❌ 미구현 | 형식(정적 이미지 / 인터랙티브) 미결정 |
| `using System;` (ScoreboardUI / ResultUI) | 🟡 미사용 | 단계 4 끝까지 System 타입 직접 참조 없음. 필요 없으면 정리 후보 |

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

## 11. 씬 구조 (2026-06-07 기준 실측)

UI 작업 시 어느 노드를 가리켜야 하는지 빠르게 확인하기 위한 단면. 좌표·instanceID 는 변할 수 있으므로 **이름·계층 구조만 신뢰**할 것.

### 11-1. Game.unity 루트 GameObject (10개)
- `Main Camera` — Camera + AudioListener + UniversalAdditionalCameraData + **CameraFollow**
- `Directional Light`
- `Ground` — MeshCollider
- `Lane_Root` — 레인/거터/핀 하위 그룹 (자식 6)
- `BowlingBall` — Rigidbody + **BowlingBall** + **BallAimer**
- `GameManager` ⓐ — **GameStateManager** + **InputController** (mainmenu 경유 진입 시 컴포넌트만 self-destroy — mainmenu 의 InputController 가 활성) + **DebugResetController**
- `GameManager` ⓑ — **GameManager** + **PhysicsSettleDetector** + **ThrowTransitionController** + **FrameManager**
- `HUD_Canvas` — 별도 Canvas (자식 1개, 점수판과 무관)
- `EventSystem` — InputSystemUIInputModule
- `Canvas` — 점수판 호스트 (layer=UI, 자식 5개) + **ScoreboardUI** (Step 5에서 부착)

> **GameManager 두 개는 의도된 분리**. ScoreboardUI 인스펙터 배선 시 `frameManager` 필드는 ⓑ 쪽 FrameManager 컴포넌트를 가리킨다.

### 11-2. Game.unity Canvas 자식 (점수판 영역, 2026-06-23 재작성)
```
Canvas (ScoreboardUI + CardLayoutRenderer 부착)
├── ScoreboardTop                  (RectTransform, 상단 stretch, height=200)
│   ├── CardContainer              (Transform, HorizontalLayoutGroup) ← CardLayoutRenderer.cardContainer
│   │   └── (FrameCard prefab 인스턴스들, Initialize 시 frameCount 만큼 동적 생성)
│   └── TotalScorePanel            (Image 배경 + 자식 2개)
│       ├── TotalLabel             (TMP_Text "TOTAL")
│       └── TotalValue             (TMP_Text 큰 숫자)            ← ScoreboardUI.totalScoreText
└── CurrentFrameLabel              (TMP_Text "프레임 N / M구")    ← ScoreboardUI.currentFrameLabel
```

기존 `total_score` / `total_score_n` / `current_frame` / `frame_n` / `frame_N_first` / `frame_/` / `frame_N_sec` 노드는 2026-06-23 에 모두 제거됨.

FrameCard prefab 자식 구조 (`Assets/Prefabs/FrameCard.prefab`):
```
FrameCard (RectTransform 140×180, FrameCardUI + LayoutElement preferredWidth=140)
├── Background      (Image)                              ← FrameCardUI.background
├── Highlight       (Image, 비활성 시작, 노란 alpha=0.25) ← FrameCardUI.highlight (옵션)
└── Content         (VerticalLayoutGroup)
    ├── FrameNumber (TMP_Text 22pt, 회색)                ← FrameCardUI.frameNumberLabel
    ├── Throws      (TMP_Text 42pt)                      ← FrameCardUI.throwsLabel
    └── Score       (TMP_Text 36pt, 연한 청색)            ← FrameCardUI.scoreLabel
```

### 11-3. mainmenu.unity 루트 GameObject (9개, 2026-06-22 갱신)
- `Main Camera` — Camera + AudioListener + UniversalAdditionalCameraData
- `Directional Light`
- `Global Volume` — URP Volume (기본)
- `GameModeSelector` — **GameModeSelector** (DontDestroyOnLoad 진입점, Game.unity 로 살아서 넘어감)
- `AudioManager` — **AudioManager** + AudioSource×2 (sfxSource / rollSource, 둘 다 outputAudioMixerGroup=SFX). DontDestroyOnLoad 진입점
- `SettingsApplier` — **SettingsApplier** (DontDestroyOnLoad 진입점, SaveData 라우터)
- `InputController` — **InputController** (DontDestroyOnLoad 진입점, settings/Game 으로 살아서 넘어감 — 리바인딩 / 게임패드 binding 보유)
- `EventSystem` — EventSystem + InputSystemUIInputModule
- `Canvas` — Canvas (ScreenSpaceOverlay) + CanvasScaler (ScaleWithScreenSize, 1920×1080) + GraphicRaycaster + **MainMenuUI**. 자식: Title / ShortButton / FullButton / **SettingsButton** (y=-300)

### 11-4. mainmenu.unity Canvas 자식 (3개)
```
Canvas
├── Title          (TMP_Text "Bowling Champion", 상단 중앙)
├── ShortButton    (Image + Button + Label TMP "쇼트 모드 (5프레임)")   ← MainMenuUI.shortButton
└── FullButton     (Image + Button + Label TMP "풀 모드 (10프레임)")    ← MainMenuUI.fullButton
```

### 11-5. CanvasScaler 설정 (2026-06-05 갱신)
씬의 모든 Canvas (Game.unity 의 점수판 Canvas / HUD_Canvas, mainmenu.unity 의 Canvas) 가 동일 설정으로 통일 — 해상도 독립성 보장.
| 필드 | 값 |
|---|---|
| `m_UiScaleMode` | 1 (ScaleWithScreenSize) |
| `m_ReferenceResolution` | 1920×1080 |
| `m_ScreenMatchMode` | 0 (MatchWidthOrHeight) |
| `m_MatchWidthOrHeight` | 0.5 (Width/Height 균형) |

> 새 Canvas 추가 시 위 설정을 답습할 것. 특히 `ConstantPixelSize` (m_UiScaleMode=0) 는 해상도마다 상대 크기·위치가 변동하므로 금지.

### 11-6. 폰트
- Game.unity 의 점수판 4개 TMP: `NotoSansKR-Black SDF` (Bold 페이스 미내장, weight 900). `<b>` 시각 효과 없음 — 강조 필요 시 `<color>` / `<size>` 사용 권장.
- mainmenu.unity TMP: 기본 폰트 (자동 지정).

---

## 12. 검증된 동작 시퀀스 (Game.unity Play 진입 시)

### 12-1. Game.unity 단독 Play (인스펙터 ruleConfig 폴백 경로)

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

### 12-2. mainmenu.unity 경유 진입 (양 모드 검증 완료)

Play (mainmenu, build index 0) → 버튼 클릭 시 다음 로그 순서가 보장된다 (양 모드 공통, 모드명만 다름):

```
[MainMenu] 초기화 완료 — 버튼 콜백 등록
[ModeSelector] 모드 선택: <모드명> (<프레임수>프레임)
[MainMenu] 씬 전이 → Game (모드: <모드명>)
[Scoreboard] 초기화 완료 — FrameManager 이벤트 구독 시작
[GameManager] ModeSelector 로부터 룰 주입: <모드명>          ← 핵심: 폴백 경로가 ModeSelector 로 덮어쓰는 시점
[FrameManager] 초기화 완료 (모드: <모드명>, 총 <프레임수>프레임)
[Scoreboard] 게임 초기화 — UI 클리어
[Scoreboard] 프레임 1 시작 — first/sec 클리어
[GameManager] BeginGame — 모드: <모드명>, 총 <프레임수> 프레임
[PinManager] 전체 핀 리셋 완료
[PinManager] 투구 전 서있는 핀 10개: [1,2,3,4,5,6,7,8,9,10]
[State Exit] Ready
[GameManager] 상태 전이: Ready → AimingPosition
[State] AimingPosition 시작
```

- `[GameManager] ModeSelector 로부터 룰 주입: ...` 로그가 빠지면 GameModeSelector 가 mainmenu 에 없거나 SelectMode 호출 전에 씬 전이가 일어난 것.
- 모드명이 인스펙터에 설정한 `ruleConfig` (보통 ShortModeRule) 로 표시된다면 GameModeSelector 로 부터의 주입이 실패한 것 — 폴백 분기 점검.

---

*이 문서는 코드 변경과 함께 갱신되어야 함. 시그니처·이벤트·필드명이 본 문서와 코드 사이에서 어긋나는 경우, **코드를 기준으로 본 문서를 수정**한다.*

*최종 갱신: 2026-06-23 (타이틀 화면 vcam 좌표 미세 조정 — PinVCam Position 최종 `(0.5, 1.8, 5)`, LaneVCam `(-3, 2.2, 0)`, LaneTarget `(2, 0.3, 8)`. 각 vcam 에 `CinemachineRotationComposer` Aim 컴포넌트 추가 — Cinemachine 3.x 에서 LookAt 동작에 필수. §7-25 절대 건드리지 말 것 항목 추가)*
