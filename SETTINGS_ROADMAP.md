# 사용자 설정 백로그 (Settings Roadmap)

> **작성일**: 2026-06-22
> **목적**: Bowling Champion 에 추가할 만한 사용자 설정 항목들을 카테고리·구현 비용·우선순위로 정리한 단일 출처.
> **선행 문서**: `AI_PROMPT_REFERENCE.md` §3-8 (오디오 인프라), `NEXT_SESSION.md` (다음 작업 우선순위)
> **범위 가정**: 볼링 (캐주얼) + 1인 PC Windows Standalone 환경

---

## 0. 한눈 요약

| 카테고리 | 핵심 항목 | 현재 상태 |
|---|---|---|
| 오디오 | Master/SFX/BGM 슬라이더 | **인프라 완성** — UI 만 만들면 즉시 동작 (`AudioManager` + `MainMixer.mixer` Exposed Parameters) |
| 디스플레이 | 해상도, 풀스크린, V-Sync, FPS 제한 | 미구현 — PC 게임 위생 항목 |
| 조작 | 키 리바인딩, 마우스/패드 지원 | 미구현 — 현재 `<Keyboard>/space` 하드코딩. M4 마일스톤 |
| 접근성 | 거터 방지, 자동 조준, 색각, TTS 등 | 미구현 — M4 마일스톤 |
| 게임플레이 / UX | 카메라 조정, 가이드라인, UI 스케일, 기록 초기화 | 미구현 |
| 큰 작업 | 다국어, 클라우드 세이브 | 본 프로젝트 범위 초과 |

---

## 1. 🎵 오디오 — 즉시 가능 (인프라 완성)

| 항목 | 구현 비용 | 비고 |
|---|---|---|
| Master / SFX / BGM 슬라이더 | **30분** | `AudioMixer` Exposed Parameters (`MasterVolume`/`SFXVolume`/`BGMVolume`) 이미 있음. `AudioManager.SetMasterVolume(float)` 등 공개 API 호출 → `SaveData.masterVolume/sfxVolume/bgmVolume` 갱신 + `SaveSystem.Save` |
| 음소거 토글 | 5분 | mute 플래그 + 슬라이더 0 매핑. **현재 0=미마이그레이션 으로 처리되므로** (`SaveSystem.NormalizeVolumes`) 의도적 음소거는 별도 `bool isMuted` 필드를 추가해 분리 권장 |

**핵심 의존**:
- `AudioManager.cs` — `SetMasterVolume(float)`, `SetSFXVolume(float)`, `SetBGMVolume(float)` 이미 노출
- `SaveData.cs` — `masterVolume`/`sfxVolume`/`bgmVolume` 필드 이미 추가
- `MainMixer.mixer` — Exposed Parameters 이미 노출

---

## 2. 🖥️ 디스플레이 — 필수 (PC 게임의 기본)

| 항목 | 구현 비용 | API | 비고 |
|---|---|---|---|
| 해상도 변경 | 1시간 | `Screen.SetResolution(w, h, mode)` | 일반적으로 720p / 1080p / 1440p / 2160p 드롭다운. `Screen.resolutions` 로 디바이스 지원 목록 조회 |
| 풀스크린 / 창모드 / 보더리스 | 30분 | `Screen.fullScreenMode` | `ExclusiveFullScreen` / `Windowed` / `FullScreenWindow`(보더리스) 3가지 |
| V-Sync on/off | 5분 | `QualitySettings.vSyncCount = 0 or 1` | |
| FPS 제한 (60/120/무제한) | 5분 | `Application.targetFrameRate` | 무제한 = `-1` |
| 그래픽 품질 (Low/Med/High) | 30분 | `QualitySettings.SetQualityLevel(int)` + URP 자산 변경 | 현재 URP 단일 자산. 품질별 자산 추가 필요 |

---

## 3. 🎮 조작 — M4 마일스톤 (현재 스페이스바 하드코딩)

| 항목 | 구현 비용 | 비고 |
|---|---|---|
| 키 리바인딩 (`<Keyboard>/space` → 임의 키) | **반나절** | Unity Input System 의 `RebindingOperation` API. 공식 샘플 `Library/PackageCache/com.unity.inputsystem@.../Samples~/RebindingUI/` 존재 — 거의 그대로 답습 가능. 바인딩 결과는 `InputActionAsset` 의 override JSON 으로 저장하여 `SaveData.inputOverridesJson` 필드 신설 권장 |
| 마우스 클릭 대안 | 1시간 | `InputController` 의 InputAction 에 `<Mouse>/leftButton` 바인딩 추가만 |
| 게임패드 지원 | 1시간 | `<Gamepad>/buttonSouth` 추가. PS/Xbox 자동 |

**현재 코드 영향**:
- `InputController.cs` — `<Keyboard>/space` 하드코딩 부분을 InputActionReference 로 교체
- `AI_PROMPT_REFERENCE.md §3-5 InputController` 갱신 필요

---

## 4. ♿ 접근성 — M4 마일스톤

| 항목 | 구현 비용 | 비고 |
|---|---|---|
| 거터 방지 (Bumper 모드) | 1시간 | 거터 영역(`|x| > 0.533`)에 벽 콜라이더 활성화 토글. 아동/초보용. **볼링 특성상 가장 효과적인 접근성 옵션** |
| 자동 조준 보조 (어시스트 강도) | 반나절 | `BallAimer.oscSpeed` 감소 + 발사 시 거터 회피 보정. 강도 슬라이더 (0~100%) |
| 깜빡임/모션 감소 (Reduced Motion) | 30분 | 신기록 효과·UI 트랜지션의 bool 토글. iOS/Android 의 "Reduce Motion" 설정과 동일 컨셉 |
| 색각 모드 (Deuteranopia / Protanopia / Tritanopia) | 반나절 | URP Volume 에 ColorAdjustments / ChannelMixer 매핑. 본 프로젝트는 점수 강조색이 노랑↔흰 정도라 영향은 작지만 표준 항목 |
| TTS (점수 읽어주기) | **1~2일** | 한국어 TTS 라이브러리 의존성 (Microsoft Speech / Google Cloud TTS / RHVoice). 비용 큼 — **후순위** |

---

## 5. 🎥 게임플레이 / UX

| 항목 | 구현 비용 | 비고 |
|---|---|---|
| 카메라 거리·각도 조정 | 30분 | `CameraFollow.stopOffsetFromHeadPin` 인스펙터 값을 런타임 슬라이더로 노출 |
| 가이드라인 (조준선 표시) | 1시간 | `BallAimer` 에 LineRenderer 추가. 거터 방지와 함께 초보 모드로 묶기 좋음 |
| UI 스케일 (90~110%) | 30분 | 모든 Canvas 의 `CanvasScaler.matchWidthOrHeight` 미세 조정. 해상도 독립성은 §11-5 통일 설정으로 이미 확보됨 |
| 기록 초기화 ("최고점 리셋") | 10분 | `SaveSystem.Save(new SaveData())` 한 줄. 확인 다이얼로그 권장 |
| 세이브 파일 폴더 열기 | 5분 | `Application.OpenURL(System.IO.Path.GetDirectoryName(SaveSystem.FilePath))` |

---

## 6. 🌐 큰 작업 (이 프로젝트 범위 초과)

| 항목 | 구현 비용 | 비고 |
|---|---|---|
| 다국어 (한국어/영어) | **며칠~일주일** | Unity Localization 패키지 도입 + 모든 한글 문자열 외부화. 로그 메시지는 한국어 유지 권장 (개발자용) |
| 클라우드 세이브 | **일주일~** | Steam Cloud 등. 배포 플랫폼 결정 후 |

---

## 7. 💡 추천 진행 순서

볼링 게임 (캐주얼) + 1인 PC 환경 기준 가성비 순:

1. **오디오 슬라이더 3개** (30분) — 인프라 완성됨, 즉시 효과 체감
2. **해상도 + 풀스크린 + V-Sync** (1~2시간) — PC 게임의 위생 항목
3. **거터 방지 (Bumper) + 자동 조준 보조** (반나절) — 볼링만의 차별 접근성
4. **키 리바인딩** (반나절) — M4 마일스톤이라 어차피 필요
5. **카메라 조정 + 가이드라인** (1시간) — 폴리싱
6. **그 외 접근성 (색각 / 모션 감소 / TTS)** — Phase 7~M4

---

## 8. 🏗️ 설정 화면 구조 제안

### 8-1. 진입점
- **별도 `settings.unity` 씬보다 `mainmenu.unity` 오버레이 패널 추천**
  - DontDestroyOnLoad 진입점들(`GameModeSelector`, `AudioManager`)이 mainmenu 에 모여 있어 일관성 ↑
  - 게임 중(`Game.unity`)에서도 동일 패널을 호출할 수 있어야 함 → 패널 자체를 Prefab 으로 만들고 mainmenu 와 Game 양쪽에서 인스턴스 보유, 또는 DontDestroyOnLoad 의 SettingsCanvas 로 분리

### 8-2. 저장 모델
- **모든 설정값을 `SaveData` 에 통합** — JSON 한 파일로 일관성
- 이미 `SaveSystem.NormalizeVolumes` 마이그레이션 패턴이 있으므로 필드 추가 시 자동 호환
- 신설 권장 필드 (현재 미존재):
  ```
  // 디스플레이
  int  screenWidth = 1920;
  int  screenHeight = 1080;
  int  fullScreenMode = 1;     // 0=ExclusiveFullScreen, 1=FullScreenWindow, 3=Windowed
  int  vSyncCount = 1;
  int  targetFrameRate = 60;
  int  qualityLevel = 2;
  // 접근성
  bool bumperMode = false;
  float aimAssist = 0f;        // 0~1
  bool reducedMotion = false;
  int  colorBlindMode = 0;     // 0=Off, 1~3
  // 조작
  string inputOverridesJson = ""; // InputActionAsset.SaveBindingOverridesAsJson
  // UI
  float uiScale = 1.0f;
  // 오디오 (이미 존재)
  bool isMuted = false;        // 의도적 음소거 — 0f 음량과 구분 필요
  ```

### 8-3. 단일 적용자 (`SettingsApplier`)
- **`SettingsApplier` 같은 단일 컴포넌트**로 `Awake` 시 SaveData 읽어 mixer/screen/quality/input override 등에 일괄 적용
- 현재 `AudioManager.Start` 가 음량만 처리하는 패턴을 확장 — 또는 AudioManager 의 역할을 그대로 두고 별도 `SettingsApplier` 가 디스플레이·접근성·입력을 담당
- 진입점: mainmenu.unity 의 새 GameObject `SettingsApplier` + DontDestroyOnLoad

### 8-4. UI 구성 (참고)
```
SettingsPanel (CanvasGroup, 평소 alpha=0)
├── Tab_Audio
│   ├── Slider_Master   (0~1)
│   ├── Slider_SFX      (0~1)
│   ├── Slider_BGM      (0~1)
│   └── Toggle_Mute
├── Tab_Display
│   ├── Dropdown_Resolution
│   ├── Dropdown_FullScreen
│   ├── Toggle_VSync
│   └── Dropdown_FPSLimit
├── Tab_Controls
│   ├── Button_Rebind_Confirm   (현재 키 표시 + 클릭 시 리바인딩)
│   └── ...
├── Tab_Accessibility
│   ├── Toggle_Bumper
│   ├── Slider_AimAssist
│   ├── Toggle_ReducedMotion
│   └── Dropdown_ColorBlind
└── Footer
    ├── Button_ResetHighScores
    ├── Button_OpenSaveFolder
    └── Button_Apply / Button_Cancel
```

---

## 9. 결정 대기 항목 (이 문서 갱신 트리거)

- [x] **무엇부터 진행할지** — 2026-06-22: 오디오부터 (Backend + UI 골격 + 오디오 탭 완성)
- [x] **음소거 표현** — 2026-06-22: `bool isMuted` 신설 채택. `SaveData.isMuted` 필드 + `AudioManager.SetMuted(bool)` API + UI Toggle 와이어링 완료
- [x] **설정 화면 진입점** — 2026-06-22: 별도 `settings.unity` 씬 채택 (Build Settings idx 3). Game 중 ESC 호출은 별도 작업
- [ ] **다국어 도입 여부** — 도입 시 모든 한글 문자열 외부화 작업 별도 마일스톤
- [ ] **TTS 도입 여부** — 한국어 TTS 의존성 (외부 SDK / 라이센스)
- [ ] **다음 탭은 무엇부터** — Display (해상도/풀스크린/V-Sync/FPS) 가 가장 가성비. 또는 Accessibility (Bumper 모드 / 자동 조준 보조) 가 볼링 차별점

---

## 10. 진행 기록 (2026-06-22 세션 1차)

**완료**:
- `SaveData.isMuted` 필드 추가 (bool, 기본 false — 마이그레이션 불필요)
- `AudioManager.SetMuted(bool)` + `IsMuted` 프로퍼티. `last{Master,Sfx,Bgm}Volume` 캐싱으로 mute 토글 시 즉시 복원/차단
- `SettingsApplier.cs` 신규 — mainmenu DontDestroyOnLoad 진입점, `RefreshFromSave()` 공개 API
- `settings.unity` 신규 (Build Settings idx 3) — Camera / Light / EventSystem / Canvas + SettingsPanel
- `SettingsUI.cs` 신규 — 탭 5개 (Audio/Display/Controls/Accessibility/UX), Audio 탭 완성 (Master/SFX/BGM 슬라이더 + Mute 토글 + 값 라벨 + Back 버튼)
- mainmenu Canvas 에 `SettingsButton` 신설 (ShortButton 복제, y=-300) + `MainMenuUI.settingsButton` 와이어링
- `AI_PROMPT_REFERENCE.md` §3-9 Settings 신설 + §6 / §7 (20·21번) / §11-3 갱신

## 11. 진행 기록 (2026-06-22 세션 2차 — Controls 탭)

**완료**:
- `SaveData.inputOverridesJson` 추가 (string, null/empty = 기본 binding)
- `InputController` 리팩토링 — binding 2개 (`[0]=<Keyboard>/space`, `[1]=<Gamepad>/buttonSouth`). 공개 API: `ConfirmAction`, `SaveBindingOverridesJson`, `LoadBindingOverridesJson`, `ResetAllBindingsToDefault`, `ResetBindingToDefault(int)`, `GetBindingDisplayString(int)`. 상수: `KeyboardBindingIndex=0`, `GamepadBindingIndex=1`. Start 에서 SaveData 자동 적용.
- `SettingsApplier.ApplyInput` 추가 — `RefreshFromSave` 에 호출 줄 추가
- `SettingsUI` — Controls 탭 SerializeField 8개 + 핸들러 + `_activeRebind` 라이프사이클 관리 + OnDestroy 누수 방지
- `settings.unity` ControlsPanel 4행 (Row_Keyboard / Row_Gamepad / Row_Device / Row_Reset) + RebindOverlay (어두운 입력 차단 패널 + "키를 누르세요" 라벨)
- 게임패드: Unity Input System `Gamepad` 표준으로 DualSense / DualShock / Xbox 자동 동시 지원 (별도 활성 토글 없음)
- 리바인딩 디바이스 필터: 키보드 리바인딩은 `<Gamepad>` 제외, 게임패드 리바인딩은 `<Keyboard>`/`<Mouse>` 제외. ESC 로 취소
- 문서 갱신 — `AI_PROMPT_REFERENCE.md` §3-5 (InputController) / §3-9 Settings (Controls 탭) / §7 (22번) / §11 settings.unity 구조

**다음 우선순위 (다음 세션)**:
1. **Display 탭** — 해상도 / 풀스크린 / V-Sync / FPS 제한. `SaveData` 에 5개 필드 추가 + `SettingsApplier.ApplyDisplay`
2. **Accessibility 탭** — Bumper 모드 + 자동 조준 보조 (볼링 차별 항목)
3. **UX 탭** — 카메라 거리 / UI 스케일 / 기록 초기화 / 세이브 폴더 열기

**미해결 / 검증 필요**:
- 게임패드 입력은 Game.unity 에 들어가야 InputController 가 생성되므로 settings 씬에선 "연결된 컨트롤러" 라벨 표시는 가능하나 게임패드 리바인딩 실제 입력 캡처는 Game 씬에서 검증 필요. 다만 settings 진입 시 InputController 가 mainmenu/settings 에는 없으므로 리바인딩 자체가 안 됨 — **해결책 후보**: (a) InputController 를 mainmenu 부터 DontDestroyOnLoad 로 진입, 또는 (b) settings 씬에 별도 InputController 인스턴스 일시 생성
- 슬라이더 핸들 / 토글 / 버튼 시각적 디자인은 임시 — 폴리싱 단계에 통일

---

*이 문서는 새 설정 항목 추가 또는 우선순위 재조정 시 갱신된다.*

*최종 갱신: 2026-06-22 (오디오 탭 완성, 결정 사항 3개 확정)*
