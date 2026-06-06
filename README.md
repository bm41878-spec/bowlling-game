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

## 14. 구현 현황 (2026-06-06 기준)

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
| &nbsp;&nbsp;6-c | 결과 화면 (`ResultUI`) — 점수/스트라이크/스페어 + 재시작·메뉴 버튼 | 🟡 **단계 1 (골격) 완료, 단계 2~5 대기** — `NEXT_SESSION.md` 참조 |
| &nbsp;&nbsp;6-d | 메인 메뉴 → 게임 씬 전이 + 모드 선택 | ✅ 완료 (`mainmenu.unity` 구성 + `MainMenuUI` + `GameModeSelector` + Build Settings 등록 + 양 모드 Play 검증) |
| &nbsp;&nbsp;6-e | 튜토리얼 화면 | ❌ 미착수 (형식 미결정) |
| 7 | 폴리싱 (효과음/BGM/파티클/스킨) | ❌ 미착수 |
| 8 | JSON 저장 시스템 | ❌ 미착수 (`SaveData`/`GameRecord` 스켈레톤만 존재) |
| 9 | 테스트·빌드 (Windows Standalone) | ❌ 미착수 |

### 14-2. 구현된 컴포넌트 카탈로그 (요약)

상세 시그니처·이벤트·계약은 **`AI_PROMPT_REFERENCE.md`** 참조 (단일 출처).

- **Core**: `GameState`(enum), `GameStateManager`(싱글톤), `GameManager`(싱글톤, `[DefaultExecutionOrder(1000)]`), **`GameModeSelector`** (DontDestroyOnLoad 싱글톤 — 신규)
- **Gameplay**: `BowlingBall`, `BallAimer`, `Pin`, `PinManager`, `InputController`(싱글톤), `CameraFollow`, `PhysicsSettleDetector`, `ThrowTransitionController`
- **Scoring** (`Bowling.Scoring`, Unity 비의존 어셈블리 `Bowling.Domain`): `Frame`, `FrameType`, `ScoreCalculator`(정적), `ScoringConstants`(정적), `FrameManager`(MonoBehaviour), `BowlingRuleConfig`(ScriptableObject, `BowlingGame` 네임스페이스 — 어셈블리 주의)
- **UI**: `PowerGaugeUI`, `ScoreboardUI`, **`MainMenuUI`** (신규), **`ResultUI`** (골격만, 신규)
- **Debug**: `DebugResetController` (R 키 → `GameManager.RestartGame()`)
- **Persistence**: `GameRecord`, `SaveData` (TODO)

### 14-3. 씬 상태

- **`Assets/Scenes/Game.unity`** — 메인 게임 씬. 점수판 포함, Play 시 즉시 1프레임 시작.
  - GameObject 10개 (Main Camera, Directional Light, Ground, Lane_Root, BowlingBall, GameManager ⓐ/ⓑ, HUD_Canvas, EventSystem, Canvas)
  - **GameManager 두 개 분리**: ⓐ는 GameStateManager+InputController+DebugResetController, ⓑ는 GameManager+PhysicsSettleDetector+ThrowTransitionController+FrameManager
  - Canvas 자식: `total_score`/`total_score_n`, `current_frame`/`frame_n`, `frame_N_first`, `frame_/`, `frame_N_sec` + `ScoreboardUI` 컴포넌트 부착
- **`Assets/Scenes/mainmenu.unity`** — 메인메뉴 씬 (Build index 0, 시작 씬)
  - 루트 GameObject 6개: Main Camera, Directional Light, Global Volume, **GameModeSelector**, EventSystem (InputSystemUIInputModule), **Canvas** (MainMenuUI 부착)
  - Canvas 자식 3개: `Title` (TMP "Bowling Champion"), `ShortButton` (Button+Image+Label), `FullButton` (Button+Image+Label)
  - MainMenuUI 인스펙터 배선: shortModeRule → ShortModeRule.asset, fullModeRule → FullModeRule.asset, gameSceneName = "Game"

### 14-4. 룰 에셋

- `Assets/Configs/ShortModeRule.asset` — 쇼트 모드 5프레임 ✅ (퍼펙트 75점)
- `Assets/Configs/FullModeRule.asset` — 풀 모드 10프레임 ✅ (퍼펙트 150점)

### 14-5. Build Settings

- index 0: `Assets/Scenes/mainmenu.unity` (시작 씬)
- index 1: `Assets/Scenes/Game.unity`

### 14-6. 다음 작업

**ResultUI 단계 2~5** 재개가 다음 우선순위. M2 (Vertical Slice, 6월 20일) 까지의 critical path 중 마지막 핵심 항목.

- 단계 1 (골격) 완료: `Assets/Scripts/UI/ResultUI.cs` — using, 직렬화 필드 7개, 메서드 스텁
- 단계 2 (구독/해제) → 3 (상수/헬퍼) → 4 (핸들러/콜백) → 5 (씬 배선)
- ResultUI 의 "메인메뉴" 버튼은 이미 작동 가능한 상태 (mainmenu.unity 구성 완료)

자세한 재개 가이드는 **`NEXT_SESSION.md`** 참조.

### 14-7. 참고 문서

| 문서 | 용도 |
|---|---|
| `README.md` (본 문서) | 설계 명세 + 구현 현황 스냅샷 |
| `AI_PROMPT_REFERENCE.md` | AI 협업용 — 컨벤션, 시그니처, 명명 규칙, "건드리지 말 것" 목록 |
| `NEXT_SESSION.md` | 다음 세션 재개 가이드 (ResultUI 작업) |
| `One-Page Concept Sheet.txt` | 게임 컨셉 / 마일스톤 (M1~M5) |
| `PROJECT_FEEDBACK.txt` | (외부 피드백 — 별도 관리) |

---

*Custom Bowling Score System — Design Specification v1.0 / Implementation Snapshot 2026-06-06*
