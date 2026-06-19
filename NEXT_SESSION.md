# 다음 세션 재개 가이드

> **작성일**: 2026-06-19 (Phase 8 JSON SaveSystem 검증 완료 + Build Settings 정리 + 레인 비주얼 작업)
> **목적**: 세션 간 컨텍스트 손실 없이 다음 우선순위를 이어가기 위한 단일 출처.
> **선행 문서**: `README.md` §14 (특히 §14-13 SaveSystem, §14-15 레인), `AI_PROMPT_REFERENCE.md` (컨벤션·구조)

---

## 0. 한눈 요약

| 항목 | 값 |
|---|---|
| 직전 완료 작업 (2026-06-19) | ① **Phase 8 SaveSystem 검증 완료** — `.meta` 미생성 원인 진단 후 ImportAsset 으로 복구. Gameover_scene 에 `best_score`/`new_record` 노드 신규 생성·와이어링·씬 저장. 프로그램 검증 7+3 시나리오 PASS. ② **Build Settings 정리** — 루트 `Assets/Game.unity`(stale) 삭제, 인덱스 1을 canonical `Assets/Scenes/Game.unity` 로 교체. ③ **레인 비주얼 변경** (사용자 작업) — `Mat_Lane`을 머티리얼 변형으로 전환 + `Mat_Lane 1/2/3` 신규 + Game.unity 에 `Lane (1)~(7)` 인스턴스 추가 |
| 다음 작업 우선순위 | 1. ✅ 수동 end-to-end 검증 (mainmenu → 쇼트/풀 완주 → Gameover 점수·최고·신기록 → save.json 생성) → 2. 10프레임 동적 점수판 (§14-6 6-b) → 3. 튜토리얼 화면 (§14-6 6-e) → 4. FrameManagerTests 리팩토링 |
| 현재 위치 | **M2 마일스톤 단축 경로 모두 해결됨**. 잔여는 폴리싱 + UX 확장 |
| M2 마일스톤 | 2026-06-20 — 1일 남음. critical path 모두 해소 |

---

## 1. 이번 세션(2026-06-19) 산출물

### 1-1. Phase 8 SaveSystem 검증 완료

**진단**: 디스크엔 `SaveSystem.cs` / `HighScoreService.cs` 가 존재하지만 `.meta` 파일이 누락되어 Unity 가 import 하지 않은 상태였음 (reflection 으로 타입 MISSING 확인). `SaveData.version` 필드, `GameResultHolder.LastBestScore`/`IsNewRecord`, `GameOverUI.bestScoreText`/`newRecordText` 모두 미반영.

**복구**: `AssetDatabase.ImportAsset(path, ForceUpdate)` 명시 호출 → `.meta` 자동 생성 (`bb0ab94f...`, `8b0a1092...`) → 재컴파일 → 7개 타입 모두 OK.

**Gameover_scene 노드 배선** — 멱등 `execute_code`:
- `best_score` (TMP_Text, anchored (0, -65), size (700, 80), 폰트 60, 흰색, "최고 점수: 0")
- `new_record` (TMP_Text, anchored (0, 90), size (700, 100), 폰트 80, 노란색 (1, 0.85, 0.2), "신기록!", 초기 비활성)
- 폰트는 기존 `gameover_score` 의 `NotoSansKR-Black SDF` 차용
- `GameOverUI.bestScoreText` / `newRecordText` 리플렉션 와이어링 + 씬 저장

**프로그램 검증 (7+3 시나리오, 모두 PASS)**:
1. 첫 실행 — `Load()` 가 빈 `SaveData(version=1)` 반환
2. 신기록 — 쇼트 모드 첫 점수 58 → `IsNewRecord=true, BestScore=58`
3. 비신기록 — 쇼트 30 → `IsNewRecord=false, BestScore=58` (직전 최고 유지)
4. 모드 분리 — 쇼트 58 / 풀 100 독립 (`GetBestScore` 각각 정확)
5. Fail-safe — 손상된 JSON 로드 시 예외 전파 없이 빈 데이터 반환
6. 정렬 — `GetHighScores` 점수 내림차순 `[75, 58, 15]`
7. Top 10 제한 — 15회 기록 후 모드당 10개로 trim
   + Holder 데이터 흐름 — `Record → SetResult` 3 시나리오 (신기록/비신기록/갱신)

**Play 모드 Gameover_scene 단독 검증**:
- 5개 SerializeField 모두 정확한 컴포넌트와 와이어링
- `gameover_score.text="0"` / `best_score.text="최고 점수: 0"` / `new_record.activeSelf=false` (HasResult=false 분기 정상)
- 콘솔 에러 0

### 1-2. Build Settings 정리

**문제**: `EditorBuildSettings.asset` 의 index 1 이 `Assets/Game.unity` (루트 중복, 2026-06-07 자) 를 가리키고 있었음. canonical 위치는 `Assets/Scenes/Game.unity` (오늘 세션 작업 + Phase 8 후크 + BallAimer 수정 포함).

**조치** — 옵션 A 적용 (canonical 유지):
1. `manage_build action="scenes"` 로 인덱스 갱신 (0=mainmenu, 1=Scenes/Game, 2=Gameover_scene)
2. `AssetDatabase.SaveAssets()` + `SetDirty` 로 디스크 저장 강제
3. `manage_asset action="delete" path="Assets/Game.unity"` — 루트 중복 + `.meta` 동시 삭제

**검증**:
- `EditorBuildSettings.asset` 디스크 반영 확인 (index 1 GUID `870c14cb56bcc3b4aa6564dad56854fe`)
- `SceneManager.LoadScene("Game")` → `Assets/Scenes/Game.unity` 로 해석됨
- `MainMenuUI.gameSceneName = "Game"` 매핑 정상

**git working tree 결과**:
```
D  Assets/Game.unity
D  Assets/Game.unity.meta
M  ProjectSettings/EditorBuildSettings.asset
```

### 1-3. 레인 비주얼 변경 (사용자 작업)

**머티리얼 (4종, 모두 매트 단색 — Metallic 0, Smoothness 0.5)**:

| 파일 | 상태 | RGB(255) | 색상 | 구조 |
|---|---|---|---|---|
| `Mat_Lane.mat` | 수정 | (151, 200, 122) | 🟢 연두/세이지 | **`Mat_Lane 1` 의 변형(variant)** — 색만 오버라이드 |
| `Mat_Lane 1.mat` | 신규 | (200, 193, 122) | 🟡 베이지/카키 | base material (다른 3개의 부모 잠재 사용) |
| `Mat_Lane 2.mat` | 신규 | (214, 115, 102) | 🔴 살구/오렌지 | standalone |
| `Mat_Lane 3.mat` | 신규 | (200, 169, 122) | 🟤 황토 | standalone (씬 미사용) |

**Game.unity 변경**:
- Lane GameObject: 1개 → 6개로 분할 (`Lane`, `Lane (1)`, `Lane (2)`, `Lane (3)`, `Lane (5)`, `Lane (7)`)
- 머티리얼 분포 (씬 내 참조): Mat_Lane×1, Mat_Lane 1×2, Mat_Lane 2×2, Mat_Lane 3×0
- diff 크기: `+620 / -136 lines`

**의도(추정)**: 단일 회색 톤이던 레인을 다색 구간으로 분할하여 시각적 구분 강화. 머티리얼 변형(Material Variant) 패턴 도입으로 공유 속성(Smoothness/Bump 등) 일괄 조정 가능.

---

## 2. 다음 작업 우선순위 1 — 수동 end-to-end 검증

이번 세션의 모든 변경이 실제 게임 흐름에서 동작하는지 사용자 직접 플레이로 확인.

### 2-1. 검증 시나리오

| 단계 | 기대 결과 |
|---|---|
| mainmenu → 쇼트 또는 풀 버튼 클릭 | `Game` 씬 로드 (Build Settings 정리로 canonical 씬이 로드되어야 함) |
| 5/10프레임 완주 | `[GameOver] 게임 종료!` 로그 + `[HighScore] 기록: ...` 로그 + `[SaveSystem] 저장 완료 — ...` 로그 → Gameover_scene 자동 전환 |
| Gameover_scene 진입 | `gameover_score` = 최종 점수 / `best_score` = "최고 점수: N" / `new_record` 첫 게임이면 표시 |
| save.json 확인 | `%USERPROFILE%/AppData/LocalLow/DefaultCompany/bowling demo/save.json` 생성. JSON 에 `modeName`/`frameCount`/`score`/`playedAt` 기록 |
| 메인메뉴 버튼 | mainmenu 씬 복귀 |
| Quit 버튼 | Editor 에서는 Play 종료, 빌드에서는 `Application.Quit()` |
| 재플레이 (낮은 점수) | `new_record` 비활성, `best_score` 직전값 유지 |
| 재플레이 (높은 점수) | `new_record` 활성, `best_score` 갱신 |
| 모드 분리 | 쇼트 모드 최고점과 풀 모드 최고점이 서로 영향 없음 |

### 2-2. 발견될 가능성 있는 이슈

- 폰트 `NotoSansKR-Black SDF` 의 `<b>` 무효 이슈 — 신기록 강조에 색 위주 사용 (현재 노란색 텍스트, 폰트 80, 적용됨)
- `new_record` 안 보이면 `IsNewRecord` 가 false 라는 뜻 — 이전 save.json 에 더 높은 기록 있을 가능성. `%USERPROFILE%/AppData/LocalLow/DefaultCompany/bowling demo/save.json` 삭제 후 재시도

---

## 3. 다음 작업 우선순위 2~4 — 후속 후보

| # | 항목 | 비고 |
|---|---|---|
| 2 | 10프레임 동적 점수판 (`README §6/§10`) | 현재 5칸 고정. 풀 모드 시 동적 10칸 확장 필요 |
| 3 | 튜토리얼 화면 (§14-6 6-e) | 형식 미결정 — 정적 이미지 vs 인터랙티브 |
| 4 | `FrameManagerTests` 리팩토링 | 예외 기대 케이스 → 새 fail-safe 명세 (`Debug.LogWarning + 무시`) 와 정렬 |
| 5 | 결과 화면 확장 | "퍼펙트!" 강조, 별/이펙트, 베스트 스코어 비교 표 |
| 6 | 폴리싱 (Phase 7) | 효과음/BGM/파티클/스킨 |
| 7 | 접근성 (M4) | TTS, 색각 모드, 자동 보정, 키 리바인딩, 깜빡임 제거 |

---

## 4. 다음 세션 재개 절차

### 4-1. 빠른 재개

```
2026-06-19 세션 마무리 됐어. 수동 end-to-end 검증부터 진행할까,
아니면 다음 우선순위 (10프레임 동적 점수판) 로 갈까?
```

### 4-2. 일반 재개

사용자가 "다음 작업 재개" 라고만 말하면 클로드가:
1. `README.md` §14-6 (우선순위 목록) + §14-13 (SaveSystem 검증 결과) + §14-15 (레인) 다시 읽어 컨텍스트 복원
2. 우선순위 1 (수동 검증) 안내. 사용자 검증 완료 시 우선순위 2 로 이동

---

## 5. 절대 잊지 말 것 (재개 시 첫 5분 체크리스트)

- [ ] `AI_PROMPT_REFERENCE.md` §6 (혼동 위험 이름) 재확인 — 특히 `FrameManager` 가 `Bowling.Scoring` 네임스페이스에 있다는 사실
- [ ] `AI_PROMPT_REFERENCE.md` §7 (절대 건드리지 말 것) 14개 항목 재확인 — 특히 "UI 표시 규칙은 UI 클래스 내부 상수에만 둘 것", "GameManager.Start() ModeSelector 폴백 분기 제거 금지"
- [ ] **Build Settings 는 이제 정리됨** — `Assets/Scenes/Game.unity` 만 사용. 다시 루트에 씬을 저장하지 말 것
- [ ] `GameManager` GameObject 두 개 중 **ⓑ (FrameManager 보유, instanceID 49714)** 쪽에 `frameManager` 가 와이어링되어 있음 — 옮기지 말 것
- [ ] `Mat_Lane` 은 이제 **머티리얼 변형(variant)** — 부모 `Mat_Lane 1` 에서 텍스처/속성을 상속. 색상 외 속성 수정은 부모(`Mat_Lane 1`) 에서

---

## 6. 본 세션에서 변경된 파일

**Phase 8 SaveSystem 검증 / Gameover_scene 배선**:
- `Assets/Scripts/Persistence/SaveSystem.cs` (untracked → tracked, `.meta` 신규)
- `Assets/Scripts/Persistence/HighScoreService.cs` (untracked → tracked, `.meta` 신규)
- `Assets/Scenes/Gameover_scene.unity` (best_score / new_record 노드 추가, GameOverUI 와이어링 갱신)

**Build Settings 정리**:
- `Assets/Game.unity` **삭제** (+ `.meta` 동반)
- `ProjectSettings/EditorBuildSettings.asset` (index 1 GUID 교체)

**레인 비주얼 (사용자 작업)**:
- `Assets/Materials/Mat_Lane.mat` (수정 — 머티리얼 변형으로 전환, 연두색)
- `Assets/Materials/Mat_Lane 1.mat` (신규, 베이지/카키)
- `Assets/Materials/Mat_Lane 2.mat` (신규, 살구)
- `Assets/Materials/Mat_Lane 3.mat` (신규, 황토)
- `Assets/Scenes/Game.unity` (Lane (1)~(7) 인스턴스 추가, 머티리얼 분배)

**이전 세션(2026-06-19 첫 작업) 산출물 (이미 working tree 에 있던 수정)**:
- `Assets/Scripts/Persistence/SaveData.cs` (version 필드)
- `Assets/Scripts/Core/GameResultHolder.cs` (LastBestScore / IsNewRecord)
- `Assets/Scripts/Core/GameManager.cs` (OnEnterGameOver 후크)
- `Assets/Scripts/UI/GameOverUI.cs` (bestScoreText/newRecordText)

**문서**:
- `README.md` §14 갱신 (Phase 8 ✅, Build Settings 정리 기록, 레인 §14-15 신설)
- `AI_PROMPT_REFERENCE.md` (Persistence / ResultUI 항목 갱신, 푸터 날짜)
- `NEXT_SESSION.md` (본 파일 — 전면 갱신)

---

*이 문서는 다음 우선순위 작업 진입 시 다시 갱신된다.*

*최종 갱신: 2026-06-19 (Phase 8 검증 완료 + Build Settings 정리 + 레인 비주얼 작업)*
