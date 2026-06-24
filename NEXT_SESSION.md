# 다음 세션 재개 가이드

> **작성일**: 2026-06-25 (WebGL Development Build 첫 산출 + 로컬 검증)

---

## 00000. 2026-06-25 세션 — WebGL Development Build

### 단계 1 — `UNITY_WEBGL` 분기 코드 4건

데스크톱 전용 API 들이 WebGL 런타임에서 실패하거나 의미가 없으므로 컴파일 분기로 안전 처리.

| 파일 | 변경 |
|---|---|
| `Assets/Scripts/Persistence/SaveSystem.cs` | `#if UNITY_WEBGL && !UNITY_EDITOR` 분기로 `File.IO` 대신 `PlayerPrefs.SetString("BowlingGame.SaveData", json)` 사용. WebGL 런타임은 PlayerPrefs 를 브라우저 `localStorage` 에 자동 sync. `StorageExists/ReadAllText/WriteAllText/StorageLocation` 내부 헬퍼로 호출부는 그대로 유지. 로그도 `→ {StorageLocation}` 으로 일반화 |
| `Assets/Scripts/Settings/DisplaySetter.cs` | `ApplyResolution` 의 `Screen.SetResolution` 호출을 WebGL 에서 스킵 + `Debug.Log` 로 안내. 브라우저가 viewport 를 관리하며 ExclusiveFullScreen 은 보안 정책상 직접 호출 불가 |
| `Assets/Scripts/UI/GameOverUI.cs` | `Refresh()` 끝부분에서 WebGL 시 `quitButton.gameObject.SetActive(false)` (브라우저는 페이지를 게임 코드가 닫을 수 없음). `OnQuitClicked` 에도 WebGL 분기 추가 (`Application.Quit()` 무력화 + 경고 로그 — 버튼이 숨겨져 있어야 정상) |
| `Assets/Scripts/UI/SettingsUI.cs` | `BindDisplayTab` 에서 WebGL 시 `resolutionDropdown.interactable = false`, `windowModeDropdown.interactable = false` (UI 스케일 슬라이더는 그대로 동작 — Canvas 만 건드리는 거라 브라우저 무관) |

### 단계 2 — WebGL Module 설치 (사용자 직접)

- Unity Hub → Installs → 6000.4.7f1 → Add Modules → WebGL Build Support 추가
- 사용자가 본 세션 초반에 설치 완료 보고

### 단계 3 — 플랫폼 스위치 + 첫 WebGL 빌드

| 항목 | 결과 |
|---|---|
| `manage_build action=platform target=webgl` | activeBuildTarget = WebGL, 도메인 리로드 60~80초 후 컴파일 종료 (분기 코드 4건 모두 통과, 콘솔 에러 0) |
| `manage_build action=build target=webgl output_path=Build/WebGL development=true` | 비동기 job. 약 **598초 (10분)** 소요 (IL2CPP 트랜스파일 + WebAssembly 빌드 + 셰이더 컴파일). errors=0 warnings=10 (URP 셰이더 스트리핑 정보성) |
| 산출물 | `Build/WebGL/` 총 **128.5 MB** |

**빌드 구조**:
```
Build/WebGL/
├── index.html                         (7.9 KB — 진입점 + Unity 부트스트랩 호출)
├── Build/
│   ├── WebGL.wasm                     (109.9 MB — IL2CPP + 엔진 + 게임 코드, 비압축)
│   ├── WebGL.data                     (17.8 MB — 씬·텍스처·사운드)
│   ├── WebGL.framework.js             (0.8 MB — Unity bootstrap)
│   └── WebGL.loader.js                (60 KB — 초기 로더)
└── TemplateData/                      (<1 MB — 로딩 UI 자산, favicon, progress bar)
```

⚠️ **Development Build 라 압축 OFF** — Release 빌드(`development=false`) 시 Brotli/Gzip 적용으로 `.wasm` ~30 MB, `.data` ~5 MB 로 축소 (총 ~35~40 MB 예상).

### 단계 4 — 로컬 서버 검증

- `cd Build/WebGL && python -m http.server 8080` (PID 17344, Python 3.14.5)
- `http://localhost:8080/` 자동 오픈하여 사용자 검증 진행
- 검증 후 사용자 지시로 서버 종료 (자식 python 프로세스까지 정리)

**WebGL 분기 코드 검증 포인트 (사용자 확인용)**:
1. F12 콘솔 → `[SaveSystem] 저장 완료 — ... → PlayerPrefs[BowlingGame.SaveData]`
2. 게임 종료 화면 → "게임 종료" 버튼 숨김 (메인메뉴 버튼만 표시)
3. 설정 → Display 탭 → 해상도/창모드 드롭다운 회색 비활성, UI 스케일 슬라이더만 작동
4. F12 → Application → Local Storage `localhost:8080` → `BowlingGame.SaveData` 키 (JSON)

---

## 0000. 2026-06-24 세션 종합 — 3단계로 진행

### 단계 1 — Display 탭 완성 + DisplaySetter 신설

| 항목 | 변경 |
|---|---|
| `SaveData` 디스플레이 4필드 | `screenWidth=1920`, `screenHeight=1080`, `fullScreenMode=1`(FullScreenWindow), `uiScale=1.0f` |
| `SaveSystem.NormalizeDisplay` 마이그레이션 | `uiScale==0` sentinel 감지 → 디스플레이 4필드 일괄 기본값 복원 |
| `DisplaySetter.cs` 정적 클래스 신설 | `Assets/Scripts/Settings/`. `GetSupportedResolutions` (중복 제거 + 캐싱) / `ApplyResolution` (Screen.SetResolution 래핑) / `ApplyUIScale` (활성 씬 CanvasScaler 들의 referenceResolution = `1920/scale × 1080/scale`) / `IndexToMode` / `ModeToIndex` |
| `SettingsApplier` 갱신 | `ApplyDisplay(save)` 추가 + `RefreshFromSave` 에 호출 줄. `SceneManager.sceneLoaded` 후크로 씬 전이 시 UI 스케일 재적용 (Single 모드만) |
| `SettingsUI` Display 탭 | SerializeField 4개 (`resolutionDropdown`, `windowModeDropdown`, `uiScaleSlider`, `uiScaleValueLabel`) + `BindDisplayTab` + 3개 핸들러. Audio 탭과 동일한 즉시 적용 + Save 패턴 |
| `settings.unity` DisplayPanel | Placeholder 제거 → VerticalLayoutGroup + 3개 row (Row_Resolution / Row_WindowMode / Row_UIScale). MCP `execute_code` + reflection 으로 `TMPro.TMP_DefaultControls.CreateDropdown` 호출하여 TMP_Dropdown 동적 생성 (TMP_DefaultControls 는 internal class → reflection 필요) |

**Play 모드 검증 통과**: 해상도 드롭다운 24개 자동 채움 (1920×1080 자동 선택), 창모드 3옵션, UI 스케일 1.0→1.2 변경 시 CanvasScaler.referenceResolution 1920×1080 → **1600×900** 정확 갱신 + save.json 영속화.

⚠️ **Editor 한계**: `Screen.SetResolution` 은 Editor Game View 에 영향 없음 — 해상도/창모드의 시각 효과는 Standalone 빌드에서만 확인됨. 이게 단계 2 의 동기.

### 단계 2 — Windows64 Standalone Development Build (첫 빌드 + 재빌드)

| 항목 | 변경 |
|---|---|
| `Assets/Audio/iCertPrintClientSetup.exe` 삭제 | `manage_asset action=delete` — 잘못 들어온 설치 파일 + `.meta` 동반 제거 |
| `.gitignore` 점검 | `/Build/` 이미 포함 — 추가 작업 불필요 |
| `Build/Win64/bowling demo.exe` 생성 | `manage_build action=build target=windows64 output_path="Build/Win64/bowling demo.exe" development=true` — 첫 빌드 약 90초 소요. 총 185 MB |
| 빌드 검증 (사용자 실사용) | 사용자가 .exe 실행 후 정상 작동 확인. 그러나 게임 종료 화면에서 **UI 겹침 버그 발견** → 단계 3 으로 |
| 재빌드 | UI 수정 후 `Build/Win64/` 동일 위치 덮어쓰기. Incremental build 약 9초. 292 파일 갱신 (level0~4, Assembly-CSharp.dll, sharedassets) — `.exe` launcher 자체는 변경 없음 (정상 동작, Unity Player bootstrap) |

**빌드 구조**:
```
Build/Win64/
├── bowling demo.exe              (667 KB — Unity Player bootstrap, 거의 변경 없음)
├── bowling demo_Data/            (게임 자산 + Managed dll. 재빌드 시 갱신)
│   ├── level0~level4             (5개 씬: title, mainmenu, Game, Gameover_scene, settings)
│   ├── Managed/Assembly-CSharp.dll  (게임 코드)
│   ├── sharedassets*.assets       (텍스처/사운드/메시 등)
│   └── ...
├── UnityPlayer.dll                (85 MB — Unity 엔진)
├── UnityCrashHandler64.exe
└── MonoBleedingEdge/              (스크립팅 런타임)
```

**다른 PC 전달 시**: `Build/Win64/` 폴더 전체를 ZIP 으로. `.exe` 만 보내면 실행 불가.

### 단계 3 — Gameover UI 재배치 (사용자 보고 버그 수정)

**근본 원인**: `Gameover_scene.unity` 의 Canvas 가 `CanvasScaler.ConstantPixelSize 800×600` 으로 설정되어 있었음. 다른 씬은 모두 `ScaleWithScreenSize 1920×1080` 통일 (§11-5). 이 불일치로 인해 1920×1080 빌드에서 UI 가 화면 일부에 800×600 비례로 압축되고, 좌표가 가까이 잡혀 있던 요소들이 모두 겹침.

| 변경 | 내용 |
|---|---|
| `Gameover_scene.unity` Canvas | `ConstantPixelSize 800×600` → `ScaleWithScreenSize 1920×1080 match=0.5` (§11-5 통일) |
| 5개 UI 요소 재배치 (anchor=center, pivot=center) | `new_record` y=+370 fs=90 / `gameover_score` y=+100 fs=220 (기존 300 → 축소) / `best_score` y=-140 fs=56 / `Mainmenu_button` y=-300 size=360×90 / `Quit_button` y=-410 size=360×90 |
| 버튼 텍스트 폰트 | 24pt (잘림) → 36pt (90px 높이에 적절) |
| `mainmenu.unity` Title | pos (-100, -150) → (0, -150) 가운데 정렬 보정 |

**시각 검증 통과**: `ScreenCapture.CaptureScreenshot` 으로 신기록 시나리오 캡처 — 신기록!/150/최고 점수: 100/메인메뉴/게임종료 5개 요소 모두 깔끔하게 분리, 겹침 없음.

---

## 00. 2026-06-23 세션 산출물 (타이틀 화면)

| 항목 | 변경 |
|---|---|
| `title.unity` 신규 | Game.unity 복제 후 게임 로직 (GameManager ⓐ/ⓑ, Canvas 점수판, HUD_Canvas) 제거. BowlingBall / Pin 들 Rigidbody.isKinematic. Build Settings idx 0 으로 등록 |
| `TitleScreenController.cs` | 시네마틱 카메라 (CinematicShot[] 인스펙터 조정 가능, 기본 3샷 Pin/Ball/Lane Overview), 페이드 인/아웃, "아무 키나 누르세요" 깜빡임, 버전 라벨, 입력 시 mainmenu 전환 |
| `title.unity` Canvas | TitleCanvas: AnyKeyHint / VersionLabel ("v" + Application.version) / FadePanel (검은 alpha=1 시작, 가장 위 자식) |
| Build Settings | 재배열: 0=title, 1=mainmenu, 2=Game, 3=Gameover_scene, 4=settings (이름 기반 LoadScene 호출은 영향 없음) |
| 입력 감지 | Update 폴링 — Keyboard.anyKey + Mouse 3버튼 + Gamepad 8버튼 |
| 문서 | `AI_PROMPT_REFERENCE.md` §3-10 신설 + 푸터 갱신, 본 파일 갱신 |

**검증 안내** — Play 모드 (idx 0 = title 자동 진입):
1. 검은 화면 → 페이드 인 → 핀 클로즈업 (Cinemachine PinVCam → PinTarget 응시, SmoothStep ease 보간) → 페이드 아웃 → 검은 hold → 페이드 인 → 볼 클로즈업 → ... → Lane Overview → 다시 반복
2. **"Bowling Champion"** 제목 화면 상단 가운데 항상 표시 (페이드와 무관, FadePanel 위)
3. "아무 키나 누르세요" 텍스트 화면 가운데 하단 깜빡임
4. 우측 하단 "v0.1.0" (Application.version 표시)
5. 키보드/마우스/게임패드 어느 버튼이든 누르면 짧은 페이드아웃 후 mainmenu 전환
6. 카메라 좌표/회전이 어색하면: vcam GameObject (PinVCam / BallVCam / LaneVCam) 의 Transform 을 직접 조정하거나, LookAt 더미 (CinematicTargets/PinTarget 등) 위치 조정. endPosition 은 TitleScreenController.Shots 배열에서 조정
7. Scene View 에 각 vcam 의 Gizmo + frustum 시각화됨 — 조정에 활용

---

## 0. 2026-06-23 세션 산출물 (점수판 재제작, 본 세션 1차)

---

## 0. 2026-06-23 세션 산출물 (점수판 재제작)

| 항목 | 변경 |
|---|---|
| `ScoreboardUI` | 이름 유지, 내부 완전 교체. FrameManager 이벤트 수집 + `layout` 추상에 위임만 담당. SerializeField: `frameManager` / `layout` / `totalScoreText` / `currentFrameLabel` |
| `ScoreboardLayoutRenderer` (신규) | 추상 베이스. 6개 추상 메서드 (`Initialize`/`UpdateThrow`/`UpdateFrameComplete`/`SetActiveFrame`/`SetGameOver`/`ClearAll`) |
| `CardLayoutRenderer` (신규) | 옵션 B 구현. cardPrefab Instantiate 로 동적 카드 생성 |
| `FrameCardUI` (신규, prefab) | 한 카드 컴포넌트. `Assets/Prefabs/FrameCard.prefab` |
| `Game.unity` Canvas | 기존 `total_score/current_frame/frame_*` 제거 → `ScoreboardTop`(CardContainer+TotalScorePanel) + `CurrentFrameLabel` |
| 문서 | `AI_PROMPT_REFERENCE.md` §3-5 (ScoreboardUI / LayoutRenderer / FrameCardUI) + §6 / §7 (23·24) + §11-2 + 푸터 갱신 |

**검증 대기**: Play 모드에서 쇼트/풀 양 모드 진입 → 매 투구 카드 갱신 / 프레임 완료 시 누적 점수 / 총점 / 현재 프레임 라벨 정상 표시 확인. 기존 사용자 보고 "점수 계산 정상 안 됨" 의 원인이 UI 였는지 도메인이었는지 이 검증으로 분리 진단됨.

---

## 0-1. 2026-06-22 세션 산출물 (이전, 보관용 요약)
> **목적**: 세션 간 컨텍스트 손실 없이 다음 우선순위를 이어가기 위한 단일 출처.
> **선행 문서**: `AI_PROMPT_REFERENCE.md` §3-5 (입력) / §3-8 (오디오) / §3-9 (설정), `SETTINGS_ROADMAP.md` §10·§11 (진행 기록)

---

## 0. 2026-06-22 세션 산출물 (요약)

| 카테고리 | 산출물 |
|---|---|
| 오디오 시스템 | `AudioManager` 싱글톤 (DontDestroyOnLoad), `MainMixer.mixer` (Master/SFX/BGM Exposed Parameters 3종), `ONHIT.wav` (핀충돌) / `BALL_LAINROLL.wav` (굴림) 배선. `BowlingBall.OnFirstPinContact` / `OnEnteredGutter` 이벤트 + 1회 게이트 |
| 설정 시스템 (1·2차) | `SettingsApplier` 싱글톤 라우터 (DontDestroyOnLoad), `SettingsUI` (탭 5개), **Audio 탭** (Master/SFX/BGM + Mute) + **Controls 탭** (키보드/게임패드 리바인딩 + 기본값 복원). `settings.unity` 신규 (Build Settings idx 3). `SaveData.isMuted` / `inputOverridesJson` 추가 |
| 입력 시스템 | `InputController` 진입점을 mainmenu DontDestroyOnLoad 로 이전 (Awake self-destroy 를 컴포넌트 단위로 변경). binding 2개 추가 (Keyboard/space + Gamepad/buttonSouth — Xbox A / PS Cross / DualSense Cross 자동 지원). 리바인딩 API 공개 |
| 진입 흐름 | mainmenu → "설정" 버튼 → settings 씬 → 메인메뉴 버튼 → mainmenu 복귀 |
| 문서 | `AI_PROMPT_REFERENCE.md` §3-5 / §3-8 / §3-9 신설, `SETTINGS_ROADMAP.md` §10·§11, 본 파일 전면 갱신 |

---

## 1. 다음 작업 우선순위

| # | 항목 | 비고 |
|---|---|---|
| **1** | 🎯 **WebGL Release 빌드 + 배포** | `manage_build target=webgl development=false` 로 압축 적용 빌드 (~35~40 MB 예상). itch.io / 자체 호스팅 / GitHub Pages 등 배포 채널 선택. PlayerPrefs(localStorage) 도메인별 격리되는 점 주의 |
| 2 | **재빌드된 .exe 로 UI 재검증 + 다른 PC 배포 테스트** | `Build/Win64/bowling demo.exe` 실행 후 게임 종료 화면 UI 정상 표시 확인 (2026-06-24 단계 3 수정 검증). Display 탭의 해상도/창모드/UI 스케일 변경이 다른 해상도 화면에서 실제로 반영되는지 함께 확인 |
| 3 | **Play 모드 검증 + 점수 도메인 디버깅** | 새 점수판 UI 가 정상 동작하는지 쇼트/풀 양 모드로 확인. 같은 증상 (점수 계산 이상) 이 남아 있으면 `FrameManager` / `ScoreCalculator` 디버깅 |
| 4 | **`TableLayoutRenderer` (옵션 C)** | 3행 테이블 (Frame / Throws / Score) 레이아웃. 2026-06-23 추상 베이스 답습 — 새 컴포넌트 1개 추가 + 인스펙터에서 layout 교체 |
| 5 | **Accessibility 탭** | Bumper 모드 + 자동 조준 보조. 볼링 차별 항목 |
| 6 | **UX 탭** | 카메라 거리 / 기록 초기화 (모달) / 세이브 폴더 열기 |
| 7 | **튜토리얼 화면** | 형식 미결정 |
| 8 | **FrameManagerTests 리팩토링** | 예외 기대 케이스 → fail-safe 명세 정렬 |
| 9 | **UI 폴리싱** | 슬라이더 핸들 / 버튼 / RebindOverlay / FrameCard 디자인 통일 |
| 10 | **mainmenu 게임종료 버튼 추가 검토** | 현재 mainmenu 에는 쇼트/풀/설정만 — 게임 종료 버튼 없음. UX 일관성 위해 추가 고려 (`Application.Quit()` + WebGL/Editor 분기 처리) |

---

## 2. 재개 절차

### 2-1. 빠른 재개

```
2026-06-22 세션 마무리 됐어. 설정 시스템 다음 탭 채우기로 가자 — Display / Accessibility / Controls / UX 중 어느 것부터?
```

### 2-2. 일반 재개

사용자가 "다음 작업 재개" 라고만 말하면 클로드가:
1. `AI_PROMPT_REFERENCE.md` §3-9 (설정 시스템) + `SETTINGS_ROADMAP.md` §10 (진행 기록 / 다음 우선순위) 다시 읽어 컨텍스트 복원
2. 우선순위 1 (Display 탭) 부터 안내. 사용자가 다른 탭 우선 원하면 그 쪽으로

---

## 3. 절대 잊지 말 것 (재개 시 첫 5분 체크리스트)

- [ ] `AI_PROMPT_REFERENCE.md` §7 (절대 건드리지 말 것) — 특히 20·21번 (`SettingsUI` SerializeField / `SaveData.isMuted` 의미)
- [ ] `AI_PROMPT_REFERENCE.md` §3-8 의 AudioMixer Exposed Parameter 이름 (`MasterVolume`/`SFXVolume`/`BGMVolume`) — 한쪽만 변경 시 SetFloat 가 조용히 실패
- [ ] 새 SaveData 필드 추가 시: JsonUtility 가 누락 필드를 default(T) 로 두므로, `SaveSystem.Load()` 의 `NormalizeVolumes` 옆에 마이그레이션 보정 추가 (필드 기본값이 의미를 갖는 경우만 — bool 은 false 가 자연스러우므로 보정 불필요)
- [ ] 새 카테고리 추가 시: `SettingsApplier.RefreshFromSave` 에 `ApplyXxx(save)` 줄 한 줄 추가 + 해당 메서드 구현
- [ ] 설정 UI 의 새 탭 추가 시: `SettingsUI` 의 Audio 탭 패턴 답습 — SerializeField → BindXxxTab → OnXxxChanged → SaveSystem.Save

---

## 4. 본 세션에서 변경된 파일

**스크립트 (신규/수정)**:
- `Assets/Scripts/Persistence/SaveData.cs` — `isMuted` 추가
- `Assets/Scripts/Audio/AudioManager.cs` — `SetMuted` / `IsMuted` / `last*` 캐싱 + `ApplyEffectiveMixer` / Start 에서 isMuted 적용
- `Assets/Scripts/Settings/SettingsApplier.cs` (신규)
- `Assets/Scripts/UI/SettingsUI.cs` (신규)
- `Assets/Scripts/UI/MainMenuUI.cs` — `settingsButton` / `settingsSceneName` 추가 + 핸들러

**자산**:
- `Assets/Audio/MainMixer.mixer` (이전 세션 작업, 그대로 사용)
- `Assets/Scenes/settings.unity` (신규)

**씬 (수정)**:
- `mainmenu.unity` — `SettingsApplier` GameObject + Canvas 의 `SettingsButton` (ShortButton 복제, y=-300)
- `settings.unity` — 전체 UI 구조

**문서**:
- `AI_PROMPT_REFERENCE.md` — §3-9 신설 + §6 / §7 / §11-3 / 푸터 갱신
- `SETTINGS_ROADMAP.md` — §9 결정 사항 체크 + §10 진행 기록 신설
- 본 파일 — 전면 갱신

---

*이 문서는 다음 우선순위 작업 진입 시 다시 갱신된다.*

*최종 갱신: 2026-06-22 (오디오 + 설정 시스템 1차 — Audio 탭 완성)*

---

# 이전 세션 (2026-06-19) 기록 (보관)

> 아래는 직전 세션의 NEXT_SESSION 본문 — 참고용 보관. M2 마일스톤 critical path 는 모두 해소되었음.

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
