using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BowlingGame
{
    /// <summary>
    /// 게임 내 단일 확정 입력을 관리하는 싱글톤. 키보드 + 게임패드 자동 동시 지원.
    /// </summary>
    /// <remarks>
    /// InputAction "Confirm" 의 binding 두 개 (인덱스 고정):
    /// <list type="bullet">
    ///   <item><see cref="KeyboardBindingIndex"/> = 0 : <c>&lt;Keyboard&gt;/space</c></item>
    ///   <item><see cref="GamepadBindingIndex"/> = 1 : <c>&lt;Gamepad&gt;/buttonSouth</c> (Xbox A / PS Cross)</item>
    /// </list>
    /// 두 binding 모두 항상 활성 — 사용자가 어느 디바이스로 눌러도 OnConfirmPressed 1회 발화.
    /// 리바인딩은 <see cref="ConfirmAction"/> 을 외부(SettingsUI) 가 가져가 <c>PerformInteractiveRebinding</c> 으로 처리.
    /// 결과는 <see cref="SaveBindingOverridesJson"/> 으로 직렬화 후 <see cref="SaveData.inputOverridesJson"/> 에 저장.
    /// </remarks>
    public class InputController : MonoBehaviour
    {
        public static InputController Instance { get; private set; }

        /// <summary>인덱스 변경 금지 — SettingsUI / SaveData 직렬화 결과와 직결.</summary>
        public const int KeyboardBindingIndex = 0;
        /// <summary>인덱스 변경 금지 — SettingsUI / SaveData 직렬화 결과와 직결.</summary>
        public const int GamepadBindingIndex  = 1;

        public event Action OnConfirmPressed;

        private InputAction confirmAction;

        /// <summary>외부(설정 UI) 가 리바인딩 수행을 위해 InputAction 직접 참조.</summary>
        public InputAction ConfirmAction => confirmAction;

        void Awake()
        {
            // 중복 인스턴스 처리 — Destroy(this) 로 컴포넌트만 파괴.
            // 같은 GameObject 에 부착된 다른 컴포넌트(Game.unity 의 GameStateManager / DebugResetController) 가
            // 함께 사라지지 않도록 보호. mainmenu 의 InputController 가 DontDestroyOnLoad 로 살아서 Game 까지 따라가는데,
            // Game.unity 의 GameManager(ⓐ) 위에 부착된 기존 InputController 는 Awake 에서 본 분기에 잡혀 자기만 자가 파괴된다.
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            confirmAction = new InputAction(name: "Confirm", type: InputActionType.Button);
            confirmAction.AddBinding("<Keyboard>/space");           // index 0
            confirmAction.AddBinding("<Gamepad>/buttonSouth");      // index 1 — Xbox A / PS Cross / DualSense Cross

            confirmAction.performed += _ => OnConfirmPressed?.Invoke();
            confirmAction.Enable();
        }

        void Start()
        {
            // 저장된 binding override 복원 — AudioManager 의 음량 복원 패턴 답습.
            // SaveData 의 inputOverridesJson 이 null/empty 면 기본 binding 그대로 사용.
            LoadBindingOverridesJson(SaveSystem.Load().inputOverridesJson);
        }

        void OnDestroy()
        {
            confirmAction?.Disable();
            confirmAction?.Dispose();
        }

        // ---------- 리바인딩 / 직렬화 ----------

        /// <summary>현재 binding override 상태를 JSON 으로 직렬화. <see cref="SaveData.inputOverridesJson"/> 에 저장한다.</summary>
        public string SaveBindingOverridesJson()
        {
            return confirmAction != null ? confirmAction.SaveBindingOverridesAsJson() : string.Empty;
        }

        /// <summary>저장된 binding override 를 복원. null/empty 면 기본 binding 유지.</summary>
        public void LoadBindingOverridesJson(string json)
        {
            if (confirmAction == null) return;
            if (string.IsNullOrEmpty(json)) return;
            confirmAction.LoadBindingOverridesFromJson(json);
        }

        /// <summary>모든 binding override 제거 — 두 binding 모두 기본값(Space / South 버튼) 복원.</summary>
        public void ResetAllBindingsToDefault()
        {
            confirmAction?.RemoveAllBindingOverrides();
        }

        /// <summary>특정 binding 1개만 기본값 복원 (예: 키보드만 리셋).</summary>
        public void ResetBindingToDefault(int bindingIndex)
        {
            confirmAction?.RemoveBindingOverride(bindingIndex);
        }

        /// <summary>UI 표시용 — 현재 binding 의 사람 읽기용 문자열 (예: "Space", "A").</summary>
        public string GetBindingDisplayString(int bindingIndex)
        {
            if (confirmAction == null) return "?";
            if (bindingIndex < 0 || bindingIndex >= confirmAction.bindings.Count) return "?";
            // InputBinding.ToDisplayString — 디바이스 별 사람 읽기용 라벨 (Localized 가능)
            return confirmAction.GetBindingDisplayString(bindingIndex);
        }
    }
}
