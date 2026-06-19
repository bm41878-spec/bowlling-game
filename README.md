# 볼링 게임 프로젝트

> 초등학생 저학년 대상 로우폴리 카툰 스타일 캐주얼 볼링 게임.
> 커스텀 독립 프레임 점수 계산 방식을 채택하며, **쇼트 모드 (5-Frame)** 와 **풀 모드 (10-Frame)** 두 가지 모드를 지원한다.

---

## 1. 프로젝트 개요

| 항목 | 내용 |
|---|---|
| 장르 | 캐주얼 스포츠 (볼링) |
| 엔진 | Unity 6 |
| 렌더 파이프라인 | URP (Universal Render Pipeline) |
| 타겟 플랫폼 | PC (Windows Standalone) |
| 카메라 | 3인칭 팔로우 카메라 (공 뒤쪽 시점에서 추적) |
| 시각 스타일 | 로우폴리 카툰 |
| 사운드 방향 | 카툰 코믹, 과장된 효과음 중심 |
| 플레이 인원 | 1인 |
| 개발 기간 | 미정 |
| 저장 방식 | 로컬 JSON 파일 |

---

## 2. 타겟 사용자 및 디자인 원칙

- 주 사용자: **초등학생 저학년**
- 핵심 목표: 높은 점수를 통한 **자기만족**
- 디자인 원칙:
  - 텍스트 최소화, 큰 아이콘과 색 대비 사용
  - 실패 페널티 약화 — 거터 시에도 부정적 표현 자제
  - 입력 방식을 **스페이스바 두 번**으로 통일하여 학습 부담 최소화
  - 카툰 코믹 효과로 긍정적 피드백 강화

---

## 3. 게임 규칙

### 3-1. 점수 계산 규칙 (커스텀 독립 프레임 방식)

본 시스템은 전통적인 볼링 점수 계산 방식과 달리, 다음 프레임이 현재 프레임 점수에 영향을 주지 않는 **독립 프레임 구조**를 채택한다.

- 각 프레임은 완전히 독립적으로 점수가 계산된다.
- 스페어/스트라이크 보너스는 고정값이며 다음 프레임과 무관하다.
- 게임 총점은 모든 프레임 점수의 단순 합산이다.

| 판정 | 조건 | 1구 | 2구 | 프레임 점수 |
|---|---|---|---|---|
| 일반 | ball1 + ball2 < 10 | ball1 | ball2 | ball1 + ball2 |
| 스페어 (/) | ball1 + ball2 = 10 (1구 ≠ 10) | ball1 | ball2 | **10 + 3 = 13점** (SPARE_BONUS = 3) |
| 스트라이크 (X) | 1구에 핀 10개 전부 | 10 | 투구 없음 | **10 + 5 = 15점** (STRIKE_BONUS = 5) |

> 판정 우선순위: 스트라이크(1순위) → 스페어(2순위) → 일반(3순위)
> 스트라이크는 1구만으로 판정, 스페어는 반드시 2구 완료 후 판정

### 3-2. 게임 모드

| 모드 | 게임 내 이름 | 프레임 수 | 퍼펙트 스코어 | 예상 플레이 시간 |
|---|---|---|---|---|
| 5-Frame | **쇼트 모드** | 5 | **75점** (15 × 5) | 약 4~5분 |
| 10-Frame | **풀 모드** | 10 | **150점** (15 × 10) | 약 8~10분 |

> 퍼펙트 스코어 공식: `15 × N` (N = 프레임 수, 전 프레임 스트라이크 기준)

---

## 4. 플레이 방법

게임 진행은 다음 상태 머신으로 구성된다.

```
[Ready] → [AimingPosition] → [AimingPower] → [Rolling] → [Scoring]
   (다음 투구) → [AimingPosition] ...  또는  (게임 종료) → [GameOver]
```

### 4-1. 위치 결정 단계 (AimingPosition)
- 볼링공이 레인 좌우 끝 사이를 일정 속도로 왕복
- 스페이스바 입력 시 공의 시작 위치 확정

### 4-2. 세기 결정 단계 (AimingPower)
- 화살표 UI 길이가 작아졌다 커졌다 반복
- 스페이스바 입력 시 투구 세기 확정

### 4-3. 투구 단계 (Rolling)
- 확정된 위치와 세기로 공이 굴러감
- 핀과 충돌 후 모든 Rigidbody가 정지하면 다음 단계로 전이

### 4-4. 점수 산정 단계 (Scoring)
- 쓰러진 핀 개수 판정 후 점수판 갱신
- 보너스 점수는 고정 상수로 즉시 확정 (다음 투구 대기 없음)

### 4-5. 반복 및 종료
- 모든 프레임 완료 시 최종 점수 표시 및 게임 종료
- 쇼트 모드: 5번째 프레임 완료 후 종료 / 풀 모드: 10번째 프레임 완료 후 종료

---

## 5. 기술 스택 및 의존성

| 구분 | 내용 |
|---|---|
| 엔진 | Unity 6 (URP) |
| 입력 시스템 | Unity Input System (신규 패키지) |
| 물리 | Built-in 3D Physics (Rigidbody, PhysicsMaterial) |
| UI | Unity UI (uGUI) + TextMeshPro |
| 저장 | JsonUtility 또는 Newtonsoft.Json |
| 버전 관리 | Git |

---

## 6. 프로젝트 구조 (예정)

```
Assets/
├── Scripts/
│   ├── Core/           # GameManager, 상태 머신
│   ├── Gameplay/       # Ball, Pin, Lane, InputController
│   ├── Scoring/        # ScoreCalculator, Frame, FrameResult
│   ├── Config/         # ScriptableObject (BowlingRuleConfig 등)
│   ├── UI/             # Scoreboard, MainMenu, Tutorial
│   ├── Audio/          # AudioManager
│   ├── Persistence/    # SaveSystem (JSON I/O)
│   └── Tests/          # 점수 계산기 유닛 테스트
├── Prefabs/            # Pin, Ball, Lane, UI 등
├── Materials/          # 로우폴리 카툰 머티리얼
├── Models/             # 3D 모델
├── Audio/              # 효과음, BGM
├── Scenes/
│   ├── Main.unity      # 메인 메뉴
│   ├── Game.unity      # 게임 플레이
│   └── Tutorial.unity  # 튜토리얼
└── Configs/            # ScriptableObject 에셋 (모드별 룰)
```

---

## 7. 핵심 시스템 설계

### 7-1. 점수 보너스 상수 정의

보너스 값은 상수로 관리하여 밸런스 조정 시 단일 지점에서만 수정하도록 설계한다.

```csharp
public const int SPARE_BONUS  = 3;   // 스페어: 10 + 3 = 13점
public const int STRIKE_BONUS = 5;   // 스트라이크: 10 + 5 = 15점
```

### 7-2. 룰 설정 (ScriptableObject)

모드별 데이터를 코드 수정 없이 에디터에서 관리 가능하도록 분리한다.

```csharp
[CreateAssetMenu(fileName = "BowlingRule", menuName = "Bowling/Rule Config")]
public class BowlingRuleConfig : ScriptableObject
{
    public string modeName;        // "쇼트 모드", "풀 모드"
    public int    frameCount;      // 5, 10
    public int    pinCount = 10;
    public float  ballSpeed;       // 좌우 이동 속도
    public float  powerGaugeSpeed; // 게이지 변동 속도
}
```

### 7-3. 점수 계산기 (독립 프레임 방식)

```csharp
public enum FrameType { Normal, Spare, Strike }

public class Frame
{
    public int       ball1;       // 1구 쓰러진 핀 수 (0~10)
    public int       ball2;       // 2구 쓰러진 핀 수 (스트라이크 시 미사용)
    public FrameType frameType;   // NORMAL / SPARE / STRIKE
    public int       frameScore;  // 투구 직후 즉시 확정 — 보너스 대기 없음

    public bool IsStrike() => ball1 == 10;
    public bool IsSpare()  => ball1 != 10 && ball1 + ball2 == 10;
}

public class ScoreCalculator
{
    public int CalculateFrameScore(int ball1, int ball2)
    {
        if (ball1 == 10)          return 10 + STRIKE_BONUS; // 15점
        if (ball1 + ball2 == 10)  return 10 + SPARE_BONUS;  // 13점
        return ball1 + ball2;                               // 일반
    }

    public int CalculateTotalScore(List<Frame> frames) =>
        frames.Sum(f => f.frameScore);
}
```

### 7-4. 게임 플로우 관리

`GameManager`는 `BowlingRuleConfig`를 주입받아 동작하며, 프레임 수에 무관하게 동일한 코드로 진행한다.

```csharp
void OnThrowComplete()
{
    if (IsFrameComplete())
    {
        if (currentFrame >= ruleConfig.frameCount - 1)
            TransitionTo(State.GameOver);
        else
        {
            currentFrame++;
            TransitionTo(State.AimingPosition);
        }
    }
}
```

### 7-5. 핀 쓰러짐 판정

각 핀의 `transform.up`과 `Vector3.up`의 각도가 임계값(예: 45도) 이상이면 쓰러진 것으로 간주한다. 임계값은 튜닝 대상이다.

```csharp
public bool IsFallen() =>
    Vector3.Angle(transform.up, Vector3.up) > fallThreshold;
```

### 7-6. 데이터 저장 (JSON)

```csharp
[Serializable]
public class GameRecord
{
    public string modeName;    // "쇼트 모드" / "풀 모드"
    public int    frameCount;
    public int    score;
    public string playedAt;   // ISO 8601
}

[Serializable]
public class SaveData
{
    public List<GameRecord> highScores;
    public string selectedBallSkin;
    public string selectedCharacterSkin;
}
```

> 저장 위치: `Application.persistentDataPath/save.json`

---

## 8. 점수 계산기 검증 시나리오

### 8-1. 유닛 테스트 케이스

| 케이스 | 입력 | 기대 결과 |
|---|---|---|
| 올 거터 | 0, 0 × N | 0점 |
| 올 스트라이크 (N=5) | X × 5 | 75점 (15 × 5) |
| 올 스트라이크 (N=10) | X × 10 | 150점 (15 × 10) |
| 일반 스페어 | ball1=6, ball2=4 | 13점 |
| 0/10 스페어 | ball1=0, ball2=10 | 13점 (스페어 처리) |
| 일반 오픈 | ball1=3, ball2=5 | 8점 |
| 5-Frame 예시 | 6/4, X, 3/5, 0/10, 7/2 → 각 13, 15, 8, 13, 9 | 누적: 13→28→36→49→58 |

### 8-2. 5-Frame 모드 (쇼트 모드) 예시

| 프레임 | 1구 | 2구 | 판정 | 프레임 점수 | 누적 총점 |
|---|---|---|---|---|---|
| 1 | 6 | 4 | 스페어 | 13 | 13 |
| 2 | 10 | - | 스트라이크 | 15 | 28 |
| 3 | 3 | 5 | 일반 | 8 | 36 |
| 4 | 0 | 10 | 스페어 (0/10) | 13 | 49 |
| 5 | 7 | 2 | 일반 | 9 | 58 |

---

## 9. 개발 로드맵

### Phase 1. 기획 및 셋업 (1주)
- GDD 작성, 와이어프레임 확정
- Unity 6 프로젝트 생성 (URP 템플릿)
- Git 저장소 초기화, 폴더 구조 정리
- Input System 패키지 설치

### Phase 2. 씬 및 물리 베이스 (1~2주)
- 레인, 거터, 핀 10개 배치 및 프리팹화
- 공/핀 Rigidbody 및 PhysicsMaterial 튜닝
- 카메라 위치 고정 및 시야각 결정

### Phase 3. 입력 시스템 (1주)
- 좌우 왕복 위치 지정 로직
- 세기 게이지 UI 및 확정 로직
- 상태 머신 초기 구현

### Phase 4. 점수 계산 시스템 (1~2주, 핵심)
- `Frame`, `ScoreCalculator` 구현
- **유닛 테스트 작성** (Section 8 참조)
- 쇼트 모드 / 풀 모드 점수 계산 검증

### Phase 5. 게임 플로우 관리 (1주)
- `GameManager` 및 상태 전이 구현
- `BowlingRuleConfig` 주입 구조 완성
- 모드 전환 동작 확인 (5/10 모두 동작 검증)

### Phase 6. UI/UX (1주)
- 점수판 (프레임 수에 따라 동적 생성)
- 메인 메뉴, 모드 선택, 결과 화면
- 튜토리얼 화면

### Phase 7. 폴리싱 (1주)
- 카툰 코믹 효과음/BGM 적용
- 파티클, 화면 흔들림 등 피드백
- 캐릭터/공 스킨 선택 시스템

### Phase 8. 저장 시스템 (0.5주)
- JSON 저장/로드 구현
- 최고 점수 표시

### Phase 9. 테스트 및 빌드 (1주)
- 초등 저학년 대상 플레이 테스트
- 난이도 튜닝 (공 속도, 게이지 속도)
- Windows Standalone 빌드

---

## 10. MVP 범위

### 포함
- [x] 5-Frame (쇼트 모드) / 10-Frame (풀 모드) 선택
- [x] 위치/세기 2단계 입력 (스페이스바 2회)
- [x] 독립 프레임 커스텀 점수 계산
- [x] 점수판 UI (프레임 수에 따라 동적 생성)
- [x] 튜토리얼 화면
- [x] 캐릭터/볼링공 스킨 선택
- [x] 최고 점수 저장 (JSON)
- [x] 효과음 / BGM

### 제외 (확장 후보)
- 멀티플레이
- 온라인 랭킹
- 모바일 / WebGL 빌드
- 트릭샷, 특수 핀 등 비전통 모드

---

## 11. 확장 계획

- **플랫폼 확장**: WebGL 빌드 검토 (물리 성능 사전 테스트 필요)
- **모드 확장**: 시간 제한 모드, 도전 과제 모드
- **콘텐츠 확장**: 캐릭터/공 스킨 추가, 시즌별 테마 레인
- **접근성**: 색약 모드, 키 리바인딩

---

## 12. 향후 확정 필요 항목

- [ ] 프로젝트 정식 명칭
- [ ] 개발 기간 및 마일스톤
- [ ] 캐릭터/공 스킨 종류 및 개수
- [ ] 튜토리얼 형식 (정적 이미지 / 인터랙티브)
- [ ] 사용 에셋 출처 및 라이선스

---

## 13. 참고 사항

- 본 프로젝트는 학습 및 포트폴리오 목적으로 진행된다.
- 사용 에셋의 라이선스는 별도 관리한다 (Unity Asset Store 또는 직접 제작).
- 점수 계산 규칙 원문 출처: `docs/bowling_rules.txt`

---

## 14. 구현 현황 (2026-06-19 기준)

본 섹션은 위 설계 명세 대비 실제 구현 진척도를 추적한다. 설계 §1~13 은 변경하지 않고 본 섹션만 갱신.

### 14-1. Phase 진행 상태

| Phase | 내용 | 상태 |
|---|---|---|
| 1 | 기획, 셋업, Input System 패키지 | ✅ 완료 |
| 2 | 씬·핀 배치·물리 튜닝 | ✅ 완료 |
| 3 | 입력 시스템 + 상태 머신 | ✅ 완료 |
| 4 | Frame / ScoreCalculator / FrameManager + EditMode 유닛 테스트 | ✅ 완료 (단, FrameManagerTests 일부 케이스가 새 fail-safe 명세와 불일치 — 리팩토링 대기) |
| 5 | GameManager 상태 전이 + BowlingRuleConfig 주입 | ✅ 완료 (`FullModeRule.asset` 생성 + GameModeSelector 폴백 로직 추가) |
| 6 | UI/UX | 🟡 진행 중 |
| &nbsp;&nbsp;6-a | 점수판 — 현재 프레임의 1구/2구/총점 (`ScoreboardUI`) | ✅ 완료 (씬 배선 + Play 검증 완료) |
| &nbsp;&nbsp;6-b | 점수판 — 10프레임 모두 동적 생성 | ❌ 미착수 |
| &nbsp;&nbsp;6-c | 결과 화면 — 점수 + 메인메뉴/Quit 버튼 | ✅ 완료 (구조 변경, 2026-06-19) — 오버레이 패널(`ResultUI`)에서 **별도 씬(`Gameover_scene` + `GameOverUI`)** 으로 전환. `GameResultHolder` 싱글턴으로 점수 인계. 재시작 버튼은 제거되고 메인메뉴/Quit 2버튼 구성 |
| &nbsp;&nbsp;6-d | 메인 메뉴 → 게임 씬 전이 + 모드 선택 | ✅ 완료 (`mainmenu.unity` 구성 + `MainMenuUI` + `GameModeSelector` + Build Settings 등록 + 양 모드 Play 검증) |
| &nbsp;&nbsp;6-e | 튜토리얼 화면 | ❌ 미착수 (형식 미결정) |
| 7 | 폴리싱 (효과음/BGM/파티클/스킨) | ❌ 미착수 |
| 8 | JSON 저장 시스템 | ✅ 완료 (2026-06-19) — `SaveSystem`/`HighScoreService` 컴파일·씬 배선·검증 모두 통과. `.meta` 누락 복구 → Gameover_scene 에 `best_score`/`new_record` TMP 노드 신규 생성·와이어링 → 프로그램 검증 7+3 시나리오 PASS (첫 실행/신기록/비신기록/모드 분리/fail-safe/정렬/Top10). 상세 §14-13 |
| 9 | 테스트·빌드 (Windows Standalone) | ❌ 미착수 |

### 14-2. 구현된 컴포넌트 카탈로그 (요약)

상세 시그니처·이벤트·계약은 **`AI_PROMPT_REFERENCE.md`** 참조 (단일 출처).

- **Core**: `GameState`(enum), `GameStateManager`(싱글톤), `GameManager`(싱글톤, `[DefaultExecutionOrder(1000)]`) — ModeSelector 폴백 분기 + `OnEnterGameOver` 가 결과 캐시 후 `Gameover_scene` 로드, **`GameModeSelector`** (DontDestroyOnLoad 싱글톤), **`GameResultHolder`** (DontDestroyOnLoad 싱글톤 — 신규, 점수+모드명 인계)
- **Gameplay**: `BowlingBall`, `BallAimer`, `Pin`, `PinManager`, `InputController`(싱글톤), `CameraFollow`, `PhysicsSettleDetector`, `ThrowTransitionController` — `BowlingBall.ResetToStartPosition()` 에 위치 검증·재시도·최종 폴백 로직 포함
- **Scoring** (`Bowling.Scoring`, Unity 비의존 어셈블리 `Bowling.Domain`): `Frame`, `FrameType`, `ScoreCalculator`(정적), `ScoringConstants`(정적), `FrameManager`(MonoBehaviour), `BowlingRuleConfig`(ScriptableObject, `BowlingGame` 네임스페이스 — 어셈블리 주의)
- **UI**: `PowerGaugeUI`, `ScoreboardUI` (`gameover_score` TMP_Text 연동 포함), **`MainMenuUI`**, **`GameOverUI`** (신규 — Gameover_scene Canvas 부착, 3개 SerializeField). `ResultUI` 는 2026-06-19 구조 변경으로 obsolete (Game.unity Canvas 에 컴포넌트 잔존 가능하나 panelRoot 등 필드 null → Validate 실패 → 자동 비활성)
- **Debug**: `DebugResetController` (R 키 → `GameManager.RestartGame()`)
- **Persistence** (`BowlingGame`, Assembly-CSharp): `GameRecord`, `SaveData`(`version`/`highScores` 필드), **`SaveSystem`**(static, JsonUtility 파일 I/O — 신규), **`HighScoreService`**(static, 모드별 Top 10 + 신기록 판정 — 신규). ⚠️ 컴파일 미검증 (§14-13)

### 14-3. 씬 상태

- **`Assets/Scenes/Game.unity`** — 메인 게임 씬. 점수판 포함, Play 시 즉시 1프레임 시작.
  - GameObject 10개 (Main Camera, Directional Light, Ground, Lane_Root, BowlingBall, GameManager ⓐ/ⓑ, HUD_Canvas, EventSystem, Canvas)
  - **GameManager 두 개 분리**: ⓐ는 GameStateManager+InputController+DebugResetController, ⓑ는 GameManager+PhysicsSettleDetector+ThrowTransitionController+FrameManager
  - Canvas 자식: `total_score`/`total_score_n`, `current_frame`/`frame_n`, `frame_N_first`, `frame_/`, `frame_N_sec`, `gameover_score`. (이전 `ResultPanel` 은 2026-06-19 구조 변경으로 삭제됨)
  - Canvas 부착 컴포넌트: `ScoreboardUI`. (`ResultUI` 컴포넌트는 잔존 가능 — 필드 null 이라 자동 비활성)
- **`Assets/Scenes/Gameover_scene.unity`** (Build index 2, 신규) — 게임 종료 결과 화면
  - 루트 GameObject 4개: Main Camera, Directional Light, EventSystem (자동 추가, 2026-06-19), Canvas (`GameOverUI` 부착)
  - Canvas 자식 3개: `Mainmenu_button` (Button + TMP label "메인메뉴"), `Quit_button` (Button + TMP label "Quit"), `gameover_score` (TMP_Text — 최종 점수)
  - GameOverUI 인스펙터 배선: `gameOverScoreText` → `gameover_score`, `mainMenuButton` → `Mainmenu_button`, `quitButton` → `Quit_button`
  - ⏳ **배선 대기 (2026-06-19, Phase 8)**: `best_score`(TMP — 최고 점수) / `new_record`(TMP — "신기록!" 강조) 2개 노드 신규 생성 + `GameOverUI.bestScoreText`/`newRecordText` 필드 할당 필요. 두 필드는 선택(null-guard)이라 미배선 시 기존 점수 표시는 정상 동작. 상세 §14-13
- **`Assets/Scenes/mainmenu.unity`** — 메인메뉴 씬 (Build index 0, 시작 씬)
  - 루트 GameObject 6개: Main Camera, Directional Light, Global Volume, **GameModeSelector**, EventSystem (InputSystemUIInputModule), **Canvas** (MainMenuUI 부착)
  - Canvas 자식 3개: `Title` (TMP "Bowling Champion"), `ShortButton` (Button+Image+Label), `FullButton` (Button+Image+Label)
  - MainMenuUI 인스펙터 배선: shortModeRule → ShortModeRule.asset, fullModeRule → FullModeRule.asset, gameSceneName = "Game"

### 14-4. 룰 에셋

- `Assets/Configs/ShortModeRule.asset` — 쇼트 모드 5프레임 ✅ (퍼펙트 75점)
- `Assets/Configs/FullModeRule.asset` — 풀 모드 10프레임 ✅ (퍼펙트 150점)

### 14-5. Build Settings

- index 0: `Assets/Scenes/mainmenu.unity` (시작 씬)
- index 1: `Assets/Scenes/Game.unity` (canonical) — 2026-06-19 정리됨. 이전엔 루트 `Assets/Game.unity` 중복이 등록되어 있었으나 제거함. 상세 §14-16
- index 2: `Assets/Scenes/Gameover_scene.unity`

### 14-6. 다음 작업

**우선순위 1 — 수동 end-to-end 검증** (사용자 직접 Play). mainmenu → 쇼트/풀 → 완주 → Gameover_scene 자동 전환 → 점수/최고점/신기록 표시 → save.json 생성. 이번 세션 모든 변경(Phase 8 + Build Settings 정리)의 통합 동작 확인. 절차: `NEXT_SESSION.md §2`.

**우선순위 2 — 10프레임 동적 점수판** (6-b). 현재 점수판은 5칸 고정. 풀 모드 시 동적 10칸 확장 필요. §6/§10 명세 참조.

**우선순위 3 — 튜토리얼 화면** (6-e). 형식 미결정 — 정적 이미지 vs 인터랙티브.

**우선순위 4 — `FrameManagerTests` 리팩토링**. 기존 예외 기대 케이스를 새 fail-safe 명세 (`Debug.LogWarning + 무시`) 와 정렬.

**우선순위 5 — Phase 7 폴리싱** / **결과 화면 확장** ("퍼펙트!" 강조, 별/이펙트, 베스트 비교) / **스킨 시스템** (`selectedBallSkin`/`selectedCharacterSkin`).

### 14-7. 이전 세션 변경 (2026-06-05)

자세한 진단·수정 기록은 **`SESSION_2026-06-05.md`** 참조.

| 영역 | 변경 |
|---|---|
| 코드 최적화 | `BowlingBall.Awake()` 에서 `BallSpawnPoint` Transform 1회 캐싱 — 매 리셋마다의 `GameObject.Find` 풀-스캔 제거 |
| 게임 상태 흐름 | `BowlingBall.HandleStateChanged` 의 AimingPosition 리셋 블록 제거 — 공 위치 리셋이 `GameManager.BeginGame` + `ThrowTransitionController.HandlePostThrow` 두 경로로 단일화 |
| 물리 안정성 | `BowlingBall.ResetBall` 6단계 패턴 — `Physics.SyncTransforms()` + `rb.Sleep()` 추가로 Unity 6 의 `autoSyncTransforms=false` + Interpolation + ContinuousDynamic CCD 결합 부작용 (stale Rigidbody.position, 누적 internal state) 차단. 스트라이크/스페어 후 공 위치 미초기화 + y 드리프트 회귀 모두 해결 |
| UI 해상도 일관성 | `Assets/Scenes/Game.unity` 의 점수판 Canvas CanvasScaler: `ConstantPixelSize 800×600` → `ScaleWithScreenSize 1920×1080 Match=0.5`. HUD_Canvas 와 통일. 랩탑 (2880×1800) ↔ 데스크탑 (QHD) 양쪽에서 점수판 위치·크기 일관성 확보 |

### 14-8. 최근 세션 변경 (2026-06-06)

| 영역 | 변경 파일 | 변경 내용 |
|---|---|---|
| 물리 안정성 — 공 리셋 위치 검증 | `BowlingBall.cs` | `ResetToStartPosition()` 에 리셋 후 위치 검증 로직 추가. 간헐적으로 공이 바닥을 관통하여 스폰되는 버그 방지. **3단계 방어**: ① 첫 시도 후 `ValidateResetPosition()` 검증 → ② 실패 시 최대 3회 즉시 재시도 (`MAX_RESET_RETRIES`) → ③ 모든 재시도 실패 시 `ForceResetCoroutine` 코루틴 기반 최종 폴백 (kinematic 강제 배치 → 1프레임 대기 → 재보정 → dynamic 복귀). 허용 오차: `RESET_POSITION_TOLERANCE = 0.05f`. 기존 `ResetBall` 6단계 패턴·public API 시그니처 변경 없음 |
| UI — 게임 종료 최종 점수 표시 | `ScoreboardUI.cs` | `gameOverScoreText` (`gameover_score` TMP_Text) 필드 추가. `HandleGameOver()` 에서 기존 점수판 클리어 후 최종 점수를 `gameover_score` 에 표시. `HandleGameInitialized()` 에서 재시작 시 `gameover_score` 도 클리어. `Validate()` 에 null 검증 추가 |
| 메인메뉴 흐름 — 신규 | `Assets/Scripts/Core/GameModeSelector.cs`, `Assets/Scripts/UI/MainMenuUI.cs`, `Assets/Scenes/mainmenu.unity`, `Assets/Configs/FullModeRule.asset` | 메인메뉴 → 게임 씬 전이 + 모드 선택 6단계(A~F) 완료. `GameModeSelector` (DontDestroyOnLoad 싱글톤) 가 선택 모드를 보존, `MainMenuUI` 가 쇼트/풀 버튼 처리, `GameManager.Start()` 가 `SelectedRule` 폴백 분기 사용 (인스펙터 `ruleConfig` 호환 유지). Build Settings 등록 (mainmenu=0, Game=1) + 양 모드 Play 검증 완료 |
| ResultUI 골격 (신규) | `Assets/Scripts/UI/ResultUI.cs` | 결과 화면 UI 5단계 점진 개발 중 **단계 1 (골격) 완료** — using, XML 주석, Header 4그룹 직렬화 필드 7개, 메서드 스텁 6개. 단계 2~5 (구독/해제·상수/헬퍼·핸들러/콜백·씬 배선) 대기 — `NEXT_SESSION.md` |
| 자동 재시작 코루틴 제거 (revert) | `GameManager.cs` | 노트북 브랜치의 `GAMEOVER_DISPLAY_DURATION` / `gameOverCoroutine` / `GameOverDelayCoroutine` / RestartGame 의 코루틴 중단 로직 / `using System.Collections;` 모두 **제거**. 사용자 결정 (2026-06-07): 자동 재시작이 ResultUI 패널의 명시 클릭(재시작/메인메뉴) 으로 대체됨. `gameover_score` 표시 자체는 ScoreboardUI 측에 유지 |

### 14-10. 최근 세션 변경 (2026-06-18) — ResultUI 단계 2~5 완료

본 세션에서 `ResultUI` 의 단계 2 (구독/해제 + Validate) → 단계 3 (상수 + 헬퍼) → 단계 4 (핸들러 + 콜백) → 단계 5 (씬 배선) 을 모두 완료했다. 코드와 씬 양쪽 모두 `Assets/Scripts/UI/ResultUI.cs` 와 `Assets/Scenes/Game.unity` 에 영구 반영됨.

#### 14-10-1. 코드 변경 (`Assets/Scripts/UI/ResultUI.cs`)

| 단계 | 추가 내용 |
|---|---|
| 2 | `Start()` — `Validate()` → 이벤트 2개 (`OnGameInitialized`, `OnGameOver`) 구독 → 버튼 2개 (`restartButton`, `mainMenuButton`) `onClick.AddListener` → `HidePanel()` 호출 → `[Result] 초기화 완료` 로그. `OnDestroy()` — null 가드 후 대칭 해제. `Validate()` — 직렬화 필드 7개 (`frameManager`, `panelRoot`, `finalScoreText`, `strikeCountText`, `spareCountText`, `restartButton`, `mainMenuButton`) null 체크 + `[Result]` prefix 에러 로그. ScoreboardUI 와 동일 패턴 |
| 3 | 상수 2개 — `COUNT_FORMAT = "{0}회"` (횟수 포맷 단일 출처), `MAIN_MENU_SCENE_NAME = "mainmenu"`. 순수 함수 3개 — `CountStrikes()` / `CountSpares()` 는 `frameManager.GetFrameCount()` 순회 + `Frame.IsStrike()`/`IsSpare()` 카운트 (0/10 스페어 포함). `FormatCount(int)` 는 `string.Format(COUNT_FORMAT, n)`. 사이드이펙트 헬퍼 2개 — `ShowPanel()`/`HidePanel()` 는 `panelRoot.SetActive(true/false)`. 단계 2 의 직접 호출 `panelRoot.SetActive(false)` 를 `HidePanel()` 로 리팩토링 |
| 4 | `HandleGameInitialized()` — `HidePanel()` + `[Result] 게임 초기화 — 패널 숨김` 로그. `HandleGameOver()` — `GetTotalScore()`/`CountStrikes()`/`CountSpares()` 계산 → `finalScoreText.text = score.ToString()` / `strikeCountText.text = FormatCount(strikes)` / `spareCountText.text = FormatCount(spares)` → `ShowPanel()` → 종합 로그. `OnRestartClicked()` — `GameManager.Instance.RestartGame()` + 로그. `OnMainMenuClicked()` — `SceneManager.LoadScene(MAIN_MENU_SCENE_NAME)` + 로그 |
| 5 | 씬 배선 — 아래 §14-10-2 참조 |

#### 14-10-2. 씬 배선 — `ResultPanel` 노드 트리

**Editor 부착 위치**: `Canvas` (instanceID 49980, 기존 ScoreboardUI 부착 노드) 에 `ResultUI` 컴포넌트 추가. `ResultPanel` 은 그 자식.

```
Canvas (ResultUI 부착, ScoreboardUI 와 공존)
└── ResultPanel  ← panelRoot 필드 = 이 GameObject
    ├── final_score      ← finalScoreText
    ├── strike_count     ← strikeCountText
    ├── spare_count      ← spareCountText
    ├── restart_button   ← restartButton (Button 컴포넌트)
    │   └── label
    └── main_menu_button ← mainMenuButton (Button 컴포넌트)
        └── label
```

**ResultPanel 자체** — RectTransform + CanvasRenderer + Image (배경)

| 속성 | 값 | 수정 의미 |
|---|---|---|
| Anchor | (0.5, 0.5)/(0.5, 0.5) | 화면 정중앙 기준 |
| Pivot | (0.5, 0.5) | 회전·스케일 기준점 |
| Anchored Position | (0, 0) | Canvas 중앙. 옮기려면 X/Y 변경 |
| Size Delta | (900, 700) | 패널 폭/높이. 작게 하려면 두 값 모두 감소 |
| Image.color | (0, 0, 0, 0.85) | 반투명 검정. 알파↑→불투명, RGB→배경 색 |
| GameObject.active | `false` (초기) | `Start()` 에서 `HidePanel()` 호출되므로 별도 조작 불필요 |

**자식 TMP_Text 3개** — 모두 anchor/pivot (0.5, 0.5), 폰트 `NotoSansKR-Black SDF` (씬 내 기존 TMP 에서 자동 차용), 색 `Color.white`, alignment `Center`

| 노드 | Anchored Position | Size Delta | Font Size | 초기 텍스트 | 비고 |
|---|---|---|---|---|---|
| `final_score`  | (0, 200) | (800, 160) | 140 | `"0"` | 가장 큰 텍스트. `HandleGameOver()` 에서 `score.ToString()` 으로 갱신 |
| `strike_count` | (0,  40) | (800,  80) |  48 | `"스트라이크 0회"` | `HandleGameOver()` 에서 `FormatCount(strikes)` 로 갱신 → `"N회"` 만 표시 (라벨 텍스트 "스트라이크" 는 초기 placeholder 일 뿐 런타임에 사라짐). 라벨을 영구 표시하려면 별도 TMP 추가 또는 `COUNT_FORMAT` 을 `"스트라이크 {0}회"` 로 변경 |
| `spare_count`  | (0, -40) | (800,  80) |  48 | `"스페어 0회"` | 동일 (스페어용) |

**자식 Button 2개** — 모두 anchor/pivot (0.5, 0.5), Image+Button 컴포넌트, `targetGraphic = Image`. 자식 `label` 은 RectTransform stretch (0/1 ~ 0/1) + TMP

| 노드 | Anchored Position | Size Delta | Image.color | Label Text | Label Font Size |
|---|---|---|---|---|---|
| `restart_button`   | (-180, -200) | (320, 110) | (0.2, 0.5, 0.9, 1) (파란색) | `"재시작"` | 42 |
| `main_menu_button` | ( 180, -200) | (320, 110) | (0.2, 0.5, 0.9, 1) (파란색) | `"메인메뉴"` | 42 |

#### 14-10-3. ResultUI 컴포넌트 — Inspector 와이어링 (7개 필드)

| 필드 | 타입 | 할당된 인스턴스 | Inspector 에서 확인 방법 |
|---|---|---|---|
| `frameManager`    | `FrameManager` | `GameManager` ⓑ (PhysicsSettleDetector·ThrowTransitionController·FrameManager 보유, instanceID 49714) | Inspector 에서 ResultUI 컴포넌트 → frameManager 필드 |
| `panelRoot`       | `GameObject`   | `Canvas > ResultPanel`                                           | 같은 위치 panelRoot 필드 |
| `finalScoreText`  | `TMP_Text`     | `Canvas > ResultPanel > final_score`                             | |
| `strikeCountText` | `TMP_Text`     | `Canvas > ResultPanel > strike_count`                            | |
| `spareCountText`  | `TMP_Text`     | `Canvas > ResultPanel > spare_count`                             | |
| `restartButton`   | `Button`       | `Canvas > ResultPanel > restart_button`                          | |
| `mainMenuButton`  | `Button`       | `Canvas > ResultPanel > main_menu_button`                        | |

> 7개 모두 본 세션의 `mcp__UnityMCP__execute_code` 호출 시 리플렉션으로 자동 할당됨. Editor 에서 직접 다시 끌어다 놓을 필요 없음.

#### 14-10-4. 자주 하게 될 수정 작업 — 빠른 레시피

| 하고 싶은 것 | 어디를 만져야 하는가 |
|---|---|
| 패널 위치 옮기기 | `ResultPanel.anchoredPosition` |
| 패널 크기 바꾸기 | `ResultPanel.sizeDelta` (자식들의 Anchored Position 도 비례해서 재배치 필요) |
| 배경 색/투명도 | `ResultPanel.Image.color` |
| 최종 점수 크기 | `final_score.fontSize` |
| "스트라이크/스페어" 라벨 영구 표시 | `ResultUI.cs` 의 `COUNT_FORMAT` 상수를 `"스트라이크 {0}회"` / `"스페어 {0}회"` 로 바꾸거나, 별도 TMP 라벨 노드를 ResultPanel 자식으로 추가 |
| 버튼 색 | 각 `*_button.Image.color` |
| 버튼 라벨 텍스트 | 각 `*_button > label` TMP `text` |
| 버튼 동작 | `ResultUI.OnRestartClicked()` / `OnMainMenuClicked()` — 게임 로직은 모두 여기에. 씬 이름 상수는 `MAIN_MENU_SCENE_NAME` |
| 폰트 변경 | 모든 TMP_Text 의 `font` 를 다른 SDF 폰트로 교체 (현재 `NotoSansKR-Black SDF`) |
| 패널 표시 타이밍 | `ResultUI.HandleGameOver()` — `FrameManager.OnGameOver` 이벤트가 트리거. 지연 표시를 원하면 `StartCoroutine` 으로 감싸기 |
| ScoreboardUI `gameover_score` 와의 역할 분담 | 현재 둘 다 동시 표시됨 (`gameover_score` 는 게임 영역에 큰 점수, `ResultPanel` 은 그 위에 오버레이). 어느 한쪽을 숨기려면 ScoreboardUI 의 `HandleGameOver` 에서 `gameOverScoreText.text = ""` 처리 또는 ResultPanel 의 배경을 불투명 (`alpha = 1.0`) 으로 |

#### 14-10-5. 재배선이 필요해질 때 — 자동화 스크립트 재실행

만약 ResultPanel 트리를 잘못 만져서 복구하고 싶다면, 본 세션에서 실행한 `mcp__UnityMCP__execute_code` 호출 (Roslyn 미사용·CodeDom 호환, `System.Func` 람다 사용) 을 다시 돌리면 됨. 호출은 멱등(idempotent) — 기존 `ResultPanel` 이 있으면 `DestroyImmediate` 로 정리 후 재생성. 스크립트 골자:

1. `GameObject.Find("Canvas")` → Canvas 참조
2. `canvas.transform.Find("ResultPanel")` → 있으면 `DestroyImmediate`
3. `FindFirstObjectByType<FrameManager>()` → fm 참조
4. 씬 내 첫 번째 TMP_Text 에서 폰트(`NotoSansKR-Black SDF`) 차용
5. ResultPanel + 자식 5개 생성 (`System.Func` 람다로 텍스트/버튼 빌더)
6. Canvas 에 `ResultUI` 컴포넌트 추가 + 7개 필드 리플렉션 와이어링
7. `EditorSceneManager.MarkSceneDirty` + `SaveScene`

#### 14-10-6. ⚠️ 미해결 — `Assets/Game.unity` 중복 + Build Settings 불일치

세션 종료 시점 점검에서 발견:

- `Assets/Game.unity` (루트, **untracked** 신규 파일) 가 존재
- `Assets/Scenes/Game.unity` (canonical, **이번 ResultUI 배선이 들어간 곳**) 도 존재
- `EditorBuildSettings.asset` (modified) 의 index 1 은 현재 `Assets/Game.unity` (루트) 를 가리키고 있음
- 즉 빌드 / `SceneManager.LoadScene("Game")` 시 실제로 로드되는 씬은 루트 `Assets/Game.unity` — ResultUI 가 없는 쪽

**이번 세션 검증이 정상 통과한 이유**: Editor 의 Play 모드는 현재 열려 있는 씬 (`Assets/Scenes/Game.unity`) 을 직접 재생하므로 Build Settings 와 무관. ResultUI 가 정상 동작한 것은 이 캐주얼 검증 한정.

**실제 게임 흐름 (mainmenu → Game) 에서는 ResultUI 가 보이지 않을 가능성 높음**. 다음 세션 첫 작업으로 정리 필요.

복구 절차 (권장):
1. Editor 에서 `Assets/Scenes/Game.unity` 열어둔 채로 — File > Save As → `Assets/Scenes/Game.unity` 그대로 (정상 위치 확정)
2. `Assets/Game.unity` 및 `Assets/Game.unity.meta` 삭제 (Project 창에서 우클릭 → Delete)
3. File > Build Settings 열어서 Scenes In Build 에 `Assets/Scenes/Game.unity` 추가 + 기존 `Assets/Game.unity` 항목 제거 (drag-drop 으로 index 1 위치 유지)
4. `EditorBuildSettings.asset` 커밋

### 14-11. 후속 세션 변경 (2026-06-19) — 볼링공 리셋 위치 미세 어긋남 수정

**증상**: 쇼트/풀 모드 무관, 매 투구 후 리셋 시 공 위치가 spawnPoint 와 미세하게 어긋나는 경우가 간헐적으로 발생. 기존 6단계 `ResetBall` + 3단계 방어 (검증 → 재시도 → ForceResetCoroutine) 가 막은 catastrophic 케이스 (바닥 관통) 와는 별개의, 사용자 가시적 작은 어긋남.

**원인**: `BallAimer.Update` 의 한 줄 `transform.position = new Vector3(x, transform.position.y, ballStartZ)` —
- Z 를 항상 인스펙터 값 `ballStartZ = 0.5f` 로 강제 스냅 (spawnPoint.z 와 무관하게 덮어씀)
- Y 를 "그 순간의 transform.position.y" 로 차용 → ResetBall 직후 Rigidbody Interpolation 의 보간 잔여값을 그대로 락

`ResetToStartPosition` 내부 동기 검증은 통과하지만, 그 다음 프레임 `BallAimer.Update` 가 위 1줄로 Y/Z 를 다시 쓰면서 어긋남이 도입됨.

**수정**: `Assets/Scripts/Gameplay/BallAimer.cs`
- `Awake()` 추가 — `BowlingBall.Awake` 와 동일 패턴으로 `BallSpawnPoint` Transform 을 1회 캐싱
- `Update()` Y/Z 산정 로직 변경 — spawnPoint 가 있으면 `spawnPoint.position.y/z` 를 canonical 출처로 사용. 없으면 종전 동작 (transform.y + ballStartZ) 으로 graceful degrade. `ballStartZ` 인스펙터 필드는 fallback 용으로 유지

검증: 사용자 Play 모드 — 쇼트/풀 모드 모두 spawn 위치 정확. 가설 2 (interpolation 잔존) / 가설 3 (catch-up FixedUpdate) 는 본 수정만으로 잔존 증상 없어 추가 적용 보류.

### 14-12. 후속 세션 변경 (2026-06-19) — 결과 화면을 별도 씬으로 분리

**동기**: ResultUI 오버레이 패널 구조를 폐기하고 별도 씬 (`Gameover_scene`) 으로 분리. 씬 단위 분리가 더 명확하고, 향후 결과 화면 확장 (퍼펙트 강조, 별/이펙트, 베스트 비교) 시 Game 씬에 영향을 안 주고 자유롭게 작업 가능.

**구조 변경**:
- 사용자가 `Gameover_scene.unity` 신규 생성 (Main Camera + Directional Light + Canvas 자식 3개: `Mainmenu_button` / `Quit_button` / `gameover_score`)
- 사용자가 `Game.unity` 에서 `ResultPanel` 삭제 (ResultUI 컴포넌트는 잔존 가능 — Validate 실패로 자동 비활성)
- 재시작 버튼 제거, 메인메뉴/Quit 2버튼 구성 — Quit 은 `Application.Quit()` (Editor 에서는 `EditorApplication.isPlaying = false`)

**신규 파일**:

| 파일 | 역할 |
|---|---|
| `Assets/Scripts/Core/GameResultHolder.cs` | DontDestroyOnLoad 싱글턴. `GameModeSelector` 와 동일 패턴. `LastScore` / `LastModeName` / `HasResult` 노출. Instance 게터는 lazy — 씬에 없으면 자동 GameObject 생성 (Gameover_scene 단독 Play 호환) |
| `Assets/Scripts/UI/GameOverUI.cs` | Gameover_scene Canvas 부착. Start 에서 `GameResultHolder.Instance.LastScore` 를 읽어 `gameover_score` TMP 갱신. 메인메뉴 버튼 → `SceneManager.LoadScene("mainmenu")`, Quit 버튼 → `Application.Quit()`. HasResult=false 시 점수 0 표시 + 경고 로그 (단독 Play 대비) |

**수정 파일**:

| 파일 | 변경 |
|---|---|
| `Assets/Scripts/Core/GameManager.cs` | `using UnityEngine.SceneManagement;` 추가. `OnEnterGameOver()` 에서 기존 로그 후 `GameResultHolder.Instance.SetResult(score, ruleConfig.ModeName)` + `SceneManager.LoadScene("Gameover_scene")` 호출. `GAMEOVER_SCENE_NAME = "Gameover_scene"` 상수 단일 출처 |

**Gameover_scene 자동 배선** (`mcp__UnityMCP__execute_code` 실행, 멱등):
1. `Canvas` 노드 발견 + 자식 3개 (`Mainmenu_button`, `Quit_button`, `gameover_score`) 매핑
2. **`EventSystem` 누락 감지 → 자동 추가** (UnityEngine.EventSystems.EventSystem + InputSystemUIInputModule). 없으면 버튼 클릭 안 됨
3. `GameOverUI` 컴포넌트 Canvas 에 추가 + 3개 SerializeField 리플렉션 와이어링
4. 씬 저장 + Build Settings index 2 등록

**검증** (Editor Play 모드, Gameover_scene 단독):
- `gameover_score.text` 가 `"0"` 으로 갱신 — `HasResult==false` 분기 정상 통과 (게임 진행 없이 직접 Play 한 케이스)
- Mainmenu 버튼 onClick.Invoke() → mainmenu 씬으로 전환 확인 (씬 카운트 1, active=mainmenu)
- Quit 버튼 동작은 Editor 에서 Play 종료를 유발하므로 자동 검증 생략 — 실제 빌드/플레이 시 확인 권장

**잔여 이슈**:
- ResultUI.cs 파일은 의도적으로 삭제하지 않음 — 추후 결과 화면을 다시 오버레이로 합치는 옵션이 열려 있도록. 정리 원하면 `Assets/Scripts/UI/ResultUI.cs` 및 Game.unity Canvas 의 ResultUI 컴포넌트 제거 가능
- Build Settings index 1 (`Assets/Game.unity` 루트) vs canonical `Assets/Scenes/Game.unity` 중복 문제는 여전히 미해결 (§14-10-6). 단, 본 변경은 양쪽 씬 모두에 동일하게 적용되므로 (스크립트 참조 공통) 새 구조 동작에는 무영향

### 14-13. 후속 세션 변경 (2026-06-19) — JSON SaveSystem (Phase 8) 완료

**동기**: 게임을 꺼도 최고 점수가 남지 않아 아이 대상 게임의 재도전 동기(신기록 갱신)가 빠져 있었다. 게임 종료 시 점수를 `save.json` 에 영속화하고, **모드별 최고 점수**를 관리하며, **Gameover 화면에 "최고 점수 + 신기록!"** 을 노출한다.

> ✅ **본 변경은 코드 작성 → 컴파일 → Gameover_scene 노드 배선 → 프로그램 검증까지 완료**. 7+3 시나리오 모두 PASS. 남은 검증은 사용자의 실제 Play 흐름(mainmenu → 완주 → Gameover) 수동 확인 — `NEXT_SESSION.md §2`.

#### 14-13-1. 확정된 설계 결정 (8개)

| 항목 | 결정 |
|---|---|
| 직렬화 라이브러리 | **JsonUtility** (Unity 내장, 의존성 0) |
| 고득점 구조 | **모드별 분리** — `modeName` 기준 그룹, 모드당 Top 10 |
| 저장 시점 | 게임 종료 즉시 (`GameManager.OnEnterGameOver`, 씬 로드 직전 동기) |
| 정렬 정책 | 점수 내림차순, 동점 시 `playedAt`(ISO 8601) 최신 우선 |
| 첫 실행 처리 | 파일 없으면 빈 `SaveData`(version=1, 빈 리스트) 반환 |
| 저장 실패 정책 | `try/catch` → `Debug.LogWarning` + 게임 흐름 계속 (예외 전파 금지) |
| 백업/복구 | 단일 파일 (.bak 없음 — 추후 과제) |
| 버전 관리 | `SaveData.version = 1` 필드로 전방호환 확보 |
| UI 노출 | Gameover 화면에 "최고 점수: N" + 신기록 시 "신기록!" 강조 |

#### 14-13-2. 신규 파일

| 파일 | 역할 |
|---|---|
| `Assets/Scripts/Persistence/SaveSystem.cs` | static. `FilePath`(`persistentDataPath/save.json`), `Load() → SaveData`(없거나 파싱 실패 시 빈 데이터), `Save(SaveData)`(JsonUtility prettyPrint → `File.WriteAllText`). 모든 I/O 가 fail-safe (`[SaveSystem]` 로그) |
| `Assets/Scripts/Persistence/HighScoreService.cs` | static. `Record(modeName, frameCount, score) → RecordResult{IsNewRecord, BestScore}`, `GetBestScore(modeName)`, `GetHighScores(modeName)`. 모드별 그룹화 후 점수 내림차순 정렬 + 모드당 `MaxPerMode(=10)` trim. `playedAt = DateTime.UtcNow.ToString("o")` |

#### 14-13-3. 수정 파일

| 파일 | 변경 |
|---|---|
| `Assets/Scripts/Persistence/SaveData.cs` | `public int version = 1;` 추가, `highScores = new List<>()` 기본 초기화. 기존 필드 유지 |
| `Assets/Scripts/Core/GameResultHolder.cs` | `SetResult(score, modeName)` → `SetResult(score, modeName, bestScore, isNewRecord)` 확장. `LastBestScore` / `IsNewRecord` 속성 추가 (호출부는 GameManager 단일) |
| `Assets/Scripts/Core/GameManager.cs` | `OnEnterGameOver` 에서 `HighScoreService.Record(ruleConfig.ModeName, ruleConfig.FrameCount, score)` 호출 후 결과를 `GameResultHolder.SetResult(...)` 로 전달 (기존 `LoadScene` 유지) |
| `Assets/Scripts/UI/GameOverUI.cs` | `bestScoreText`/`newRecordText` (선택, null-guard) SerializeField + `ApplyBestScore()` 추가. 상수 `BEST_FORMAT = "최고 점수: {0}"`, `NEW_RECORD_TEXT = "신기록!"`. 신기록 아니면 `new_record` 비활성 |

#### 14-13-4. 데이터 흐름

```
게임 종료 → GameManager.OnEnterGameOver
  → HighScoreService.Record(mode, frameCount, score)
       → SaveSystem.Load() → 직전 최고점 캡처 → GameRecord 추가
       → 모드별 Top 10 정리 → SaveSystem.Save()
       → RecordResult{ IsNewRecord = score > 직전최고, BestScore = max(...) }
  → GameResultHolder.SetResult(score, mode, BestScore, IsNewRecord)
  → SceneManager.LoadScene("Gameover_scene")
       → GameOverUI.Start → gameover_score / best_score / new_record 표시
```

#### 14-13-5. save.json 예시

```json
{
    "version": 1,
    "highScores": [
        { "modeName": "쇼트 모드", "frameCount": 5, "score": 58, "playedAt": "2026-06-19T..." }
    ],
    "selectedBallSkin": "",
    "selectedCharacterSkin": ""
}
```
저장 경로(Windows): `%USERPROFILE%/AppData/LocalLow/<회사>/<제품>/save.json`

#### 14-13-6. 검증 결과 (2026-06-19 후속 작업 완료)

**복구 단계**: 디스크엔 `SaveSystem.cs` / `HighScoreService.cs` 가 존재했지만 `.meta` 누락으로 Unity 가 import 하지 않은 상태였음. `AssetDatabase.ImportAsset(path, ForceUpdate)` 명시 호출로 `.meta` 자동 생성(`bb0ab94f...`, `8b0a1092...`) → 재컴파일 → 7개 타입(`SaveSystem`/`HighScoreService`/`SaveData`/`GameRecord`/`GameResultHolder`/`GameOverUI`/`GameManager`) 모두 reflection 으로 OK 확인. `FilePath = C:/Users/bmbm7/AppData/LocalLow/DefaultCompany/bowling demo/save.json`.

**Gameover_scene 노드 배선** (멱등 `execute_code`):
- `best_score` TMP_Text — anchored (0, -65), size (700, 80), 폰트 60, 흰색, 초기 "최고 점수: 0"
- `new_record` TMP_Text — anchored (0, 90), size (700, 100), 폰트 80, 노란색 (1, 0.85, 0.2), "신기록!", **초기 비활성** (GameOverUI 가 IsNewRecord 시 SetActive)
- 폰트 `NotoSansKR-Black SDF` 는 기존 `gameover_score` 에서 차용
- `GameOverUI.bestScoreText` / `newRecordText` 리플렉션 와이어링 + 씬 저장

**프로그램 검증 — 7+3 시나리오 모두 PASS**:
1. 첫 실행 — `Load()` 가 빈 `SaveData(version=1, highScores.Count=0)` 반환 ✅
2. 신기록 — 쇼트 모드 첫 점수 58 → `IsNewRecord=true, BestScore=58` ✅
3. 비신기록 — 쇼트 30 → `IsNewRecord=false, BestScore=58` (직전 최고 유지) ✅
4. 모드 분리 — 쇼트 58 / 풀 100 독립 (`GetBestScore` 각각 정확) ✅
5. Fail-safe — 손상된 JSON 로드 시 예외 전파 없이 빈 데이터 반환 ✅
6. 정렬 — `GetHighScores` 점수 내림차순 `[75, 58, 15]` ✅
7. Top 10 제한 — 15회 기록 후 모드당 10개로 trim ✅
   + 추가 3 시나리오 — `Record → SetResult` 데이터 흐름 (신기록/비신기록/갱신) ✅

**Play 모드 Gameover_scene 단독 검증**:
- 5개 SerializeField 모두 정확한 컴포넌트와 와이어링 ✅
- HasResult=false 분기 정상 (gameover_score="0", best_score="최고 점수: 0", new_record 비활성) ✅
- 콘솔 에러 0 ✅

**잔여**: 사용자의 실제 게임 플레이 (mainmenu → 쇼트/풀 완주 → Gameover_scene 자동 전환) end-to-end 수동 검증. 절차: `NEXT_SESSION.md §2`.

### 14-15. 후속 세션 변경 (2026-06-19) — Build Settings 정리

**문제**: `EditorBuildSettings.asset` 의 index 1 이 `Assets/Game.unity` (루트 중복) 를 가리키고 있었음. canonical 은 `Assets/Scenes/Game.unity` (이번 세션의 Phase 8 후크·BallAimer 수정·Gameover_scene 통합 등이 모두 들어간 곳). 실제 빌드/`SceneManager.LoadScene("Game")` 시 stale 한 루트 씬이 로드되어 mainmenu → Game 흐름에서 변경분이 보이지 않을 가능성.

**조치 — 옵션 A 적용** (canonical 유지, 루트 폐기):
1. `manage_build action="scenes"` 로 인덱스 재등록 (0=mainmenu, 1=Scenes/Game, 2=Gameover_scene)
2. `AssetDatabase.SaveAssets()` + `SetDirty` 로 `EditorBuildSettings.asset` 디스크 강제 저장
3. `manage_asset action="delete" path="Assets/Game.unity"` — 루트 `.unity` + `.meta` 동시 삭제

**검증**:
- `EditorBuildSettings.asset` 디스크 반영: index 1 GUID `5520e85b...`(루트) → `870c14cb...`(canonical) ✅
- `SceneManager.LoadScene("Game")` → `Assets/Scenes/Game.unity` 로 해석됨 ✅
- `MainMenuUI.gameSceneName = "Game"` 매핑 정상 ✅

**git working tree 결과**:
```
D  Assets/Game.unity
D  Assets/Game.unity.meta
M  ProjectSettings/EditorBuildSettings.asset
```

> ⚠️ **재발 방지**: Editor 에서 `File > Save As` 로 씬을 다시 루트에 저장하지 말 것. 모든 씬은 `Assets/Scenes/` 하위에 둔다.

### 14-16. 후속 세션 변경 (2026-06-19) — 레인 비주얼 작업 (사용자 직접 작업)

본 변경은 사용자가 Editor 에서 직접 수행. 결과 정리만 본 섹션에 기록.

**머티리얼 (4종, 모두 매트 단색 — Metallic 0 / Smoothness 0.5 / Glossiness 0 / BumpScale 1)**:

| 파일 | 상태 | GUID | RGB (255) | 색상 | 구조 |
|---|---|---|---|---|---|
| `Mat_Lane.mat` | 수정 | `eb6cce01031a12249861b0ddf9828d34` | (151, 200, 122) | 🟢 연두/세이지 | **`Mat_Lane 1` 의 변형(Material Variant)** — 색만 오버라이드 |
| `Mat_Lane 1.mat` | 신규 | `2f5328dc6abc49841bdd0f003c6e8286` | (200, 193, 122) | 🟡 베이지/카키 | base material (`Mat_Lane` 의 부모) |
| `Mat_Lane 2.mat` | 신규 | `a010a8930978120419e252e76bf22edd` | (214, 115, 102) | 🔴 살구/오렌지 | standalone |
| `Mat_Lane 3.mat` | 신규 | `b4b7c95aac5103d4bb6001377309cb20` | (200, 169, 122) | 🟤 황토 | standalone (씬 현재 미사용) |

> 핵심: `Mat_Lane` 의 `m_Parent` 가 `Mat_Lane 1` 로 설정되어 텍스처/속성 슬롯들이 부모에서 상속됨. 자체 파일에서 `_BaseMap`/`_BumpMap` 등의 항목은 제거되고 색상 오버라이드만 남음. **앞으로 공유 속성(Smoothness/Bump 등) 일괄 조정은 부모 `Mat_Lane 1` 에서**.

**Game.unity 변경**:
- Lane GameObject 개수: 1 → 6 (`Lane`, `Lane (1)`, `Lane (2)`, `Lane (3)`, `Lane (5)`, `Lane (7)`)
- 씬 내 머티리얼 참조 분포: `Mat_Lane` ×1, `Mat_Lane 1` ×2, `Mat_Lane 2` ×2, `Mat_Lane 3` ×0
- diff 크기: `+620 / -136 lines`

**의도(추정)**: 단일 톤이던 레인을 다색 구간으로 분할하여 시각적 구분 강화. 정확한 인스턴스별 트랜스폼/머티리얼 매핑은 Editor 에서 직접 확인 (본 세션 종료 시점 Unity MCP 일시 연결 끊김으로 자동 인벤토리 미생성).

### 14-14. 참고 문서

| 문서 | 용도 |
|---|---|
| `README.md` (본 문서) | 설계 명세 + 구현 현황 스냅샷 |
| `AI_PROMPT_REFERENCE.md` | AI 협업용 — 컨벤션, 시그니처, 명명 규칙, "건드리지 말 것" 목록 |
| `SESSION_2026-06-05.md` | 이전 세션 (최적화·상태 흐름·해상도 UI) 진단·수정·검증 기록 |
| `NEXT_SESSION.md` | 다음 세션 진입점 — Phase 8 검증 완료 + Build Settings 정리 + 레인 비주얼 작업 반영 (2026-06-19 갱신) |
| `One-Page Concept Sheet.txt` | 게임 컨셉 / 마일스톤 (M1~M5) |
| `PROJECT_FEEDBACK.txt` | (외부 피드백 — 별도 관리) |

---

*Custom Bowling Score System — Design Specification v1.0 / Implementation Snapshot 2026-06-19 (Phase 8 검증 완료 + Build Settings 정리 + 레인 비주얼)*
