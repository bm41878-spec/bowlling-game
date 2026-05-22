// [TEST ONLY] R 키로 GameManager.RestartGame() 호출. Phase 5 이전 개발 테스트용.
using UnityEngine;
using UnityEngine.InputSystem;

namespace BowlingGame
{
    /// <summary>
    /// R 키 입력을 받아 <see cref="GameManager.RestartGame"/> 으로 위임한다.
    /// 이전 구현(공/핀 직접 리셋 + 리플렉션) 은 PinManager·FrameManager 와 스냅샷이 어긋날 수 있어 폐기.
    /// </summary>
    public class DebugResetController : MonoBehaviour
    {
        private InputAction resetAction;

        void Start()
        {
            resetAction = new InputAction(type: InputActionType.Button);
            resetAction.AddBinding("<Keyboard>/r");
            resetAction.performed += _ => OnResetPressed();
            resetAction.Enable();
        }

        void OnDestroy()
        {
            resetAction?.Disable();
        }

        private void OnResetPressed()
        {
            var gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogWarning("[Debug] GameManager.Instance 없음 — 씬에 GameManager 컴포넌트가 배치/초기화되었는지 확인하세요.");
                return;
            }
            gm.RestartGame();
        }
    }
}
