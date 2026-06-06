# 다음 세션 재개 가이드

> **작성일**: 2026-06-07 (2026-05-23 최초 작성 / 2026-06-06 메인메뉴·ResultUI 골격 반영)
> **목적**: 세션 간 컨텍스트 손실 없이 ResultUI 작업을 이어가기 위한 단일 출처.
> **선행 문서**: `README.md` §14 (구현 현황), `AI_PROMPT_REFERENCE.md` (컨벤션·구조)

---

## 0. 한눈 요약

| 항목 | 값 |
|---|---|
| 직전 완료 작업 | **ResultUI 단계 1 (골격)** + **메인메뉴 6단계 (A~F)** + Build Settings + 양 모드 Play 검증 |
| 다음 작업 | **`ResultUI` 단계 2~5** 이어가기 |
| 현재 위치 | 단계 2 (구독/해제 + Validate) **착수 대기** |
| 작업 방식 | 매 단계마다 (1) 클로드 추천안 제시 → (2) 사용자 명세 확정 → (3) 클로드 구현 → (4) 검증 → (5) 다음 단계 |
| 진행 형식 | `ScoreboardUI` 때와 동일한 4단계 + 씬 배선 패턴 |

---

## 1. 어디까지 왔나 (누적 산출물)

### 1-1. 이전 세션 산출물 (2026-05-23)
- `Assets/Scripts/UI/ScoreboardUI.cs` — 점수판 UI. Play 검증 완료.
- `AI_PROMPT_REFERENCE.md`, `README.md` §14, `NEXT_SESSION.md` 신규 작성.
- `Assets/Scenes/Game.unity`: ScoreboardUI 배선 완료.

### 1-2. 직전 세션 산출물 (2026-06-06)

**ResultUI 단계 1 (골격) — 완료:**
- `Assets/Scripts/UI/ResultUI.cs` 생성 — using 6개, XML 주석, Header 4그룹 직렬화 필드 7개, 메서드 스텁 6개. 컴파일·Play 검증 통과 (회귀 없음).

**메인메뉴 작업 6단계 (A~F) — 완료:**
- A. `Assets/Configs/FullModeRule.asset` 생성 — 풀 모드 10프레임, 퍼펙트 150점.
- B. `Assets/Scripts/Core/GameModeSelector.cs` — DontDestroyOnLoad 싱글톤. `SelectedRule` 보유, `SelectMode(rule)` 캐시.
- C. `GameManager.Start()` 첫 분기에 ModeSelector 폴백 로직 추가 — 인스펙터 `ruleConfig` 호환 유지.
- D. `Assets/Scripts/UI/MainMenuUI.cs` — 5개 직렬화 필드, 버튼 콜백 → SelectMode → LoadScene("Game").
- E. `Assets/Scenes/mainmenu.unity` 구성 — GameModeSelector + EventSystem + Canvas (Title + ShortButton + FullButton) + MainMenuUI 배선.
- F. Build Settings 등록 (mainmenu=index 0, Game=index 1) + 쇼트/풀 양 모드 Play 검증 통과.

### 1-3. 검증된 시퀀스

**Game.unity 단독 Play** — ScoreboardUI 3개 로그 시퀀스 그대로:
```
[Scoreboard] 초기화 완료 → 게임 초기화 → 프레임 1 시작
```

**mainmenu.unity 경유 (양 모드 공통, 모드명만 다름)**:
```
[MainMenu] 초기화 완료 → [ModeSelector] 모드 선택 → [MainMenu] 씬 전이 → Game
→ [Scoreboard] 초기화 완료 → [GameManager] ModeSelector 로부터 룰 주입
→ [FrameManager] 초기화 완료 → [Scoreboard] 게임 초기화/프레임 1 시작
→ [GameManager] BeginGame → AimingPosition
```

### 1-4. 확인된 잔여 이슈
- **`FrameManagerTests`** 일부 케이스(`InvalidOperationException` 기대)가 실제 fail-safe 구현과 불일치 — 테스트 리팩토링 필요. ResultUI 작업과는 독립.
- **폰트**: `NotoSansKR-Black SDF` 는 이미 weight 900 이라 `<b>` 가 시각적 효과 거의 없음. 강조 필요 시 색상/크기 태그 권장.
- **`using System;`** (ScoreboardUI.cs / ResultUI.cs) 미사용 — 정리 후보 (ResultUI 단계 4 종료 시점에 판단).

---

## 2. 다음 작업: ResultUI 구현

### 2-1. 작업 동기
- README §6 Phase 6 의 "결과 화면" 항목 — Vertical Slice (M2, 2026-06-20, 약 4주) 도달을 위한 critical path.
- 현재 게임 종료 시 사용자는 자기 점수를 화면에서 확인할 수 없음 (콘솔 로그만 존재). 명백한 UX 갭.

### 2-2. 사용자가 확정한 요구사항
- **표시 내용** (3개 수치):
  - 최종 점수 (예: `58`)
  - 스트라이크 횟수 (예: `2회`)
  - 스페어 처리 횟수 (예: `2회` — `Frame.IsSpare()` 기준이므로 0/10 스페어도 포함)
- **버튼** (2개):
  - 재시작 → `GameManager.Instance.RestartGame()`
  - 메인메뉴 → `SceneManager.LoadScene("mainmenu")`

### 2-3. 컴포넌트 설계 (합의된 요지)

| 항목 | 값 |
|---|---|
| 네임스페이스 | `BowlingGame` |
| 어셈블리 | `Assembly-CSharp` |
| 파일 경로 | `Assets/Scripts/UI/ResultUI.cs` |
| 로그 prefix | `[Result]` |
| 직렬화 필드 (7개) | `frameManager`(FrameManager), `panelRoot`(GameObject), `finalScoreText`/`strikeCountText`/`spareCountText`(TMP_Text), `restartButton`/`mainMenuButton`(Button) |
| 표시 상수 | `COUNT_FORMAT = "{0}회"`, `MAIN_MENU_SCENE_NAME = "mainmenu"` |
| 헬퍼 (순수 함수) | `CountStrikes()`, `CountSpares()`, `FormatCount(int)` |
| 사이드이펙트 헬퍼 | `ShowPanel()`, `HidePanel()` |
| 구독 이벤트 | `OnGameInitialized` → 숨김 / `OnGameOver` → 표시 + 3개 갱신 |
| 버튼 콜백 | `OnRestartClicked`, `OnMainMenuClicked` |

### 2-4. 5단계 점진 개발 계획

| 단계 | 산출물 | 검증 | 상태 |
|---|---|---|---|
| **1. 골격** | 클래스, using, 직렬화 필드 7개, 메서드 스텁 | 컴파일 통과 | ✅ 완료 (2026-06-06) |
| **2. 구독/해제** | Start/OnDestroy/Validate. 이벤트 2개 구독 + 버튼 2개 `onClick.AddListener` (해제도 대칭) | Play 시 `[Result] 초기화 완료` 로그 | ⏳ **착수 대기** |
| **3. 상수/헬퍼** | 표시 상수 + `CountStrikes`/`CountSpares`/`FormatCount`/`ShowPanel`/`HidePanel` | 컴파일 통과 | ⏳ |
| **4. 핸들러+콜백** | `HandleGameInitialized`(숨김), `HandleGameOver`(표시+갱신), `OnRestartClicked`, `OnMainMenuClicked` | Play 시작 시 패널 숨김 확인 | ⏳ |
| **5. 씬 배선** | Canvas 하위 `ResultPanel` (TMP 3 + Button 2 + 배경 Image), ResultUI 부착, 7개 필드 할당 | Play → 5프레임 완주 → 패널 표시 + 점수/스트라이크/스페어 확인 + 버튼 동작 | ⏳ |

각 단계는 한 메시지 안에서 완결.

> Build Settings 의 `mainmenu` 등록은 메인메뉴 작업에서 이미 완료됨 — 단계 5 사전 점검에서 제거 가능.

---

## 3. 현재 위치: 단계 2 (구독/해제 + Validate) 착수 대기

단계 1 (골격) 완료. 다음은 ScoreboardUI 의 단계 2 패턴 답습.

### 3-1. 단계 2 추천 골자 (사용자 확정 후 클로드가 그대로 작성)

**Start():**
```csharp
private void Start()
{
    if (!Validate()) { enabled = false; return; }

    frameManager.OnGameInitialized += HandleGameInitialized;
    frameManager.OnGameOver        += HandleGameOver;

    restartButton.onClick.AddListener(OnRestartClicked);
    mainMenuButton.onClick.AddListener(OnMainMenuClicked);

    // 초기 상태: 패널 숨김 (HidePanel 은 단계 3 에서 추가 — 단계 2 에선 panelRoot.SetActive(false) 직접 호출)
    panelRoot.SetActive(false);

    Debug.Log("[Result] 초기화 완료 — FrameManager 이벤트 + 버튼 콜백 구독");
}
```

**OnDestroy():**
```csharp
private void OnDestroy()
{
    if (frameManager != null)
    {
        frameManager.OnGameInitialized -= HandleGameInitialized;
        frameManager.OnGameOver        -= HandleGameOver;
    }
    if (restartButton  != null) restartButton.onClick.RemoveListener(OnRestartClicked);
    if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
}
```

**Validate():** ScoreboardUI 와 동일 패턴 — 7개 필드 각각 null 체크 + `[Result]` prefix 에러 로그.

### 3-2. 단계 2 의사결정 후보
- **A. `panelRoot.SetActive(false)` 를 Start 에서 직접 호출할지, 단계 3 의 `HidePanel()` 헬퍼로 미룰지** — 클로드 추천: 단계 2 에선 직접 호출, 단계 3 에서 헬퍼로 리팩토링.
- **B. `[Result] 초기화 완료` 로그 메시지 문구** — ScoreboardUI 패턴 답습.

### 3-3. 단계 2 검증
- Play (mainmenu) → 쇼트 또는 풀 버튼 클릭 → Game 씬 진입 → 콘솔에 `[Result] 초기화 완료 — ...` 로그 확인.
- ResultUI 가 부착될 GameObject 는 단계 5 에서 결정 — 단계 2~4 동안은 Game.unity 의 Canvas 에 임시 부착하거나 단순히 컴파일만 검증.

---

## 4. 단계 1 시 합의된 결정사항 (단계 2 이후에도 유지)

| 항목 | 결정 |
|---|---|
| A. `panelRoot` 분리 | ✅ 분리. 본 컴포넌트는 Canvas 같은 항상 활성 오브젝트에 부착 |
| B. GameManager 참조 방식 | ✅ 싱글톤 (`GameManager.Instance.RestartGame()`). 직렬화 필드 추가 안 함 |
| C. `using System;` | 단계 1 에서는 둠. 단계 4 종료 후 미사용이면 제거 |
| D. 명명 컨벤션 | Header 그룹, TMP `~Text`, Button `~Button` 접미사, 핸들러 `Handle~` / 버튼 `On~Clicked` |
| E. 씬 노드 이름 | snake_case: `final_score`, `strike_count`, `spare_count`, `restart_button`, `main_menu_button`, `ResultPanel` |

권장 씬 구조 (단계 5 작업):
```
Canvas (ResultUI 부착)
└── ResultPanel (= panelRoot)
    ├── final_score
    ├── strike_count
    ├── spare_count
    ├── restart_button
    └── main_menu_button
```

---

## 5. 사전 점검 (단계 5 직전 확인)

- ~~Build Settings 에 `mainmenu` 등록~~ — ✅ **이미 완료** (메인메뉴 작업 F 단계에서 mainmenu=index 0, Game=index 1 등록).
- `mainmenu.unity` 내부 상태 — ✅ MainMenuUI / GameModeSelector / 2개 버튼 모두 구성 완료. ResultUI 의 "메인메뉴" 버튼이 `SceneManager.LoadScene("mainmenu")` 호출 시 실제 동작 검증 가능.

---

## 6. 다음 세션 재개 절차

### 6-1. 빠른 재개 (단계 2 바로 진입)
```
ResultUI 단계 2 진행해줘.
의사결정 A,B 클로드 추천안 그대로 ✅
```

### 6-2. 일반 재개
사용자가 "ResultUI 작업 재개" 라고만 말하면 클로드가:
1. 본 문서를 다시 읽어 컨텍스트 복원
2. 단계 2 추천안을 다시 제시 (§3 내용)
3. 사용자 확정 대기

### 6-3. 진행 방식 변경 시
사용자가 "단계 2~5 한 번에 다 작성" 또는 "자동 진행" 등을 요청하면 그에 맞춰 페이스 조정.

---

## 7. ResultUI 완료 이후 후보 (별도 합의 필요)

플랜에 명시된 후속 작업 (우선순위 미정):

1. **결과 화면 확장**: "퍼펙트!" 강조, 별/이펙트, 베스트 스코어 비교
2. ~~메인 메뉴 흐름: `mainmenu.unity` ↔ `Game.unity` 씬 전이 + 모드 선택 UI~~ — ✅ **완료 (2026-06-06)**
3. ~~`FullModeRule.asset` 생성: 10프레임 모드 활성화~~ — ✅ **완료 (2026-06-06)**
4. **10프레임 동적 점수판**: README §6/§10 의 "프레임 수에 따라 동적 생성" 항목
5. **튜토리얼 화면**: 형식 미결정 (정적 이미지 / 인터랙티브)
6. **JSON SaveSystem** (Phase 8)
7. **FrameManagerTests 리팩토링** (예외 기대 → fail-safe 명세 정렬)
8. **접근성 옵션** (M4, 2026-07-31 목표) — TTS, 색각 모드, 자동 보정, 키 리바인딩, 깜빡임 제거

---

## 8. 절대 잊지 말 것 (재개 시 첫 5분 체크리스트)

- [ ] `AI_PROMPT_REFERENCE.md` §6 (혼동 위험 이름) 재확인 — 특히 `FrameManager` 가 `Bowling.Scoring` 네임스페이스에 있다는 사실
- [ ] `AI_PROMPT_REFERENCE.md` §7 (절대 건드리지 말 것) 14개 항목 재확인 — 특히 "UI 표시 규칙은 UI 클래스 내부 상수에만 둘 것", "GameManager.Start() ModeSelector 폴백 분기 제거 금지"
- [ ] `ScoreboardUI.cs` 의 4단계 패턴 → ResultUI 도 동일 답습
- [ ] `GameManager` GameObject 두 개 중 **ⓑ (FrameManager 보유)** 쪽에 `frameManager` 인스펙터 연결
- [ ] 폰트 `NotoSansKR-Black SDF` 의 `<b>` 무효 이슈 — ResultUI 의 시각 강조 필요 시 색상/크기 태그 사용
- [ ] 메인메뉴 경유로 게임에 진입할 때 `[GameManager] ModeSelector 로부터 룰 주입: ...` 로그가 떠야 정상 (AI_PROMPT_REFERENCE.md §12-2)

---

*이 문서는 ResultUI 작업이 완료되거나, 다음 작업이 결정되면 갱신/대체된다.*

*최종 갱신: 2026-06-07 (단계 1 완료 + 메인메뉴 작업 완료 반영, 단계 2 진입 가이드로 전환)*
