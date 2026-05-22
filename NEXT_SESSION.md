# 다음 세션 재개 가이드

> **작성일**: 2026-05-23
> **목적**: 세션 간 컨텍스트 손실 없이 ResultUI 작업을 이어가기 위한 단일 출처.
> **선행 문서**: `README.md` §14 (구현 현황), `AI_PROMPT_REFERENCE.md` (컨벤션·구조)

---

## 0. 한눈 요약

| 항목 | 값 |
|---|---|
| 직전 완료 작업 | `ScoreboardUI` 구현 5단계 + Play 모드 검증 |
| 다음 작업 | **`ResultUI`** (결과 화면) 5단계 점진 구현 |
| 현재 위치 | 단계 1 (골격) **추천안 제시 → 사용자 확정 대기** |
| 작업 방식 | 매 단계마다 (1) 클로드 추천안 제시 → (2) 사용자 명세 확정 → (3) 클로드 구현 → (4) 검증 → (5) 다음 단계 |
| 진행 형식 | `ScoreboardUI` 때와 동일한 4단계 + 씬 배선 패턴 |

---

## 1. 어디까지 왔나 (이번 세션 누적 산출물)

### 1-1. 신규 파일
- `Assets/Scripts/UI/ScoreboardUI.cs` — 점수판 UI (현재 프레임 1구/2구/총점). 5개 직렬화 필드, 5개 이벤트 핸들러, 4개 Format 헬퍼, 4개 표시 상수.
- `AI_PROMPT_REFERENCE.md` — 다른 AI 협업용 구조 레퍼런스. 12개 섹션.
- `README.md` §14 — 구현 현황 스냅샷 (본 갱신과 함께).
- `NEXT_SESSION.md` — 본 문서.

### 1-2. 씬 변경
- `Assets/Scenes/Game.unity`: Canvas (instanceID 49910) 에 `BowlingGame.ScoreboardUI` 컴포넌트 추가 + 5개 SerializeField 배선 완료.

### 1-3. 검증
- Play 모드 진입 → 콘솔 3개 로그 정상 순서 발생, 에러·경고 0건:
  ```
  [Scoreboard] 초기화 완료 — FrameManager 이벤트 구독 시작
  [Scoreboard] 게임 초기화 — UI 클리어
  [Scoreboard] 프레임 1 시작 — first/sec 클리어
  ```

### 1-4. 확인된 잔여 이슈
- **`FrameManagerTests`** 일부 케이스(`InvalidOperationException` 기대)가 실제 fail-safe 구현과 불일치 — 테스트 리팩토링 필요. ResultUI 작업과는 독립.
- **폰트**: `NotoSansKR-Black SDF` 는 이미 weight 900 이라 `<b>` 가 시각적 효과 거의 없음. 강조 필요 시 색상/크기 태그 권장.
- **`using System;`** (ScoreboardUI.cs) 미사용 — 정리 후보.

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

| 단계 | 산출물 | 검증 |
|---|---|---|
| **1. 골격** | 클래스, using, 직렬화 필드 7개, 메서드 스텁 | 컴파일 통과 |
| **2. 구독/해제** | Start/OnDestroy/Validate. 이벤트 2개 구독 + 버튼 2개 `onClick.AddListener` (해제도 대칭) | Play 시 `[Result] 초기화 완료` 로그 |
| **3. 상수/헬퍼** | 표시 상수 + `CountStrikes`/`CountSpares`/`FormatCount`/`ShowPanel`/`HidePanel` | 컴파일 통과 |
| **4. 핸들러+콜백** | `HandleGameInitialized`(숨김), `HandleGameOver`(표시+갱신), `OnRestartClicked`, `OnMainMenuClicked` | Play 시작 시 패널 숨김 확인 |
| **5. 씬 배선** | Canvas 하위 `ResultPanel` (TMP 3 + Button 2 + 배경 Image), ResultUI 부착, 7개 필드 할당, Build Settings 에 `mainmenu` 추가 | Play → 5프레임 완주 → 패널 표시 + 점수/스트라이크/스페어 확인 + 버튼 동작 |

각 단계는 한 메시지 안에서 완결.

---

## 3. 현재 위치: 단계 1 (골격) 추천안 (사용자 확정 대기)

세션 종료 시점에 클로드가 아래 추천안을 제시. 다음 세션에서 사용자가 확정하면 그대로 구현.

### 3-1. Using 선언
```csharp
using System;                       // (단계 4까지 미사용 가능 — 종료 시 정리)
using UnityEngine;
using UnityEngine.UI;               // Button
using UnityEngine.SceneManagement;  // SceneManager.LoadScene
using TMPro;                        // TMP_Text
using Bowling.Scoring;              // FrameManager, Frame
```

### 3-2. 클래스 헤더 XML 주석 (한국어)
```csharp
/// <summary>
/// 게임 종료 결과 화면 UI. FrameManager OnGameOver 발생 시 패널을 표시하고
/// 최종 점수 / 스트라이크 횟수 / 스페어 처리 횟수를 갱신한다.
/// </summary>
/// <remarks>
/// 동작:
///   OnGameInitialized → 패널 숨김 (게임 시작/재시작 시점)
///   OnGameOver        → 패널 표시 + 3개 수치 갱신
///   재시작 버튼       → GameManager.Instance.RestartGame()
///   메인메뉴 버튼     → SceneManager.LoadScene("mainmenu")
/// 표시 규칙은 본 클래스에 캡슐화. FrameManager 는 표시 규칙을 모름.
/// </remarks>
```

### 3-3. 직렬화 필드 (Header 4그룹)
```csharp
[Header("References")]
[SerializeField, Tooltip("GameManager 가 들고 있는 동일 FrameManager 인스턴스. ScoreboardUI 와 같은 인스턴스여야 함.")]
private FrameManager frameManager;

[Header("Panel")]
[SerializeField, Tooltip("결과 패널 컨테이너. 본 컴포넌트가 붙은 오브젝트와는 별도 — 본 컴포넌트는 항상 활성 유지, panelRoot 만 SetActive 토글.")]
private GameObject panelRoot;

[Header("Text Targets")]
[SerializeField, Tooltip("Canvas > ResultPanel > final_score")]      private TMP_Text finalScoreText;
[SerializeField, Tooltip("Canvas > ResultPanel > strike_count")]     private TMP_Text strikeCountText;
[SerializeField, Tooltip("Canvas > ResultPanel > spare_count")]      private TMP_Text spareCountText;

[Header("Buttons")]
[SerializeField, Tooltip("재시작 — GameManager.Instance.RestartGame()")]                  private Button restartButton;
[SerializeField, Tooltip("메인메뉴 — SceneManager.LoadScene(\"mainmenu\")")]              private Button mainMenuButton;
```

### 3-4. 메서드 스텁
```csharp
private void Start()      { /* 단계 2 */ }
private void OnDestroy()  { /* 단계 2 */ }

private void HandleGameInitialized() { /* 단계 4 */ }
private void HandleGameOver()        { /* 단계 4 */ }

private void OnRestartClicked()      { /* 단계 4 */ }
private void OnMainMenuClicked()     { /* 단계 4 */ }
```

---

## 4. 의사결정 필요 항목 (단계 1 시작 전 확정)

### A. `panelRoot` 분리 (클로드 추천: ✅ 분리)
ResultUI 컴포넌트는 Canvas 같은 **항상 활성 오브젝트**에 부착하고, `panelRoot` (= `ResultPanel`) 만 `SetActive` 로 토글.
- 이유: 본 컴포넌트가 비활성화되면 `OnDestroy` 시점·이벤트 구독 해제가 깨질 수 있음.
- 권장 씬 구조:
  ```
  Canvas (ResultUI 부착)
  └── ResultPanel (= panelRoot)
      ├── final_score
      ├── strike_count
      ├── spare_count
      ├── restart_button
      └── main_menu_button
  ```

### B. GameManager 참조 방식 (클로드 추천: 싱글톤 직접 접근)
- **싱글톤** (추천): `GameManager.Instance.RestartGame()` 직접 호출. 직렬화 필드 불필요. null 가드만.
- **인스펙터 주입**: 명시적 의존성 가시화. 필드 추가 시 8개로 늘어남.
- 근거: `frameManager` 는 이벤트 구독 핵심 의존이라 명시 주입이 맞지만, `GameManager` 는 콜백 한 곳에서만 호출 — 싱글톤이 적절.

### C. `using System;` 운명 (클로드 추천: 단계 1 에서는 둠, 단계 4 종료 후 미사용이면 제거)

### D. 명명 컨벤션 — Header 그룹, TMP `~Text`, Button `~Button` 접미사, 핸들러 `Handle~` / 버튼 `On~Clicked` (이미 합의)

### E. 씬 노드 이름 (클로드 제안)
- snake_case 일관성 유지: `final_score`, `strike_count`, `spare_count`, `restart_button`, `main_menu_button`, `ResultPanel`

---

## 5. 사전 점검 (단계 5 직전 확인 필요)

- **Build Settings 에 `Assets/Scenes/mainmenu.unity` 등록 여부** — 미등록 시 `SceneManager.LoadScene("mainmenu")` 가 런타임 에러. 미등록이면 `mcp__UnityMCP__manage_build` 도구로 추가.
- **`mainmenu.unity` 내부 상태** — 현재 빈 씬으로 추정. 본 단계는 LoadScene 호출만 보장하면 됨. 메뉴 UI 구현은 후속 단계.

---

## 6. 다음 세션 재개 절차

### 6-1. 빠른 재개 (의사결정 사전 확정)
사용자가 다음 세션 시작 시 아래 형식으로 결정사항을 명시하면 클로드가 바로 단계 1 구현 진입:
```
ResultUI 재개. 결정사항:
A. panelRoot 분리 ✅
B. GameManager 싱글톤 ✅
C. using System; 단계 1 에서는 둠 ✅
D. 명명 컨벤션 그대로 ✅
E. 씬 노드 이름 (snake_case 안) 그대로 ✅
단계 1 진행해줘
```

### 6-2. 일반 재개
사용자가 "ResultUI 작업 재개" 라고만 말하면 클로드가:
1. 본 문서를 다시 읽어 컨텍스트 복원
2. 단계 1 추천안을 다시 제시 (§3 내용)
3. 사용자 확정 대기

### 6-3. 진행 방식 변경 시
사용자가 "이번엔 클로드가 자동 진행" 또는 "한 번에 단계 1~4 다 작성" 등을 요청하면 그에 맞춰 페이스 조정.

---

## 7. 본 단계 완료 이후 후보 (별도 합의 필요)

플랜에 명시된 후속 작업 (우선순위 미정):

1. **결과 화면 확장**: "퍼펙트!" 강조, 별/이펙트, 베스트 스코어 비교
2. **메인 메뉴 흐름**: `mainmenu.unity` ↔ `Game.unity` 씬 전이 + 모드 선택 UI
3. **`FullModeRule.asset` 생성**: 10프레임 모드 활성화
4. **10프레임 동적 점수판**: README §6/§10 의 "프레임 수에 따라 동적 생성" 항목
5. **튜토리얼 화면**: 형식 미결정 (정적 이미지 / 인터랙티브)
6. **JSON SaveSystem** (Phase 8)
7. **FrameManagerTests 리팩토링** (예외 기대 → fail-safe 명세 정렬)
8. **접근성 옵션** (M4, 2026-07-31 목표) — TTS, 색각 모드, 자동 보정, 키 리바인딩, 깜빡임 제거

---

## 8. 절대 잊지 말 것 (재개 시 첫 5분 체크리스트)

- [ ] `AI_PROMPT_REFERENCE.md` §6 (혼동 위험 이름) 재확인 — 특히 `FrameManager` 가 `Bowling.Scoring` 네임스페이스에 있다는 사실
- [ ] `AI_PROMPT_REFERENCE.md` §7 (절대 건드리지 말 것) 12개 항목 재확인 — 특히 "UI 표시 규칙은 UI 클래스 내부 상수에만 둘 것"
- [ ] `ScoreboardUI.cs` 의 4단계 패턴 → ResultUI 도 동일 답습
- [ ] `GameManager` GameObject 두 개 중 **ⓑ (FrameManager 보유)** 쪽에 `frameManager` 인스펙터 연결
- [ ] 폰트 `NotoSansKR-Black SDF` 의 `<b>` 무효 이슈 — ResultUI 의 시각 강조 필요 시 색상/크기 태그 사용

---

*이 문서는 ResultUI 작업이 완료되거나, 다음 작업이 결정되면 갱신/대체된다.*
