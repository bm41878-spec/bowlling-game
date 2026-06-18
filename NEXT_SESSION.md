# 다음 세션 재개 가이드

> **작성일**: 2026-06-19 (이전: 2026-06-18 ResultUI 단계 2~5 완료)
> **목적**: 세션 간 컨텍스트 손실 없이 다음 우선순위를 이어가기 위한 단일 출처.
> **선행 문서**: `README.md` §14 (특히 §14-10 ~ §14-12), `AI_PROMPT_REFERENCE.md` (컨벤션·구조)

---

## 0. 한눈 요약

| 항목 | 값 |
|---|---|
| 직전 완료 작업 (2026-06-19) | **결과 화면 구조 변경** — 오버레이 패널 → 별도 씬 (`Gameover_scene` + `GameOverUI` + `GameResultHolder`). 자동 배선 + EventSystem 추가 + Build Settings index 2 등록 + Play 검증 통과. **BallAimer 리셋 위치 어긋남 수정** (가설 1 확정, Y/Z spawnPoint 차용) |
| 다음 작업 우선순위 | 1. ⚠️ Build Settings / 씬 중복 정리 → 2. ResultUI.cs / Game.unity Canvas 의 ResultUI 컴포넌트 정리 (선택) → 3. JSON SaveSystem → 4. 10프레임 동적 점수판 / 튜토리얼 / 테스트 리팩토링 |
| 현재 위치 | **우선순위 1 (Build Settings 복구) 착수 대기** |
| M2 마일스톤 | 2026-06-20 — 1일 남음. 우선순위 1 만 critical path (결과 화면 자체는 완료) |

---

## 1. 어디까지 왔나 (누적 산출물)

### 1-1. 이전 세션 산출물

- 2026-05-23: `ScoreboardUI.cs` (단계 1~5)
- 2026-06-05: BowlingBall 최적화·물리 안정성·UI 해상도 일관성
- 2026-06-06: 메인메뉴 흐름 6단계 (mainmenu.unity + GameModeSelector + MainMenuUI + FullModeRule + Build Settings)
- 2026-06-07: ResultUI 단계 1 (골격)

### 1-2. 직전 세션 산출물 (2026-06-18)

**ResultUI 단계 2~5 완료** — 오버레이 패널 구조. 자세한 좌표·치수·수정 가이드는 `README.md` §14-10 참조.

### 1-3. 본 세션 산출물 (2026-06-19)

**볼링공 리셋 위치 어긋남 수정** (`Assets/Scripts/Gameplay/BallAimer.cs`):
- 가설 1 (`BallAimer` 의 Z=0.5 강제 스냅 + Y interpolation 잔여값 락) 직격
- `BallSpawnPoint` Transform 을 `Awake` 에서 1회 캐싱 (BowlingBall.cs 와 동일 패턴)
- `Update` 의 Y/Z 차용을 `spawnPoint.position.y`/`.z` 로 변경. 사용자 검증 완료

**결과 화면 구조 변경** — 오버레이 → 별도 씬:
- 신규 `Assets/Scripts/Core/GameResultHolder.cs` — DontDestroyOnLoad 싱글턴. `LastScore` / `LastModeName` / `HasResult` 노출. Lazy `Instance` getter (Gameover_scene 단독 Play 호환)
- 신규 `Assets/Scripts/UI/GameOverUI.cs` — Gameover_scene Canvas 부착. `gameover_score` 표시 + 메인메뉴/Quit 버튼 콜백
- `Assets/Scripts/Core/GameManager.cs` — `using UnityEngine.SceneManagement` 추가, `OnEnterGameOver` 에서 `GameResultHolder.SetResult` + `SceneManager.LoadScene("Gameover_scene")`
- `Assets/Scenes/Gameover_scene.unity` 자동 배선 — Canvas 에 GameOverUI 부착 + 3 필드 와이어링 + EventSystem 추가 (누락 감지) + Build Settings index 2 등록
- 검증: Gameover_scene 단독 Play → `gameover_score.text='0'` (HasResult=false 경로) → Mainmenu 버튼 click → mainmenu 씬 전환 확인

### 1-4. 검증된 시퀀스 (2026-06-19)

**Game 씬 단독 Play** (BallAimer 수정 후):
```
[Scoreboard] / [FrameManager] / [GameManager BeginGame] / [PinManager] / [State] AimingPosition
... 사용자 검증 — 쇼트/풀 모드 양쪽 공 리셋 정확
```

**Gameover_scene 단독 Play**:
```
gameover_score.text='0' (HasResult=false 경로)
Mainmenu_button.onClick.Invoke() → 씬 카운트 1, active=mainmenu
```

**미검증 (수동):**
- 전체 흐름 mainmenu → Game → 5프레임 완주 → OnEnterGameOver → Gameover_scene 자동 전환 → 점수 표시
- Quit 버튼 — Editor 에서는 Play 종료 (자동 검증 시 Editor 정지 유발), 실제 빌드/플레이 시 확인 권장

---

## 2. 우선순위 1 — Build Settings / 씬 중복 정리 ⚠️

### 2-1. 문제 진단

세션 종료 시점 점검에서 발견:

| 항목 | 현재 상태 |
|---|---|
| `Assets/Game.unity` (루트) | **untracked 신규 파일** — 누군가 씬을 루트로 복제 저장 |
| `Assets/Scenes/Game.unity` (canonical) | 정상. **본 세션 ResultUI 배선이 들어간 곳** |
| `EditorBuildSettings.asset` | modified — index 1 이 `Assets/Game.unity` (루트) 를 가리킴 |

**증상**: 빌드 / `SceneManager.LoadScene("Game")` 호출 시 실제로 로드되는 씬은 `Assets/Game.unity` (루트) — ResultUI 가 없는 쪽. 본 세션 검증이 통과한 것은 Editor 의 Play 모드가 "현재 열려 있는 씬" 을 직접 재생했기 때문 (Build Settings 무관). mainmenu → Game 흐름에서는 ResultUI 가 보이지 않을 가능성 높음.

### 2-2. 복구 절차 (권장 — 옵션 A: canonical 위치 유지)

1. Editor 에서 `Assets/Scenes/Game.unity` 열린 상태 유지
2. Project 창에서 `Assets/Game.unity` 및 `Assets/Game.unity.meta` 우클릭 → Delete
3. File > Build Settings 열기 → Scenes In Build 에 `Assets/Scenes/Game.unity` 추가 (drag-drop, index 1 위치)
4. 기존 `Assets/Game.unity` 항목 (deleted, 빨간 표시) 제거
5. Build Settings 닫고 `Ctrl+S` 로 씬 저장 + `Assets > Save Project`
6. `git add EditorBuildSettings.asset Assets/Scenes/Game.unity.meta` + 커밋

### 2-3. 검증

- mainmenu → 쇼트 버튼 → Game 씬 진입 → **`[Result] 초기화 완료` 로그가 떠야 정상** (이전엔 ResultUI 없는 씬이라 로그 없음)
- 5프레임 완주 → ResultPanel 표시 확인

### 2-4. 대안 (옵션 B — 루트 위치 인정)

`Assets/Game.unity` 가 더 최신이라면 `Assets/Scenes/Game.unity` 를 폐기하고 루트를 표준 위치로 인정. 단 `.meta` GUID 충돌 가능성과 README §14-3 (canonical 경로 명시) 갱신 부담이 있어 비권장.

---

## 3. 우선순위 2 — ResultUI end-to-end 수동 검증

### 3-1. 시나리오

mainmenu 에서 시작 → 쇼트 모드 → 5프레임 완주 (스트라이크 1회 + 스페어 1회 포함) → 다음 모두 확인:

| 확인 항목 | 기대 결과 |
|---|---|
| 콘솔에 `[Result] 게임 종료 — 패널 표시 (점수 N, 스트라이크 M회, 스페어 K회)` 로그 출력 | ✓ |
| Canvas 중앙에 검은 반투명 `ResultPanel` 표시 | ✓ |
| `final_score` 에 정확한 최종 점수 (예: `58`) | ✓ |
| `strike_count` 에 `"N회"` 형식 (예: `"1회"`) | ✓ |
| `spare_count` 에 `"M회"` 형식 (0/10 스페어도 카운트됨) | ✓ |
| `restart_button` 클릭 → 패널 숨김 + 1프레임 재시작 + `[Result] 게임 초기화 — 패널 숨김` 로그 | ✓ |
| `main_menu_button` 클릭 → mainmenu 씬 로드 + Canvas 자체 사라짐 | ✓ |

### 3-2. 발견될 가능성 높은 이슈 후보

- `strike_count` / `spare_count` 의 텍스트가 `FormatCount` 결과로 덮어쓰여 **"스트라이크" / "스페어" 라벨이 사라짐**. README §14-10-4 참고 — 라벨 영구 표시하려면 `COUNT_FORMAT` 을 `"스트라이크 {0}회"` 로 바꾸거나 별도 라벨 노드 추가
- ScoreboardUI 의 `gameover_score` 가 ResultPanel 뒤에서 동시 표시될 수 있음 — UX 결정 필요 (배경 불투명화, gameover_score 숨김, 또는 ResultPanel 안으로 통합)
- 버튼 hover/pressed 상태 시각 피드백이 없음 — Button.colors 기본값만 적용된 상태. 필요 시 Inspector 에서 Highlighted/Pressed 색 조정

---

## 4. 우선순위 3 — JSON SaveSystem (Phase 8)

### 4-1. 작업 윤곽 (2026-06-05 검토 시 정의됨)

- `SaveSystem` (static): `Save(SaveData)` / `Load() → SaveData` + 파일 I/O + 예외 처리
- `HighScoreService`: `GameRecord` 생성·정렬·상위 N개 유지·중복 정책
- `GameManager.OnEnterGameOver` 에서 `HighScoreService.Record(...)` 호출 연동
- 저장 경로: `Application.persistentDataPath/save.json`
- 직렬화: JsonUtility (1순위) 또는 Newtonsoft.Json (확장 시 검토)

### 4-2. 사용자 결정 필요한 8개 항목

1. **직렬화 라이브러리** — JsonUtility (기본, 단순) vs Newtonsoft.Json (이미 패키지 설치됨, 유연)
2. **저장 시점** — `HandleGameOver` 직후 즉시 vs `OnRestartClicked`/`OnMainMenuClicked` 시점에 묶어서
3. **highScores 상한** — Top 5 / Top 10 / 무제한
4. **정렬 정책** — 점수 내림차순 (동점 시 날짜 최신순)
5. **첫 실행 처리** — 파일 없을 때 빈 `SaveData` 반환 vs `null` 후 호출자 책임
6. **저장 실패 정책** — 로그만 vs 사용자 알림 vs 백업
7. **백업·복구** — `.json.bak` 회전 vs 단일 파일
8. **세이브 데이터 버전 관리** — `version: 1` 필드 추가 vs 미정

### 4-3. 영향 받을 기존 파일

- `Assets/Scripts/Persistence/SaveData.cs` (스켈레톤만 존재)
- `Assets/Scripts/Persistence/GameRecord.cs` (스켈레톤만 존재)
- 신규: `Assets/Scripts/Persistence/SaveSystem.cs`, `Assets/Scripts/Persistence/HighScoreService.cs`
- `Assets/Scripts/Core/GameManager.cs` — `OnEnterGameOver` 또는 별도 후크에 `HighScoreService.Record(...)` 추가
- (선택) `Assets/Scripts/UI/ResultUI.cs` — "베스트 스코어 갱신!" 강조 표시 시 `HighScoreService` 조회 후 비교

---

## 5. 우선순위 4 — 후속 후보

| 항목 | 비고 |
|---|---|
| 6-b 10프레임 동적 점수판 | README §6/§10 의 "프레임 수에 따라 동적 생성". 풀 모드 시 점수판 5칸 → 10칸 동적 확장 |
| 6-e 튜토리얼 화면 | 형식 미결정 (정적 이미지 vs 인터랙티브) |
| `FrameManagerTests` 리팩토링 | 예외 기대 케이스 → 새 fail-safe 명세 (`Debug.LogWarning + 무시`) 와 정렬 |
| 결과 화면 확장 | "퍼펙트!" 강조, 별/이펙트, 베스트 스코어 비교 (SaveSystem 의존) |
| 접근성 옵션 (M4, 2026-07-31) | TTS, 색각 모드, 자동 보정, 키 리바인딩, 깜빡임 제거 |

---

## 6. 다음 세션 재개 절차

### 6-1. 빠른 재개

```
Build Settings 정리부터 진행해줘.
README §14-10-6 절차 그대로 ✅
```

### 6-2. 일반 재개

사용자가 "다음 작업 재개" 라고만 말하면 클로드가:
1. `README.md` §14-6 (우선순위 목록) + §14-10-6 (Build Settings 진단) 다시 읽어 컨텍스트 복원
2. 우선순위 1 (Build Settings) 부터 안내. 사용자 결정 (옵션 A/B) 후 진행
3. 우선순위 2 (수동 검증) → 3 (JSON SaveSystem) 순차 진행

### 6-3. 진행 방식 변경 시

JSON SaveSystem 을 먼저 하고 싶으면 사용자가 명시. 단 Build Settings 미해결 상태에서 SaveSystem 작업은 의미가 약함 — Game 씬 정상 진입 + GameOver 시점 확정 후 진행하는 것이 자연스러움.

---

## 7. 절대 잊지 말 것 (재개 시 첫 5분 체크리스트)

- [ ] `AI_PROMPT_REFERENCE.md` §6 (혼동 위험 이름) 재확인 — 특히 `FrameManager` 가 `Bowling.Scoring` 네임스페이스에 있다는 사실
- [ ] `AI_PROMPT_REFERENCE.md` §7 (절대 건드리지 말 것) 14개 항목 재확인 — 특히 "UI 표시 규칙은 UI 클래스 내부 상수에만 둘 것", "GameManager.Start() ModeSelector 폴백 분기 제거 금지"
- [ ] Build Settings 가 정리되기 전까지는 **반드시 Editor 에서 `Assets/Scenes/Game.unity` 를 열어둔 상태로 Play** — 아니면 ResultUI 가 안 보임
- [ ] `GameManager` GameObject 두 개 중 **ⓑ (FrameManager 보유, instanceID 49714)** 쪽에 `frameManager` 가 와이어링되어 있음 — 옮기지 말 것
- [ ] 폰트 `NotoSansKR-Black SDF` 의 `<b>` 무효 이슈 — 시각 강조 필요 시 색상/크기 태그 사용

---

## 8. 본 세션에서 변경된 파일 (commit 후보)

- `Assets/Scripts/UI/ResultUI.cs` — 단계 2~4 본문 추가 (구독·헬퍼·핸들러·콜백)
- `Assets/Scenes/Game.unity` — Canvas 에 ResultUI + ResultPanel 트리 (자식 5개) 추가
- `Assets/Scenes/Game.unity.meta` — Unity 자동 갱신
- `README.md` — §14 전반 갱신 (특히 §14-10 신설)
- `NEXT_SESSION.md` — 본 파일 (2026-06-18 우선순위 재정의)

`Assets/Game.unity` (루트, untracked) 와 `EditorBuildSettings.asset` (modified) 는 **본 세션의 변경이 아님** — 이전 세션 잔여물. 우선순위 1 작업 후 함께 정리 권장.

---

*이 문서는 우선순위 1 (Build Settings 복구) 가 완료되면 갱신되며, 우선순위 3 (JSON SaveSystem) 작업 시점에 다시 재구성된다.*

*최종 갱신: 2026-06-18 (ResultUI 단계 2~5 완료 + Build Settings 정리 가이드 + JSON SaveSystem 사전 검토)*
