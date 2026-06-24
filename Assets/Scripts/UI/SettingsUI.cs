using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace BowlingGame
{
    /// <summary>
    /// settings.unity 의 Canvas 에 부착되어 설정 패널 (탭 5개) 을 관리한다.
    /// </summary>
    /// <remarks>
    /// 탭 구조 (인덱스):
    /// <list type="number">
    ///   <item>0 — Audio (이번 세션 구현)</item>
    ///   <item>1 — Display (placeholder)</item>
    ///   <item>2 — Controls (placeholder)</item>
    ///   <item>3 — Accessibility (placeholder)</item>
    ///   <item>4 — UX (placeholder)</item>
    /// </list>
    /// 값 변경 흐름: 슬라이더/토글 입력 → SaveData 캐시 갱신 → AudioManager 즉시 반영 → SaveSystem.Save.
    /// 표시 로직 인라인 금지 (AI_PROMPT_REFERENCE.md §7-12) — 핸들러는 모델 갱신과 시스템 호출만, 표시 갱신은 RefreshAudioLabels 헬퍼로 분리.
    /// </remarks>
    public class SettingsUI : MonoBehaviour
    {
        private const string LogPrefix = "[Settings]";

        [Header("탭 패널 — [0]=Audio, [1]=Display, [2]=Controls, [3]=Accessibility, [4]=UX")]
        [SerializeField] private GameObject[] tabPanels;
        [SerializeField] private Button[] tabButtons;
        [SerializeField] private int defaultTab = 0;

        [Header("Audio 탭")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Toggle muteToggle;
        [SerializeField] private TMP_Text masterValueLabel;
        [SerializeField] private TMP_Text sfxValueLabel;
        [SerializeField] private TMP_Text bgmValueLabel;

        [Header("Display 탭")]
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private TMP_Dropdown windowModeDropdown;
        [SerializeField] private Slider uiScaleSlider;
        [SerializeField] private TMP_Text uiScaleValueLabel;

        [Header("Controls 탭")]
        [SerializeField] private Button rebindKeyboardButton;
        [SerializeField] private Button rebindGamepadButton;
        [SerializeField] private Button resetControlsButton;
        [SerializeField] private TMP_Text keyboardBindingLabel;
        [SerializeField] private TMP_Text gamepadBindingLabel;
        [SerializeField] private TMP_Text connectedDeviceLabel;
        [SerializeField] private GameObject rebindOverlay;
        [SerializeField] private TMP_Text rebindOverlayLabel;

        [Header("Accessibility 탭")]
        [SerializeField] private Toggle audioGuideToggle;
        [SerializeField] private TMP_Dropdown colorblindDropdown;

        [Header("Footer")]
        [SerializeField] private Button backToMainMenuButton;
        [SerializeField] private string mainMenuSceneName = "mainmenu";

        // SaveData 캐시 — UI 입력 시 갱신 후 SaveSystem.Save 로 영속화.
        // 매 변경마다 Load 하지 않는 이유: UI 단일 인스턴스가 동시간 SaveData 의 유일 권한자.
        private SaveData _saveCache;

        // Start 의 초기 UI 동기화 중 onValueChanged 가 발화되어 SaveSystem.Save 가 불필요 호출되는 것을 막는 가드.
        private bool _initializingUI;

        // 진행 중인 리바인딩 작업 — Cancel 또는 컴포넌트 파괴 시 Dispose.
        private InputActionRebindingExtensions.RebindingOperation _activeRebind;

        // Display 탭 — 드롭다운 인덱스 → 해상도 매핑.
        private List<Resolution> _supportedResolutions;

        void Start()
        {
            _saveCache = SaveSystem.Load();

            _initializingUI = true;
            BindAudioTab();
            BindDisplayTab();
            BindControlsTab();
            BindAccessibilityTab();
            _initializingUI = false;

            BindTabs();
            BindFooter();
            ActivateTab(defaultTab);

            Debug.Log($"{LogPrefix} SettingsUI 초기화 완료 — 기본 탭 {defaultTab}");
        }

        void OnDestroy()
        {
            // 리바인딩 진행 중 씬 전환 / 컴포넌트 파괴 시 누수 방지.
            if (_activeRebind != null)
            {
                _activeRebind.Dispose();
                _activeRebind = null;
                InputController.Instance?.ConfirmAction?.Enable();
            }
        }

        // ---------- 바인딩 ----------

        private void BindAudioTab()
        {
            if (masterSlider != null) { masterSlider.value = _saveCache.masterVolume; masterSlider.onValueChanged.AddListener(OnMasterChanged); }
            if (sfxSlider != null)    { sfxSlider.value    = _saveCache.sfxVolume;    sfxSlider.onValueChanged.AddListener(OnSfxChanged); }
            if (bgmSlider != null)    { bgmSlider.value    = _saveCache.bgmVolume;    bgmSlider.onValueChanged.AddListener(OnBgmChanged); }
            if (muteToggle != null)   { muteToggle.isOn    = _saveCache.isMuted;      muteToggle.onValueChanged.AddListener(OnMuteChanged); }
            RefreshAudioLabels();
        }

        private void BindTabs()
        {
            if (tabButtons == null) return;
            for (int i = 0; i < tabButtons.Length; i++)
            {
                int idx = i;  // 클로저 캡처용 로컬 복사 — 어기면 모든 버튼이 마지막 인덱스로 묶인다
                if (tabButtons[i] != null)
                    tabButtons[i].onClick.AddListener(() => ActivateTab(idx));
            }
        }

        private void BindFooter()
        {
            if (backToMainMenuButton != null)
                backToMainMenuButton.onClick.AddListener(OnBackToMainMenu);
        }

        private void BindDisplayTab()
        {
            // 해상도 드롭다운 — 시스템 지원 해상도 채우기 + 현재 값 선택
            if (resolutionDropdown != null)
            {
                _supportedResolutions = new List<Resolution>(DisplaySetter.GetSupportedResolutions());
                resolutionDropdown.ClearOptions();
                var labels = new List<string>(_supportedResolutions.Count);
                foreach (var r in _supportedResolutions)
                    labels.Add($"{r.width} × {r.height}");
                resolutionDropdown.AddOptions(labels);

                int idx = FindResolutionIndex(_saveCache.screenWidth, _saveCache.screenHeight);
                if (idx < 0) idx = FindResolutionIndex(Screen.currentResolution.width, Screen.currentResolution.height);
                if (idx < 0) idx = _supportedResolutions.Count - 1;
                resolutionDropdown.SetValueWithoutNotify(idx);
                resolutionDropdown.RefreshShownValue();
                resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            }

            // 창 모드 드롭다운 — 3개 옵션 고정
            if (windowModeDropdown != null)
            {
                windowModeDropdown.ClearOptions();
                windowModeDropdown.AddOptions(new List<string> { "전체 화면", "테두리 없는 창", "창 모드" });
                windowModeDropdown.SetValueWithoutNotify(DisplaySetter.ModeToIndex((FullScreenMode)_saveCache.fullScreenMode));
                windowModeDropdown.RefreshShownValue();
                windowModeDropdown.onValueChanged.AddListener(OnWindowModeChanged);
            }

            // UI 스케일 슬라이더
            if (uiScaleSlider != null)
            {
                uiScaleSlider.minValue = 0.7f;
                uiScaleSlider.maxValue = 1.5f;
                uiScaleSlider.value = _saveCache.uiScale;
                uiScaleSlider.onValueChanged.AddListener(OnUIScaleChanged);
            }
            RefreshDisplayLabels();
        }

        private int FindResolutionIndex(int w, int h)
        {
            if (_supportedResolutions == null) return -1;
            for (int i = 0; i < _supportedResolutions.Count; i++)
                if (_supportedResolutions[i].width == w && _supportedResolutions[i].height == h) return i;
            return -1;
        }

        private void BindControlsTab()
        {
            if (rebindKeyboardButton != null) rebindKeyboardButton.onClick.AddListener(OnRebindKeyboardClicked);
            if (rebindGamepadButton  != null) rebindGamepadButton.onClick.AddListener(OnRebindGamepadClicked);
            if (resetControlsButton  != null) resetControlsButton.onClick.AddListener(OnResetControlsClicked);
            if (rebindOverlay != null) rebindOverlay.SetActive(false);
            RefreshControlsLabels();
        }

        private void BindAccessibilityTab()
        {
            if (audioGuideToggle != null)
            {
                audioGuideToggle.isOn = _saveCache.aimingAudioGuide;
                audioGuideToggle.onValueChanged.AddListener(OnAudioGuideChanged);
            }

            if (colorblindDropdown != null)
            {
                colorblindDropdown.ClearOptions();
                // 인덱스 = SaveData.colorblindMode (0=Off, 1=적색맹, 2=녹색맹, 3=청색맹).
                colorblindDropdown.AddOptions(new List<string> { "끔", "적색맹 (Protanopia)", "녹색맹 (Deuteranopia)", "청색맹 (Tritanopia)" });
                int idx = Mathf.Clamp(_saveCache.colorblindMode, 0, 3);
                colorblindDropdown.SetValueWithoutNotify(idx);
                colorblindDropdown.RefreshShownValue();
                colorblindDropdown.onValueChanged.AddListener(OnColorblindChanged);
            }
        }

        // ---------- 탭 전환 ----------

        private void ActivateTab(int idx)
        {
            if (tabPanels == null) return;
            for (int i = 0; i < tabPanels.Length; i++)
                if (tabPanels[i] != null) tabPanels[i].SetActive(i == idx);
            Debug.Log($"{LogPrefix} 탭 활성: {idx}");
        }

        // ---------- Audio 핸들러 ----------

        private void OnMasterChanged(float v)
        {
            if (_initializingUI) return;
            _saveCache.masterVolume = v;
            AudioManager.Instance?.SetMasterVolume(v);
            SaveSystem.Save(_saveCache);
            RefreshAudioLabels();
        }

        private void OnSfxChanged(float v)
        {
            if (_initializingUI) return;
            _saveCache.sfxVolume = v;
            AudioManager.Instance?.SetSFXVolume(v);
            SaveSystem.Save(_saveCache);
            RefreshAudioLabels();
        }

        private void OnBgmChanged(float v)
        {
            if (_initializingUI) return;
            _saveCache.bgmVolume = v;
            AudioManager.Instance?.SetBGMVolume(v);
            SaveSystem.Save(_saveCache);
            RefreshAudioLabels();
        }

        private void OnMuteChanged(bool muted)
        {
            if (_initializingUI) return;
            _saveCache.isMuted = muted;
            AudioManager.Instance?.SetMuted(muted);
            SaveSystem.Save(_saveCache);
        }

        // ---------- Display 핸들러 ----------

        private void OnResolutionChanged(int idx)
        {
            if (_initializingUI || _supportedResolutions == null) return;
            if (idx < 0 || idx >= _supportedResolutions.Count) return;
            var r = _supportedResolutions[idx];
            _saveCache.screenWidth = r.width;
            _saveCache.screenHeight = r.height;
            DisplaySetter.ApplyResolution(r.width, r.height, (FullScreenMode)_saveCache.fullScreenMode);
            SaveSystem.Save(_saveCache);
        }

        private void OnWindowModeChanged(int idx)
        {
            if (_initializingUI) return;
            var mode = DisplaySetter.IndexToMode(idx);
            _saveCache.fullScreenMode = (int)mode;
            DisplaySetter.ApplyResolution(_saveCache.screenWidth, _saveCache.screenHeight, mode);
            SaveSystem.Save(_saveCache);
        }

        private void OnUIScaleChanged(float v)
        {
            if (_initializingUI) return;
            _saveCache.uiScale = v;
            DisplaySetter.ApplyUIScale(v);
            SaveSystem.Save(_saveCache);
            RefreshDisplayLabels();
        }

        private void RefreshDisplayLabels()
        {
            if (uiScaleValueLabel != null) uiScaleValueLabel.text = Mathf.RoundToInt(_saveCache.uiScale * 100) + "%";
        }

        // ---------- Accessibility 핸들러 ----------

        private void OnAudioGuideChanged(bool on)
        {
            if (_initializingUI) return;
            _saveCache.aimingAudioGuide = on;
            SaveSystem.Save(_saveCache);
            Debug.Log($"{LogPrefix} 조준 음향 가이드 {(on ? "ON" : "OFF")}");
        }

        private void OnColorblindChanged(int idx)
        {
            if (_initializingUI) return;
            _saveCache.colorblindMode = Mathf.Clamp(idx, 0, 3);
            SaveSystem.Save(_saveCache);
            // 실제 셰이더/머티리얼 보정은 추후 업데이트 — 현재는 선택값 저장만.
            Debug.Log($"{LogPrefix} 색맹 모드 = {_saveCache.colorblindMode} (실제 적용은 추후 업데이트)");
        }

        // ---------- 헬퍼 ----------

        private void RefreshAudioLabels()
        {
            if (masterValueLabel != null) masterValueLabel.text = FormatPercent(_saveCache.masterVolume);
            if (sfxValueLabel != null)    sfxValueLabel.text    = FormatPercent(_saveCache.sfxVolume);
            if (bgmValueLabel != null)    bgmValueLabel.text    = FormatPercent(_saveCache.bgmVolume);
        }

        private static string FormatPercent(float v) => Mathf.RoundToInt(Mathf.Clamp01(v) * 100) + "%";

        // ---------- Controls 핸들러 ----------

        private void OnRebindKeyboardClicked()
        {
            // 게임패드 입력 제외 — 키보드 키만 잡도록 필터링
            StartRebind(InputController.KeyboardBindingIndex, "<Gamepad>", "키를 누르세요 (ESC 취소)");
        }

        private void OnRebindGamepadClicked()
        {
            // 키보드/마우스 제외 — 게임패드 버튼만 잡도록
            // WithControlsExcluding 은 1회 호출당 1개만 받으므로 두 번 체이닝 필요.
            // 본 메서드에서 별도 처리 — StartRebind 의 단일 excludeFilter 한계 우회.
            var action = InputController.Instance?.ConfirmAction;
            if (action == null) { Debug.LogWarning($"{LogPrefix} InputController 없음 — 리바인딩 무시"); return; }
            BeginRebindOverlay("버튼을 누르세요 (ESC 취소)");
            action.Disable();
            _activeRebind = action
                .PerformInteractiveRebinding(InputController.GamepadBindingIndex)
                .WithControlsExcluding("<Keyboard>")
                .WithControlsExcluding("<Mouse>")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnComplete(op => CompleteRebind(op))
                .OnCancel(op => CompleteRebind(op))
                .Start();
        }

        private void OnResetControlsClicked()
        {
            if (InputController.Instance == null) return;
            InputController.Instance.ResetAllBindingsToDefault();
            _saveCache.inputOverridesJson = InputController.Instance.SaveBindingOverridesJson();
            SaveSystem.Save(_saveCache);
            RefreshControlsLabels();
            Debug.Log($"{LogPrefix} 조작 키 기본값 복원");
        }

        private void StartRebind(int bindingIndex, string excludeFilter, string overlayText)
        {
            var action = InputController.Instance?.ConfirmAction;
            if (action == null) { Debug.LogWarning($"{LogPrefix} InputController 없음 — 리바인딩 무시"); return; }

            BeginRebindOverlay(overlayText);
            action.Disable();
            _activeRebind = action
                .PerformInteractiveRebinding(bindingIndex)
                .WithControlsExcluding(excludeFilter)
                .WithCancelingThrough("<Keyboard>/escape")
                .OnComplete(op => CompleteRebind(op))
                .OnCancel(op => CompleteRebind(op))
                .Start();
        }

        private void BeginRebindOverlay(string overlayText)
        {
            if (rebindOverlay != null) rebindOverlay.SetActive(true);
            if (rebindOverlayLabel != null) rebindOverlayLabel.text = overlayText;
        }

        private void CompleteRebind(InputActionRebindingExtensions.RebindingOperation op)
        {
            op.Dispose();
            _activeRebind = null;

            var action = InputController.Instance?.ConfirmAction;
            action?.Enable();

            if (rebindOverlay != null) rebindOverlay.SetActive(false);

            // 저장 — SaveBindingOverridesJson 은 모든 binding override 를 한 JSON 으로 직렬화
            if (InputController.Instance != null)
            {
                _saveCache.inputOverridesJson = InputController.Instance.SaveBindingOverridesJson();
                SaveSystem.Save(_saveCache);
            }
            RefreshControlsLabels();
            Debug.Log($"{LogPrefix} 리바인딩 완료");
        }

        private void RefreshControlsLabels()
        {
            if (keyboardBindingLabel != null)
                keyboardBindingLabel.text = InputController.Instance != null
                    ? InputController.Instance.GetBindingDisplayString(InputController.KeyboardBindingIndex)
                    : "Space";  // InputController 없는 환경 (mainmenu 진입 직후) 기본 표시

            if (gamepadBindingLabel != null)
                gamepadBindingLabel.text = InputController.Instance != null
                    ? InputController.Instance.GetBindingDisplayString(InputController.GamepadBindingIndex)
                    : "A / Cross";

            if (connectedDeviceLabel != null)
            {
                var pad = Gamepad.current;
                connectedDeviceLabel.text = pad != null ? pad.displayName : "없음";
            }
        }

        // ---------- Footer ----------

        private void OnBackToMainMenu()
        {
            Debug.Log($"{LogPrefix} 메인메뉴 복귀 → 씬 로드: {mainMenuSceneName}");
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
